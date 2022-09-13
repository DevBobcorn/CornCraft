using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using MinecraftClient.Mapping;

namespace MinecraftClient.Resource
{
    public class ResourcePackManager
    {
        // Identifier -> Texture path
        public readonly Dictionary<ResourceLocation, string> textureTable = new Dictionary<ResourceLocation, string>();

        // Identifier -> Block model
        public readonly Dictionary<ResourceLocation, BlockModel> modelsTable = new Dictionary<ResourceLocation, BlockModel>();

        // Block state numeral id -> Block state geometries (One single block state may have a list of models to use randomly)
        public readonly Dictionary<int, BlockStateModel> finalTable = new Dictionary<int, BlockStateModel>();

        public readonly BlockModelLoader blockModelLoader;
        public readonly BlockStateModelLoader stateModelLoader;

        public readonly List<ResourcePack> packs = new List<ResourcePack>();

        public ResourcePackManager()
        {
            blockModelLoader = new BlockModelLoader(this);
            stateModelLoader = new BlockStateModelLoader(this);
        }

        public void AddPack(ResourcePack pack)
        {
            packs.Add(pack);
        }

        public void ClearPacks()
        {
            packs.Clear();
            textureTable.Clear();
            modelsTable.Clear();
            finalTable.Clear();
        }

        public IEnumerator LoadPacks(CoroutineFlag flag, LoadStateInfo loadStateInfo)
        {
            float startTime = Time.realtimeSinceStartup;

            foreach (var pack in packs)
            {
                if (pack.IsValid)
                {
                    yield return pack.LoadResources(this, loadStateInfo);
                }
                
            }

            foreach (var pack in packs)
            {
                if (pack.IsValid)
                {
                    yield return pack.BuildStateGeometries(this, loadStateInfo);
                }
                
            }

            // Perform integrity check...
            var statesTable = Block.Palette.StatesTable;

            foreach (var stateItem in statesTable)
            {
                if (!finalTable.ContainsKey(stateItem.Key))
                {
                    Debug.LogWarning("Model for " + stateItem.Value.ToString() + "(state Id " + stateItem.Key + ") not loaded!");
                }
            }

            loadStateInfo.infoText = string.Empty;

            Debug.Log("Resource packs loaded in " + (Time.realtimeSinceStartup - startTime) + " seconds.");
            Debug.Log("Built " + finalTable.Count + " block state geometry lists.");

            flag.done = true;

        }


    }
}