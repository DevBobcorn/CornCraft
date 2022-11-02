using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

using UnityEngine;
using Unity.Mathematics;
using MinecraftClient.Rendering;
using MinecraftClient.Resource;

namespace MinecraftClient.Mapping
{
    public class BlockStatePalette
    {
        public static readonly BlockStatePalette INSTANCE = new();

        public BlockState FromId(int stateId)
        {
            return statesTable.GetValueOrDefault(stateId, BlockState.AIR_STATE);
        }

        public ResourceLocation GetBlock(int stateId)
        {
            return blocksTable.GetValueOrDefault(stateId, BlockState.AIR_ID);
        }

        public HashSet<int> GetStatesOfBlock(ResourceLocation blockId)
        {
            return stateListTable.GetValueOrDefault(blockId, new HashSet<int>());
        }

        private readonly Dictionary<ResourceLocation, HashSet<int>> stateListTable = new Dictionary<ResourceLocation, HashSet<int>>();
        public Dictionary<ResourceLocation, HashSet<int>> StateListTable { get { return stateListTable; } }

        private readonly Dictionary<int, ResourceLocation> blocksTable = new Dictionary<int, ResourceLocation>();
        public Dictionary<int, ResourceLocation> BlocksTable { get { return blocksTable; } }

        private readonly Dictionary<int, BlockState> statesTable = new Dictionary<int, BlockState>();
        public Dictionary<int, BlockState> StatesTable { get { return statesTable; } }

        private readonly Dictionary<int, RenderType> renderTypeTable = new Dictionary<int, RenderType>();
        public RenderType GetRenderType(int stateId) => renderTypeTable.GetValueOrDefault(stateId, RenderType.SOLID);

        private readonly Dictionary<int, Func<World, Location, BlockState, float3>> blockColorRules = new();

        public float3 GetBlockColor(int stateId, World world, Location loc, BlockState state)
        {
            if (blockColorRules.ContainsKey(stateId))
                return blockColorRules[stateId].Invoke(world, loc, state);
            return BlockGeometry.DEFAULT_COLOR;
        } 

        private static readonly ResourceLocation LAVA = new("lava");

        public IEnumerator PrepareData(string dataVersion, CoroutineFlag flag, LoadStateInfo loadStateInfo)
        {
            // Clean up first...
            statesTable.Clear();
            blocksTable.Clear();
            stateListTable.Clear();

            HashSet<int> knownStates = new HashSet<int>();

            string statesPath = PathHelper.GetExtraDataFile($"blocks-{dataVersion}.json");
            string listsPath  = PathHelper.GetExtraDataFile("block_lists-1.19.json");
            string colorsPath = PathHelper.GetExtraDataFile("block_colors-1.19.json");

            if (!File.Exists(statesPath) || !File.Exists(listsPath) || !File.Exists(colorsPath))
                throw new FileNotFoundException("Block data not complete!");

            // First read special block lists...
            var noOcclusion = new List<ResourceLocation>();
            var noCollision = new List<ResourceLocation>();
            var waterBlocks = new List<ResourceLocation>();
            var emptyBlocks = new List<ResourceLocation>();
            var alwaysFulls = new List<ResourceLocation>();

            Json.JSONData spLists = Json.ParseJson(File.ReadAllText(listsPath, Encoding.UTF8));
            loadStateInfo.infoText = $"Reading special lists from {listsPath}";

            int count = 0, yieldCount = 200;

            if (spLists.Properties.ContainsKey("no_occlusion"))
            {
                foreach (var block in spLists.Properties["no_occlusion"].DataArray)
                {
                    noOcclusion.Add(ResourceLocation.fromString(block.StringValue));
                    count++;
                    if (count % yieldCount == 0)
                        yield return null;
                }
            }

            if (spLists.Properties.ContainsKey("no_collision"))
            {
                foreach (var block in spLists.Properties["no_collision"].DataArray)
                {
                    noCollision.Add(ResourceLocation.fromString(block.StringValue));
                    count++;
                    if (count % yieldCount == 0)
                        yield return null;
                }
            }

            if (spLists.Properties.ContainsKey("water_blocks"))
            {
                foreach (var block in spLists.Properties["water_blocks"].DataArray)
                {
                    waterBlocks.Add(ResourceLocation.fromString(block.StringValue));
                    count++;
                    if (count % yieldCount == 0)
                        yield return null;
                }
            }

            if (spLists.Properties.ContainsKey("always_fulls"))
            {
                foreach (var block in spLists.Properties["always_fulls"].DataArray)
                {
                    alwaysFulls.Add(ResourceLocation.fromString(block.StringValue));
                    count++;
                    if (count % yieldCount == 0)
                        yield return null;
                }
            }

            emptyBlocks.Add(new ResourceLocation("air"));
            emptyBlocks.Add(new ResourceLocation("void_air"));
            emptyBlocks.Add(new ResourceLocation("cave_air"));
            emptyBlocks.Add(new ResourceLocation("structure_void"));
            emptyBlocks.Add(new ResourceLocation("light"));
            emptyBlocks.Add(new ResourceLocation("water"));
            emptyBlocks.Add(LAVA);
            emptyBlocks.Add(new ResourceLocation("barrier"));

            // Then read block states...
            Json.JSONData palette = Json.ParseJson(File.ReadAllText(statesPath, Encoding.UTF8));
            Debug.Log("Reading block states from " + statesPath);
            count = 0;
            foreach (KeyValuePair<string, Json.JSONData> item in palette.Properties)
            {
                ResourceLocation blockId = ResourceLocation.fromString(item.Key);

                if (stateListTable.ContainsKey(blockId))
                    throw new InvalidDataException($"Duplicate block id {blockId}!");
                
                stateListTable[blockId] = new HashSet<int>();

                foreach (Json.JSONData state in item.Value.Properties["states"].DataArray)
                {
                    int stateId = int.Parse(state.Properties["id"].StringValue);

                    if (knownStates.Contains(stateId))
                        throw new InvalidDataException($"Duplicate state id {stateId}!?");

                    knownStates.Add(stateId);
                    blocksTable[stateId] = blockId;
                    stateListTable[blockId].Add(stateId);

                    if (state.Properties.ContainsKey("properties"))
                    {
                        // This block state contains block properties
                        var props = new Dictionary<string, string>();

                        var inWater = waterBlocks.Contains(blockId);

                        foreach (var prop in state.Properties["properties"].Properties)
                        {
                            props.Add(prop.Key, prop.Value.StringValue);

                            // Special proc for waterlogged property...
                            if (prop.Key == "waterlogged" && prop.Value.StringValue == "true")
                                inWater = true;

                        }

                        statesTable[stateId] = new BlockState(blockId, props)
                        {
                            NoOcclusion = noOcclusion.Contains(blockId),
                            NoCollision = noCollision.Contains(blockId),
                            InWater = inWater,
                            InLava  = blockId == LAVA,
                            LikeAir = emptyBlocks.Contains(blockId),
                            FullSolid = (!noOcclusion.Contains(blockId)) && alwaysFulls.Contains(blockId)
                        };
                    }
                    else
                    {
                        statesTable[stateId] = new BlockState(blockId)
                        {
                            NoOcclusion = noOcclusion.Contains(blockId),
                            NoCollision = noCollision.Contains(blockId),
                            InWater = waterBlocks.Contains(blockId),
                            InLava  = blockId == LAVA,
                            LikeAir = emptyBlocks.Contains(blockId),
                            FullSolid = (!noOcclusion.Contains(blockId)) && alwaysFulls.Contains(blockId)
                        };
                    }

                    // Count per state so that loading time can be more evenly distributed
                    count++;
                    if (count % 10 == 0)
                    {
                        loadStateInfo.infoText = $"Loading states of block {item.Key}";
                        yield return null;
                    }

                }
            }

            Debug.Log($"{statesTable.Count} block states loaded.");

            // Load block color rules...
            blockColorRules.Clear();
            loadStateInfo.infoText = $"Loading block color rules";
            yield return null;

            Json.JSONData colorRules = Json.ParseJson(File.ReadAllText(colorsPath, Encoding.UTF8));

            if (colorRules.Properties.ContainsKey("dynamic"))
            {
                foreach (var dynamicRule in colorRules.Properties["dynamic"].Properties)
                {
                    var ruleName = dynamicRule.Key;

                    Func<World, Location, BlockState, float3> ruleFunc = ruleName switch {
                        "foliage"  => (world, loc, state) => world.GetFoliageColor(loc),
                        "grass"    => (world, loc, state) => world.GetGrassColor(loc),
                        "redstone" => (world, loc, state) => {
                            if (state.props.ContainsKey("power"))
                                return new(float.Parse(state.props["power"]) / 16F, 0F, 0F);
                            return BlockGeometry.DEFAULT_COLOR;
                        },

                        _         => (world, loc, state) => float3.zero
                    };

                    foreach (var block in dynamicRule.Value.DataArray)
                    {
                        var blockId = ResourceLocation.fromString(block.StringValue);

                        if (stateListTable.ContainsKey(blockId))
                        {
                            foreach (var stateId in stateListTable[blockId])
                            {
                                if (!blockColorRules.TryAdd(stateId, ruleFunc))
                                    Debug.LogWarning($"Failed to apply dynamic color rules to {blockId} ({stateId})!");
                            }
                        }
                        else
                            Debug.LogWarning($"Applying dynamic color rules to undefined block {blockId}!");
                        
                        count++;
                        if (count % yieldCount == 0)
                            yield return null;
                    }
                }
            }

            if (colorRules.Properties.ContainsKey("fixed"))
            {
                foreach (var fixedRule in colorRules.Properties["fixed"].Properties)
                {
                    var blockId = ResourceLocation.fromString(fixedRule.Key);

                    if (stateListTable.ContainsKey(blockId))
                    {
                        var fixedColor = VectorUtil.Json2Float3(fixedRule.Value) / 255F;
                        Func<World, Location, BlockState, float3> ruleFunc = (world, loc, state) => fixedColor;

                        foreach (var stateId in stateListTable[blockId])
                        {
                            if (!blockColorRules.TryAdd(stateId, ruleFunc))
                                Debug.LogWarning($"Failed to apply fixed color rules to {blockId} ({stateId})!");
                            count++;
                            if (count % yieldCount == 0)
                                yield return null;
                        }
                    }
                    else
                        Debug.LogWarning($"Applying fixed color rules to undefined block {blockId}!");
                }
            }

            yield return null;
            
            // Load and apply block render types...
            renderTypeTable.Clear();
            loadStateInfo.infoText = $"Loading lists of render types";
            yield return null;

            string renderTypePath = PathHelper.GetExtraDataFile("block_render_type.json");
            if (File.Exists(renderTypePath))
            {
                try
                {
                    string renderTypeText = File.ReadAllText(renderTypePath);
                    var renderTypes = Json.ParseJson(renderTypeText);

                    foreach (var typeItem in renderTypes.Properties)
                    {
                        var blockId = ResourceLocation.fromString(typeItem.Key);

                        if (stateListTable.ContainsKey(blockId))
                        {
                            foreach (var stateId in stateListTable[blockId])
                            {
                                if (!renderTypeTable.ContainsKey(stateId))
                                {
                                    renderTypeTable.Add(
                                        stateId,
                                        typeItem.Value.StringValue.ToLower() switch
                                        {
                                            "solid"         => RenderType.SOLID,
                                            "cutout"        => RenderType.CUTOUT,
                                            "cutout_mipped" => RenderType.CUTOUT_MIPPED,
                                            "translucent"   => RenderType.TRANSLUCENT,

                                            _               => RenderType.SOLID
                                        }
                                    );
                                }
                                else
                                    Debug.LogWarning($"Render type of {statesTable[stateId].ToString()} registered more than once!");

                            }
                        }

                    }

                }
                catch (IOException e)
                {
                    Debug.LogWarning("Failed to load block render types: " + e.Message);
                }
            }
            else
            {
                Debug.LogWarning("Block render types not found at " + renderTypePath);
            }

            Debug.Log($"Render type of {renderTypeTable.Count} blocks loaded.");

            flag.done = true;

        }

    }
}