using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

using MinecraftClient.Mapping;

namespace MinecraftClient.Resource
{
    public class BlockStateModelLoader
    {
        private readonly ResourcePackManager manager;

        public BlockStateModelLoader(ResourcePackManager manager)
        {
            this.manager = manager;
        }

        public void LoadBlockStateModel(ResourcePackManager manager, ResourceLocation blockId, string path)
        {
            if (File.Exists(path))
            {
                Json.JSONData stateData = Json.ParseJson(File.ReadAllText(path));

                if (stateData.Properties.ContainsKey("variants"))
                {
                    //Debug.Log("Load variant state model: " + blockId.ToString());
                    LoadVariantsFormat(stateData.Properties["variants"].Properties, blockId, manager);
                }
                else if (stateData.Properties.ContainsKey("multipart"))
                {
                    //Debug.Log("Load multipart state model: " + blockId.ToString());
                    LoadMultipartFormat(stateData.Properties["multipart"].DataArray, blockId, manager);
                }
                else
                {
                    Debug.LogWarning("Invalid state model file: " + path);
                }

            }
            else
            {
                Debug.LogWarning("Cannot find block state model file: " + path);
            }

        }

        private void LoadVariantsFormat(Dictionary<string, Json.JSONData> variants, ResourceLocation blockId, ResourcePackManager manager)
        {
            foreach (var variant in variants)
            {
                var conditions = BlockStatePredicate.fromString(variant.Key);

                // Block states can contain properties don't make a difference to their block geometry list
                // In this way they can share a single copy of geometry list...
                List<BlockGeometry> results = new List<BlockGeometry>();
                if (variant.Value.Type == Json.JSONData.DataType.Array) // A list...
                {
                    foreach (var wrapperData in variant.Value.DataArray)
                    {
                        results.Add(new BlockGeometry(BlockModelWrapper.fromJson(manager, wrapperData)).Finalize());
                    }
                }
                else // Only a single item...
                {
                    results.Add(new BlockGeometry(BlockModelWrapper.fromJson(manager, variant.Value)).Finalize());
                }

                foreach (var stateId in BlockStatePalette.INSTANCE.StateListTable[blockId])
                {
                    // For every possible state of this block, select the states that belong
                    // to this variant and give them this geometry list to use...
                    if (!manager.finalTable.ContainsKey(stateId) && conditions.check(BlockStatePalette.INSTANCE.StatesTable[stateId]))
                    {
                        // Then this block state belongs to the current variant...
                        manager.finalTable.Add(stateId, new BlockStateModel(results));

                    }

                }

            }

        }

        private void LoadMultipartFormat(List<Json.JSONData> parts, ResourceLocation blockId, ResourcePackManager manager)
        {
            Dictionary<int, BlockGeometry> resultsList = new Dictionary<int, BlockGeometry>();
            foreach (var stateId in BlockStatePalette.INSTANCE.StateListTable[blockId])
            {
                resultsList.Add(stateId, new BlockGeometry());
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
                        partWrapper = BlockModelWrapper.fromJson(manager, part.Properties["apply"].DataArray[0]);
                    }
                    else
                    {
                        partWrapper = BlockModelWrapper.fromJson(manager, part.Properties["apply"]);
                    }

                    if (part.Properties.ContainsKey("when"))
                    {
                        Json.JSONData whenData = part.Properties["when"];
                        if (whenData.Properties.ContainsKey("OR"))
                        {   // 'when.OR' contains multiple predicates...
                            foreach (var stateItem in resultsList) // For each state
                            {
                                int stateId = stateItem.Key;
                                // Check and apply...
                                bool apply = false;
                                // An array of predicates in the value of 'OR'
                                foreach (var conditionData in whenData.Properties["OR"].DataArray)
                                {
                                    if (BlockStatePredicate.fromJson(conditionData).check(BlockStatePalette.INSTANCE.StatesTable[stateId]))
                                    {
                                        apply = true;
                                        break;
                                    }
                                }

                                if (apply) // Apply this part to the current state
                                    resultsList[stateId].AppendWrapper(partWrapper);

                            }

                        }
                        else // 'when' is only a single predicate...
                        {
                            foreach (var stateItem in resultsList) // For each state
                            {
                                int stateId = stateItem.Key;
                                // Check and apply...
                                if (BlockStatePredicate.fromJson(whenData).check(BlockStatePalette.INSTANCE.StatesTable[stateId]))
                                    resultsList[stateId].AppendWrapper(partWrapper);

                            }
                        }
                    }
                    else // No predicate at all, apply anyway...
                    {
                        foreach (var stateItem in resultsList) // For each state
                            resultsList[stateItem.Key].AppendWrapper(partWrapper);

                    }

                }

            }

            // Get the table into manager...
            foreach (var resultItem in resultsList)
            {
                manager.finalTable.Add(resultItem.Key, new BlockStateModel(new BlockGeometry[]{ resultItem.Value.Finalize() }.ToList()));
            }

        }

    }

}
