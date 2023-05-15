#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using Unity.Mathematics;

using MinecraftClient.Mapping;
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

        public void AddPack(ResourcePack pack) => packs.Add(pack);

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

        public void LoadPacks(DataLoadFlag flag, Action<string> updateStatus)
        {
            System.Diagnostics.Stopwatch sw = new();
            sw.Start();

            // Gather all textures and model files
            updateStatus("status.info.gather_resource");
            foreach (var pack in packs) pack.GatherResources(this);

            var atlasGenFlag = new DataLoadFlag();

            // Load texture atlas (on main thread)...
            updateStatus("status.info.create_texture");
            Loom.QueueOnMainThread(() => {
                Loom.Current.StartCoroutine(GenerateAtlas(atlasGenFlag));
            });
            
            while (!atlasGenFlag.Finished) { /* Wait */ }

            updateStatus("status.info.load_block_model");

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

            updateStatus("status.info.build_blockstate_geometry");
            BuildStateGeometries();

            updateStatus("status.info.build_item_geometry");
            BuildItemGeometries();

            // Perform integrity check...
            var statesTable = BlockStatePalette.INSTANCE.StatesTable;

            foreach (var stateItem in statesTable)
            {
                if (!StateModelTable.ContainsKey(stateItem.Key))
                {
                    Debug.LogWarning($"Model for {stateItem.Value}(state Id {stateItem.Key}) not loaded!");
                }
            }

            Debug.Log($"Resource packs loaded in {sw.ElapsedMilliseconds} ms.");
            Debug.Log($"Built {StateModelTable.Count} block state geometry lists.");

            flag.Finished = true;
        }

        public void BuildStateGeometries()
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

        public void BuildItemGeometries()
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
        
        public readonly TextureInfo DEFAULT_TEXTURE_INFO = new(new(), 0, false);

        private Dictionary<ResourceLocation, TextureInfo> texAtlasTable = new();

        public float3[] GetUVs(ResourceLocation identifier, Vector4 part, int areaRot)
        {
            var info = GetTextureInfo(identifier);
            return GetUVsAt(info.bounds, info.index, info.animatable, part, areaRot);
        }

        private float3[] GetUVsAt(Rect bounds, int index, bool animatable, Vector4 part, int areaRot)
        {
            float oneU = bounds.width;
            float oneV;

            if (animatable)
            {
                // Use width here because a texture can contain multiple frames
                // TODO Real support for animatable texture
                // TODO Solve the texture bleeding problem
                oneV = bounds.width; 
            }
            else
                oneV = bounds.height;

            // Get texture offset in atlas
            float3 o = new(bounds.xMin, bounds.yMin, index + 0.1F);

            // vect:  x,  y,  z,  w
            // vect: x1, y1, x2, y2
            float u1 = part.x * oneU, v1 = part.y * oneV;
            float u2 = part.z * oneU, v2 = part.w * oneV;

            return areaRot switch
            {
                0 => new float3[]{ new float3(       u1, oneV - v1, 0F) + o, new float3(       u2, oneV - v1, 0F) + o, new float3(       u1, oneV - v2, 0F) + o, new float3(       u2, oneV - v2, 0F) + o }, //   0 Deg
                1 => new float3[]{ new float3(       v1,        u1, 0F) + o, new float3(       v1,        u2, 0F) + o, new float3(       v2,        u1, 0F) + o, new float3(       v2,        u2, 0F) + o }, //  90 Deg
                2 => new float3[]{ new float3(oneU - u1,        v1, 0F) + o, new float3(oneU - u2,        v1, 0F) + o, new float3(oneU - u1,        v2, 0F) + o, new float3(oneU - u2,        v2, 0F) + o }, // 180 Deg
                3 => new float3[]{ new float3(oneV - v1, oneV - u1, 0F) + o, new float3(oneV - v1, oneU - u2, 0F) + o, new float3(oneV - v2, oneU - u1, 0F) + o, new float3(oneV - v2, oneU - u2, 0F) + o }, // 270 Deg

                _ => new float3[]{ new float3(       u1, oneV - v1, 0F) + o, new float3(       u2, oneV - v1, 0F) + o, new float3(       u1, oneV - v2, 0F) + o, new float3(       u2, oneV - v2, 0F) + o }  // Default
            };
        }

        private TextureInfo GetTextureInfo(ResourceLocation identifier)
        {
            if (texAtlasTable.ContainsKey(identifier))
                return texAtlasTable[identifier];
            
            return DEFAULT_TEXTURE_INFO;
        }

        private readonly Texture2DArray?[] atlasArrays = new Texture2DArray?[2];

        public Texture2DArray GetAtlasArray(RenderType type)
        {
            return type switch
            {
                RenderType.CUTOUT        => atlasArrays[0]!,
                RenderType.CUTOUT_MIPPED => atlasArrays[1]!,
                RenderType.SOLID         => atlasArrays[0]!,
                RenderType.TRANSLUCENT   => atlasArrays[0]!,

                _                        => atlasArrays[0]!
            };
        }

        public const int ATLAS_SIZE = 512;
        
        private IEnumerator GenerateAtlas(DataLoadFlag atlasGenFlag)
        {
            texAtlasTable.Clear(); // Clear previously loaded table...

            var texDict = TextureFileTable;

            int count = 0;

            var textureIdSet = new HashSet<ResourceLocation>();

            // Collect referenced textures
            var modelFilePaths = BlockModelFileTable.Values.ToList();
            modelFilePaths.AddRange(ItemModelFileTable.Values);
            
            foreach (var modelFile in modelFilePaths)
            {
                var model = Json.ParseJson(File.ReadAllText(modelFile));

                if (model.Properties.ContainsKey("textures"))
                {
                    var texData = model.Properties["textures"].Properties;
                    foreach (var texItem in texData)
                    {
                        if (!texItem.Value.StringValue.StartsWith('#'))
                        {
                            var texId = ResourceLocation.fromString(texItem.Value.StringValue);

                            if (texDict.ContainsKey(texId))
                                textureIdSet.Add(texId);
                            //else
                            //    Debug.LogWarning($"Texture {texId} not found in dictionary! (Referenced in {modelFile})");
                        }
                            
                    }
                }
            }

            // Append liquid textures, which are not referenced in model files, but will be used by fluid mesh
            foreach (var liquidTex in FluidGeometry.LiquidTextures)
                textureIdSet.Add(liquidTex);

            var textures = new Texture2D[textureIdSet.Count];
            var ids = new ResourceLocation[textureIdSet.Count];

            foreach (var texId in textureIdSet) // Load texture files...
            {
                var texFilePath = texDict[texId];
                ids[count] = texId;
                //Debug.Log($"Loading {texId} from {texFilePath}");

                Texture2D tex = new(2, 2);
                tex.name = texId.ToString();
                tex.LoadImage(File.ReadAllBytes(texFilePath));

                textures[count++] = tex;
                if (count % 5 == 0) yield return null;
            }
            
            int curTexIndex = 0, curAtlasIndex = 0;
            List<Texture2D> atlases = new();

            int totalVolume = ATLAS_SIZE * ATLAS_SIZE;
            int maxContentVolume = (int)(totalVolume * 0.95F);

            do
            {
                // First count all the textures to be stitched onto this atlas
                int lastTexIndex = curTexIndex - 1, curVolume = 0; // lastTexIndex is inclusive

                while (true)
                {
                    if (lastTexIndex >= textureIdSet.Count - 1)
                        break;

                    var nextTex = textures[lastTexIndex + 1];
                    curVolume += nextTex.width * nextTex.height;

                    if (curVolume < maxContentVolume)
                        lastTexIndex++;
                    else
                        break;
                }

                int consumedTexCount = lastTexIndex + 1 - curTexIndex;

                if (consumedTexCount == 0)
                {
                    // In this occasion the texture is too large and can only be scaled a bit and placed on a separate atlas
                    lastTexIndex = curTexIndex;
                    consumedTexCount = 1;
                }

                // Then we go stitch 'em        (inclusive)..(exclusive)
                var texturesConsumed = textures[curTexIndex..(lastTexIndex + 1)];

                var atlas = new Texture2D(ATLAS_SIZE, ATLAS_SIZE); // First assign a placeholder
                atlas.filterMode = FilterMode.Point;

                var rects = atlas.PackTextures(texturesConsumed, 0, ATLAS_SIZE, false);

                if (atlas.width != ATLAS_SIZE || atlas.height != ATLAS_SIZE)
                {
                    // Size not right, replace it (usually the last atlas in array which doesn't
                    // have enough textures to take up all the place and thus is smaller in size)
                    var newAtlas = new Texture2D(ATLAS_SIZE, ATLAS_SIZE);

                    Graphics.CopyTexture(atlas, 0, 0, 0, 0, atlas.width, atlas.height, newAtlas, 0, 0, 0, 0);

                    float scaleX = (float) atlas.width  / (float) ATLAS_SIZE;
                    float scaleY = (float) atlas.height / (float) ATLAS_SIZE;

                    // Rescale the texture boundaries
                    for (int i = 0;i < rects.Length;i++)
                    {
                        rects[i] = new Rect(
                            rects[i].x     * scaleX,    rects[i].y      * scaleY,
                            rects[i].width * scaleX,    rects[i].height * scaleY
                        );
                    }

                    atlas = newAtlas;
                }

                atlases.Add(atlas);

                for (int i = 0;i < consumedTexCount;i++)
                {
                    //Debug.Log($"{ids[curTexIndex + i]} => ({curAtlasIndex}) {rects[i].xMin} {rects[i].xMax} {rects[i].yMin} {rects[i].yMax}");
                    
                    // TODO Read texture meta file and use that information
                    bool animatable = ids[curTexIndex + i].Path.StartsWith("item") || ids[curTexIndex + i].Path.StartsWith("block");
                    
                    texAtlasTable.Add(ids[curTexIndex + i], new(rects[i], curAtlasIndex, animatable));
                }

                curTexIndex += consumedTexCount;

                curAtlasIndex++;

                yield return null;

            }
            while (curTexIndex < textureIdSet.Count);

            var atlasArray0 = new Texture2DArray(ATLAS_SIZE, ATLAS_SIZE, curAtlasIndex, TextureFormat.RGBA32,  2, false);
            var atlasArray1 = new Texture2DArray(ATLAS_SIZE, ATLAS_SIZE, curAtlasIndex, TextureFormat.RGBA32,  4, false);

            atlasArray0.filterMode = FilterMode.Point;
            atlasArray1.filterMode = FilterMode.Point;

            for (int index = 0;index < atlases.Count;index++)
            {
                atlasArray0.SetPixels(atlases[index].GetPixels(), index, 0);
                atlasArray1.SetPixels(atlases[index].GetPixels(), index, 0);

                yield return null;
            }

            atlasArray0.Apply(true, false);
            atlasArray1.Apply(true, false);

            atlasArrays[0] = atlasArray0;
            atlasArrays[1] = atlasArray1;

            atlasGenFlag.Finished = true;
        }

    }
}