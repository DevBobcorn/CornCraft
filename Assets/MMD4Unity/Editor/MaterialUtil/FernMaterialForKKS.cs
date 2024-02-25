using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MMD
{
    /// <summary>
    /// Editor utility for setting up materials for KKS models exported
    /// via KKBP (https://github.com/FlailingFog/KK-Blender-Porter-Pack)
    /// </summary>
    public static class FernMaterialForKKS
    {
        public static readonly string[] BAKED_TEXTURE_SUFFIXES = { "light", "dark" };

        public static void DrawGUI(string prefabPath, ref int selectedSuffix, Dictionary<FernMaterialCategory, List<Material>> targetMaterials)
        {
            GUI.enabled = false;
            GUILayout.Label($"Target texture path: {prefabPath}");
            GUI.enabled = true;

            GUILayout.Label("Base Textures", EditorStyles.boldLabel);

            EditorGUILayout.Popup("Baked Texture Type:", selectedSuffix, BAKED_TEXTURE_SUFFIXES);

            GUILayout.BeginHorizontal();
                if (GUILayout.Button("Find and Assign Base Textures"))
                {
                    var allMaterials = targetMaterials.SelectMany(x => x.Value).ToList();
                    AssignBaseTextures(prefabPath, selectedSuffix, allMaterials);
                }

                if (GUILayout.Button("Clear Base Textures"))
                {
                    var allMaterials = targetMaterials.SelectMany(x => x.Value).ToList();
                    ClearBaseTextures(allMaterials);
                }

                if (GUILayout.Button("Reset Base Colors"))
                {
                    var allMaterials = targetMaterials.SelectMany(x => x.Value).ToList();
                    ResetBaseColors(allMaterials);
                }
            GUILayout.EndHorizontal();

            GUILayout.Label("Normal Textures", EditorStyles.boldLabel);

            GUILayout.BeginHorizontal();
                if (GUILayout.Button("Find and Assign Normal Textures"))
                {
                    var allMaterials = targetMaterials.SelectMany(x => x.Value).ToList();
                    AssignNormalTextures(prefabPath, allMaterials);
                }

                if (GUILayout.Button("Clear Normal Textures"))
                {
                    var allMaterials = targetMaterials.SelectMany(x => x.Value).ToList();
                    ClearNormalTextures(allMaterials);
                }
            GUILayout.EndHorizontal();

            GUILayout.Label("Render Types", EditorStyles.boldLabel);

            GUILayout.BeginHorizontal();
                if (GUILayout.Button("Guess and Assign Render Types"))
                {
                    var allMaterials = targetMaterials.SelectMany(x => x.Value).ToList();
                    AssignRenderTypes(allMaterials);
                }
            GUILayout.EndHorizontal();
        }

        private static void AssignBaseTextures(string prefabPath, int selectedSuffix, List<Material> materials)
        {
            foreach (var material in materials)
            {
                var texFilePath = $"{prefabPath}/{material.name} {BAKED_TEXTURE_SUFFIXES[selectedSuffix]}.png";
                if (File.Exists(texFilePath))
                {
                    var tex = (Texture2D) AssetDatabase.LoadAssetAtPath(texFilePath, typeof (Texture2D));
                    material.SetTexture("_BaseMap", tex);
                }
                else
                {
                    Debug.LogWarning($"Base texture for [{material.name}] not found!");
                }
            }
        }

        private static void ClearBaseTextures(List<Material> materials)
        {
            foreach (var material in materials)
            {
                material.SetTexture("_BaseMap", null);
            }
        }

        private static void ResetBaseColors(List<Material> materials)
        {
            foreach (var material in materials)
            {
                material.SetColor("_BaseColor", Color.white);
            }
        }

        private static void AssignNormalTextures(string prefabPath, List<Material> materials)
        {
            foreach (var material in materials)
            {
                var texFilePath = $"{prefabPath}/{material.name} normal.png";
                if (File.Exists(texFilePath))
                {
                    material.SetFloat("_BumpMapKeyword", 1F);
                    material.EnableKeyword("_NORMALMAP"); // First make sure the keyword is enabled

                    // Mark this texture as normal map
                    // https://forum.unity.com/threads/how-to-use-textureimporter-to-change-textures-format-and-re-import-again.86177/
                    var importer = (TextureImporter) TextureImporter.GetAtPath(texFilePath);
                    importer.textureType = TextureImporterType.NormalMap;
                    importer.SaveAndReimport();

                    var tex = (Texture2D) AssetDatabase.LoadAssetAtPath(texFilePath, typeof (Texture2D));
                    
                    material.SetTexture("_BumpMap", tex);
                }
                else
                {
                    Debug.LogWarning($"Normal texture for [{material.name}] not found!");
                }
            }
        }

        private static void ClearNormalTextures(List<Material> materials)
        {
            foreach (var material in materials)
            {
                material.SetFloat("_BumpMapKeyword", 0F);
                material.DisableKeyword("_NORMALMAP"); // First disable the keyword
                
                material.SetTexture("_BumpMap", null);
            }
        }
    
        private static void AssignRenderTypes(List<Material> materials)
        {
            foreach (var material in materials)
            {
                var renderType = FernMaterialUtilFunctions.GuessRenderType(material.name);

                // Use cutout by default for clothes and accessories
                // TODO: Guess the render types for these entries
                if (renderType == FernMaterialRenderType.Unknown)
                {
                    renderType = FernMaterialRenderType.Cutout;
                }

                FernMaterialUtilFunctions.SetRenderType(material, renderType);
            }
        }
    }
}