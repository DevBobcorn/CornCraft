using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

using CraftSharp.Resource;
using CraftSharp.Resource.BedrockEntity;

namespace CraftSharp.Rendering
{
    public class EntityMaterialManager : MonoBehaviour
    {
        private const string BEDROCK_ENTITY_NAMESPACE = "bedrock_entity";
        private const string SKIN_NAMESPACE = "skin";
        private static readonly int BASE_MAP = Shader.PropertyToID("_BaseMap");
        
        [SerializeField] private Color m_EntityBaseColor = new(220F / 255F, 220F / 255F, 220F / 255F);
        public Color EntityBaseColor => m_EntityBaseColor;
        [SerializeField] private Material m_EntityDissolveMaterial;
        public Material EntityDissolveMaterial => m_EntityDissolveMaterial;
        [SerializeField] private string m_EntityDissolveMaterialTextureName = "_Texture";
        public string EntityDissolveMaterialTextureName => m_EntityDissolveMaterialTextureName;
        [SerializeField] private string m_EntityDissolveMaterialColorName = "_Colour";
        public string EntityDissolveMaterialColorName => m_EntityDissolveMaterialColorName;

        public Material BedrockEntitySolid;
        public Material BedrockEntityCutout;
        public Material BedrockEntityCutoutDoubleSided;
        public Material BedrockEntityTranslucent;

        /// <summary>
        /// A material instance is created for each rendertype-texture pair,
        /// and all entities that uses this material share the same instance.
        /// This helps to avoid unnecessary copies of materials and makes
        /// texture updates much easier.
        /// </summary>
        private readonly Dictionary<(EntityRenderType, ResourceLocation), Material> CachedEntityMaterials = new();
        
        public readonly Dictionary<ResourceLocation, bool> SkinModels = new();
        
        private readonly Dictionary<int, Texture2D> CachedBannerTextures = new();

        /// <summary>
        /// Map a material to an instance in the global entity material table.
        /// </summary>
        /// <param name="renderType">Render type of this material</param>
        /// <param name="textureId">Texture identifier</param>
        /// <param name="defaultMaterial">The material template to be used if this material is not yet present in table</param>
        /// <param name="callback">Callback to be invoked after applying the material</param>
        /// <param name="expectedWidth">Expected texture width, used for Bedrock entity model textures</param>
        /// <param name="expectedHeight">Expected texture height, used for Bedrock entity model textures</param>
        public void ApplyMaterial(EntityRenderType renderType, ResourceLocation textureId, Material defaultMaterial,
            Action<Material> callback, int expectedWidth = 0, int expectedHeight = 0)
        {
            var key = (renderType, textureId);
            
            if (!CachedEntityMaterials.TryGetValue(key, out var material))
            {
                // This entry is not present, instantiate it
                //Debug.Log($"Creating entity material {textureId} ({renderType})");

                ApplyTextureOrSkin(textureId, tex =>
                {
                    material = new Material(defaultMaterial)
                    {
                        // Read and apply textures from ResourcePackManager
                        mainTexture = tex,
                        name = $"Material {textureId} ({renderType})",
                        color = EntityBaseColor
                    };

                    material.SetTexture(BASE_MAP, tex);

                    CachedEntityMaterials.Add(key, material);
                    callback.Invoke(material);
                }, expectedWidth, expectedHeight);
            }
            else
            {
                callback.Invoke(material);
            }
        }

        /// <summary>
        /// Map a material to an instance in the global entity material table.
        /// </summary>
        /// <param name="renderType">Render type of this material</param>
        /// <param name="textureName">Texture name</param>
        /// <param name="callback">Callback to be invoked after applying the material</param>
        /// <param name="expectedWidth">Texture width defined in Bedrock geometry file</param>
        /// <param name="expectedHeight">Texture height defined in Bedrock geometry file</param>
        public void ApplyBedrockMaterial(EntityRenderType renderType, string textureName,
            Action<Material> callback, int expectedWidth, int expectedHeight)
        {
            ApplyMaterial(renderType, new(BEDROCK_ENTITY_NAMESPACE, textureName),
                GetBedrockEntityMaterialTemplate(renderType), callback, expectedWidth, expectedHeight);
        }

        /// <summary>
        /// Get a texture with given id, or load it if not present.
        /// </summary>
        /// <param name="textureId">Texture identifier</param>
        /// <param name="callback">Callback to be invoked after applying the texture</param>
        /// <param name="expectedWidth">Expected texture width, used for Bedrock entity model textures</param>
        /// <param name="expectedHeight">Expected texture height, used for Bedrock entity model textures</param>
        public void ApplyTextureOrSkin(ResourceLocation textureId, Action<Texture2D> callback,
            int expectedWidth = 0, int expectedHeight = 0)
        {
            var resManager = ResourcePackManager.Instance;
            
            if (textureId.Namespace == SKIN_NAMESPACE)
            {
                StartCoroutine(ApplyPlayerSkin(textureId, callback));
            }
            else if (textureId.Namespace == BEDROCK_ENTITY_NAMESPACE)
            {
                var tex = BedrockEntityResourceManager.Instance
                    .LoadBedrockEntityTexture(expectedWidth, expectedHeight, textureId.Path);
                callback.Invoke(tex);
            }
            else
            {
                var tex = resManager.GetEntityTextureFromTable(textureId);
                callback.Invoke(tex);
            }
        }

        private static Texture2D ColorizeTexture(Texture2D sourceTexture, Color color)
        {
            // Create a new texture to preserve the original
            var coloredTexture = new Texture2D(sourceTexture.width, sourceTexture.height, sourceTexture.format, false);
        
            // Get all pixels from the source texture
            Color[] pixels = sourceTexture.GetPixels();
        
            // Apply color tint to each pixel
            for (int i = 0; i < pixels.Length; i++)
            {
                // Multiply the color values (preserves alpha)
                pixels[i] = new Color(
                    pixels[i].r * color.r,
                    pixels[i].g * color.g,
                    pixels[i].b * color.b,
                    1F
                );
            }
        
            // Apply the modified pixels to the new texture
            coloredTexture.SetPixels(pixels);
            coloredTexture.Apply();
        
            return coloredTexture;
        }
        
        private static Texture2D ColorizeTextureAndStackToBase(Texture2D sourceTexture, Texture2D baseTexture, Color color)
        {
            // Create a new texture to preserve the original
            var coloredTexture = new Texture2D(sourceTexture.width, sourceTexture.height, sourceTexture.format, false);
        
            // Get all pixels from the source texture
            Color[] pixels = sourceTexture.GetPixels();
            Color[] basePixels = baseTexture.GetPixels();

            // Apply color tint to each pixel
            for (int i = 0; i < pixels.Length; i++)
            {
                var colorizedPixel = new Color(
                    pixels[i].r * color.r,
                    pixels[i].g * color.g,
                    pixels[i].b * color.b,
                    1F
                );
                // Multiply the color values (preserves alpha)
                pixels[i] = Color32.Lerp(basePixels[i], colorizedPixel, pixels[i].a);
            }
        
            // Apply the modified pixels to the new texture
            coloredTexture.SetPixels(pixels);
            coloredTexture.Apply();
        
            return coloredTexture;
        }
        
        public static Sprite CreateSpriteFromTexturePart(
            Texture2D sourceTexture, 
            int x, 
            int y, 
            int width, 
            int height,
            Vector2? optionalPivot = null,
            float pixelsPerUnit = 100.0f
        ) {
            // Validate bounds
            if (x < 0 || y < 0 || width <= 0 || height <= 0 || 
                x + width > sourceTexture.width || 
                y + height > sourceTexture.height)
            {
                Debug.LogError("Region exceeds texture bounds.");
                return null;
            }

            // Convert top-left y to bottom-left origin
            float rectY = sourceTexture.height - (y + height);

            // Define the texture region
            Rect spriteRect = new Rect(x, rectY, width, height);
        
            // Use provided pivot or default to center
            Vector2 pivot = optionalPivot ?? new Vector2(0.5f, 0.5f);
        
            // Create the sprite
            return Sprite.Create(
                sourceTexture, 
                spriteRect, 
                pivot, 
                pixelsPerUnit
            );
        }

        public void ApplyBannerTexture(BannerPatternSequence patterns, Action<Texture2D> callback)
        {
            int patternsHash = patterns.GetHashCode();

            if (CachedBannerTextures.TryGetValue(patternsHash, out var bannerTexture))
            {
                callback.Invoke(bannerTexture);

                return;
            }
            
            // Patterns not cached, generate it
            var resManager = ResourcePackManager.Instance;

            int step = 0, stepPatternsHash = 0;
            // Base texture with longest common subsequence which can be used
            // as a base for creating texture for current pattern sequence
            Texture2D baseTexture = null;
            
            while (step < patterns.records.Length)
            {
                var patternRecord = patterns.records[step];
                var newBaseHash = HashCode.Combine(patternRecord, stepPatternsHash);
                
                if (CachedBannerTextures.TryGetValue(newBaseHash, out var newBaseTexture))
                {
                    baseTexture = newBaseTexture;
                    stepPatternsHash = newBaseHash;
                    step++;
                }
                else
                {
                    break;
                }
            }

            for (; step < patterns.records.Length; step++)
            {
                var patternRecord = patterns.records[step];
                var patternTexture = resManager.GetEntityTextureFromTable(
                    new(patternRecord.Type.Namespace, $"entity/banner/{patternRecord.Type.Path}"));
                var patternColor = patternRecord.Color.GetColor32();
                
                stepPatternsHash = HashCode.Combine(patternRecord, stepPatternsHash);

                var generatedTexture = baseTexture == null ?
                    // Use colorized pattern texture
                    ColorizeTexture(patternTexture, patternColor) :
                    // Stack colorized pattern texture onto base texture
                    ColorizeTextureAndStackToBase(patternTexture, baseTexture, patternColor);

                generatedTexture.filterMode = FilterMode.Point;
                
                // Cache generated texture
                CachedBannerTextures[stepPatternsHash] = generatedTexture;
                
                //Debug.Log($"Generating texture for pattern sequence... Step {step}/{patterns.records.Length} Hash: {stepPatternsHash}");
                
                // Use current texture as base in next iteration
                baseTexture = generatedTexture;
            }
            
            // Final iteration gives us the final texture
            callback.Invoke(baseTexture);
            
            //Debug.Log($"Generated texture for pattern sequence {patterns}. Actual Hash: {stepPatternsHash}");
        }
        
        /// <summary>
        /// Only kept for current session
        /// </summary>
        private readonly Dictionary<Guid, PlayerTextureInfo> playerUUID2TextureInfo = new();

        private class PlayerTextureInfo
        {
            public bool Failed = false;
            public string SkinUrl = string.Empty;
            public bool SlimModel = false;
        }

        private IEnumerator RequestPlayerSkinInfo(Guid playerUUID, PlayerTextureInfo req)
        {
            var uri = $"https://sessionserver.mojang.com/session/minecraft/profile/{playerUUID:N}";
            Debug.Log($"Requesting {uri}");

            using UnityWebRequest webRequest = UnityWebRequest.Get(uri);
            yield return webRequest.SendWebRequest();

            switch (webRequest.result)
            {
                case UnityWebRequest.Result.ConnectionError:
                case UnityWebRequest.Result.DataProcessingError:
                case UnityWebRequest.Result.ProtocolError:
                    Debug.LogWarning("Failed to get player skin hash: " + webRequest.error);
                    req.Failed = true;
                    break;
                case UnityWebRequest.Result.Success:
                    try
                    {
                        var resultJson = Json.ParseJson(webRequest.downloadHandler.text);
                        var props = resultJson.Properties["properties"].DataArray;
                        var found = false;
                        foreach (var prop in props)
                        {
                            if (prop.Properties["name"].StringValue == "textures")
                            {
                                var nestedTextB64 = prop.Properties["value"].StringValue;
                                var nestedTextBytes = Convert.FromBase64String(nestedTextB64);
                                var nestedJson = Json.ParseJson(Encoding.UTF8.GetString(nestedTextBytes));

                                var skinEntry = nestedJson.Properties["textures"].Properties["SKIN"];
                                req.SkinUrl = skinEntry.Properties["url"].StringValue;

                                found = true;
                                playerUUID2TextureInfo[playerUUID] = req;
                                
                                if (skinEntry.Properties.TryGetValue("metadata", out Json.JSONData metaEntry))
                                {
                                    if (metaEntry.Properties.TryGetValue("model", out
                                            Json.JSONData modelType) && modelType.StringValue == "slim")
                                    {
                                        req.SlimModel = true;
                                    }
                                }

                                break;
                            }
                        }
                        if (!found)
                        {
                            Debug.LogWarning("Failed to get player skin hash: player textures not defined!");
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning("Failed to parse player skin hash: " + e);
                        //Debug.Log(webRequest.downloadHandler.text);
                        req.Failed = true;
                    }
                    break;
            }
        }

        private static IEnumerator DownloadPlayerSkin(string skinUrl, string cachePath)
        {
            if (skinUrl.StartsWith("http:"))
            {
                skinUrl = "https:" + skinUrl[5..];
            }

            Debug.Log($"Downloading {skinUrl}");

            using UnityWebRequest webRequest = UnityWebRequest.Get(skinUrl);
            yield return webRequest.SendWebRequest();

            switch (webRequest.result)
            {
                case UnityWebRequest.Result.ConnectionError:
                case UnityWebRequest.Result.DataProcessingError:
                case UnityWebRequest.Result.ProtocolError:
                    Debug.LogWarning("Failed to download skin texture: " + webRequest.error);
                    break;
                case UnityWebRequest.Result.Success:
                    File.WriteAllBytes(cachePath, webRequest.downloadHandler.data);
                    break;
            }
        }

        private IEnumerator ApplyPlayerSkin(ResourceLocation textureId, Action<Texture2D> callback)
        {
            var playerUUID = Guid.Parse(textureId.Path);
            var cachePath = PathHelper.GetRootDirectory() + Path.DirectorySeparatorChar + "Cached" + Path.DirectorySeparatorChar + "skins";

            if (!Directory.Exists(cachePath))
            {
                Directory.CreateDirectory(cachePath);
            }

            PlayerTextureInfo texInfo;

            if (playerUUID == Guid.Empty)
            {
                yield break;
                // Don't use callback
            }
            
            if (!playerUUID2TextureInfo.TryGetValue(playerUUID, out var info))
            {
                texInfo = new PlayerTextureInfo();
                yield return RequestPlayerSkinInfo(playerUUID, texInfo);
            }
            else
            {
                texInfo = info;
            }

            if (!texInfo.Failed)
            {
                SkinModels[textureId] = texInfo.SlimModel;

                var skinFileName = texInfo.SkinUrl[texInfo.SkinUrl.LastIndexOf('/')..] + ".png";
                var skinPath = cachePath + Path.DirectorySeparatorChar + skinFileName;

                if (!File.Exists(skinPath))
                {
                    yield return DownloadPlayerSkin(texInfo.SkinUrl, skinPath);
                }

                if (File.Exists(skinPath))
                {
                    var tex = new Texture2D(2, 2) { filterMode = FilterMode.Point };
                    tex.LoadImage(File.ReadAllBytes(skinPath));

                    if (tex.height < tex.width) // Classic single layer skin, convert it to double layer format
                    {
                        // Double the height
                        var convertedTex = new Texture2D(tex.width, tex.height << 1) { filterMode = FilterMode.Point };

                        var armWidth = tex.width * (texInfo.SlimModel ? 3 : 4) / 64; // 3px or 4px
                        var legWidth = tex.width * 4 / 64;      // 4px
                        var limbThickness = tex.width * 4 / 64; // 4px
                        var limbHeight = tex.width * 12 / 64;   // 12px

                        var armSrcX = (tex.width >> 1) + (tex.width >> 3);
                        
                        void copyMirrored(int srcX, int srcY, int dstX, int dstY, int width, int height)
                        {
                            for (int x = 0; x < width; x++)
                            {
                                for (int y = 0; y < height; y++)
                                {
                                    convertedTex.SetPixel(dstX + x, dstY + y, tex.GetPixel(srcX + width - 1 - x, srcY + y));
                                }
                            }
                        };

                        // Copy original parts and fill expanded area with transparency
                        for (int y = 0; y < tex.height; y++)
                        {
                            for (int x = 0; x < tex.width; x++)
                            {
                                convertedTex.SetPixel(x, y, Color.clear);

                                if (x >= (tex.width >> 1) && y >= (tex.height >> 1)) // Discard outer layer for player head since it's a single layer skin
                                {
                                    convertedTex.SetPixel(x, y + tex.height, Color.clear);
                                }
                                else
                                {
                                    convertedTex.SetPixel(x, y + tex.height, tex.GetPixel(x, y));
                                }
                            }
                        }

                        // Copy left leg to right leg
                        copyMirrored(0, 0, tex.width >> 2, 0, limbThickness + legWidth + limbThickness, limbHeight); // Left + Front + Right
                        copyMirrored(limbThickness, limbHeight, (tex.width >> 2) + limbThickness, limbHeight, legWidth, limbThickness); // Top
                        copyMirrored(limbThickness + legWidth, limbHeight, (tex.width >> 2) + limbThickness + legWidth, limbHeight, legWidth, limbThickness); // Bottom
                        copyMirrored(limbThickness + legWidth + limbThickness, 0, (tex.width >> 2) + limbThickness + legWidth + limbThickness, 0, legWidth, limbHeight); // Back

                        // Copy left arm to right arm
                        copyMirrored(armSrcX, 0, tex.width >> 1, 0, limbThickness + armWidth + limbThickness, limbHeight); // Left + Front + Right
                        copyMirrored(armSrcX + limbThickness, limbHeight, (tex.width >> 1) + limbThickness, limbHeight, armWidth, limbThickness); // Top
                        copyMirrored(armSrcX + limbThickness + armWidth, limbHeight, (tex.width >> 1) + limbThickness + armWidth, limbHeight, armWidth, limbThickness); // Bottom
                        copyMirrored(armSrcX + limbThickness + armWidth + limbThickness, 0, (tex.width >> 1) + limbThickness + armWidth + limbThickness, 0, armWidth, limbHeight); // Back

                        // Apply changes to the new texture
                        convertedTex.Apply();

                        tex = convertedTex;
                    }

                    callback.Invoke(tex);
                }
                else
                {
                    Debug.LogWarning($"Player skin [{texInfo.SkinUrl}] is not present after an download attempt.");
                    // Don't use callback
                }
            }
            else
            {
                Debug.LogWarning($"Failed to get player texture info for {playerUUID}!");
                // Don't use callback
            }
        }

        private Material GetBedrockEntityMaterialTemplate(EntityRenderType renderType)
        {
            return renderType switch
            {
                EntityRenderType.SOLID          => BedrockEntitySolid,
                EntityRenderType.CUTOUT         => BedrockEntityCutout,
                EntityRenderType.CUTOUT_CULLOFF => BedrockEntityCutoutDoubleSided,
                EntityRenderType.TRANSLUCENT    => BedrockEntityTranslucent,

                EntityRenderType.SOLID_EMISSIVE          => BedrockEntitySolid,
                EntityRenderType.CUTOUT_EMISSIVE         => BedrockEntityCutout,
                EntityRenderType.CUTOUT_CULLOFF_EMISSIVE => BedrockEntityCutoutDoubleSided,
                EntityRenderType.TRANSLUCENT_EMISSIVE    => BedrockEntityTranslucent,

                _ =>                            BedrockEntitySolid
            };
        }

        public void ClearTables()
        {
            playerUUID2TextureInfo.Clear();
            CachedEntityMaterials.Clear();
        }
    }
}