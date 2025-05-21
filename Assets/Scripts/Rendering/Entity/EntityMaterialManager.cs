using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

using CraftSharp.Resource;

namespace CraftSharp.Rendering
{
    public class EntityMaterialManager : MonoBehaviour
    {
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
        public readonly Dictionary<EntityRenderType, Dictionary<ResourceLocation,
                Material>> EntityMaterials = InitializeTables();
        
        public readonly Dictionary<ResourceLocation, bool> SkinModels = new();

        /// <summary>
        /// Map a material to an instance in the global entity material table.
        /// </summary>
        /// <param name="renderType">Render type of this material</param>
        /// <param name="textureId">Texture identifier</param>
        /// <param name="defaultMaterial">The material template to be used if this material is not yet present in table</param>
        /// <param name="callback">Callback to be invoked after applying the material</param>
        public void ApplyMaterial(EntityRenderType renderType, ResourceLocation textureId, Material defaultMaterial, Action<Material> callback)
        {
            if (!EntityMaterials[renderType].ContainsKey(textureId))
            {
                // This entry is not present, instantiate it
                //Debug.Log($"Creating entity material {textureId} ({renderType})");

                ApplyTextureOrSkin(textureId, tex =>
                {
                    var matInstance = new Material(defaultMaterial)
                    {
                        // Read and apply textures from ResourcePackManager
                        mainTexture = tex,
                        name = $"Material {textureId} ({renderType})",
                        color = EntityBaseColor
                    };

                    matInstance.SetTexture(BASE_MAP, tex);

                    EntityMaterials[renderType].Add(textureId, matInstance);
                    callback.Invoke(matInstance);
                });
            }
            else
            {
                callback.Invoke(EntityMaterials[renderType][textureId]);
            }
        }

        /// <summary>
        /// Get a texture with given id, or load it if not present.
        /// </summary>
        /// <param name="textureId">Texture identifier</param>
        /// <param name="callback">Callback to be invoked after applying the texture</param>
        public void ApplyTextureOrSkin(ResourceLocation textureId, Action<Texture2D> callback)
        {
            var resManager = ResourcePackManager.Instance;
            
            if (textureId.Namespace == "skin")
            {
                StartCoroutine(ApplyPlayerSkin(textureId, callback));
            }
            else
            {
                var tex = resManager.GetEntityTextureFromTable(textureId);
                callback.Invoke(tex);
            }
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

        private IEnumerator DownloadPlayerSkin(string skinUrl, string cachePath)
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

        public IEnumerator ApplyPlayerSkin(ResourceLocation textureId, Action<Texture2D> callback)
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

        public Material GetBedrockEntityMaterialTemplate(EntityRenderType renderType)
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