using System.Collections.Generic;
using UnityEngine;

using MinecraftClient.Resource;

namespace MinecraftClient.Rendering
{
    public static class MaterialManager {
        private static Dictionary<RenderType, Material> blockMaterials = new Dictionary<RenderType, Material>();
        private static Dictionary<RenderType, Material> plcboMaterials = new Dictionary<RenderType, Material>();

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
            // Solid
            var sshader = Shader.Find("Unicorn/BlockSolid");
            //var sshader = Shader.Find("Standard");

            var s1 = new Material(sshader);
            s1.name = "Block Solid";
            s1.SetTexture("_MainTex", AtlasManager.GetAtlasTexture(RenderType.SOLID));
            s1.SetFloat("_Glossiness", 0F);
            blockMaterials.Add(RenderType.SOLID, s1);

            var s2 = new Material(sshader);
            s2.name = "Placebo Solid";
            s2.SetTexture("_MainTex", AtlasManager.PlcboTexture);
            s2.SetFloat("_Glossiness", 0F);
            plcboMaterials.Add(RenderType.SOLID, s2);

            // Cutout & Cutout Mipped
            var cshader = Shader.Find("Unicorn/BlockCutout");
            //var cshader = Shader.Find("Standard");
            
            var c1 = new Material(cshader);
            c1.name = "Block Cutout";
            c1.SetTexture("_MainTex", AtlasManager.GetAtlasTexture(RenderType.CUTOUT));
            c1.EnableKeyword("_ALPHATEST_ON");
            c1.SetFloat("_Mode", 1);
            c1.renderQueue = 2450;
            c1.SetFloat("_Glossiness", 0F);
            c1.SetFloat("_Cutoff", 0.75F);
            blockMaterials.Add(RenderType.CUTOUT, c1);

            var c2 = new Material(cshader);
            c2.name = "Placebo Cutout";
            c2.SetTexture("_MainTex", AtlasManager.PlcboTexture);
            c2.EnableKeyword("_ALPHATEST_ON");
            c2.SetFloat("_Mode", 1);
            c2.renderQueue = 2450;
            c2.SetFloat("_Glossiness", 0F);
            c2.SetFloat("_Cutoff", 0.75F);
            plcboMaterials.Add(RenderType.CUTOUT, c2);

            var cm1 = new Material(cshader);
            cm1.name = "Block Cutout Mipped";
            cm1.SetTexture("_MainTex", AtlasManager.GetAtlasTexture(RenderType.CUTOUT_MIPPED));
            cm1.EnableKeyword("_ALPHATEST_ON");
            cm1.SetFloat("_Mode", 1);
            cm1.renderQueue = 2450;
            cm1.SetFloat("_Glossiness", 0F);
            cm1.SetFloat("_Cutoff", 0.75F);
            blockMaterials.Add(RenderType.CUTOUT_MIPPED, cm1);

            var cm2 = new Material(cshader);
            cm2.name = "Placebo Cutout Mipped";
            cm2.SetTexture("_MainTex", AtlasManager.PlcboTexture);
            cm2.EnableKeyword("_ALPHATEST_ON");
            cm2.SetFloat("_Mode", 1);
            cm2.renderQueue = 2450;
            cm2.SetFloat("_Glossiness", 0F);
            cm2.SetFloat("_Cutoff", 0.75F);
            plcboMaterials.Add(RenderType.CUTOUT_MIPPED, cm2);

            // Translucent
            var tshader = Shader.Find("Unicorn/BlockTranslucent");
            //var tshader = Shader.Find("Standard");

            var t1 = new Material(tshader);
            t1.name = "Block Transparent";
            t1.SetTexture("_MainTex", AtlasManager.GetAtlasTexture(RenderType.TRANSLUCENT));
            t1.SetFloat("_Mode", 2);
            t1.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            t1.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            t1.SetInt("_ZWrite", 0);
            t1.DisableKeyword("_ALPHATEST_ON");
            t1.EnableKeyword("_ALPHABLEND_ON");
            t1.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            t1.renderQueue = 3000;
            t1.SetFloat("_Glossiness", 0F);
            blockMaterials.Add(RenderType.TRANSLUCENT, t1);

            var t2 = new Material(tshader);
            t2.name = "Placebo Transparent";
            t2.SetTexture("_MainTex", AtlasManager.PlcboTexture);
            t2.SetFloat("_Mode", 2);
            t2.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            t2.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            t2.SetInt("_ZWrite", 0);
            t2.DisableKeyword("_ALPHATEST_ON");
            t2.EnableKeyword("_ALPHABLEND_ON");
            t2.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            t2.renderQueue = 3000;
            t2.SetFloat("_Glossiness", 0F);
            plcboMaterials.Add(RenderType.TRANSLUCENT, t2);

            initialized = true;

        }

    }

}