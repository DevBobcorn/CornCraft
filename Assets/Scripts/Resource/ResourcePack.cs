using System.IO;
using UnityEngine;

namespace MinecraftClient.Resource
{
    public class ResourcePack
    {
        private bool isValid;
        public bool IsValid { get { return isValid; } }

        private string packName;

        public ResourcePack(string name)
        {
            isValid = false;
            packName = name;

            // Read meta file...
            DirectoryInfo packDir = new DirectoryInfo(PathHelper.GetPackDirectoryNamed(packName));

            if (packDir.Exists)
            {
                string metaPath = packDir + "/pack.mcmeta";
                if (File.Exists(metaPath))
                {
                    string meta = File.ReadAllText(metaPath);
                    Json.JSONData metaData = Json.ParseJson(meta);

                    if (metaData.Properties.ContainsKey("pack"))
                    {
                        Json.JSONData packData = metaData.Properties["pack"];
                        if (packData.Properties.ContainsKey("pack_format"))
                        {
                            string packFormat = packData.Properties["pack_format"].StringValue;
                            Debug.Log($"Pack [{packName}] is valid, with pack format version {packFormat}");
                            isValid = true;
                        }

                        if (packData.Properties.ContainsKey("description"))
                        {
                            string desc = packData.Properties["description"].StringValue;
                            Debug.Log($"Description: {desc}");
                        }

                    }
                }
            }
            else
                Debug.LogWarning($"No resource pack found at {packDir}");

        }

        public void GatherResources(ResourcePackManager manager, LoadStateInfo loadStateInfo)
        {
            if (isValid)
            {
                loadStateInfo.InfoText = $"Gathering resources from {packName}";
                
                // Assets folder...
                var assetsDir = new DirectoryInfo(PathHelper.GetPackDirectoryNamed($"{packName}/assets"));
                if (assetsDir.Exists)
                {
                    // Load textures and models
                    foreach (var nameSpaceDir in assetsDir.GetDirectories())
                    {
                        string nameSpace = nameSpaceDir.Name;

                        // Load and store all texture files...
                        var texturesDir = new DirectoryInfo($"{nameSpaceDir}/textures/");
                        int texDirLen = texturesDir.FullName.Length;

                        if (texturesDir.Exists)
                        {
                            foreach (var texFile in texturesDir.GetFiles("*.png", SearchOption.AllDirectories)) // Allow sub folders...
                            {
                                string texId = texFile.FullName.Replace('\\', '/');
                                texId = texId.Substring(texDirLen); // e.g. 'block/grass_block_top.png'

                                if (texId.StartsWith("effect/") || texId.StartsWith("font/") ||
                                    texId.StartsWith("gui/")    || texId.StartsWith("misc/") ||
                                    texId.StartsWith("mob_effect/") || texId.StartsWith("painting/"))
                                {
                                    // Debug.Log($"Skipping texture {texId}");
                                    continue;
                                }

                                texId = texId.Substring(0, texId.LastIndexOf('.')); // e.g. 'block/grass_block_top'

                                ResourceLocation identifier = new ResourceLocation(nameSpace, texId);
                                if (!manager.TextureFileTable.ContainsKey(identifier))
                                {
                                    // This texture is not provided by previous resource packs, so add it here...
                                    manager.TextureFileTable.Add(identifier, texFile.FullName.Replace('\\', '/'));
                                }
                                else // Overwrite it
                                    manager.TextureFileTable[identifier] = texFile.FullName.Replace('\\', '/');
                            }
                        }

                        // Load and store all model files...
                        var modelsDir = new DirectoryInfo($"{nameSpaceDir}/models/");
                        int modelDirLen = modelsDir.FullName.Length;

                        if (new DirectoryInfo($"{nameSpaceDir}/models/block").Exists)
                        {
                            foreach (var modelFile in modelsDir.GetFiles("block/*.json", SearchOption.AllDirectories)) // Allow sub folders...
                            {
                                string modelId = modelFile.FullName.Replace('\\', '/');
                                modelId = modelId.Substring(modelDirLen); // e.g. 'block/acacia_button.json'
                                modelId = modelId.Substring(0, modelId.LastIndexOf('.')); // e.g. 'block/acacia_button'
                                ResourceLocation identifier = new ResourceLocation(nameSpace, modelId);
                                if (!manager.BlockModelFileTable.ContainsKey(identifier))
                                {
                                    // This model is not provided by previous resource packs, so add it here...
                                    manager.BlockModelFileTable.Add(identifier, modelFile.FullName.Replace('\\', '/'));
                                }
                                else // Overwrite it
                                    manager.BlockModelFileTable[identifier] = modelFile.FullName.Replace('\\', '/');
                            }
                        }

                        if (new DirectoryInfo($"{nameSpaceDir}/models/item").Exists)
                        {
                            foreach (var modelFile in modelsDir.GetFiles("item/*.json", SearchOption.AllDirectories)) // Allow sub folders...
                            {
                                string modelId = modelFile.FullName.Replace('\\', '/');
                                modelId = modelId.Substring(modelDirLen); // e.g. 'item/acacia_boat.json'
                                modelId = modelId.Substring(0, modelId.LastIndexOf('.')); // e.g. 'item/acacia_boat'
                                ResourceLocation identifier = new ResourceLocation(nameSpace, modelId);

                                if (!manager.ItemModelFileTable.ContainsKey(identifier))
                                {
                                    // This model is not provided by previous resource packs, so add it here...
                                    manager.ItemModelFileTable.Add(identifier, modelFile.FullName.Replace('\\', '/'));
                                }
                                else // Overwrite it
                                    manager.ItemModelFileTable[identifier] = modelFile.FullName.Replace('\\', '/');
                            }
                        }

                        // Load and store all blockstate files...
                        var blockstatesDir = new DirectoryInfo($"{nameSpaceDir}/blockstates/");
                        int blockstateDirLen = blockstatesDir.FullName.Length;

                        if (blockstatesDir.Exists)
                        {
                            foreach (var statesFile in blockstatesDir.GetFiles("*.json", SearchOption.TopDirectoryOnly)) // No sub folders...
                            {
                                string blockId = statesFile.FullName.Replace('\\', '/');
                                blockId = blockId.Substring(blockstateDirLen); // e.g. 'grass_block.json'
                                blockId = blockId.Substring(0, blockId.LastIndexOf('.')); // e.g. 'grass_block'
                                ResourceLocation identifier = new ResourceLocation(nameSpace, blockId);
                                if (!manager.BlockStateFileTable.ContainsKey(identifier))
                                {
                                    // This file is not provided by previous resource packs, so add it here...
                                    manager.BlockStateFileTable.Add(identifier, statesFile.FullName.Replace('\\', '/'));
                                }
                                else // Overwrite it
                                    manager.BlockStateFileTable[identifier] = statesFile.FullName.Replace('\\', '/');
                            }
                        }

                    }

                }
                else
                    Debug.LogWarning($"Cannot find path {assetsDir.FullName}");

            }
            else
                Debug.LogWarning("Trying to load resources from an invalid resource pack!");

        }

        
    }

}