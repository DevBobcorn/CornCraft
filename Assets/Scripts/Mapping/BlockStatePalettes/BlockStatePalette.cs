using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

using MinecraftClient.Rendering;

namespace MinecraftClient.Mapping.BlockStatePalettes
{
    public abstract class BlockStatePalette
    {
        public BlockStatePalette()
        {
            PrepareData();
        }

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

        private bool blockStatesReady = false, buzy = false;
        public bool BlockStatesReady
        {
            get {
                return blockStatesReady;
            }
        }

        private readonly Dictionary<ResourceLocation, HashSet<int>> stateListTable = new Dictionary<ResourceLocation, HashSet<int>>();
        public Dictionary<ResourceLocation, HashSet<int>> StateListTable { get { return stateListTable; } }

        private readonly Dictionary<int, ResourceLocation> blocksTable = new Dictionary<int, ResourceLocation>();
        public Dictionary<int, ResourceLocation> BlocksTable { get { return blocksTable; } }

        private readonly Dictionary<int, BlockState> statesTable = new Dictionary<int, BlockState>();
        public Dictionary<int, BlockState> StatesTable { get { return statesTable; } }

        private readonly Dictionary<int, RenderType> renderTypeTable = new Dictionary<int, RenderType>();
        public RenderType GetRenderType(int stateId)
        {
            return renderTypeTable.GetValueOrDefault(stateId, RenderType.SOLID);
        }

        protected abstract string GetBlockStatesFile();

        protected abstract string GetBlockListsFile();

        private void PrepareData()
        {
            if (buzy || blockStatesReady) return;

            buzy = true;

            // Clean up first...
            statesTable.Clear();
            blocksTable.Clear();
            stateListTable.Clear();

            HashSet<int> knownStates = new HashSet<int>();

            string statesPath = PathHelper.GetExtraDataFile(GetBlockStatesFile());
            string listsPath  = PathHelper.GetExtraDataFile(GetBlockListsFile());

            if (!File.Exists(statesPath) || !File.Exists(listsPath))
            {
                throw new FileNotFoundException("Block data not complete!");
            }

            // First read special block lists...
            var noOcclusion = new List<ResourceLocation>();
            var noCollision = new List<ResourceLocation>();
            var waterBlocks = new List<ResourceLocation>();
            var emptyBlocks = new List<ResourceLocation>();
            var alwaysFulls = new List<ResourceLocation>();

            Json.JSONData spLists = Json.ParseJson(File.ReadAllText(listsPath, Encoding.UTF8));
            Debug.Log("Reading special lists from " + listsPath);

            if (spLists.Properties.ContainsKey("no_occlusion"))
            {
                foreach (var block in spLists.Properties["no_occlusion"].DataArray)
                {
                    noOcclusion.Add(ResourceLocation.fromString(block.StringValue));
                }
            }

            if (spLists.Properties.ContainsKey("no_collision"))
            {
                foreach (var block in spLists.Properties["no_collision"].DataArray)
                {
                    noCollision.Add(ResourceLocation.fromString(block.StringValue));
                }
            }

            if (spLists.Properties.ContainsKey("water_blocks"))
            {
                foreach (var block in spLists.Properties["water_blocks"].DataArray)
                {
                    waterBlocks.Add(ResourceLocation.fromString(block.StringValue));
                }
            }

            if (spLists.Properties.ContainsKey("always_fulls"))
            {
                foreach (var block in spLists.Properties["always_fulls"].DataArray)
                {
                    alwaysFulls.Add(ResourceLocation.fromString(block.StringValue));
                }
            }

            emptyBlocks.Add(new ResourceLocation("air"));
            emptyBlocks.Add(new ResourceLocation("void_air"));
            emptyBlocks.Add(new ResourceLocation("cave_air"));
            emptyBlocks.Add(new ResourceLocation("structure_void"));
            emptyBlocks.Add(new ResourceLocation("light"));
            emptyBlocks.Add(new ResourceLocation("water"));
            emptyBlocks.Add(new ResourceLocation("barrier"));

            // Then read block states...
            Json.JSONData palette = Json.ParseJson(File.ReadAllText(statesPath, Encoding.UTF8));
            Debug.Log("Reading block states from " + statesPath);

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
                            LikeAir = emptyBlocks.Contains(blockId),
                            FullSolid = (!noOcclusion.Contains(blockId)) && alwaysFulls.Contains(blockId)
                        };
                    }

                }
            }

            Debug.Log($"{statesTable.Count} block states loaded.");

            LoadRenderTypes();
            Debug.Log($"Render type of {renderTypeTable.Count} blocks loaded.");

            blockStatesReady = true;
            buzy = false;

        }

        public void LoadRenderTypes()
        {
            renderTypeTable.Clear();
            
            // Load and apply block render types...
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

        }

    }
}