using System.Collections.Generic;
using UnityEngine;

using CraftSharp.Resource;

namespace CraftSharp.Rendering
{
    public class ChunkMaterialManager : MonoBehaviour
    {
        public Material AtlasSolid;
        public Material AtlasCutout;
        public Material AtlasCutoutMipped;
        public Material AtlasTranslucent;
        public Material StylizedWater;
        public Material Foliage;
        public Material Plants;

        private readonly Dictionary<RenderType, Material> atlasMaterials = new();
        private Material defaultAtlasMaterial;

        private bool atlasInitialized = false;

        public Material GetAtlasMaterial(RenderType renderType)
        {
            EnsureAtlasInitialized();
            return atlasMaterials.GetValueOrDefault(renderType, defaultAtlasMaterial);
        }

        public void EnsureAtlasInitialized()
        {
            if (!atlasInitialized) Initialize();
        }

        private void Initialize()
        {
            atlasMaterials.Clear();
            var packManager = ResourcePackManager.Instance;

            // Solid
            var solid = new Material(AtlasSolid);
            solid.SetTexture("_BaseMap", packManager.GetAtlasArray(false));
            atlasMaterials.Add(RenderType.SOLID, solid);

            defaultAtlasMaterial = solid;

            // Cutout & Cutout Mipped
            var cutout = new Material(AtlasCutout);
            cutout.SetTexture("_BaseMap", packManager.GetAtlasArray(false));
            atlasMaterials.Add(RenderType.CUTOUT, cutout);

            var cutoutMipped = new Material(AtlasCutoutMipped);
            cutoutMipped.SetTexture("_BaseMap", packManager.GetAtlasArray(true));
            atlasMaterials.Add(RenderType.CUTOUT_MIPPED, cutoutMipped);

            // Translucent
            var translucent = new Material(AtlasTranslucent);
            translucent.SetTexture("_BaseMap", packManager.GetAtlasArray(false));
            translucent.EnableKeyword("_ENVIRO3_FOG");
            atlasMaterials.Add(RenderType.TRANSLUCENT, translucent);

            // Water
            var water = new Material(StylizedWater);
            //water.SetTexture("_BaseMap", packManager.GetAtlasArray(false));
            atlasMaterials.Add(RenderType.WATER, water);

            // Foliage
            var foliage = new Material(Foliage);
            foliage.SetTexture("_BaseMap", packManager.GetAtlasArray(true));
            atlasMaterials.Add(RenderType.FOLIAGE, foliage);

            // Plants
            var plants = new Material(Plants);
            plants.SetTexture("_BaseMap", packManager.GetAtlasArray(false));
            atlasMaterials.Add(RenderType.PLANTS, plants);

            // Tall Plants
            var tallPlants = new Material(Plants);
            tallPlants.SetTexture("_BaseMap", packManager.GetAtlasArray(false));
            atlasMaterials.Add(RenderType.TALL_PLANTS, tallPlants);

            atlasInitialized = true;
        }
    }
}