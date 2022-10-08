using System.Collections.Generic;
using UnityEngine;

using MinecraftClient.Resource;

namespace MinecraftClient.Rendering
{
    public static class MaterialManager {
        private static Dictionary<RenderType, Material> blockMaterials = new();
        private static Dictionary<RenderType, Material> plcboMaterials = new();

        private static Material solid, solidPlacebo;

        private static bool initialized = false;

        public static Material GetBlockMaterial(RenderType renderType)
        {
            EnsureInitialized();
            return blockMaterials.GetValueOrDefault(renderType, solid);
        }

        public static Material GetPlaceboMaterial(RenderType renderType)
        {
            EnsureInitialized();
            return plcboMaterials.GetValueOrDefault(renderType, solidPlacebo);
        }

        public static void EnsureInitialized()
        {
            if (!initialized) Initialize();
        }

        private static void Initialize()
        {
            blockMaterials.Clear();
            plcboMaterials.Clear();

            // Solid
            var s1 = Resources.Load<Material>("Materials/Block/Block Solid");
            s1.SetTexture("_BaseMap", AtlasManager.GetAtlasTexture(RenderType.SOLID));
            blockMaterials.Add(RenderType.SOLID, s1);

            solid = s1;

            var s2 = Resources.Load<Material>("Materials/Block/Placebo Solid");
            s2.SetTexture("_BaseMap", AtlasManager.PlcboTexture);
            plcboMaterials.Add(RenderType.SOLID, s2);

            solidPlacebo = s2;

            // Cutout & Cutout Mipped
            var c1 = Resources.Load<Material>("Materials/Block/Block Cutout");
            c1.SetTexture("_BaseMap", AtlasManager.GetAtlasTexture(RenderType.CUTOUT));
            blockMaterials.Add(RenderType.CUTOUT, c1);

            var c2 = Resources.Load<Material>("Materials/Block/Placebo Cutout");
            c2.SetTexture("_BaseMap", AtlasManager.PlcboTexture);
            plcboMaterials.Add(RenderType.CUTOUT, c2);

            var cm1 = Resources.Load<Material>("Materials/Block/Block Cutout Mipped");
            cm1.SetTexture("_BaseMap", AtlasManager.GetAtlasTexture(RenderType.CUTOUT_MIPPED));
            blockMaterials.Add(RenderType.CUTOUT_MIPPED, cm1);

            var cm2 = Resources.Load<Material>("Materials/Block/Placebo Cutout Mipped");
            cm2.SetTexture("_BaseMap", AtlasManager.PlcboTexture);
            plcboMaterials.Add(RenderType.CUTOUT_MIPPED, cm2);

            // Translucent
            var t1 = Resources.Load<Material>("Materials/Block/Block Transparent");
            t1.SetTexture("_BaseMap", AtlasManager.GetAtlasTexture(RenderType.TRANSLUCENT));
            blockMaterials.Add(RenderType.TRANSLUCENT, t1);

            var t2 = Resources.Load<Material>("Materials/Block/Placebo Transparent");
            t2.SetTexture("_BaseMap", AtlasManager.PlcboTexture);
            plcboMaterials.Add(RenderType.TRANSLUCENT, t2);

            // Water
            var w = Resources.Load<Material>("Materials/WaterTest");
            blockMaterials.Add(RenderType.WATER, w);
            plcboMaterials.Add(RenderType.WATER, w);

            initialized = true;

        }

    }

}