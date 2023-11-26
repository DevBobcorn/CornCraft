#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

using CraftSharp.Resource;

namespace CraftSharp.Rendering
{
    public class MaterialManager : MonoBehaviour
    {
        [SerializeField] public Material? AtlasSolid;
        [SerializeField] public Material? AtlasCutout;
        [SerializeField] public Material? AtlasCutoutMipped;
        [SerializeField] public Material? AtlasTranslucent;
        [SerializeField] public Material? AtlasWater;
        [SerializeField] public Material? AtlasInventory;

        [SerializeField] public Material? PlayerSkin;

        private readonly Dictionary<RenderType, Material> atlasMaterials = new();
        private Dictionary<RenderType, Material> foglessAtlasMaterials = new();
        private Material? defaultAtlasMaterial;
        private Dictionary<string, Texture2D> skinTextures = new(); // First assign a place holder...
        private Dictionary<string, Material> skinMaterials = new();
        public Dictionary<string, Material> SkinMaterials => skinMaterials;

        private bool atlasInitialized = false;

        public Material GetAtlasMaterial(RenderType renderType, bool disableFog = false)
        {
            EnsureAtlasInitialized();
            if (disableFog)
            {
                return foglessAtlasMaterials.GetValueOrDefault(renderType, defaultAtlasMaterial!);
            }
            else
            {
                return atlasMaterials.GetValueOrDefault(renderType, defaultAtlasMaterial!);
            }
        }

        public void LoadPlayerSkins()
        {
            Debug.Log("Loading player skin textures...");
            // Clear loaded things...
            skinTextures.Clear();
            skinMaterials.Clear();

            var skinPath = PathHelper.GetPackDirectoryNamed("skins");
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
                        var newSkinMat = new Material(PlayerSkin);
                        newSkinMat.SetTexture("_BaseMap", skin);

                        skinMaterials.Add(nameLower, newSkinMat);

                    }
                    else
                        Debug.LogWarning($"Unexpected skin texture format {skinFile.Extension}, should be png format");
                    
                }
            }
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
            var solid = new Material(AtlasSolid!);
            solid.SetTexture("_BaseMap", packManager.GetAtlasArray(false));
            atlasMaterials.Add(RenderType.SOLID, solid);

            defaultAtlasMaterial = solid;

            // Cutout & Cutout Mipped
            var cutout = new Material(AtlasCutout!);
            cutout.SetTexture("_BaseMap", packManager.GetAtlasArray(false));
            atlasMaterials.Add(RenderType.CUTOUT, cutout);

            var cutoutMipped = new Material(AtlasCutoutMipped!);
            cutoutMipped.SetTexture("_BaseMap", packManager.GetAtlasArray(true));
            atlasMaterials.Add(RenderType.CUTOUT_MIPPED, cutoutMipped);

            // Translucent
            var translucent = new Material(AtlasTranslucent!);
            translucent.SetTexture("_BaseMap", packManager.GetAtlasArray(false));
            atlasMaterials.Add(RenderType.TRANSLUCENT, translucent);

            // Water
            var water = new Material(AtlasWater!);
            water.SetTexture("_BaseMap", packManager.GetAtlasArray(false));
            atlasMaterials.Add(RenderType.WATER, water);

            // Make fogless variants
            foglessAtlasMaterials = atlasMaterials.ToDictionary(x => x.Key, x =>
            {
                // Make a copy of the material
                var m = new Material(x.Value);
                // Set disable fog keyword
                m.EnableKeyword("_DISABLE_FOG");
                return m;
            });

            atlasInitialized = true;
        }
    }
}