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
        [SerializeField] private Color m_EntityBaseColor = new(220F / 255F, 220F / 255F, 220F / 255F);
        public Color EntityBaseColor => m_EntityBaseColor;
        [SerializeField] private Material m_EnityDissolveMaterial;
        public Material EnityDissolveMaterial => m_EnityDissolveMaterial;
        [SerializeField] private string m_EnityDissolveMaterialTextureName = "_Texture";
        public string EnityDissolveMaterialTextureName => m_EnityDissolveMaterialTextureName;

        /// <summary>
        /// A material instance is created for each rendertype-texture pair,
        /// and all entities that uses this material share the same instance.
        /// This helps to avoid unnecessary copies of materials and makes
        /// texture updates much easier.
        /// </summary>
        public readonly Dictionary<EntityRenderType, Dictionary<ResourceLocation,
                Material>> EntityMaterials = InitializeTables();

        /// <summary>
        /// Map a material to an instance in the global entity material table.
        /// </summary>
        /// <param name="renderType">Render type of this material</param>
        /// <param name="textureId">Texture identifier</param>
        /// <param name="defaultMaterial">The material template to be used if this material is not yet present in table</param>
        public void ApplyMaterial(EntityRenderType renderType, ResourceLocation textureId, Material defaultMaterial, Action<Material> callback)
        {
            if (!EntityMaterials[renderType].ContainsKey(textureId))
            {
                var resManager = ResourcePackManager.Instance;
                // This entry is not present, instanciate it
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

                    matInstance.SetTexture("_BaseMap", tex);

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
        public void ApplyTextureOrSkin(ResourceLocation textureId, Action<Texture2D> callback)
        {
            var resManager = ResourcePackManager.Instance;
            
            if (textureId.Namespace == "skin")
            {
                StartCoroutine(ApplyPlayerSkin(Guid.Parse(textureId.Path), callback));
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
                                playerUUID2TextureInfo.Add(playerUUID, req);
                                
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

        public IEnumerator ApplyPlayerSkin(Guid playerUUID, Action<Texture2D> callback)
        {
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
            else if (!playerUUID2TextureInfo.ContainsKey(playerUUID))
            {
                texInfo = new PlayerTextureInfo();
                yield return RequestPlayerSkinInfo(playerUUID, texInfo);
            }
            else
            {
                texInfo = playerUUID2TextureInfo[playerUUID];
            }

            if (!texInfo.Failed)
            {
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