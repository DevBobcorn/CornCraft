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

            TextureId = Regex.Replace(TextureId, pattern,
                    m => { vars.Add(m.Value[1..^1]); return $"{{{vars.Count - 1}}}"; });

            if (vars.Count == 0)
            {
                Debug.LogWarning($"Malformed texture id: {TextureId}");
            }
            else
            {
                TextureIdVariables = vars.ToArray();
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

        public void InitializeMaterials(Dictionary<string, string>? variables, Dictionary<int, object?>? metadata)
        {
            var client = CornApp.CurrentClient!;
            var matManager = client.EntityMaterialManager;

            var isRagdoll = GetComponent<EntityRagdoll>() != null;

            string getVariableValue(string variable)
            {
                Debug.Log($"Parsing {variable}");

                var split = variable.Split('=');
                var defaultValue = split.Length > 1 ? split[1] : "?";

                if (split[0].StartsWith("meta@"))
                {
                    if (metadata is null)
                    {
                        return defaultValue;
                    }

                    var metaEntry = split[0][5..].Split('@');
                    int metaSlot;
                    
                    if (!int.TryParse(metaEntry[0], out metaSlot))
                    {
                        Debug.LogWarning($"{metaEntry[0]} is not a valid entity metadata slot!");
                        return defaultValue;
                    }

                    Debug.Log($"W {metadata[metaSlot]}");
                    // Return the value directly
                    return metadata[metaSlot]?.ToString() ?? defaultValue;
                }
                else
                {
                    // Look in variable table
                    return variables?.GetValueOrDefault(split[0], defaultValue) ?? defaultValue;
                }
            };

            foreach (var entry in m_MaterialEntries)
            {
                ResourceLocation textureId;

                if (entry.TextureId.Contains('{'))
                {
                    // Extract variables in texture id
                    entry.SetupDynamicTextureId();
                }

                if (entry.DynamicTextureId)
                {
                    var vars = entry.TextureIdVariables.Select(x => getVariableValue(x)).ToArray();
                    Debug.Log($"interpolating {entry.TextureId} with {string.Join(",", vars)}");
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
                
                foreach (var renderer in entry.Renderers)
                {
                    renderer.sharedMaterial = matInstance;
                }
            }
        }
    }
}