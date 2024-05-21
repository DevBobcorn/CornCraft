#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using CraftSharp.Resource;
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

        void Start()
        {
            var client = CornApp.CurrentClient!;
            var matManager = client.EntityMaterialManager;

            foreach (var entry in m_MaterialEntries)
            {
                var textureId = ResourceLocation.FromString(entry.TextureId);
	            var matInstance = matManager.MapMaterial(entry.RenderType, textureId, entry.DefaultMaterial);
                
                foreach (var renderer in entry.Renderers)
                {
                    renderer.sharedMaterial = matInstance;
                }
            }
        }
    }
}