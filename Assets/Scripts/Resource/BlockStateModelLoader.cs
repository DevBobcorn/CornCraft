using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace CraftSharp.Resource
{
    public class BlockStateModelLoader
    {
        private readonly ResourcePackManager manager;

        public BlockStateModelLoader(ResourcePackManager manager)
        {
            this.manager = manager;
        }

        public void LoadBlockStateModel(ResourceLocation blockId, string path, RenderType renderType)
        {
            if (File.Exists(path))
            {
                Json.JSONData stateData = Json.ParseJson(File.ReadAllText(path));

                if (stateData.Properties.ContainsKey("variants"))
                {
                    //Debug.Log("Load variant state model: " + blockId.ToString());
                    LoadVariantsFormat(stateData.Properties["variants"].Properties, blockId, renderType, manager);
                }
                else if (stateData.Properties.ContainsKey("multipart"))
                {
                    //Debug.Log("Load multipart state model: " + blockId.ToString());
                    LoadMultipartFormat(stateData.Properties["multipart"].DataArray, blockId, renderType, manager);
                }
                else
                    Debug.LogWarning("Invalid state model file: " + path);

            }
            else
                Debug.LogWarning("Cannot find block state model file: " + path);
        }

        private void LoadVariantsFormat(Dictionary<string, Json.JSONData> variants, ResourceLocation blockId, RenderType renderType, ResourcePackManager manager)
        {
            foreach (var variant in variants)
            {
                var conditions = BlockStatePredicate.FromString(variant.Key);

                // Block states can contain properties don't make a difference to their block geometry list
                // In this way they can share a single copy of geometry list...
                List<BlockGeometry> results = new List<BlockGeometry>();
                if (variant.Value.Type == Json.JSONData.DataType.Array) // A list...
                {
                    foreach (var wrapperData in variant.Value.DataArray)
                    {
                        results.Add(new BlockGeometryBuilder(BlockModelWrapper.FromJson(manager, wrapperData)).Build());
                    }
                }
                else // Only a single item...
                {
                    results.Add(new BlockGeometryBuilder(BlockModelWrapper.FromJson(manager, variant.Value)).Build());
                }

                foreach (var stateId in BlockStatePalette.INSTANCE.StateListTable[blockId])
                {
                    // For every possible state of this block, select the states that belong
                    // to this variant and give them this geometry list to use...
                    if (!manager.StateModelTable.ContainsKey(stateId) && conditions.Check(BlockStatePalette.INSTANCE.StatesTable[stateId]))
                    {
                        // Then this block state belongs to the current variant...
                        manager.StateModelTable.Add(stateId, new(results, renderType));
                    }
                }
            }
        }

        private void LoadMultipartFormat(List<Json.JSONData> parts, ResourceLocation blockId, RenderType renderType, ResourcePackManager manager)
        {
            var buildersList = new Dictionary<int, BlockGeometryBuilder>();
            foreach (var stateId in BlockStatePalette.INSTANCE.StateListTable[blockId])
            {
                buildersList.Add(stateId, new BlockGeometryBuilder());
            }

            foreach (var part in parts)
            {
                // Check part validity...
                if (part.Properties.ContainsKey("apply"))
                {
                    // Prepare the part wrapper...
                    BlockModelWrapper partWrapper;
                    if (part.Properties["apply"].Type == Json.JSONData.DataType.Array)
                    {
                        // Don't really support a list here, just use the first value instead...
                        partWrapper = BlockModelWrapper.FromJson(manager, part.Properties["apply"].DataArray[0]);
                    }
                    else
                    {
                        partWrapper = BlockModelWrapper.FromJson(manager, part.Properties["apply"]);
                    }

                    if (part.Properties.ContainsKey("when"))
                    {
                        Json.JSONData whenData = part.Properties["when"];
                        if (whenData.Properties.ContainsKey("OR"))
                        {   // 'when.OR' contains multiple predicates...
                            foreach (var stateItem in buildersList) // For each state
                            {
                                int stateId = stateItem.Key;
                                // Check and apply...
                                bool apply = false;
                                // An array of predicates in the value of 'OR'
                                foreach (var conditionData in whenData.Properties["OR"].DataArray)
                                {
                                    if (BlockStatePredicate.FromJson(conditionData).Check(BlockStatePalette.INSTANCE.StatesTable[stateId]))
                                    {
                                        apply = true;
                                        break;
                                    }
                                }

                                if (apply) // Apply this part to the current state
                                    buildersList[stateId].AppendWrapper(partWrapper);
                            }
                        }
                        else // 'when' is only a single predicate...
                        {
                            foreach (var stateItem in buildersList) // For each state
                            {
                                int stateId = stateItem.Key;
                                // Check and apply...
                                if (BlockStatePredicate.FromJson(whenData).Check(BlockStatePalette.INSTANCE.StatesTable[stateId]))
                                    buildersList[stateId].AppendWrapper(partWrapper);
                            }
                        }
                    }
                    else // No predicate at all, apply anyway...
                    {
                        foreach (var stateItem in buildersList) // For each state
                            buildersList[stateItem.Key].AppendWrapper(partWrapper);
                    }
                }
            }

            // Get the table into manager...
            foreach (var resultItem in buildersList)
            {
                manager.StateModelTable.Add(resultItem.Key, new(new BlockGeometry[]{ resultItem.Value.Build() }.ToList(), renderType));
            }
        }
    }
}