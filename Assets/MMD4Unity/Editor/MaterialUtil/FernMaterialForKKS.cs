using System.Collections;
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
        public static readonly string[] BAKED_TEXTURE_SUFFIXES = { "light", "dark", "normal" };

        public static void DrawGUI(string prefabPath, ref int selectedSuffix, Dictionary<FernMaterialCategory, List<Material>> targetMaterials)
        {
            GUI.enabled = false;
            GUILayout.Label($"Target texture path: {prefabPath}");
            GUI.enabled = true;

            EditorGUILayout.Popup("Baked Texture Type:", selectedSuffix, BAKED_TEXTURE_SUFFIXES);

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Auto-Assign Base Textures"))
            {
                var allMaterials = targetMaterials.SelectMany(x => x.Value);
                AssignBaseTextures(prefabPath, selectedSuffix, allMaterials);
            }

            if (GUILayout.Button("Clear Base Textures"))
            {
                var allMaterials = targetMaterials.SelectMany(x => x.Value);
                ClearBaseTextures(allMaterials);
            }

            if (GUILayout.Button("Reset Base Colors"))
            {
                var allMaterials = targetMaterials.SelectMany(x => x.Value);
                ClearBaseTextures(allMaterials);
            }

            GUILayout.EndHorizontal();
        }

        private static void AssignBaseTextures(string prefabPath, int selectedSuffix, IEnumerable<Material> materials)
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
                    Debug.LogWarning($"Texture for [{material.name}] not found!");
                }
            }
        }

        private static void ClearBaseTextures(IEnumerable<Material> materials)
        {
            foreach (var material in materials)
            {
                material.SetTexture("_BaseMap", null);
            }
        }

        private static void ResetBaseColors(IEnumerable<Material> materials)
        {
            foreach (var material in materials)
            {
                material.SetColor("_BaseColor", Color.white);
            }
        }
    }
}