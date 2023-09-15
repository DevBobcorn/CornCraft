using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using Unity.Mathematics;

using CraftSharp.Resource;

namespace CraftSharp
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

        private readonly Dictionary<ResourceLocation, int> defaultStateTable = new Dictionary<ResourceLocation, int>();
        public Dictionary<ResourceLocation, int> DefaultStateTable { get { return defaultStateTable; } }

        private readonly Dictionary<int, ResourceLocation> blocksTable = new Dictionary<int, ResourceLocation>();
        public Dictionary<int, ResourceLocation> BlocksTable { get { return blocksTable; } }

        private readonly Dictionary<int, BlockState> statesTable = new Dictionary<int, BlockState>();
        public Dictionary<int, BlockState> StatesTable { get { return statesTable; } }

        public readonly Dictionary<ResourceLocation, RenderType> RenderTypeTable = new();

        private readonly Dictionary<int, Func<AbstractWorld, Location, BlockState, float3>> blockColorRules = new();

        public float3 GetBlockColor(int stateId, AbstractWorld world, Location loc, BlockState state)
        {
            if (blockColorRules.ContainsKey(stateId))
                return blockColorRules[stateId].Invoke(world, loc, state);
            return BlockGeometry.DEFAULT_COLOR;
        } 

        public void PrepareData(string dataVersion, DataLoadFlag flag)
        {
            // Clean up first...
            statesTable.Clear();
            blocksTable.Clear();
            stateListTable.Clear();
            defaultStateTable.Clear();
            blockColorRules.Clear();
            RenderTypeTable.Clear();

            HashSet<int> knownStates = new HashSet<int>();

            string statesPath = PathHelper.GetExtraDataFile($"blocks-{dataVersion}.json");
            string listsPath  = PathHelper.GetExtraDataFile("block_lists.json");
            string colorsPath = PathHelper.GetExtraDataFile("block_colors.json");
            string renderTypePath = PathHelper.GetExtraDataFile("block_render_type.json");

            if (!File.Exists(statesPath) || !File.Exists(listsPath) || !File.Exists(colorsPath) || !File.Exists(renderTypePath))
            {
                Debug.LogWarning("Block data not complete!");
                flag.Finished = true;
                flag.Failed = true;
                return;
            }

            // First read special block lists...
            var lists = new Dictionary<string, HashSet<ResourceLocation>>
            {
                { "no_occlusion", new() },
                { "no_collision", new() },
                { "water_blocks", new() },
                { "always_fulls", new() },
                { "empty_blocks", new() }
            };

            Json.JSONData spLists = Json.ParseJson(File.ReadAllText(listsPath, Encoding.UTF8));
            foreach (var pair in lists)
            {
                if (spLists.Properties.ContainsKey(pair.Key))
                {
                    foreach (var block in spLists.Properties[pair.Key].DataArray)
                        pair.Value.Add(ResourceLocation.FromString(block.StringValue));
                }
            }

            // References for later use
            ResourceLocation lavaId   = new("lava");
            var noOcclusion = lists["no_occlusion"];
            var noCollision = lists["no_collision"];
            var waterBlocks = lists["water_blocks"];
            var alwaysFulls = lists["always_fulls"];
            var emptyBlocks = lists["empty_blocks"];

            // Then read block states...
            Json.JSONData palette = Json.ParseJson(File.ReadAllText(statesPath, Encoding.UTF8));
            Debug.Log("Reading block states from " + statesPath);

            foreach (KeyValuePair<string, Json.JSONData> item in palette.Properties)
            {
                ResourceLocation blockId = ResourceLocation.FromString(item.Key);

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

                    if (state.Properties.ContainsKey("default"))
                    {
                        if (state.Properties["default"].StringValue.ToLower() == "true")
                        {
                            defaultStateTable[blockId] = stateId;
                        }
                    }

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

                        statesTable[stateId] = new(blockId, props)
                        {
                            NoOcclusion = noOcclusion.Contains(blockId),
                            NoCollision = noCollision.Contains(blockId),
                            InWater = inWater,
                            InLava  = blockId == lavaId,
                            LikeAir = emptyBlocks.Contains(blockId),
                            FullCollider = alwaysFulls.Contains(blockId)
                        };
                    }
                    else
                    {
                        statesTable[stateId] = new(blockId)
                        {
                            NoOcclusion = noOcclusion.Contains(blockId),
                            NoCollision = noCollision.Contains(blockId),
                            InWater = waterBlocks.Contains(blockId),
                            InLava  = blockId == lavaId,
                            LikeAir = emptyBlocks.Contains(blockId),
                            FullCollider = alwaysFulls.Contains(blockId)
                        };
                    }
                }
            
                if (!defaultStateTable.ContainsKey(blockId)) // Default block state of this block is not specified
                {
                    var firstStateId = stateListTable[blockId].First();
                    defaultStateTable[blockId] = firstStateId;
                    Debug.LogWarning($"Default blockstate of {blockId} is not specified, using first state ({firstStateId})");
                }
            }

            Debug.Log($"{statesTable.Count} block states loaded.");

            // Load block color rules...
            Json.JSONData colorRules = Json.ParseJson(File.ReadAllText(colorsPath, Encoding.UTF8));

            if (colorRules.Properties.ContainsKey("dynamic"))
            {
                foreach (var dynamicRule in colorRules.Properties["dynamic"].Properties)
                {
                    var ruleName = dynamicRule.Key;

                    Func<AbstractWorld, Location, BlockState, float3> ruleFunc = ruleName switch {
                        "foliage"  => (world, loc, state) => world.GetFoliageColor(loc),
                        "grass"    => (world, loc, state) => world.GetGrassColor(loc),
                        "redstone" => (world, loc, state) => {
                            if (state.Properties.ContainsKey("power"))
                                return new(float.Parse(state.Properties["power"]) / 16F, 0F, 0F);
                            return BlockGeometry.DEFAULT_COLOR;
                        },

                        _         => (world, loc, state) => float3.zero
                    };

                    foreach (var block in dynamicRule.Value.DataArray)
                    {
                        var blockId = ResourceLocation.FromString(block.StringValue);

                        if (stateListTable.ContainsKey(blockId))
                        {
                            foreach (var stateId in stateListTable[blockId])
                            {
                                if (!blockColorRules.TryAdd(stateId, ruleFunc))
                                    Debug.LogWarning($"Failed to apply dynamic color rules to {blockId} ({stateId})!");
                            }
                        }
                        //else
                        //    Debug.LogWarning($"Applying dynamic color rules to undefined block {blockId}!");
                    }
                }
            }

            if (colorRules.Properties.ContainsKey("fixed"))
            {
                foreach (var fixedRule in colorRules.Properties["fixed"].Properties)
                {
                    var blockId = ResourceLocation.FromString(fixedRule.Key);

                    if (stateListTable.ContainsKey(blockId))
                    {
                        var fixedColor = VectorUtil.Json2Float3(fixedRule.Value) / 255F;
                        Func<AbstractWorld, Location, BlockState, float3> ruleFunc = (world, loc, state) => fixedColor;

                        foreach (var stateId in stateListTable[blockId])
                        {
                            if (!blockColorRules.TryAdd(stateId, ruleFunc))
                                Debug.LogWarning($"Failed to apply fixed color rules to {blockId} ({stateId})!");
                        }
                    }
                    //else
                    //    Debug.LogWarning($"Applying fixed color rules to undefined block {blockId}!");
                }
            }
            
            // Load and apply block render types...
            try
            {
                var renderTypeText = File.ReadAllText(renderTypePath);
                var renderTypes = Json.ParseJson(renderTypeText);

                var allBlockIds = stateListTable.Keys.ToHashSet();

                foreach (var pair in renderTypes.Properties)
                {
                    var blockId = ResourceLocation.FromString(pair.Key);

                    if (allBlockIds.Contains(blockId))
                    {
                        var type = pair.Value.StringValue.ToLower() switch
                        {
                            "solid"         => RenderType.SOLID,
                            "cutout"        => RenderType.CUTOUT,
                            "cutout_mipped" => RenderType.CUTOUT_MIPPED,
                            "translucent"   => RenderType.TRANSLUCENT,

                            _               => RenderType.SOLID
                        };

                        RenderTypeTable.Add(blockId, type);

                        allBlockIds.Remove(blockId);
                    }

                }

                foreach (var blockId in allBlockIds) // Other blocks which doesn't its render type specifically stated
                {
                    RenderTypeTable.Add(blockId, RenderType.SOLID); // Default to solid
                }

            }
            catch (IOException e)
            {
                Debug.LogWarning($"Failed to load block render types: {e.Message}");
                flag.Failed = true;
            }

            Debug.Log($"Render type of {RenderTypeTable.Count} blocks loaded.");

            flag.Finished = true;

        }

    }
}