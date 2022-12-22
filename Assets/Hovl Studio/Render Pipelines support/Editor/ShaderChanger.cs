#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace HovlStudio
{
    public class RPChanger : EditorWindow
    {

        private int pipeline;
        [MenuItem("Tools/RP changer for Hovl Studio assets")]

        public static void ShowWindow()
        {
            RPChanger window = (RPChanger)EditorWindow.GetWindow(typeof(RPChanger));
            window.minSize = new Vector2(250, 120);
            window.maxSize = new Vector2(250, 120);
        }

        public void OnGUI()
        {
            GUILayout.Label("Change VFX pipeline to:");

            if (GUILayout.Button("Standard RP"))
            {
                FindShaders();
                ChangeToSRP();
            }
            if (GUILayout.Button("Universal RP"))
            {
                pipeline = 1;
                ImportPipelinePackage();
            }
            GUILayout.Label("Don't forget to enable Depth and Opaque\ncheck-buttons in your URP asset seeting.", GUILayout.ExpandWidth(true));
            if (GUILayout.Button("HDRP"))
            {
                pipeline = 2;
                ImportPipelinePackage();
            }
        }

        Shader Add_CG;
        Shader Blend_CG;
        Shader LightGlow;
        Shader Lit_CenterGlow;
        Shader Blend_TwoSides;
        Shader Blend_Normals;
        Shader Ice;
        Shader Distortion;
        Shader ParallaxIce;
        Shader BlendDistort;
        Shader VolumeLaser;
        Shader Explosion;
        Shader SwordSlash;
        Shader ShockWave;
        Shader SoftNoise;

        Shader Add_CG_URP;
        Shader Blend_CG_URP;
        Shader LightGlow_URP;
        Shader Lit_CenterGlow_URP;
        Shader Blend_TwoSides_URP;
        Shader Blend_Normals_URP;
        Shader Ice_URP;
        Shader Distortion_URP;
        Shader ParallaxIce_URP;
        Shader BlendDistort_URP;
        Shader VolumeLaser_URP;
        Shader Explosion_URP;
        Shader SwordSlash_URP;
        Shader ShockWave_URP;
        Shader SoftNoise_URP;

        Shader Add_CG_HDRP;
        Shader Blend_CG_HDRP;
        Shader LightGlow_HDRP;
        Shader Lit_CenterGlow_HDRP;
        Shader Blend_TwoSides_HDRP;
        Shader Blend_Normals_HDRP;
        Shader Ice_HDRP;
        Shader Distortion_HDRP;
        Shader ParallaxIce_HDRP;
        Shader BlendDistort_HDRP;
        Shader VolumeLaser_HDRP;
        Shader Explosion_HDRP;
        Shader SwordSlash_HDRP;
        Shader ShockWave_HDRP;
        Shader SoftNoise_HDRP;

        Material[] shaderMaterials;

        private void FindShaders()
        {
            if (Shader.Find("Hovl/Particles/Add_CenterGlow") != null) Add_CG = Shader.Find("Hovl/Particles/Add_CenterGlow");
            if (Shader.Find("Hovl/Particles/Blend_CenterGlow") != null) Blend_CG = Shader.Find("Hovl/Particles/Blend_CenterGlow");
            if (Shader.Find("Hovl/Particles/LightGlow") != null) LightGlow = Shader.Find("Hovl/Particles/LightGlow");
            if (Shader.Find("Hovl/Particles/Lit_CenterGlow") != null) Lit_CenterGlow = Shader.Find("Hovl/Particles/Lit_CenterGlow");
            if (Shader.Find("Hovl/Particles/Blend_TwoSides") != null) Blend_TwoSides = Shader.Find("Hovl/Particles/Blend_TwoSides");
            if (Shader.Find("Hovl/Particles/Blend_Normals") != null) Blend_Normals = Shader.Find("Hovl/Particles/Blend_Normals");
            if (Shader.Find("Hovl/Particles/Ice") != null) Ice = Shader.Find("Hovl/Particles/Ice");
            if (Shader.Find("Hovl/Particles/Distortion") != null) Distortion = Shader.Find("Hovl/Particles/Distortion");
            if (Shader.Find("Hovl/Opaque/ParallaxIce") != null) ParallaxIce = Shader.Find("Hovl/Opaque/ParallaxIce");
            if (Shader.Find("Hovl/Particles/BlendDistort") != null) BlendDistort = Shader.Find("Hovl/Particles/BlendDistort");
            if (Shader.Find("Hovl/Particles/VolumeLaser") != null) VolumeLaser = Shader.Find("Hovl/Particles/VolumeLaser");
            if (Shader.Find("Hovl/Particles/Explosion") != null) Explosion = Shader.Find("Hovl/Particles/Explosion");
            if (Shader.Find("Hovl/Particles/SwordSlash") != null) SwordSlash = Shader.Find("Hovl/Particles/SwordSlash");
            if (Shader.Find("Hovl/Particles/ShockWave") != null) ShockWave = Shader.Find("Hovl/Particles/ShockWave");
            if (Shader.Find("Hovl/Particles/SoftNoise") != null) SoftNoise = Shader.Find("Hovl/Particles/SoftNoise");

            if (Shader.Find("ERB/LWRP/Particles/LightGlow") != null) LightGlow_URP = Shader.Find("ERB/LWRP/Particles/LightGlow");
            if (Shader.Find("Shader Graphs/URP_Lit_CenterGlow") != null) Lit_CenterGlow_URP = Shader.Find("Shader Graphs/URP_Lit_CenterGlow");
            if (Shader.Find("Shader Graphs/URP_Blend_TwoSides") != null) Blend_TwoSides_URP = Shader.Find("Shader Graphs/URP_Blend_TwoSides");
            if (Shader.Find("Shader Graphs/URP_Blend_Normals") != null) Blend_Normals_URP = Shader.Find("Shader Graphs/URP_Blend_Normals");
            if (Shader.Find("Shader Graphs/URP_Ice") != null) Ice_URP = Shader.Find("Shader Graphs/URP_Ice");
            if (Shader.Find("Shader Graphs/URP_Distortion") != null) Distortion_URP = Shader.Find("Shader Graphs/URP_Distortion");
            if (Shader.Find("Shader Graphs/URP_ParallaxIce") != null) ParallaxIce_URP = Shader.Find("Shader Graphs/URP_ParallaxIce");
            if (Shader.Find("Shader Graphs/URP_Add_CG") != null) Add_CG_URP = Shader.Find("Shader Graphs/URP_Add_CG");
            if (Shader.Find("Shader Graphs/URP_Blend_CG") != null) Blend_CG_URP = Shader.Find("Shader Graphs/URP_Blend_CG");
            if (Shader.Find("Shader Graphs/URP_BlendDistort") != null) BlendDistort_URP = Shader.Find("Shader Graphs/URP_BlendDistort");
            if (Shader.Find("Shader Graphs/URP_VolumeLaser") != null) VolumeLaser_URP = Shader.Find("Shader Graphs/URP_VolumeLaser");
            if (Shader.Find("Shader Graphs/URP_Explosion") != null) Explosion_URP = Shader.Find("Shader Graphs/URP_Explosion");
            if (Shader.Find("Shader Graphs/URP_SwordSlash") != null) SwordSlash_URP = Shader.Find("Shader Graphs/URP_SwordSlash");
            if (Shader.Find("Shader Graphs/URP_ShockWave") != null) ShockWave_URP = Shader.Find("Shader Graphs/URP_ShockWave");
            if (Shader.Find("Shader Graphs/URP_SoftNoise") != null) SoftNoise_URP = Shader.Find("Shader Graphs/URP_SoftNoise");

            if (Shader.Find("ERB/HDRP/Particles/LightGlow") != null) LightGlow_HDRP = Shader.Find("ERB/HDRP/Particles/LightGlow");
            if (Shader.Find("Shader Graphs/HDRP_Lit_CenterGlow") != null) Lit_CenterGlow_HDRP = Shader.Find("Shader Graphs/HDRP_Lit_CenterGlow");
            if (Shader.Find("Shader Graphs/HDRP_Blend_TwoSides") != null) Blend_TwoSides_HDRP = Shader.Find("Shader Graphs/HDRP_Blend_TwoSides");
            if (Shader.Find("Shader Graphs/HDRP_Blend_Normals") != null) Blend_Normals_HDRP = Shader.Find("Shader Graphs/HDRP_Blend_Normals");
            if (Shader.Find("Shader Graphs/HDRP_Ice") != null) Ice_HDRP = Shader.Find("Shader Graphs/HDRP_Ice");
            if (Shader.Find("Shader Graphs/HDRP_Distortion") != null) Distortion_HDRP = Shader.Find("Shader Graphs/HDRP_Distortion");
            if (Shader.Find("Shader Graphs/HDRP_ParallaxIce") != null) ParallaxIce_HDRP = Shader.Find("Shader Graphs/HDRP_ParallaxIce");
            if (Shader.Find("Shader Graphs/HDRP_Add_CG") != null) Add_CG_HDRP = Shader.Find("Shader Graphs/HDRP_Add_CG");
            if (Shader.Find("Shader Graphs/HDRP_Blend_CG") != null) Blend_CG_HDRP = Shader.Find("Shader Graphs/HDRP_Blend_CG");
            if (Shader.Find("Shader Graphs/HDRP_BlendDistort") != null) BlendDistort_HDRP = Shader.Find("Shader Graphs/HDRP_BlendDistort");
            if (Shader.Find("Shader Graphs/HDRP_VolumeLaser") != null) VolumeLaser_HDRP = Shader.Find("Shader Graphs/HDRP_VolumeLaser");
            if (Shader.Find("Shader Graphs/HDRP_Explosion") != null) Explosion_HDRP = Shader.Find("Shader Graphs/HDRP_Explosion");
            if (Shader.Find("Shader Graphs/HDRP_SwordSlash") != null) SwordSlash_HDRP = Shader.Find("Shader Graphs/HDRP_SwordSlash");
            if (Shader.Find("Shader Graphs/HDRP_ShockWave") != null) ShockWave_HDRP = Shader.Find("Shader Graphs/HDRP_ShockWave");
            if (Shader.Find("Shader Graphs/HDRP_SoftNoise") != null) SoftNoise_HDRP = Shader.Find("Shader Graphs/HDRP_SoftNoise");

            string[] folderMat = AssetDatabase.FindAssets("t:Material", new[] { "Assets" });
            shaderMaterials = new Material[folderMat.Length];

            for (int i = 0; i < folderMat.Length; i++)
            {
                var patch = AssetDatabase.GUIDToAssetPath(folderMat[i]);
                shaderMaterials[i] = (Material)AssetDatabase.LoadAssetAtPath(patch, typeof(Material));
            }
        }

        private void ImportPipelinePackage()
        {
#if UNITY_2019_2
        switch (pipeline)
        {
            case 1:
                if (AssetDatabase.GUIDToAssetPath("f84b1a03ad7e89847a42377fdc96d921") != null)
                    AssetDatabase.ImportPackage(AssetDatabase.GUIDToAssetPath("f84b1a03ad7e89847a42377fdc96d921"), false);
                else
                    AssetDatabase.ImportPackage("Assets/Hovl Studio/Render Pipelines support/Unity 2019.2+ URP.unitypackage", false);
                AssetDatabase.importPackageCompleted += OnImportPackageCompleted;
                break;
            case 2:
                if (AssetDatabase.GUIDToAssetPath("d3b0d4375975afb4abf9fc745a5c788b") != null)
                    AssetDatabase.ImportPackage(AssetDatabase.GUIDToAssetPath("d3b0d4375975afb4abf9fc745a5c788b"), false);
                else
                    AssetDatabase.ImportPackage("Assets/Hovl Studio/Render Pipelines support/Unity 2019.2+ HDRP.unitypackage", false);
                AssetDatabase.importPackageCompleted += OnImportPackageCompleted;
                break;
            default:
                Debug.Log("You didn't choose pipeline");
                break;
        }
#endif
#if (UNITY_2019_3_OR_NEWER && UNITY_2019)
        switch (pipeline)
        {
            case 1:
                if (AssetDatabase.GUIDToAssetPath("ed9c841398c7fc1459cc7ad939bda692") != null)
                    AssetDatabase.ImportPackage(AssetDatabase.GUIDToAssetPath("ed9c841398c7fc1459cc7ad939bda692"), false);
                else
                    AssetDatabase.ImportPackage("Assets/Hovl Studio/Render Pipelines support/Unity 2019.3+ URP.unitypackage", false);
                AssetDatabase.importPackageCompleted += OnImportPackageCompleted;
                break;
            case 2:
                if (AssetDatabase.GUIDToAssetPath("dbb762d9e9eb76343b2843640c4ede68") != null)
                    AssetDatabase.ImportPackage(AssetDatabase.GUIDToAssetPath("dbb762d9e9eb76343b2843640c4ede68"), false);
                else
                    AssetDatabase.ImportPackage("Assets/Hovl Studio/Render Pipelines support/Unity 2019.3+ HDRP.unitypackage", false);
                AssetDatabase.importPackageCompleted += OnImportPackageCompleted;
                break;
            default:
                Debug.Log("You didn't choose pipeline");
                break;
        }
#endif
#if UNITY_2020_1_OR_NEWER
            switch (pipeline)
            {
                case 1:
                    if (AssetDatabase.GUIDToAssetPath("e7ce4ef7e809f0e489f5dd61cfe34b01") != null)
                        AssetDatabase.ImportPackage(AssetDatabase.GUIDToAssetPath("e7ce4ef7e809f0e489f5dd61cfe34b01"), false);
                    else
                        AssetDatabase.ImportPackage("Assets/Hovl Studio/Render Pipelines support/Unity 2020+ URP.unitypackage", false);
                    AssetDatabase.importPackageCompleted += OnImportPackageCompleted;
                    break;
                case 2:
                    if (AssetDatabase.GUIDToAssetPath("1c827ac5cb1890a488436295a34d4d25") != null)
                        AssetDatabase.ImportPackage(AssetDatabase.GUIDToAssetPath("1c827ac5cb1890a488436295a34d4d25"), false);
                    else
                        AssetDatabase.ImportPackage("Assets/Hovl Studio/Render Pipelines support/Unity 2020+ HDRP.unitypackage", false);
                    AssetDatabase.importPackageCompleted += OnImportPackageCompleted;
                    break;
                default:
                    Debug.Log("You didn't choose pipeline");
                    break;
            }
#endif
        }
        private void OnImportPackageCompleted(string packagename)
        {
            //Debug.Log($"Imported package: {packagename}");
            FindShaders();
            switch (pipeline)
            {
                case 1:
                    ChangeToURP();
                    break;
                case 2:
                    ChangeToHDRP();
                    ChangeToSRP();
                    ChangeToHDRP();
                    break;
                default:
                    Debug.Log("You didn't choose pipeline");
                    break;
            }
        }

        private void ChangeToURP()
        {
            foreach (var material in shaderMaterials)
            {
                if (Shader.Find("ERB/LWRP/Particles/LightGlow") != null)
                {
                    if (material.shader == LightGlow || material.shader == LightGlow_HDRP)
                    {
                        material.shader = LightGlow_URP;
                    }
                }
                /*----------------------------------------------------------------------------------------------------*/
                if (Shader.Find("Shader Graphs/URP_Lit_CenterGlow") != null)
                {
                    if (material.shader == Lit_CenterGlow || material.shader == Lit_CenterGlow_HDRP)
                    {
                        material.shader = Lit_CenterGlow_URP;
                    }
                }
                /*----------------------------------------------------------------------------------------------------*/
                if (Shader.Find("Shader Graphs/URP_Blend_TwoSides") != null)
                {
                    if (material.shader == Blend_TwoSides || material.shader == Blend_TwoSides_HDRP)
                    {
                        if (material.GetTexture("_MainTex") != null || material.GetTexture("_Noise") != null || material.HasProperty("_Cutoff"))
                        {
                            Vector2 MainScale = material.GetTextureScale("_MainTex");
                            Vector2 MainOffset = material.GetTextureOffset("_MainTex");
                            Vector2 NoiseScale = material.GetTextureScale("_Noise");
                            Vector2 NoiseOffset = material.GetTextureOffset("_Noise");
                            float Cutoff = material.GetFloat("_Cutoff");
                            material.shader = Blend_TwoSides_URP;
                            if (material.HasProperty("_MaskClipValue"))
                                material.SetFloat("_MaskClipValue", Cutoff);
                            if (material.HasProperty("_MainTexTiling"))
                                material.SetVector("_MainTexTiling", new Vector4(MainScale[0], MainScale[1], MainOffset[0], MainOffset[1]));
                            if (material.HasProperty("_NoiseTiling"))
                                material.SetVector("_NoiseTiling", new Vector4(NoiseScale[0], NoiseScale[1], NoiseOffset[0], NoiseOffset[1]));
                        }
                        else
                            material.shader = Blend_TwoSides_URP;
                    }
                }
                /*----------------------------------------------------------------------------------------------------*/
                if (Shader.Find("Shader Graphs/URP_Blend_Normals") != null)
                {
                    if (material.shader == Blend_Normals || material.shader == Blend_Normals_HDRP)
                    {
                        if (material.GetTexture("_MainTex") != null || material.GetTexture("_Noise") != null)
                        {
                            Vector2 MainScale = material.GetTextureScale("_MainTex");
                            Vector2 MainOffset = material.GetTextureOffset("_MainTex");
                            Vector2 NoiseScale = material.GetTextureScale("_Noise");
                            Vector2 NoiseOffset = material.GetTextureOffset("_Noise");
                            material.shader = Blend_Normals_URP;
                            if (material.HasProperty("_MainTexTiling"))
                                material.SetVector("_MainTexTiling", new Vector4(MainScale[0], MainScale[1], MainOffset[0], MainOffset[1]));
                            if (material.HasProperty("_NoiseTiling"))
                                material.SetVector("_NoiseTiling", new Vector4(NoiseScale[0], NoiseScale[1], NoiseOffset[0], NoiseOffset[1]));
                        }
                        else
                            material.shader = Blend_Normals_URP;
                    }
                }
                /*----------------------------------------------------------------------------------------------------*/
                if (Shader.Find("Shader Graphs/URP_Ice") != null)
                {
                    if (material.shader == Ice || material.shader == Ice_HDRP)
                    {
                        if (material.GetTexture("_MainTex") != null)
                        {
                            Vector2 MainScale = material.GetTextureScale("_MainTex");
                            Vector2 MainOffset = material.GetTextureOffset("_MainTex");
                            material.shader = Ice_URP;
                            if (material.HasProperty("_Tiling"))
                                material.SetVector("_Tiling", new Vector4(MainScale[0], MainScale[1], MainOffset[0], MainOffset[1]));
                        }
                        else
                            material.shader = Ice_URP;
                    }
                }
                /*----------------------------------------------------------------------------------------------------*/
                if (Shader.Find("Shader Graphs/URP_ParallaxIce") != null)
                {
                    if (material.shader == ParallaxIce || material.shader == ParallaxIce_HDRP)
                    {
                        if (material.GetTexture("_Emission") != null)
                        {
                            Vector2 MainScale = material.GetTextureScale("_Emission");
                            Vector2 MainOffset = material.GetTextureOffset("_Emission");
                            material.shader = ParallaxIce_URP;
                            if (material.HasProperty("_EmissionTiling"))
                                material.SetVector("_EmissionTiling", new Vector4(MainScale[0], MainScale[1], MainOffset[0], MainOffset[1]));
                        }
                        else
                            material.shader = ParallaxIce_URP;
                    }
                }
                /*----------------------------------------------------------------------------------------------------*/
                if (Shader.Find("Shader Graphs/URP_Distortion") != null)
                {
                    if (material.shader == Distortion || material.shader == Distortion_HDRP)
                    {
                        material.SetFloat("_ZWrite", 0);
                        material.shader = Distortion_URP;
                        material.renderQueue = 2750;
                    }
                }
                /*----------------------------------------------------------------------------------------------------*/
                if (Shader.Find("Shader Graphs/URP_Add_CG") != null)
                {
                    if (material.shader == Add_CG || material.shader == Add_CG_HDRP)
                    {
                        if (material.GetTexture("_MainTex") != null || material.GetTexture("_Noise") != null
                            || material.GetTexture("_Flow") != null || material.GetTexture("_Mask") != null)
                        {
                            Vector2 MainScale = material.GetTextureScale("_MainTex");
                            Vector2 MainOffset = material.GetTextureOffset("_MainTex");
                            Vector2 NoiseScale = material.GetTextureScale("_Noise");
                            Vector2 NoiseOffset = material.GetTextureOffset("_Noise");
                            Vector2 FlowScale = material.GetTextureScale("_Flow");
                            Vector2 FlowOffset = material.GetTextureOffset("_Flow");
                            Vector2 MaskScale = material.GetTextureScale("_Mask");
                            Vector2 MaskOffset = material.GetTextureOffset("_Mask");
                            material.shader = Add_CG_URP;
                            if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0);
                            if (material.HasProperty("_MainTexTiling"))
                                material.SetVector("_MainTexTiling", new Vector4(MainScale[0], MainScale[1], MainOffset[0], MainOffset[1]));
                            if (material.HasProperty("_NoiseTiling"))
                                material.SetVector("_NoiseTiling", new Vector4(NoiseScale[0], NoiseScale[1], NoiseOffset[0], NoiseOffset[1]));
                            if (material.HasProperty("_FlowTiling"))
                                material.SetVector("_FlowTiling", new Vector4(FlowScale[0], FlowScale[1], FlowOffset[0], FlowOffset[1]));
                            if (material.HasProperty("_MaskTiling"))
                                material.SetVector("_MaskTiling", new Vector4(MaskScale[0], MaskScale[1], MaskOffset[0], MaskOffset[1]));
                        }
                        else
                            material.shader = Add_CG_URP;
                        Debug.Log("Shaders changed successfully");
                    }
                }
                else Debug.Log("First import shaders!");
                /*----------------------------------------------------------------------------------------------------*/
                if (Shader.Find("Shader Graphs/URP_Blend_CG") != null)
                {
                    if (material.shader == Blend_CG || material.shader == Blend_CG_HDRP)
                    {
                        if (material.GetTexture("_MainTex") != null || material.GetTexture("_Noise") != null
                            || material.GetTexture("_Flow") != null || material.GetTexture("_Mask") != null)
                        {
                            Vector2 MainScale = material.GetTextureScale("_MainTex");
                            Vector2 MainOffset = material.GetTextureOffset("_MainTex");
                            Vector2 NoiseScale = material.GetTextureScale("_Noise");
                            Vector2 NoiseOffset = material.GetTextureOffset("_Noise");
                            Vector2 FlowScale = material.GetTextureScale("_Flow");
                            Vector2 FlowOffset = material.GetTextureOffset("_Flow");
                            Vector2 MaskScale = material.GetTextureScale("_Mask");
                            Vector2 MaskOffset = material.GetTextureOffset("_Mask");
                            material.shader = Blend_CG_URP;
                            if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0);
                            if (material.HasProperty("_MainTexTiling"))
                                material.SetVector("_MainTexTiling", new Vector4(MainScale[0], MainScale[1], MainOffset[0], MainOffset[1]));
                            if (material.HasProperty("_NoiseTiling"))
                                material.SetVector("_NoiseTiling", new Vector4(NoiseScale[0], NoiseScale[1], NoiseOffset[0], NoiseOffset[1]));
                            if (material.HasProperty("_FlowTiling"))
                                material.SetVector("_FlowTiling", new Vector4(FlowScale[0], FlowScale[1], FlowOffset[0], FlowOffset[1]));
                            if (material.HasProperty("_MaskTiling"))
                                material.SetVector("_MaskTiling", new Vector4(MaskScale[0], MaskScale[1], MaskOffset[0], MaskOffset[1]));
                        }
                        else
                            material.shader = Blend_CG_URP;
                    }
                }
                /*----------------------------------------------------------------------------------------------------*/
                if (Shader.Find("Shader Graphs/URP_BlendDistort") != null)
                {
                    if (material.shader == BlendDistort || material.shader == BlendDistort_HDRP)
                    {
                        if (material.GetTexture("_MainTex") != null || material.GetTexture("_Noise") != null
                            || material.GetTexture("_Flow") != null || material.GetTexture("_Mask") != null || material.GetTexture("_NormalMap") != null)
                        {
                            Vector2 MainScale = material.GetTextureScale("_MainTex");
                            Vector2 MainOffset = material.GetTextureOffset("_MainTex");
                            Vector2 NoiseScale = material.GetTextureScale("_Noise");
                            Vector2 NoiseOffset = material.GetTextureOffset("_Noise");
                            Vector2 FlowScale = material.GetTextureScale("_Flow");
                            Vector2 FlowOffset = material.GetTextureOffset("_Flow");
                            Vector2 MaskScale = material.GetTextureScale("_Mask");
                            Vector2 MaskOffset = material.GetTextureOffset("_Mask");
                            Vector2 NormalScale = material.GetTextureScale("_NormalMap");
                            Vector2 NormalOffset = material.GetTextureOffset("_NormalMap");
                            material.shader = BlendDistort_URP;
                            if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0);
                            if (material.HasProperty("_MainTexTiling"))
                                material.SetVector("_MainTexTiling", new Vector4(MainScale[0], MainScale[1], MainOffset[0], MainOffset[1]));
                            if (material.HasProperty("_NoiseTiling"))
                                material.SetVector("_NoiseTiling", new Vector4(NoiseScale[0], NoiseScale[1], NoiseOffset[0], NoiseOffset[1]));
                            if (material.HasProperty("_FlowTiling"))
                                material.SetVector("_FlowTiling", new Vector4(FlowScale[0], FlowScale[1], FlowOffset[0], FlowOffset[1]));
                            if (material.HasProperty("_MaskTiling"))
                                material.SetVector("_MaskTiling", new Vector4(MaskScale[0], MaskScale[1], MaskOffset[0], MaskOffset[1]));
                            if (material.HasProperty("_NormalMapTiling"))
                                material.SetVector("_NormalMapTiling", new Vector4(NormalScale[0], NormalScale[1], NormalOffset[0], NormalOffset[1]));
                        }
                        else
                            material.shader = BlendDistort_URP;
                    }
                }
                /*----------------------------------------------------------------------------------------------------*/
                if (Shader.Find("Shader Graphs/URP_VolumeLaser") != null)
                {
                    if (material.shader == VolumeLaser || material.shader == VolumeLaser_HDRP)
                    {
                        material.shader = VolumeLaser_URP;
                    }
                }
                /*----------------------------------------------------------------------------------------------------*/
                if (Shader.Find("Shader Graphs/URP_Explosion") != null)
                {
                    if (material.shader == Explosion || material.shader == Explosion_HDRP)
                    {
                        material.shader = Explosion_URP;
                    }
                }
                /*----------------------------------------------------------------------------------------------------*/
                if (Shader.Find("Shader Graphs/URP_SwordSlash") != null)
                {
                    if (material.shader == SwordSlash || material.shader == SwordSlash_HDRP)
                    {
                        if (material.GetTexture("_MainTexture") != null || material.GetTexture("_EmissionTex") != null
                            || material.GetTexture("_Dissolve") != null)
                        {
                            Vector2 MainScale = material.GetTextureScale("_MainTexture");
                            Vector2 MainOffset = material.GetTextureOffset("_MainTexture");
                            Vector2 EmissionTexScale = material.GetTextureScale("_EmissionTex");
                            Vector2 EmissionTexOffset = material.GetTextureOffset("_EmissionTex");
                            Vector2 DissolveScale = material.GetTextureScale("_Dissolve");
                            Vector2 DissolveOffset = material.GetTextureOffset("_Dissolve");
                            material.shader = SwordSlash_URP;
                            if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0);
                            if (material.HasProperty("_MainTextureTiling"))
                                material.SetVector("_MainTextureTiling", new Vector4(MainScale[0], MainScale[1], MainOffset[0], MainOffset[1]));
                            if (material.HasProperty("_EmissionTexTiling"))
                                material.SetVector("_EmissionTexTiling", new Vector4(EmissionTexScale[0], EmissionTexScale[1], EmissionTexOffset[0], EmissionTexOffset[1]));
                            if (material.HasProperty("_DissolveTiling"))
                                material.SetVector("_DissolveTiling", new Vector4(DissolveScale[0], DissolveScale[1], DissolveOffset[0], DissolveOffset[1]));
                        }
                        else
                            material.shader = SwordSlash_URP;
                    }
                }
                /*----------------------------------------------------------------------------------------------------*/
                if (Shader.Find("Shader Graphs/URP_ShockWave") != null)
                {
                    if (material.shader == ShockWave || material.shader == ShockWave_HDRP)
                    {
                        if (material.GetTexture("_MainTexture") != null || material.GetTexture("_Noise") != null
                            || material.GetTexture("_Flow") != null || material.GetTexture("_Mask") != null)
                        {
                            Vector2 MainScale = material.GetTextureScale("_MainTexture");
                            Vector2 MainOffset = material.GetTextureOffset("_MainTexture");
                            Vector2 NoiseScale = material.GetTextureScale("_Noise");
                            Vector2 NoiseOffset = material.GetTextureOffset("_Noise");
                            Vector2 FlowScale = material.GetTextureScale("_Flow");
                            Vector2 FlowOffset = material.GetTextureOffset("_Flow");
                            Vector2 MaskScale = material.GetTextureScale("_Mask");
                            Vector2 MaskOffset = material.GetTextureOffset("_Mask");
                            material.shader = ShockWave_URP;
                            if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0);
                            if (material.HasProperty("_MainTexTiling"))
                                material.SetVector("_MainTexTiling", new Vector4(MainScale[0], MainScale[1], MainOffset[0], MainOffset[1]));
                            if (material.HasProperty("_NoiseTiling"))
                                material.SetVector("_NoiseTiling", new Vector4(NoiseScale[0], NoiseScale[1], NoiseOffset[0], NoiseOffset[1]));
                            if (material.HasProperty("_FlowTiling"))
                                material.SetVector("_FlowTiling", new Vector4(FlowScale[0], FlowScale[1], FlowOffset[0], FlowOffset[1]));
                            if (material.HasProperty("_MaskTiling"))
                                material.SetVector("_MaskTiling", new Vector4(MaskScale[0], MaskScale[1], MaskOffset[0], MaskOffset[1]));
                        }
                        else
                            material.shader = ShockWave_URP;
                    }
                }
                /*----------------------------------------------------------------------------------------------------*/
                if (Shader.Find("Shader Graphs/URP_SoftNoise") != null)
                {
                    if (material.shader == SoftNoise || material.shader == SoftNoise_HDRP)
                    {
                        if (material.GetTexture("_MainTex") != null || material.GetTexture("_Noise") != null
                            || material.GetTexture("_OpacityTex") != null || material.GetTexture("_Mask") != null)
                        {
                            Vector2 MainScale = material.GetTextureScale("_MainTex");
                            Vector2 MainOffset = material.GetTextureOffset("_MainTex");
                            Vector2 NoiseScale = material.GetTextureScale("_Noise");
                            Vector2 NoiseOffset = material.GetTextureOffset("_Noise");
                            Vector2 OpacityTexScale = material.GetTextureScale("_OpacityTex");
                            Vector2 OpacityTexOffset = material.GetTextureOffset("_OpacityTex");
                            Vector2 MaskScale = material.GetTextureScale("_Mask");
                            Vector2 MaskOffset = material.GetTextureOffset("_Mask");
                            material.shader = SoftNoise_URP;
                            if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0);
                            if (material.HasProperty("_MainTexTiling"))
                                material.SetVector("_MainTexTiling", new Vector4(MainScale[0], MainScale[1], MainOffset[0], MainOffset[1]));
                            if (material.HasProperty("_NoiseTiling"))
                                material.SetVector("_NoiseTiling", new Vector4(NoiseScale[0], NoiseScale[1], NoiseOffset[0], NoiseOffset[1]));
                            if (material.HasProperty("_OpacityTexTiling"))
                                material.SetVector("_OpacityTexTiling", new Vector4(OpacityTexScale[0], OpacityTexScale[1], OpacityTexOffset[0], OpacityTexOffset[1]));
                            if (material.HasProperty("_MaskTiling"))
                                material.SetVector("_MaskTiling", new Vector4(MaskScale[0], MaskScale[1], MaskOffset[0], MaskOffset[1]));
                        }
                        else
                            material.shader = SoftNoise_URP;
                    }
                }
            }
        }

        private void ChangeToSRP()
        {

            foreach (var material in shaderMaterials)
            {
                if (Shader.Find("Hovl/Particles/LightGlow") != null)
                {
                    if (material.shader == LightGlow_URP || material.shader == LightGlow_HDRP)
                    {
                        material.shader = LightGlow;
                    }
                }
                /*----------------------------------------------------------------------------------------------------*/
                if (Shader.Find("Hovl/Particles/Lit_CenterGlow") != null)
                {
                    if (material.shader == Lit_CenterGlow_URP || material.shader == Lit_CenterGlow_HDRP)
                    {
                        material.shader = Lit_CenterGlow;
                    }
                }
                /*----------------------------------------------------------------------------------------------------*/
                if (Shader.Find("Hovl/Particles/Blend_TwoSides") != null)
                {
                    if (material.shader == Blend_TwoSides_URP || material.shader == Blend_TwoSides_HDRP)
                    {
                        if (material.HasProperty("_MainTexTiling") && material.HasProperty("_NoiseTiling"))
                        {
                            Vector4 MainTiling = material.GetVector("_MainTexTiling");
                            Vector4 NoiseTiling = material.GetVector("_NoiseTiling");
                            material.shader = Blend_TwoSides;
                            if (material.GetTexture("_MainTex") != null && material.GetTexture("_Noise") != null)
                            {
                                material.SetTextureScale("_MainTex", new Vector2(MainTiling[0], MainTiling[1]));
                                material.SetTextureOffset("_MainTex", new Vector2(MainTiling[2], MainTiling[3]));
                                material.SetTextureScale("_Noise", new Vector2(NoiseTiling[0], NoiseTiling[1]));
                                material.SetTextureOffset("_Noise", new Vector2(NoiseTiling[2], NoiseTiling[3]));
                            }
                        }
                        else
                            material.shader = Blend_TwoSides;
                    }
                }
                /*----------------------------------------------------------------------------------------------------*/
                if (Shader.Find("Hovl/Particles/Blend_Normals") != null)
                {
                    if (material.shader == Blend_Normals_URP || material.shader == Blend_Normals_HDRP)
                    {
                        if (material.HasProperty("_MainTexTiling") && material.HasProperty("_NoiseTiling"))
                        {
                            Vector4 MainTiling = material.GetVector("_MainTexTiling");
                            Vector4 NoiseTiling = material.GetVector("_NoiseTiling");
                            material.shader = Blend_Normals;
                            if (material.GetTexture("_MainTex") != null && material.GetTexture("_Noise") != null)
                            {
                                material.SetTextureScale("_MainTex", new Vector2(MainTiling[0], MainTiling[1]));
                                material.SetTextureOffset("_MainTex", new Vector2(MainTiling[2], MainTiling[3]));
                                material.SetTextureScale("_Noise", new Vector2(NoiseTiling[0], NoiseTiling[1]));
                                material.SetTextureOffset("_Noise", new Vector2(NoiseTiling[2], NoiseTiling[3]));
                            }
                        }
                        else
                            material.shader = Blend_Normals;
                    }
                }
                /*----------------------------------------------------------------------------------------------------*/
                if (Shader.Find("Hovl/Particles/Ice") != null)
                {
                    if (material.shader == Ice_URP || material.shader == Ice_HDRP)
                    {
                        if (material.HasProperty("_Tiling"))
                        {
                            Vector4 MainTiling = material.GetVector("_Tiling");
                            material.shader = Ice;
                            if (material.GetTexture("_MainTex") != null)
                            {
                                material.SetTextureScale("_MainTex", new Vector2(MainTiling[0], MainTiling[1]));
                                material.SetTextureOffset("_MainTex", new Vector2(MainTiling[2], MainTiling[3]));
                            }
                        }
                        else
                            material.shader = Ice;
                    }
                }
                /*----------------------------------------------------------------------------------------------------*/
                if (Shader.Find("Hovl/Opaque/ParallaxIce") != null)
                {
                    if (material.shader == ParallaxIce_URP || material.shader == ParallaxIce_HDRP)
                    {
                        if (material.HasProperty("_EmissionTiling"))
                        {
                            Vector4 MainTiling = material.GetVector("_EmissionTiling");
                            material.shader = ParallaxIce;
                            if (material.GetTexture("_Emission") != null)
                            {
                                material.SetTextureScale("_Emission", new Vector2(MainTiling[0], MainTiling[1]));
                                material.SetTextureOffset("_Emission", new Vector2(MainTiling[2], MainTiling[3]));
                            }
                        }
                        else
                            material.shader = ParallaxIce;
                    }
                }
                /*----------------------------------------------------------------------------------------------------*/
                if (Shader.Find("Hovl/Particles/Distortion") != null)
                {
                    if (material.shader == Distortion_URP || material.shader == Distortion_HDRP)
                    {
                        material.shader = Distortion;
                        material.renderQueue = 2750;
                    }
                }
                /*----------------------------------------------------------------------------------------------------*/
                if (Shader.Find("Hovl/Particles/Add_CenterGlow") != null)
                {
                    if (material.shader == Add_CG_URP || material.shader == Add_CG_HDRP)
                    {
                        if (material.HasProperty("_MainTexTiling") && material.HasProperty("_NoiseTiling")
                            && material.HasProperty("_FlowTiling") && material.HasProperty("_MaskTiling"))
                        {
                            Vector4 MainTiling = material.GetVector("_MainTexTiling");
                            Vector4 NoiseTiling = material.GetVector("_NoiseTiling");
                            Vector4 FlowTiling = material.GetVector("_FlowTiling");
                            Vector4 MaskTiling = material.GetVector("_MaskTiling");
                            material.shader = Add_CG;
                            if (material.GetTexture("_MainTex") != null && material.GetTexture("_Noise") != null)
                            {
                                material.SetTextureScale("_MainTex", new Vector2(MainTiling[0], MainTiling[1]));
                                material.SetTextureOffset("_MainTex", new Vector2(MainTiling[2], MainTiling[3]));
                                material.SetTextureScale("_Noise", new Vector2(NoiseTiling[0], NoiseTiling[1]));
                                material.SetTextureOffset("_Noise", new Vector2(NoiseTiling[2], NoiseTiling[3]));
                                material.SetTextureScale("_Flow", new Vector2(FlowTiling[0], FlowTiling[1]));
                                material.SetTextureOffset("_Flow", new Vector2(FlowTiling[2], FlowTiling[3]));
                                material.SetTextureScale("_Mask", new Vector2(MaskTiling[0], MaskTiling[1]));
                                material.SetTextureOffset("_Mask", new Vector2(MaskTiling[2], MaskTiling[3]));
                            }
                        }
                        else
                            material.shader = Add_CG;
                        Debug.Log("Shaders changed successfully");
                    }
                }
                else Debug.Log("First import shaders!");
                /*----------------------------------------------------------------------------------------------------*/
                if (Shader.Find("Hovl/Particles/Blend_CenterGlow") != null)
                {
                    if (material.shader == Blend_CG_URP || material.shader == Blend_CG_HDRP)
                    {
                        if (material.HasProperty("_MainTexTiling") && material.HasProperty("_NoiseTiling")
                            && material.HasProperty("_FlowTiling") && material.HasProperty("_MaskTiling"))
                        {
                            Vector4 MainTiling = material.GetVector("_MainTexTiling");
                            Vector4 NoiseTiling = material.GetVector("_NoiseTiling");
                            Vector4 FlowTiling = material.GetVector("_FlowTiling");
                            Vector4 MaskTiling = material.GetVector("_MaskTiling");
                            material.shader = Blend_CG;
                            if (material.GetTexture("_MainTex") != null && material.GetTexture("_Noise") != null)
                            {
                                material.SetTextureScale("_MainTex", new Vector2(MainTiling[0], MainTiling[1]));
                                material.SetTextureOffset("_MainTex", new Vector2(MainTiling[2], MainTiling[3]));
                                material.SetTextureScale("_Noise", new Vector2(NoiseTiling[0], NoiseTiling[1]));
                                material.SetTextureOffset("_Noise", new Vector2(NoiseTiling[2], NoiseTiling[3]));
                                material.SetTextureScale("_Flow", new Vector2(FlowTiling[0], FlowTiling[1]));
                                material.SetTextureOffset("_Flow", new Vector2(FlowTiling[2], FlowTiling[3]));
                                material.SetTextureScale("_Mask", new Vector2(MaskTiling[0], MaskTiling[1]));
                                material.SetTextureOffset("_Mask", new Vector2(MaskTiling[2], MaskTiling[3]));
                            }
                        }
                        else
                            material.shader = Blend_CG;
                    }
                }
                /*----------------------------------------------------------------------------------------------------*/
                if (Shader.Find("Hovl/Particles/BlendDistort") != null)
                {
                    if (material.shader == BlendDistort_URP || material.shader == BlendDistort_HDRP)
                    {
                        if (material.HasProperty("_MainTexTiling") && material.HasProperty("_NoiseTiling")
                            && material.HasProperty("_FlowTiling") && material.HasProperty("_MaskTiling") && material.HasProperty("_NormalMapTiling"))
                        {
                            Vector4 MainTiling = material.GetVector("_MainTexTiling");
                            Vector4 NoiseTiling = material.GetVector("_NoiseTiling");
                            Vector4 FlowTiling = material.GetVector("_FlowTiling");
                            Vector4 MaskTiling = material.GetVector("_MaskTiling");
                            Vector4 NormalTiling = material.GetVector("_NormalMapTiling");
                            material.shader = BlendDistort;
                            if (material.GetTexture("_MainTex") != null && material.GetTexture("_Noise") != null)
                            {
                                material.SetTextureScale("_MainTex", new Vector2(MainTiling[0], MainTiling[1]));
                                material.SetTextureOffset("_MainTex", new Vector2(MainTiling[2], MainTiling[3]));
                                material.SetTextureScale("_Noise", new Vector2(NoiseTiling[0], NoiseTiling[1]));
                                material.SetTextureOffset("_Noise", new Vector2(NoiseTiling[2], NoiseTiling[3]));
                                material.SetTextureScale("_Flow", new Vector2(FlowTiling[0], FlowTiling[1]));
                                material.SetTextureOffset("_Flow", new Vector2(FlowTiling[2], FlowTiling[3]));
                                material.SetTextureScale("_Mask", new Vector2(MaskTiling[0], MaskTiling[1]));
                                material.SetTextureOffset("_Mask", new Vector2(MaskTiling[2], MaskTiling[3]));
                                material.SetTextureScale("_NormalMap", new Vector2(NormalTiling[0], NormalTiling[1]));
                                material.SetTextureOffset("_NormalMap", new Vector2(NormalTiling[2], NormalTiling[3]));
                            }
                        }
                        else
                            material.shader = BlendDistort;
                    }
                }
                /*----------------------------------------------------------------------------------------------------*/
                if (Shader.Find("Hovl/Particles/VolumeLaser") != null)
                {
                    if (material.shader == VolumeLaser_URP || material.shader == VolumeLaser_HDRP)
                    {
                        material.shader = VolumeLaser;
                    }
                }
                /*----------------------------------------------------------------------------------------------------*/
                if (Shader.Find("Hovl/Particles/Explosion") != null)
                {
                    if (material.shader == Explosion_URP || material.shader == Explosion_HDRP)
                    {
                        material.shader = Explosion;
                    }
                }
                /*----------------------------------------------------------------------------------------------------*/
                if (Shader.Find("Hovl/Particles/SwordSlash") != null)
                {
                    if (material.shader == SwordSlash_URP || material.shader == SwordSlash_HDRP)
                    {
                        if (material.HasProperty("_MainTextureTiling") && material.HasProperty("_EmissionTexTiling") && material.HasProperty("_DissovleTiling"))
                        {
                            Vector4 MainTiling = material.GetVector("_MainTextureTiling");
                            Vector4 EmissionTiling = material.GetVector("_EmissionTexTiling");
                            Vector4 DissolveTiling = material.GetVector("_DissovleTiling");
                            material.shader = SwordSlash;
                            if (material.GetTexture("_MainTex") != null && material.GetTexture("_Noise") != null)
                            {
                                material.SetTextureScale("_MainTex", new Vector2(MainTiling[0], MainTiling[1]));
                                material.SetTextureOffset("_MainTex", new Vector2(MainTiling[2], MainTiling[3]));
                                material.SetTextureScale("_Noise", new Vector2(EmissionTiling[0], EmissionTiling[1]));
                                material.SetTextureOffset("_Noise", new Vector2(EmissionTiling[2], EmissionTiling[3]));
                                material.SetTextureScale("_Flow", new Vector2(DissolveTiling[0], DissolveTiling[1]));
                                material.SetTextureOffset("_Flow", new Vector2(DissolveTiling[2], DissolveTiling[3]));
                            }
                        }
                        else
                            material.shader = SwordSlash;
                    }
                }
                /*----------------------------------------------------------------------------------------------------*/
                if (Shader.Find("Hovl/Particles/ShockWave") != null)
                {
                    if (material.shader == ShockWave_URP || material.shader == ShockWave_HDRP)
                    {
                        if (material.HasProperty("_MainTexTiling") && material.HasProperty("_NoiseTiling")
                            && material.HasProperty("_FlowTiling") && material.HasProperty("_MaskTiling"))
                        {
                            Vector4 MainTiling = material.GetVector("_MainTexTiling");
                            Vector4 NoiseTiling = material.GetVector("_NoiseTiling");
                            Vector4 FlowTiling = material.GetVector("_FlowTiling");
                            Vector4 MaskTiling = material.GetVector("_MaskTiling");
                            material.shader = ShockWave;
                            if (material.GetTexture("_MainTexture") != null && material.GetTexture("_Noise") != null)
                            {
                                material.SetTextureScale("_MainTexture", new Vector2(MainTiling[0], MainTiling[1]));
                                material.SetTextureOffset("_MainTexture", new Vector2(MainTiling[2], MainTiling[3]));
                                material.SetTextureScale("_Noise", new Vector2(NoiseTiling[0], NoiseTiling[1]));
                                material.SetTextureOffset("_Noise", new Vector2(NoiseTiling[2], NoiseTiling[3]));
                                material.SetTextureScale("_Flow", new Vector2(FlowTiling[0], FlowTiling[1]));
                                material.SetTextureOffset("_Flow", new Vector2(FlowTiling[2], FlowTiling[3]));
                                material.SetTextureScale("_Mask", new Vector2(MaskTiling[0], MaskTiling[1]));
                                material.SetTextureOffset("_Mask", new Vector2(MaskTiling[2], MaskTiling[3]));
                            }
                        }
                        else
                            material.shader = ShockWave;
                    }
                }
                /*----------------------------------------------------------------------------------------------------*/
                if (Shader.Find("Hovl/Particles/SoftNoise") != null)
                {
                    if (material.shader == SoftNoise_URP || material.shader == SoftNoise_HDRP)
                    {
                        if (material.HasProperty("_MainTexTiling") && material.HasProperty("_NoiseTiling")
                            && material.HasProperty("_OpacityTexTiling") && material.HasProperty("_MaskTiling"))
                        {
                            Vector4 MainTiling = material.GetVector("_MainTexTiling");
                            Vector4 NoiseTiling = material.GetVector("_NoiseTiling");
                            Vector4 OpacityTexTiling = material.GetVector("_OpacityTexTiling");
                            Vector4 MaskTiling = material.GetVector("_MaskTiling");
                            material.shader = SoftNoise;
                            if (material.GetTexture("_MainTex") != null && material.GetTexture("_Noise") != null)
                            {
                                material.SetTextureScale("_MainTex", new Vector2(MainTiling[0], MainTiling[1]));
                                material.SetTextureOffset("_MainTex", new Vector2(MainTiling[2], MainTiling[3]));
                                material.SetTextureScale("_Noise", new Vector2(NoiseTiling[0], NoiseTiling[1]));
                                material.SetTextureOffset("_Noise", new Vector2(NoiseTiling[2], NoiseTiling[3]));
                                material.SetTextureScale("_OpacityTex", new Vector2(OpacityTexTiling[0], OpacityTexTiling[1]));
                                material.SetTextureOffset("_OpacityTex", new Vector2(OpacityTexTiling[2], OpacityTexTiling[3]));
                                material.SetTextureScale("_Mask", new Vector2(MaskTiling[0], MaskTiling[1]));
                                material.SetTextureOffset("_Mask", new Vector2(MaskTiling[2], MaskTiling[3]));
                            }
                        }
                        else
                            material.shader = SoftNoise;
                    }
                }
            }
        }

        private void ChangeToHDRP()
        {
            foreach (var material in shaderMaterials)
            {
                if (Shader.Find("ERB/HDRP/Particles/LightGlow") != null)
                {
                    if (material.shader == LightGlow || material.shader == LightGlow_URP)
                    {
                        material.shader = LightGlow_HDRP;
                    }
                }
                /*----------------------------------------------------------------------------------------------------*/
                if (Shader.Find("Shader Graphs/HDRP_Lit_CenterGlow") != null)
                {
                    if (material.shader == Lit_CenterGlow || material.shader == Lit_CenterGlow_URP)
                    {
                        material.shader = Lit_CenterGlow_HDRP;
                    }
                }
                /*----------------------------------------------------------------------------------------------------*/
                if (Shader.Find("Shader Graphs/HDRP_Blend_TwoSides") != null)
                {
                    if (material.shader == Blend_TwoSides || material.shader == Blend_TwoSides_URP)
                    {
                        if (material.GetTexture("_MainTex") != null || material.GetTexture("_Noise") != null)
                        {
                            Vector2 MainScale = material.GetTextureScale("_MainTex");
                            Vector2 MainOffset = material.GetTextureOffset("_MainTex");
                            Vector2 NoiseScale = material.GetTextureScale("_Noise");
                            Vector2 NoiseOffset = material.GetTextureOffset("_Noise");
                            material.shader = Blend_TwoSides_HDRP;
                            if (material.HasProperty("_MainTexTiling"))
                                material.SetVector("_MainTexTiling", new Vector4(MainScale[0], MainScale[1], MainOffset[0], MainOffset[1]));
                            if (material.HasProperty("_NoiseTiling"))
                                material.SetVector("_NoiseTiling", new Vector4(NoiseScale[0], NoiseScale[1], NoiseOffset[0], NoiseOffset[1]));
                        }
                        else
                            material.shader = Blend_TwoSides_HDRP;
                    }
                }
                /*----------------------------------------------------------------------------------------------------*/
                if (Shader.Find("Shader Graphs/HDRP_Blend_Normals") != null)
                {
                    if (material.shader == Blend_Normals || material.shader == Blend_Normals_URP)
                    {
                        if (material.GetTexture("_MainTex") != null || material.GetTexture("_Noise") != null)
                        {
                            Vector2 MainScale = material.GetTextureScale("_MainTex");
                            Vector2 MainOffset = material.GetTextureOffset("_MainTex");
                            Vector2 NoiseScale = material.GetTextureScale("_Noise");
                            Vector2 NoiseOffset = material.GetTextureOffset("_Noise");
                            material.shader = Blend_Normals_HDRP;
                            if (material.HasProperty("_MainTexTiling"))
                                material.SetVector("_MainTexTiling", new Vector4(MainScale[0], MainScale[1], MainOffset[0], MainOffset[1]));
                            if (material.HasProperty("_NoiseTiling"))
                                material.SetVector("_NoiseTiling", new Vector4(NoiseScale[0], NoiseScale[1], NoiseOffset[0], NoiseOffset[1]));
                        }
                        else
                            material.shader = Blend_Normals_HDRP;
                    }
                }
                /*----------------------------------------------------------------------------------------------------*/
                if (Shader.Find("Shader Graphs/HDRP_Ice") != null)
                {
                    if (material.shader == Ice || material.shader == Ice_URP)
                    {
                        if (material.GetTexture("_MainTex") != null)
                        {
                            Vector2 MainScale = material.GetTextureScale("_MainTex");
                            Vector2 MainOffset = material.GetTextureOffset("_MainTex");
                            material.shader = Ice_HDRP;
                            if (material.HasProperty("_Tiling"))
                                material.SetVector("_Tiling", new Vector4(MainScale[0], MainScale[1], MainOffset[0], MainOffset[1]));
                        }
                        else
                            material.shader = Ice_HDRP;
                    }
                }
                /*----------------------------------------------------------------------------------------------------*/
                if (Shader.Find("Shader Graphs/HDRP_ParallaxIce") != null)
                {
                    if (material.shader == ParallaxIce || material.shader == ParallaxIce_URP)
                    {
                        if (material.GetTexture("_Emission") != null)
                        {
                            Vector2 MainScale = material.GetTextureScale("_Emission");
                            Vector2 MainOffset = material.GetTextureOffset("_Emission");
                            material.shader = ParallaxIce_HDRP;
                            if (material.HasProperty("_EmissionTiling"))
                                material.SetVector("_EmissionTiling", new Vector4(MainScale[0], MainScale[1], MainOffset[0], MainOffset[1]));
                        }
                        else
                            material.shader = ParallaxIce_HDRP;
                    }
                }
                /*----------------------------------------------------------------------------------------------------*/
                if (Shader.Find("Shader Graphs/HDRP_Distortion") != null)
                {
                    if (material.shader == Distortion || material.shader == Distortion_URP)
                    {
                        material.SetFloat("_ZWrite", 0);
                        material.shader = Distortion_HDRP;
                        material.renderQueue = 2750;
                    }
                }
                /*----------------------------------------------------------------------------------------------------*/
                if (Shader.Find("Shader Graphs/HDRP_Add_CG") != null)
                {
                    if (material.shader == Add_CG || material.shader == Add_CG_URP)
                    {
                        if (material.GetTexture("_MainTex") != null || material.GetTexture("_Noise") != null
                            || material.GetTexture("_Flow") != null || material.GetTexture("_Mask") != null)
                        {
                            Vector2 MainScale = material.GetTextureScale("_MainTex");
                            Vector2 MainOffset = material.GetTextureOffset("_MainTex");
                            Vector2 NoiseScale = material.GetTextureScale("_Noise");
                            Vector2 NoiseOffset = material.GetTextureOffset("_Noise");
                            Vector2 FlowScale = material.GetTextureScale("_Flow");
                            Vector2 FlowOffset = material.GetTextureOffset("_Flow");
                            Vector2 MaskScale = material.GetTextureScale("_Mask");
                            Vector2 MaskOffset = material.GetTextureOffset("_Mask");
                            material.SetFloat("_StencilRef", 0);
                            material.SetFloat("_AlphaDstBlend", 1);
                            material.SetFloat("_DstBlend", 1);
                            material.SetFloat("_ZWrite", 0);
                            material.SetFloat("_SrcBlend", 1);
                            material.EnableKeyword("_BLENDMODE_ADD _DOUBLESIDED_ON _SURFACE_TYPE_TRANSPARENT");
                            material.SetShaderPassEnabled("TransparentBackface", false);
                            material.SetOverrideTag("RenderType", "Transparent");
                            material.SetFloat("_CullModeForward", 0);
                            material.shader = Add_CG_HDRP;
                            if (material.HasProperty("_MainTexTiling"))
                                material.SetVector("_MainTexTiling", new Vector4(MainScale[0], MainScale[1], MainOffset[0], MainOffset[1]));
                            if (material.HasProperty("_NoiseTiling"))
                                material.SetVector("_NoiseTiling", new Vector4(NoiseScale[0], NoiseScale[1], NoiseOffset[0], NoiseOffset[1]));
                            if (material.HasProperty("_FlowTiling"))
                                material.SetVector("_FlowTiling", new Vector4(FlowScale[0], FlowScale[1], FlowOffset[0], FlowOffset[1]));
                            if (material.HasProperty("_MaskTiling"))
                                material.SetVector("_MaskTiling", new Vector4(MaskScale[0], MaskScale[1], MaskOffset[0], MaskOffset[1]));
                        }
                        else
                            material.shader = Add_CG_HDRP;
                        Debug.Log("Shaders changed successfully");
                    }
                }
                else Debug.Log("First import shaders!");
                /*----------------------------------------------------------------------------------------------------*/
                if (Shader.Find("Shader Graphs/HDRP_Blend_CG") != null)
                {
                    if (material.shader == Blend_CG || material.shader == Blend_CG_URP)
                    {
                        if (material.GetTexture("_MainTex") != null || material.GetTexture("_Noise") != null
                            || material.GetTexture("_Flow") != null || material.GetTexture("_Mask") != null)
                        {
                            Vector2 MainScale = material.GetTextureScale("_MainTex");
                            Vector2 MainOffset = material.GetTextureOffset("_MainTex");
                            Vector2 NoiseScale = material.GetTextureScale("_Noise");
                            Vector2 NoiseOffset = material.GetTextureOffset("_Noise");
                            Vector2 FlowScale = material.GetTextureScale("_Flow");
                            Vector2 FlowOffset = material.GetTextureOffset("_Flow");
                            Vector2 MaskScale = material.GetTextureScale("_Mask");
                            Vector2 MaskOffset = material.GetTextureOffset("_Mask");
                            material.SetFloat("_ZWrite", 0);
                            material.SetFloat("_StencilRef", 0);
                            material.SetShaderPassEnabled("TransparentBackface", false);
                            material.SetOverrideTag("RenderType", "Transparent");
                            material.SetFloat("_AlphaDstBlend", 10);
                            material.SetFloat("_DstBlend", 10);
                            material.SetFloat("_SrcBlend", 1);
                            material.EnableKeyword("_BLENDMODE_ALPHA _DOUBLESIDED_ON _SURFACE_TYPE_TRANSPARENT");
                            if (material.HasProperty("_CullModeForward")) material.SetFloat("_CullModeForward", 0);
                            material.shader = Blend_CG_HDRP;
                            if (material.HasProperty("_MainTexTiling"))
                                material.SetVector("_MainTexTiling", new Vector4(MainScale[0], MainScale[1], MainOffset[0], MainOffset[1]));
                            if (material.HasProperty("_NoiseTiling"))
                                material.SetVector("_NoiseTiling", new Vector4(NoiseScale[0], NoiseScale[1], NoiseOffset[0], NoiseOffset[1]));
                            if (material.HasProperty("_FlowTiling"))
                                material.SetVector("_FlowTiling", new Vector4(FlowScale[0], FlowScale[1], FlowOffset[0], FlowOffset[1]));
                            if (material.HasProperty("_MaskTiling"))
                                material.SetVector("_MaskTiling", new Vector4(MaskScale[0], MaskScale[1], MaskOffset[0], MaskOffset[1]));
                        }
                        else
                            material.shader = Blend_CG_HDRP;
                    }
                }
                /*----------------------------------------------------------------------------------------------------*/
                if (Shader.Find("Shader Graphs/HDRP_BlendDistort") != null)
                {
                    if (material.shader == BlendDistort || material.shader == BlendDistort_URP)
                    {
                        if (material.GetTexture("_MainTex") != null || material.GetTexture("_Noise") != null
                            || material.GetTexture("_Flow") != null || material.GetTexture("_Mask") != null || material.GetTexture("_NormalMap") != null)
                        {
                            Vector2 MainScale = material.GetTextureScale("_MainTex");
                            Vector2 MainOffset = material.GetTextureOffset("_MainTex");
                            Vector2 NoiseScale = material.GetTextureScale("_Noise");
                            Vector2 NoiseOffset = material.GetTextureOffset("_Noise");
                            Vector2 FlowScale = material.GetTextureScale("_Flow");
                            Vector2 FlowOffset = material.GetTextureOffset("_Flow");
                            Vector2 MaskScale = material.GetTextureScale("_Mask");
                            Vector2 MaskOffset = material.GetTextureOffset("_Mask");
                            Vector2 NormalScale = material.GetTextureScale("_NormalMap");
                            Vector2 NormalOffset = material.GetTextureOffset("_NormalMap");
                            material.shader = BlendDistort_HDRP;
                            if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0);
                            if (material.HasProperty("_MainTexTiling"))
                                material.SetVector("_MainTexTiling", new Vector4(MainScale[0], MainScale[1], MainOffset[0], MainOffset[1]));
                            if (material.HasProperty("_NoiseTiling"))
                                material.SetVector("_NoiseTiling", new Vector4(NoiseScale[0], NoiseScale[1], NoiseOffset[0], NoiseOffset[1]));
                            if (material.HasProperty("_FlowTiling"))
                                material.SetVector("_FlowTiling", new Vector4(FlowScale[0], FlowScale[1], FlowOffset[0], FlowOffset[1]));
                            if (material.HasProperty("_MaskTiling"))
                                material.SetVector("_MaskTiling", new Vector4(MaskScale[0], MaskScale[1], MaskOffset[0], MaskOffset[1]));
                            if (material.HasProperty("_NormalMapTiling"))
                                material.SetVector("_NormalMapTiling", new Vector4(NormalScale[0], NormalScale[1], NormalOffset[0], NormalOffset[1]));
                        }
                        else
                            material.shader = BlendDistort_HDRP;
                    }
                }
                /*----------------------------------------------------------------------------------------------------*/
                if (Shader.Find("Shader Graphs/HDRP_VolumeLaser") != null)
                {
                    if (material.shader == VolumeLaser || material.shader == VolumeLaser_URP)
                    {
                        material.shader = VolumeLaser_HDRP;
                    }
                }
                /*----------------------------------------------------------------------------------------------------*/
                if (Shader.Find("Shader Graphs/HDRP_Explosion") != null)
                {
                    if (material.shader == Explosion || material.shader == Explosion_URP)
                    {
                        material.SetFloat("_StencilRef", 0);
                        material.SetFloat("_AlphaDstBlend", 1);
                        material.SetFloat("_DstBlend", 1);
                        material.SetFloat("_ZWrite", 0);
                        material.SetFloat("_SrcBlend", 1);
                        material.EnableKeyword("_BLENDMODE_ADD _DOUBLESIDED_ON _SURFACE_TYPE_TRANSPARENT");
                        material.SetShaderPassEnabled("TransparentBackface", false);
                        material.SetOverrideTag("RenderType", "Transparent");
                        material.SetFloat("_CullModeForward", 0);
                        material.shader = Explosion_HDRP;
                    }
                }
                /*----------------------------------------------------------------------------------------------------*/
                if (Shader.Find("Shader Graphs/HDRP_SwordSlash") != null)
                {
                    if (material.shader == SwordSlash || material.shader == SwordSlash_URP)
                    {
                        if (material.GetTexture("_MainTexture") != null || material.GetTexture("_EmissionTex") != null
                            || material.GetTexture("_Dissolve") != null)
                        {
                            Vector2 MainScale = material.GetTextureScale("_MainTexture");
                            Vector2 MainOffset = material.GetTextureOffset("_MainTexture");
                            Vector2 EmissionTexScale = material.GetTextureScale("_EmissionTex");
                            Vector2 EmissionTexOffset = material.GetTextureOffset("_EmissionTex");
                            Vector2 DissolveScale = material.GetTextureScale("_Dissolve");
                            Vector2 DissolveOffset = material.GetTextureOffset("_Dissolve");
                            material.SetFloat("_ZWrite", 0);
                            material.SetFloat("_StencilRef", 0);
                            material.SetShaderPassEnabled("TransparentBackface", false);
                            material.SetOverrideTag("RenderType", "Transparent");
                            material.SetFloat("_AlphaDstBlend", 10);
                            material.SetFloat("_DstBlend", 10);
                            material.SetFloat("_SrcBlend", 1);
                            material.EnableKeyword("_BLENDMODE_ALPHA _DOUBLESIDED_ON _SURFACE_TYPE_TRANSPARENT");
                            material.shader = SwordSlash_HDRP;
                            if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0);
                            if (material.HasProperty("_MainTextureTiling"))
                                material.SetVector("_MainTextureTiling", new Vector4(MainScale[0], MainScale[1], MainOffset[0], MainOffset[1]));
                            if (material.HasProperty("_EmissionTexTiling"))
                                material.SetVector("_EmissionTexTiling", new Vector4(EmissionTexScale[0], EmissionTexScale[1], EmissionTexOffset[0], EmissionTexOffset[1]));
                            if (material.HasProperty("_DissolveTiling"))
                                material.SetVector("_DissolveTiling", new Vector4(DissolveScale[0], DissolveScale[1], DissolveOffset[0], DissolveOffset[1]));
                        }
                        else
                            material.shader = SwordSlash_HDRP;
                    }
                }
                /*----------------------------------------------------------------------------------------------------*/
                if (Shader.Find("Shader Graphs/HDRP_ShockWave") != null)
                {
                    if (material.shader == ShockWave || material.shader == ShockWave_URP)
                    {
                        if (material.GetTexture("_MainTexture") != null || material.GetTexture("_Noise") != null
                            || material.GetTexture("_Flow") != null || material.GetTexture("_Mask") != null)
                        {
                            Vector2 MainScale = material.GetTextureScale("_MainTexture");
                            Vector2 MainOffset = material.GetTextureOffset("_MainTexture");
                            Vector2 NoiseScale = material.GetTextureScale("_Noise");
                            Vector2 NoiseOffset = material.GetTextureOffset("_Noise");
                            Vector2 FlowScale = material.GetTextureScale("_Flow");
                            Vector2 FlowOffset = material.GetTextureOffset("_Flow");
                            Vector2 MaskScale = material.GetTextureScale("_Mask");
                            Vector2 MaskOffset = material.GetTextureOffset("_Mask");
                            material.SetFloat("_StencilRef", 0);
                            material.SetFloat("_AlphaDstBlend", 1);
                            material.SetFloat("_DstBlend", 1);
                            material.SetFloat("_ZWrite", 0);
                            material.SetFloat("_SrcBlend", 1);
                            material.EnableKeyword("_BLENDMODE_ADD _DOUBLESIDED_ON _SURFACE_TYPE_TRANSPARENT");
                            material.SetShaderPassEnabled("TransparentBackface", false);
                            material.SetOverrideTag("RenderType", "Transparent");
                            material.SetFloat("_CullModeForward", 0);
                            material.shader = ShockWave_HDRP;
                            if (material.HasProperty("_MainTexTiling"))
                                material.SetVector("_MainTexTiling", new Vector4(MainScale[0], MainScale[1], MainOffset[0], MainOffset[1]));
                            if (material.HasProperty("_NoiseTiling"))
                                material.SetVector("_NoiseTiling", new Vector4(NoiseScale[0], NoiseScale[1], NoiseOffset[0], NoiseOffset[1]));
                            if (material.HasProperty("_FlowTiling"))
                                material.SetVector("_FlowTiling", new Vector4(FlowScale[0], FlowScale[1], FlowOffset[0], FlowOffset[1]));
                            if (material.HasProperty("_MaskTiling"))
                                material.SetVector("_MaskTiling", new Vector4(MaskScale[0], MaskScale[1], MaskOffset[0], MaskOffset[1]));
                        }
                        else
                            material.shader = ShockWave_HDRP;
                    }
                }
                /*----------------------------------------------------------------------------------------------------*/
                if (Shader.Find("Shader Graphs/HDRP_SoftNoise") != null)
                {
                    if (material.shader == SoftNoise || material.shader == SoftNoise_URP)
                    {
                        if (material.GetTexture("_MainTex") != null || material.GetTexture("_Noise") != null
                            || material.GetTexture("_OpacityTex") != null || material.GetTexture("_Mask") != null)
                        {
                            Vector2 MainScale = material.GetTextureScale("_MainTex");
                            Vector2 MainOffset = material.GetTextureOffset("_MainTex");
                            Vector2 NoiseScale = material.GetTextureScale("_Noise");
                            Vector2 NoiseOffset = material.GetTextureOffset("_Noise");
                            Vector2 OpacityTexScale = material.GetTextureScale("_OpacityTex");
                            Vector2 OpacityTexOffset = material.GetTextureOffset("_OpacityTex");
                            Vector2 MaskScale = material.GetTextureScale("_Mask");
                            Vector2 MaskOffset = material.GetTextureOffset("_Mask");
                            material.shader = SoftNoise_HDRP;
                            if (material.HasProperty("_MainTexTiling"))
                                material.SetVector("_MainTexTiling", new Vector4(MainScale[0], MainScale[1], MainOffset[0], MainOffset[1]));
                            if (material.HasProperty("_NoiseTiling"))
                                material.SetVector("_NoiseTiling", new Vector4(NoiseScale[0], NoiseScale[1], NoiseOffset[0], NoiseOffset[1]));
                            if (material.HasProperty("_OpacityTexTiling"))
                                material.SetVector("_OpacityTexTiling", new Vector4(OpacityTexScale[0], OpacityTexScale[1], OpacityTexOffset[0], OpacityTexOffset[1]));
                            if (material.HasProperty("_MaskTiling"))
                                material.SetVector("_MaskTiling", new Vector4(MaskScale[0], MaskScale[1], MaskOffset[0], MaskOffset[1]));
                        }
                        else
                            material.shader = SoftNoise_HDRP;
                    }
                }
            }
        }
    }
}
#endif