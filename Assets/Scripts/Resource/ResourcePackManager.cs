using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using MinecraftClient.Mapping;
using System.IO;
using MinecraftClient.Rendering;

namespace MinecraftClient.Resource
{
    public class ResourcePackManager
    {
        // Identifier -> Texture file path
        public readonly Dictionary<ResourceLocation, string> TextureFileTable = new();

        // Identidier -> Block json model file path
        public readonly Dictionary<ResourceLocation, string> BlockModelFileTable = new();

        // Identidier -> Item json model file path
        public readonly Dictionary<ResourceLocation, string> ItemModelFileTable = new();

        // Identidier -> BlockState json model file path
        public readonly Dictionary<ResourceLocation, string> BlockStateFileTable = new();

        // Identifier -> Block model
        public readonly Dictionary<ResourceLocation, JsonModel> BlockModelTable = new();

        // Block state numeral id -> Block state geometries (One single block state may have a list of models to use randomly)
        public readonly Dictionary<int, BlockStateModel> StateModelTable = new();

        // Identifier -> Raw item model
        public readonly Dictionary<ResourceLocation, JsonModel> RawItemModelTable = new();

        // Item numeral id -> Item model
        public readonly Dictionary<int, ItemModel> ItemModelTable = new();

        public readonly HashSet<ResourceLocation> GeneratedItemModels = new();
        public readonly HashSet<ResourceLocation> TintableItemModels  = new();

        public readonly BlockModelLoader BlockModelLoader;
        public readonly BlockStateModelLoader StateModelLoader;

        public readonly ItemModelLoader ItemModelLoader;

        public int GeneratedItemModelPrecision { get; set; } = 16;
        public int GeneratedItemModelThickness { get; set; } =  1;

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
            TextureFileTable.Clear();
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

            // Gather all textures and model files
            foreach (var pack in packs)
            {
                if (pack.IsValid)
                {
                    yield return pack.GatherResources(this, loadStateInfo);
                }
                
            }

            // Load texture atlas...
            yield return AtlasManager.Generate(this, loadStateInfo);

            // Load block models...
            foreach (var blockModelId in BlockModelFileTable.Keys)
            {
                // This model loader will load this model, its parent model(if not yet loaded),
                // and then add them to the manager's model dictionary
                BlockModelLoader.LoadBlockModel(blockModelId);
            }

            // Load item models...
            foreach (var itemModelId in ItemModelFileTable.Keys)
            {
                // This model loader will load this model, its parent model(if not yet loaded),
                // and then add them to the manager's model dictionary
                ItemModelLoader.LoadItemModel(itemModelId);
                
                if (GeneratedItemModels.Contains(itemModelId)) // This model should be generated
                {
                    var model = RawItemModelTable[itemModelId];

                    // Get layer count of this item model
                    int layerCount = model.Textures.Count;
                    var useItemCol = TintableItemModels.Contains(itemModelId);

                    model.Elements.AddRange(
                            ItemModelLoader.GetGeneratedItemModelElements(
                                    layerCount, GeneratedItemModelPrecision,
                                            GeneratedItemModelThickness, useItemCol).ToArray());
                }
            }

            yield return BuildStateGeometries(this, loadStateInfo);

            yield return BuildItemGeometries(this, loadStateInfo);

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

        public IEnumerator BuildStateGeometries(ResourcePackManager manager, LoadStateInfo loadStateInfo)
        {
            var fileTable = manager.BlockStateFileTable;

            // Load all blockstate files and build their block meshes...
            int count = 0;

            foreach (var blockPair in BlockStatePalette.INSTANCE.StateListTable)
            {
                var blockId = blockPair.Key;
                
                if (fileTable.ContainsKey(blockId)) // Load the state model definition of this block
                {
                    var renderType =
                        BlockStatePalette.INSTANCE.RenderTypeTable.GetValueOrDefault(blockId, RenderType.SOLID);

                    manager.StateModelLoader.LoadBlockStateModel(manager, blockId, fileTable[blockId], renderType);
                    count++;
                    if (count % 10 == 0)
                    {
                        loadStateInfo.infoText = $"Building model for block {blockId}";
                        yield return null;
                    }
                    
                }
                else
                    Debug.LogWarning($"Block state model definition not assigned for {blockId}!");
                
            }

        }

        public IEnumerator BuildItemGeometries(ResourcePackManager manager, LoadStateInfo loadStateInfo)
        {
            var fileTable = manager.ItemModelFileTable;

            // Load all item model files and build their item meshes...
            int count = 0;

            foreach (var numId in ItemPalette.INSTANCE.ItemsTable.Keys)
            {
                var item = ItemPalette.INSTANCE.ItemsTable[numId];
                var itemId = item.itemId;

                var itemModelId = new ResourceLocation(itemId.Namespace, $"item/{itemId.Path}");

                if (fileTable.ContainsKey(itemModelId))
                {
                    if (manager.RawItemModelTable.ContainsKey(itemModelId))
                    {
                        var itemGeometry = new ItemGeometry(manager.RawItemModelTable[itemModelId]);

                        RenderType renderType;

                        if (manager.GeneratedItemModels.Contains(itemModelId))
                            renderType = RenderType.CUTOUT; // Set render type to cutout for all generated item models
                        else
                            renderType = BlockStatePalette.INSTANCE.RenderTypeTable.GetValueOrDefault(itemId, RenderType.SOLID);

                        var itemModel = new ItemModel(itemGeometry, renderType);

                        var tintable = ItemPalette.INSTANCE.IsTintable(numId);

                        if (tintable) // Mark this item model as tintable
                        {
                            manager.TintableItemModels.Add(itemModelId);
                            //Debug.Log($"Marked {itemModelId} as tintable");
                        }

                        // Look for and append geometry overrides to the item model
                        Json.JSONData modelData = Json.ParseJson(File.ReadAllText(fileTable[itemModelId]));

                        if (modelData.Properties.ContainsKey("overrides"))
                        {
                            var overrides = modelData.Properties["overrides"].DataArray;

                            foreach (var o in overrides)
                            {
                                var overrideModelId = ResourceLocation.fromString(o.Properties["model"].StringValue);

                                if (tintable) // Mark this override model as tintable
                                {
                                    manager.TintableItemModels.Add(overrideModelId);
                                    //Debug.Log($"Marked {itemModelId} as tintable");
                                }

                                if (manager.RawItemModelTable.ContainsKey(overrideModelId)) // Build this override
                                {
                                    var overrideGeometry = new ItemGeometry(manager.RawItemModelTable[overrideModelId]);
                                    var predicate = ItemModelPredicate.fromJson(o.Properties["predicate"]);
                                    
                                    itemModel.AddOverride(predicate, overrideGeometry);
                                }
                                
                            }
                        }

                        manager.ItemModelTable.Add(numId, itemModel);
                        count++;
                        if (count % 8 == 0)
                        {
                            loadStateInfo.infoText = $"Building model for item {itemId}";
                            yield return null;
                        }
                    }
                    else
                        Debug.LogWarning($"Item model for {itemId} not found at {itemModelId}!");
                }
                else
                    Debug.LogWarning($"Item model not assigned for {itemModelId}");

            }

        }

    }
}