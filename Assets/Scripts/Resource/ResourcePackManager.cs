using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using MinecraftClient.Mapping;

namespace MinecraftClient.Resource
{
    public class ResourcePackManager
    {
        // Identifier -> Texture path
        public readonly Dictionary<ResourceLocation, string> TextureTable = new Dictionary<ResourceLocation, string>();

        // Identifier -> Block model
        public readonly Dictionary<ResourceLocation, JsonModel> BlockModelTable = new Dictionary<ResourceLocation, JsonModel>();

        // Block state numeral id -> Block state geometries (One single block state may have a list of models to use randomly)
        public readonly Dictionary<int, BlockStateModel> StateModelTable = new Dictionary<int, BlockStateModel>();

        // Identifier -> Raw item model
        public readonly Dictionary<ResourceLocation, JsonModel> RawItemModelTable = new Dictionary<ResourceLocation, JsonModel>();

        // Item numeral id -> Item model
        public readonly Dictionary<int, ItemModel> ItemModelTable = new Dictionary<int, ItemModel>();

        public readonly HashSet<ResourceLocation> GeneratedItemModels = new();
        public readonly HashSet<ResourceLocation> TintableItemModels  = new();

        public readonly BlockModelLoader BlockModelLoader;
        public readonly BlockStateModelLoader StateModelLoader;

        public readonly ItemModelLoader ItemModelLoader;

        private readonly List<ResourcePack> packs = new List<ResourcePack>();

        public ResourcePackManager()
        {
            // Block model loaders
            BlockModelLoader = new BlockModelLoader(this);
            StateModelLoader = new BlockStateModelLoader(this);

            // Item model loader
            ItemModelLoader = new ItemModelLoader(this);
        }

        public void AddPack(ResourcePack pack)
        {
            packs.Add(pack);
        }

        public void ClearPacks()
        {
            packs.Clear();
            TextureTable.Clear();
            BlockModelTable.Clear();
            StateModelTable.Clear();
            RawItemModelTable.Clear();
            ItemModelTable.Clear();
            GeneratedItemModels.Clear();
            TintableItemModels.Clear();
        }

        public IEnumerator LoadPacks(MonoBehaviour loader, CoroutineFlag flag, LoadStateInfo loadStateInfo)
        {
            float startTime = Time.realtimeSinceStartup;

            foreach (var pack in packs)
            {
                if (pack.IsValid)
                {
                    yield return pack.LoadResources(this, loadStateInfo);
                }
                
            }

            var wait = new WaitForSecondsRealtime(0.1F);

            var atlasLoadFlag = new CoroutineFlag();
            loader.StartCoroutine(AtlasManager.Generate(this, atlasLoadFlag, loadStateInfo));

            while (!atlasLoadFlag.done)
                yield return wait;

            foreach (var pack in packs)
            {
                if (pack.IsValid)
                {
                    yield return pack.BuildStateGeometries(this, loadStateInfo);
                    yield return pack.BuildItemGeometries(this, loadStateInfo);
                }
                
            }

            // Perform integrity check...
            var statesTable = BlockStatePalette.INSTANCE.StatesTable;

            foreach (var stateItem in statesTable)
            {
                if (!StateModelTable.ContainsKey(stateItem.Key))
                {
                    Debug.LogWarning($"Model for {stateItem.Value}(state Id {stateItem.Key}) not loaded!");
                }
            }

            loadStateInfo.infoText = string.Empty;

            Debug.Log($"Resource packs loaded in {Time.realtimeSinceStartup - startTime} seconds.");
            Debug.Log($"Built {StateModelTable.Count} block state geometry lists.");

            flag.done = true;

        }


    }
}