using System.Collections;
using System.IO;
using UnityEngine;

using MinecraftClient.Mapping;
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
                            Debug.Log("Pack " + packName + " is valid, with pack format version " + packFormat);
                            isValid = true;
                        }

                        if (packData.Properties.ContainsKey("description"))
                        {
                            string desc = packData.Properties["description"].StringValue;
                            Debug.Log("Description: " + desc);
                        }

                    }
                }
            }
            else
            {
                Debug.LogWarning("No resource pack found at " + packDir);
            }

        }

        public IEnumerator LoadResources(ResourcePackManager manager, LoadStateInfo loadStateInfo)
        {
            if (isValid)
            {
                // Assets folder...
                DirectoryInfo assetsDir = new DirectoryInfo(PathHelper.GetPackDirectoryNamed(packName) + "/assets");
                if (assetsDir.Exists)
                {
                    // Load textures and models
                    foreach (var nameSpaceDir in assetsDir.GetDirectories())
                    {
                        int count = 0;

                        string nameSpace = nameSpaceDir.Name;

                        // Load and store all texture files...
                        DirectoryInfo texturesDir = new DirectoryInfo(nameSpaceDir + "/textures/");
                        int texDirLen = texturesDir.FullName.Length;
                        // TODO Allow other textures instead of block textures only...
                        foreach (var texFile in texturesDir.GetFiles("block/*.png", SearchOption.AllDirectories)) // Allow sub folders...
                        {
                            string texId = texFile.FullName.Replace('\\', '/');
                            texId = texId.Substring(texDirLen); // e.g. 'block/grass_block_top.png'
                            texId = texId.Substring(0, texId.LastIndexOf('.')); // e.g. 'block/grass_block_top'
                            ResourceLocation identifier = new ResourceLocation(nameSpace, texId);
                            if (!manager.textureTable.ContainsKey(identifier))
                            {
                                // This texture is not provided by previous resource packs, so add it here...
                                manager.textureTable.Add(identifier, texFile.FullName.Replace('\\', '/'));
                            }
                            count++;
                            if (count % 100 == 0)
                            {
                                loadStateInfo.infoText = $"Loading texture {identifier}";
                                yield return null;
                            }
                        }

                        // Load and store all model files...
                        DirectoryInfo modelsDir = new DirectoryInfo(nameSpaceDir + "/models/");
                        int modelDirLen = modelsDir.FullName.Length;

                        // TODO Allow other models instead of block models only...
                        foreach (var modelFile in modelsDir.GetFiles("block/*.json", SearchOption.AllDirectories)) // Allow sub folders...
                        {
                            string modelId = modelFile.FullName.Replace('\\', '/');
                            modelId = modelId.Substring(modelDirLen); // e.g. 'block/acacia_button.json'
                            modelId = modelId.Substring(0, modelId.LastIndexOf('.')); // e.g. 'block/acacia_button'
                            ResourceLocation identifier = new ResourceLocation(nameSpace, modelId);
                            // This model loader will load this model, its parent model(if not yet loaded),
                            // and then add them to the manager's model dictionary
                            manager.blockModelLoader.LoadBlockModel(identifier, assetsDir.FullName.Replace('\\', '/'));
                            count++;
                            if (count % 5 == 0)
                            {
                                loadStateInfo.infoText =  $"Loading block model {identifier}";
                                yield return null;
                            }
                        }

                    }

                }
                else
                {
                    Debug.LogWarning("Cannot find path " + assetsDir.FullName);
                }

            }
            else
            {
                Debug.LogWarning("Trying to load resources from an invalid resource pack!");
            }
        }

        public IEnumerator BuildStateGeometries(ResourcePackManager manager, LoadStateInfo loadStateInfo)
        {
            // Load all blockstate files, make and assign their block meshes...
            if (Block.Palette is not null && isValid)
            {
                // Assets folder...
                DirectoryInfo assetsDir = new DirectoryInfo(PathHelper.GetPackDirectoryNamed(packName) + "/assets");
                if (assetsDir.Exists)
                {
                    int count = 0;
                    foreach (var blockPair in Block.Palette.StateListTable)
                    {
                        var blockId = blockPair.Key;
                        bool shouldLoad = false;
                        foreach (var stateId in blockPair.Value)
                        {
                            if (!manager.finalTable.ContainsKey(stateId))
                                shouldLoad = true;
                        }
                        
                        // All states are loaded (from previous packs), just skip...
                        if (!shouldLoad) continue;

                        // Load the state model definition of this block
                        string statePath = assetsDir.FullName + '/' + blockId.nameSpace + "/blockstates/" + blockId.path + ".json";
                        manager.stateModelLoader.LoadBlockStateModel(manager, blockId, statePath);
                        count++;
                        if (count % 10 == 0)
                        {
                            loadStateInfo.infoText = $"Building model for block {blockId}";
                            yield return null;
                        }
                    }

                }

            }
            else
            {
                Debug.LogWarning("Block state list not loaded, or resource pack invalid!");
            }

        }
    }

}