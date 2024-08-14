#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace CraftSharp.Rendering
{
    [Serializable]
    public class EntityMaterialEntry
    {
        [SerializeField] public EntityRenderType RenderType = EntityRenderType.SOLID;
        [SerializeField] private Material? m_DefaultMaterial = null;
        public Material DefaultMaterial => m_DefaultMaterial!;
        [SerializeField] public string TextureId = string.Empty;
        [SerializeField] private Renderer[] m_Renderers = { };
        public Renderer[] Renderers => m_Renderers;

        public bool DynamicTextureId { get; private set; } = false;
        public string TextureIdTemplate { get; private set; } = string.Empty;
        public string[] TextureIdVariables { get; private set; } = { };
        public HashSet<int> TextureIdMetaSlots { get; private set; } = new();

        public EntityMaterialEntry(EntityRenderType renderType, Material material, Renderer[] renderers)
        {
            m_DefaultMaterial = material;
            RenderType = renderType;

            if (material != null && material.mainTexture != null)
            {
                TextureId = material.mainTexture.name;
            }

            m_Renderers = renderers;
        }

        public void SetupDynamicTextureId()
        {
            if (DynamicTextureId)
            {
                Debug.LogWarning("Dynamic texture already parsed!");
                return;
            }

            // Turn texture id into a string template. e.g. entity/cow/{COLOR}_mooshroom
            string pattern = @"\{.*?\}"; // Non-greedy matching using '?'
            List<string> vars = new();
            HashSet<int> metaSlots = new();

            string convert(string slotText)
            {
                if (slotText.StartsWith("meta@"))
                {
                    var slotNum = slotText[5..].Split("@")[0];
                    if (int.TryParse(slotNum, out int slot))
                    {
                        metaSlots.Add(slot);
                    }
                }

                vars.Add(slotText[1..^1]);
                
                return $"{{{vars.Count - 1}}}";
            }

            TextureId = Regex.Replace(TextureId, pattern, m => convert(m.Value));

            if (vars.Count == 0)
            {
                Debug.LogWarning($"Malformed texture id: {TextureId}");
            }
            else
            {
                TextureIdVariables = vars.ToArray();
                TextureIdMetaSlots = metaSlots;
                DynamicTextureId = true;
            }
        }
    }

    public class EntityMaterialAssigner : MonoBehaviour
    {
        [SerializeField] private EntityMaterialEntry[] m_MaterialEntries = { };
        public EntityMaterialEntry[] MaterialEntries => m_MaterialEntries;

        public void InitializeRenderers()
        {
            var renderers = gameObject.GetComponentsInChildren<Renderer>();
            var entries = new Dictionary<Material, List<Renderer>>();

            foreach (var renderer in renderers)
            {
                if (renderer.sharedMaterial == null) continue;

                if (!entries.ContainsKey(renderer.sharedMaterial))
                {
                    entries.Add(renderer.sharedMaterial, new());
                }
                
                entries[renderer.sharedMaterial].Add(renderer);
            }

            m_MaterialEntries = entries.Select(x => new EntityMaterialEntry(
                    EntityRenderType.SOLID, x.Key, x.Value.ToArray() ) ).ToArray();
        }

        private string GetVariableValue(string variable, Dictionary<string, string>? variables, Dictionary<int, object?>? metadata)
        {
            var split = variable.Split('=');
            var defaultValue = split.Length > 1 ? split[1] : "<missing>";

            if (split[0].StartsWith("meta@"))
            {
                if (metadata is null)
                {
                    return defaultValue;
                }

                var metaEntry = split[0][5..].Split('@', 2);

                if (!int.TryParse(metaEntry[0], out int metaSlot))
                {
                    Debug.LogWarning($"{metaEntry[0]} is not a valid entity metadata slot!");
                    return defaultValue;
                }

                // Return the value directly
                return metadata[metaSlot]?.ToString() ?? defaultValue;
            }
            else
            {
                // Look in variable table
                return variables?.GetValueOrDefault(split[0], defaultValue) ?? defaultValue;
            }
        }

        private bool IsTextureIdAffected(EntityMaterialEntry entry, HashSet<string>? updatedVars, HashSet<int>? updatedMeta)
        {
            if (updatedVars is not null && entry.TextureIdVariables.Any(x => updatedVars.Contains(x)))
            {
                return true;
            }

            if (updatedMeta is not null && entry.TextureIdMetaSlots.Any(x => updatedMeta.Contains(x)))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Update materials after variable/updatedMeta value change
        /// </summary>
        public void UpdateMaterials(HashSet<string>? updatedVars, HashSet<int>? updatedMeta, Dictionary<string, string>? variables, Dictionary<int, object?>? metadata)
        {
            var client = CornApp.CurrentClient!;
            var matManager = client.EntityMaterialManager;

            var isRagdoll = GetComponent<EntityRagdoll>() != null;

            Debug.Log($"Updating meta ({gameObject.name}): {string.Join(", ", updatedMeta)}");

            for (int i = 0; i < m_MaterialEntries.Length; i++)
            {
                var entry = m_MaterialEntries[i];
                ResourceLocation textureId;

                if (entry.DynamicTextureId && IsTextureIdAffected(entry, updatedVars, updatedMeta))
                {
                    var vars = entry.TextureIdVariables.Select(x =>
                            GetVariableValue(x, variables, metadata)).ToArray();
                    var interpolated = string.Format(entry.TextureId, vars);
                    Debug.Log($"Updating texture {entry.TextureId} with {string.Join(", ", vars)}");
                    textureId = ResourceLocation.FromString(interpolated);
                }
                else
                {
                    // Not affected, skip.
                    continue;
                }

                Material matInstance;

                if (isRagdoll)
                {
                    matInstance = new Material(matManager.EnityDissolveMaterial);
                    var texture = matManager.GetTexture(textureId);
                    matInstance.SetTexture(matManager.EnityDissolveMaterialTextureName, texture);
                }
                else
                {
                    matInstance = matManager.MapMaterial(entry.RenderType, textureId, entry.DefaultMaterial);
                }

                for (int j = 0; j < entry.Renderers.Length; j++)
                {
                    entry.Renderers[j].sharedMaterial = matInstance;
                }
            }
        }

        public void InitializeMaterials(Dictionary<string, string>? variables, Dictionary<int, object?>? metadata)
        {
            var client = CornApp.CurrentClient!;
            var matManager = client.EntityMaterialManager;

            var isRagdoll = GetComponent<EntityRagdoll>() != null;

            for (int i = 0; i < m_MaterialEntries.Length; i++)
            {
                var entry = m_MaterialEntries[i];
                ResourceLocation textureId;

                if (entry.TextureId.Contains('{'))
                {
                    // Extract updatedVars in texture id
                    entry.SetupDynamicTextureId();
                }

                if (entry.DynamicTextureId)
                {
                    var vars = entry.TextureIdVariables.Select(x =>
                            GetVariableValue(x, variables, metadata)).ToArray();
                    var interpolated = string.Format(entry.TextureId, vars);
                    textureId = ResourceLocation.FromString(interpolated);
                }
                else
                {
                    textureId = ResourceLocation.FromString(entry.TextureId);
                }

	            Material matInstance;
                
                if (isRagdoll)
                {
                    matInstance = new Material(matManager.EnityDissolveMaterial);
                    var texture = matManager.GetTexture(textureId);
                    matInstance.SetTexture(matManager.EnityDissolveMaterialTextureName, texture);
                }
                else
                {
                    matInstance = matManager.MapMaterial(entry.RenderType, textureId, entry.DefaultMaterial);
                }

                for (int j = 0; j < entry.Renderers.Length; j++)
                {
                    entry.Renderers[j].sharedMaterial = matInstance;
                }
            }
        }
    }
}