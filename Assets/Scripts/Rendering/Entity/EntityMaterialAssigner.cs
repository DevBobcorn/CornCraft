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
        [SerializeField] public EntityRenderType RenderType;
        [SerializeField] private Material m_DefaultMaterial = null;
        public Material DefaultMaterial => m_DefaultMaterial!;
        [SerializeField] public string TextureId = string.Empty;
        [SerializeField] private Renderer[] m_Renderers;
        public Renderer[] Renderers => m_Renderers;
        
        #nullable enable

        public bool DynamicTextureId { get; private set; } = false;
        public List<(string, string)>? TextureIdVariables { get; private set; }
        public HashSet<int>? DependentMetaSlots { get; private set; }
        
        #nullable disable

        public EntityMaterialEntry(EntityRenderType renderType, Material material, Renderer[] renderers)
        {
            m_DefaultMaterial = material;
            RenderType = renderType;

            if (material && material.mainTexture)
            {
                TextureId = material.mainTexture.name;
            }

            m_Renderers = renderers;
        }

        public void SetupDynamicTextureId(EntityType entityType)
        {
            if (DynamicTextureId)
            {
                Debug.LogWarning("Dynamic texture already parsed!");
                return;
            }

            TextureIdVariables = new();
            DependentMetaSlots = new();

            // Turn texture id into a string template. e.g. entity/cow/{COLOR}_mooshroom
            const string pattern = @"\{.*?\}"; // Non-greedy matching using '?'

            TextureId = Regex.Replace(TextureId, pattern, m => convert(m.Value[1..^1]));

            if (TextureIdVariables.Count == 0)
            {
                Debug.LogWarning($"Malformed texture id: {TextureId}");
            }
            else
            {
                DynamicTextureId = true;
            }

            return;

            string convert(string variable)
            {
                var split = variable.Split('=');
                var variableName = split[0];
                var defaultValue = split.Length > 1 ? split[1] : "<missing>";

                if (variableName.StartsWith("meta@"))
                {
                    var metaName = variableName[5..].Split("@")[0];

                    if (!entityType.MetaEntriesByName.TryGetValue(metaName, out EntityMetaEntry metaEntry))
                    {
                        Debug.LogWarning($"{metaName} is not a valid entity metadata slot for {entityType}!");
                        return $"[{metaName}]";
                    }

                    var metaSlot = entityType.MetaSlotByName[metaEntry.Name];
                    DependentMetaSlots!.Add(metaSlot);
                    //Debug.Log($"{TextureId} depends on meta [{metaSlot}] {metaEntry.Name}");
                }
                /*
                else
                {
                    Debug.Log($"{TextureId} depends on variable named {variableName}");
                }
                */

                TextureIdVariables!.Add((variableName, defaultValue));
                
                return $"{{{TextureIdVariables.Count - 1}}}";
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
                if (!renderer.sharedMaterial) continue;

                if (!entries.ContainsKey(renderer.sharedMaterial))
                {
                    entries.Add(renderer.sharedMaterial, new());
                }
                
                entries[renderer.sharedMaterial].Add(renderer);
            }

            m_MaterialEntries = entries.Select(x => new EntityMaterialEntry(
                    EntityRenderType.SOLID, x.Key, x.Value.ToArray() ) ).ToArray();
        }
        
        #nullable enable

        private static string GetVariableValue(EntityType entityType, string variableName, string defaultValue, Dictionary<string, string>? variables, Dictionary<int, object?>? metadata)
        {
            if (variableName.StartsWith("meta@"))
            {
                if (metadata is null)
                {
                    return defaultValue;
                }

                var metaEntrySplit = variableName[5..].Split('@', 2);
                var metaName = metaEntrySplit[0];

                if (!entityType.MetaEntriesByName.TryGetValue(metaName, out EntityMetaEntry metaEntry))
                {
                    Debug.LogWarning($"{metaName} is not a valid entity metadata slot for {entityType}!");
                    return defaultValue;
                }

                var metaSlot = entityType.MetaSlotByName[metaEntry.Name];
                var metaValue = metadata[metaSlot];

                if (metaValue is null)
                {
                    Debug.LogWarning($"Failed to get meta {metaName} at slot {metaSlot} for {entityType}!");
                    return defaultValue;
                }

                // Return the value directly
                return metaValue.ToString();
            }

            // Look in variable table
            var value = variables?.GetValueOrDefault(variableName, defaultValue);

            if (value is null)
            {
                Debug.LogWarning($"Failed to get variable {variableName} for {entityType}!");
                return defaultValue;
            }

            return value;
        }

        private static bool IsTextureIdAffected(EntityMaterialEntry entry, HashSet<string>? updatedVars, HashSet<int>? updatedMeta)
        {
            if (updatedVars is not null && entry.TextureIdVariables!.Any(x => updatedVars.Contains(x.Item1)))
            {
                return true;
            }

            return updatedMeta is not null && entry.DependentMetaSlots!.Any(updatedMeta.Contains);
        }

        /// <summary>
        /// Update materials after variable/updatedMeta value change
        /// </summary>
        public void UpdateMaterials(EntityType entityType, HashSet<string>? updatedVars, HashSet<int>? updatedMeta, Dictionary<string, string>? variables, Dictionary<int, object?>? metadata)
        {
            var client = CornApp.CurrentClient!;
            var matManager = client.EntityMaterialManager;

            var isRagdoll = GetComponent<EntityRagdoll>();

            //var info = updatedMeta.Select(x => entityType.MetaEntries[x].Name + ": [" + metadata?[x] + "],");
            //Debug.Log($"Updating meta ({gameObject.name}):\n{string.Join("\n", info)}");

            for (int i = 0; i < m_MaterialEntries.Length; i++)
            {
                var entry = m_MaterialEntries[i];
                ResourceLocation textureId;

                if (entry.DynamicTextureId && IsTextureIdAffected(entry, updatedVars, updatedMeta))
                {
                    var vars = entry.TextureIdVariables!.Select(x =>
                        (object) GetVariableValue(entityType, x.Item1, x.Item2, variables, metadata)).ToArray();
                    var interpolated = string.Format(entry.TextureId, vars);
                    //Debug.Log($"Updating texture {entry.TextureId} with {string.Join(", ", vars)}");
                    textureId = ResourceLocation.FromString(interpolated);
                }
                else
                {
                    // Not affected, skip.
                    continue;
                }

                if (isRagdoll)
                {
                    var matInstance = new Material(matManager.EntityDissolveMaterial);
                    var entry1 = entry;
                    matManager.ApplyTextureOrSkin(textureId, tex =>
                    {
                        matInstance.SetTexture(matManager.EntityDissolveMaterialTextureName, tex);
                        matInstance.SetColor(matManager.EntityDissolveMaterialColorName, matManager.EntityBaseColor);
                        AssignMaterialToRenderer(entry1.Renderers, matInstance);
                    });
                }
                else
                {
                    matManager.ApplyMaterial(entry.RenderType, textureId, entry.DefaultMaterial, matInstance =>
                    {
                        AssignMaterialToRenderer(entry.Renderers, matInstance);
                    });
                }
            }
        }

        private static void AssignMaterialToRenderer(Renderer[] renderers, Material matInstance)
        {
            for (int j = 0; j < renderers.Length; j++)
            {
                renderers[j].sharedMaterial = matInstance;
            }
        }

        public void InitializeMaterials(EntityType entityType, Dictionary<string, string>? variables, Action<EntityMaterialManager, ResourceLocation, Material> callbackForEach)
        {
            var client = CornApp.CurrentClient!;
            var matManager = client.EntityMaterialManager;

            var isRagdoll = GetComponent<EntityRagdoll>();

            for (int i = 0; i < m_MaterialEntries.Length; i++)
            {
                var entry = m_MaterialEntries[i];
                ResourceLocation textureId;

                if (entry.TextureId.Contains('{'))
                {
                    // Extract updatedVars in texture id
                    entry.SetupDynamicTextureId(entityType);
                }

                if (entry.DynamicTextureId)
                {
                    // Metadata is not available during initialization(will be sent afterwards)
                    var vars = entry.TextureIdVariables!.Select(x =>
                            (object) GetVariableValue(entityType, x.Item1, x.Item2, variables, null)).ToArray();
                    var interpolated = string.Format(entry.TextureId, vars);
                    textureId = ResourceLocation.FromString(interpolated);
                }
                else
                {
                    textureId = ResourceLocation.FromString(entry.TextureId);
                }
                
                if (isRagdoll)
                {
                    var matInstance = new Material(matManager.EntityDissolveMaterial);
                    matManager.ApplyTextureOrSkin(textureId, texture =>
                    {
                        matInstance.SetTexture(matManager.EntityDissolveMaterialTextureName, texture);
                        matInstance.SetColor(matManager.EntityDissolveMaterialColorName, matManager.EntityBaseColor);
                        AssignMaterialToRenderer(entry.Renderers, matInstance);

                        callbackForEach.Invoke(matManager, textureId, matInstance);
                    });
                }
                else
                {
                    matManager.ApplyMaterial(entry.RenderType, textureId, entry.DefaultMaterial, matInstance =>
                    {
                        AssignMaterialToRenderer(entry.Renderers, matInstance);

                        callbackForEach.Invoke(matManager, textureId, matInstance);
                    });
                }
            }
        }
    }
}