#nullable enable
using System;
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
    }

    public class EntityMaterialAssigner : MonoBehaviour
    {
        [SerializeField] private EntityMaterialEntry[] m_MaterialEntries = { };
        [SerializeField] private Renderer[] rendererGroups = { };

        void Start()
        {
            var matManager = CornApp.CurrentClient!.EntityMaterialManager;

            foreach (var entry in m_MaterialEntries)
            {
                var textureId = ResourceLocation.FromString(entry.TextureId);

                matManager.MapMaterial(entry.RenderType, textureId, entry.DefaultMaterial);
            }
        }
    }
}