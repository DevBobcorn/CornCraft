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
        }

        public void LoadPacks(DataLoadFlag flag, LoadStateInfo loadStateInfo)
        {
            System.Diagnostics.Stopwatch sw = new();
            sw.Start();

            // Gather all textures and model files
            loadStateInfo.InfoText = $"Gathering resources";
            foreach (var pack in packs) pack.GatherResources(this);

            var atlasGenFlag = new DataLoadFlag();

            // Load texture atlas (on main thread)...
            loadStateInfo.InfoText = $"Creating textures";
            Loom.QueueOnMainThread(() => {
                Loom.Current.StartCoroutine(AtlasManager.Generate(this, atlasGenFlag));
            });
            
            while (!atlasGenFlag.Finished) { /* Wait */ }

            loadStateInfo.InfoText = $"Loading models";

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
            }

            loadStateInfo.InfoText = $"Building block state geometries";
            BuildStateGeometries(loadStateInfo);

            loadStateInfo.InfoText = $"Building item geometries";
            BuildItemGeometries(loadStateInfo);

            // Perform integrity check...
            var statesTable = BlockStatePalette.INSTANCE.StatesTable;

            foreach (var stateItem in statesTable)
            {
                if (!StateModelTable.ContainsKey(stateItem.Key))
                {
                    Debug.LogWarning($"Model for {stateItem.Value}(state Id {stateItem.Key}) not loaded!");
                }
            }

            loadStateInfo.InfoText = string.Empty;

            Debug.Log($"Resource packs loaded in {sw.ElapsedMilliseconds} ms.");
            Debug.Log($"Built {StateModelTable.Count} block state geometry lists.");

            flag.Finished = true;
        }

        public void BuildStateGeometries(LoadStateInfo loadStateInfo)
        {
            // Load all blockstate files and build their block meshes...
            foreach (var blockPair in BlockStatePalette.INSTANCE.StateListTable)
            {
                var blockId = blockPair.Key;
                
                if (BlockStateFileTable.ContainsKey(blockId)) // Load the state model definition of this block
                {
                    var renderType =
                        BlockStatePalette.INSTANCE.RenderTypeTable.GetValueOrDefault(blockId, RenderType.SOLID);

                    StateModelLoader.LoadBlockStateModel(this, blockId, BlockStateFileTable[blockId], renderType);
                }
                else
                    Debug.LogWarning($"Block state model definition not assigned for {blockId}!");
                
            }

        }

        public void BuildItemGeometries(LoadStateInfo loadStateInfo)
        {
            // Load all item model files and build their item meshes...
            foreach (var numId in ItemPalette.INSTANCE.ItemsTable.Keys)
            {
                var item = ItemPalette.INSTANCE.ItemsTable[numId];
                var itemId = item.ItemId;

                var itemModelId = new ResourceLocation(itemId.Namespace, $"item/{itemId.Path}");

                if (ItemModelFileTable.ContainsKey(itemModelId))
                {
                    if (RawItemModelTable.ContainsKey(itemModelId))
                    {
                        var rawModel = RawItemModelTable[itemModelId];
                        var tintable = ItemPalette.INSTANCE.IsTintable(numId);
                        var generated = GeneratedItemModels.Contains(itemModelId);

                        if (generated) // This model should be generated
                        {
                            // Get layer count of this item model
                            int layerCount = rawModel.Textures.Count;

                            rawModel.Elements.AddRange(
                                    ItemModelLoader.GetGeneratedItemModelElements(
                                            layerCount, GeneratedItemModelPrecision,
                                                    GeneratedItemModelThickness, tintable).ToArray());
                            
                            //Debug.Log($"Generating item model for {itemModelId} tintable: {tintable}");
                        }

                        var itemGeometry = new ItemGeometry(rawModel, generated);

                        RenderType renderType;

                        if (GeneratedItemModels.Contains(itemModelId))
                            renderType = RenderType.CUTOUT; // Set render type to cutout for all generated item models
                        else
                            renderType = BlockStatePalette.INSTANCE.RenderTypeTable.GetValueOrDefault(itemId, RenderType.SOLID);

                        var itemModel = new ItemModel(itemGeometry, renderType);
                        

                        // Look for and append geometry overrides to the item model
                        Json.JSONData modelData = Json.ParseJson(File.ReadAllText(ItemModelFileTable[itemModelId]));

                        if (modelData.Properties.ContainsKey("overrides"))
                        {
                            var overrides = modelData.Properties["overrides"].DataArray;

                            foreach (var o in overrides)
                            {
                                var overrideModelId = ResourceLocation.fromString(o.Properties["model"].StringValue);

                                if (RawItemModelTable.ContainsKey(overrideModelId)) // Build this override
                                {
                                    var rawOverrideModel = RawItemModelTable[overrideModelId];
                                    var overrideGenerated = GeneratedItemModels.Contains(overrideModelId);

                                    if (overrideGenerated) // This model should be generated
                                    {
                                        // Get layer count of this item model
                                        int layerCount = rawModel.Textures.Count;

                                        rawOverrideModel.Elements.AddRange(
                                                ItemModelLoader.GetGeneratedItemModelElements(
                                                        layerCount, GeneratedItemModelPrecision,
                                                                GeneratedItemModelThickness, tintable).ToArray());
                                        
                                        //Debug.Log($"Generating item model for {itemModelId} tintable: {tintable}");
                                    }

                                    var overrideGeometry = new ItemGeometry(rawOverrideModel, overrideGenerated);
                                    var predicate = ItemModelPredicate.fromJson(o.Properties["predicate"]);
                                    
                                    itemModel.AddOverride(predicate, overrideGeometry);
                                }
                                
                            }
                        }

                        ItemModelTable.Add(numId, itemModel);
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