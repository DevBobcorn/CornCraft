using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using CraftSharp.Resource;

namespace CraftSharp.Rendering
{
    public class EntityMaterialManager : MonoBehaviour
    {
        [SerializeField] private Material m_EnityDissolveMaterial;
        public Material EnityDissolveMaterial => m_EnityDissolveMaterial;
        [SerializeField] private string m_EnityDissolveMaterialTextureName = "_Texture";
        public string EnityDissolveMaterialTextureName => m_EnityDissolveMaterialTextureName;

        /// <summary>
        /// A material instance is created for each rendertype-texture pair,
        /// and all entities that uses this material share the same instance.
        /// This helps to avoid unnecessary copies of materials and makes
        /// texture updates much easier.
        /// </summary>
        public readonly Dictionary<EntityRenderType, Dictionary<ResourceLocation,
                Material>> EntityMaterials = InitializeTables();

        /// <summary>
        /// Map a material to an instance in the global entity material table.
        /// </summary>
        /// <param name="renderType">Render type of this material</param>
        /// <param name="textureId">Texture identifier</param>
        /// <param name="defaultMaterial">The material template to be used if this material is not yet present in table</param>
        public Material MapMaterial(EntityRenderType renderType, ResourceLocation textureId, Material defaultMaterial)
        {
            if (!EntityMaterials[renderType].ContainsKey(textureId))
            {
                var resManager = ResourcePackManager.Instance;
                // This entry is not present, instanciate it
                //Debug.Log($"Creating entity material {textureId} ({renderType})");
                Texture2D mt;
                if (resManager.EntityTexture2DTable.ContainsKey(textureId))
                {
                    mt = resManager.EntityTexture2DTable[textureId];
                }
                else
                {
                    // The entry is not present, try loading this texture
                    mt = resManager.LoadEntityTextureFromPacks(textureId);
                }

                var matInstance = new Material(defaultMaterial)
                {
                    // Read and apply textures from ResourcePackManager
                    mainTexture = mt,
                    name = $"Material {textureId} ({renderType})"
                };

                matInstance.SetTexture("_BaseMap", mt);

                EntityMaterials[renderType].Add(textureId, matInstance);
            }

            return EntityMaterials[renderType][textureId];
        }

        /// <summary>
        /// Get a texture with given id, or load it if not present. This should ONLY be used when
        /// creating special materials which cannot be retrieved from entity material table.
        /// </summary>
        /// <param name="textureId">Texture identifier</param>
        public Texture2D GetTexture(ResourceLocation textureId)
        {
            var resManager = ResourcePackManager.Instance;

            if (resManager.EntityTexture2DTable.ContainsKey(textureId))
            {
                return resManager.EntityTexture2DTable[textureId];
            }
            else
            {
                // The entry is not present, try loading this texture
                return resManager.LoadEntityTextureFromPacks(textureId);
            }
        }

        public void ClearTables()
        {
            EntityMaterials.Clear();
            Enum.GetValues(typeof (EntityRenderType)).OfType<EntityRenderType>()
                    .ToList().ForEach(x => EntityMaterials.Add(x, new Dictionary<ResourceLocation, Material>()));
        }

        private static Dictionary<EntityRenderType, Dictionary<ResourceLocation, Material>> InitializeTables()
        {
            return Enum.GetValues(typeof (EntityRenderType)).OfType<EntityRenderType>()
                    .ToDictionary(x => x, _ => new Dictionary<ResourceLocation, Material>() );
        }
    }
}