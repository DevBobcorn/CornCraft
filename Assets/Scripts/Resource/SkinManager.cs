#nullable enable
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace MinecraftClient.Resource
{
    public static class SkinManager
    {
        private static Dictionary<string, Texture2D> skinTextures = new(); // First assign a place holder...
        private static Dictionary<string, Material> skinMaterials = new();

        public static Dictionary<string, Material> SkinMaterials
        {
            get {
                return skinMaterials;
            }
        }

        public static void Load()
        {
            var defoSkinMat = Resources.Load<Material>("Materials/Entity/Player Default");

            Debug.Log("Loading player skin overrides...");

            // Clear loaded things...
            skinTextures.Clear();
            skinMaterials.Clear();

            var skinPath = PathHelper.GetPacksDirectory() + "/skins";
            var skinDir = new DirectoryInfo(skinPath);

            if (skinDir.Exists)
            {
                var skinFiles = skinDir.GetFiles();

                foreach (var skinFile in skinFiles)
                {
                    if (skinFile.Extension == ".png")
                    {
                        // Take the file base name in lower case as skin owner's name
                        var nameLower = skinFile.Name[..^4].ToLower();
                        Debug.Log($"Loading skin for [{nameLower}]");

                        var skin = new Texture2D(2, 2);

                        skin.LoadImage(File.ReadAllBytes(skinFile.FullName));
                        skin.filterMode = FilterMode.Point;

                        skinTextures.Add(nameLower, skin);

                        // Clone a new skin material
                        var newSkinMat = new Material(defoSkinMat);
                        newSkinMat.SetTexture("_BaseMap", skin);

                        skinMaterials.Add(nameLower, newSkinMat);

                    }
                    else
                        Debug.LogWarning($"Unexpected skin texture format {skinFile.Extension}, should be png format");
                    
                }
            }

        }
    }
}