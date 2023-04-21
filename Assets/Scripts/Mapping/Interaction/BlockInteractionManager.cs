#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

using MinecraftClient.Protocol;

namespace MinecraftClient.Mapping
{
    public class BlockInteractionManager
    {
        public static readonly BlockInteractionManager INSTANCE = new();

        private static readonly Dictionary<int, BlockInteractionDefinition> interactionTable = new();

        public Dictionary<int, BlockInteractionDefinition> InteractionTable => interactionTable;

        public void PrepareData(DataLoadFlag flag)
        {
            string interactionPath = PathHelper.GetExtraDataFile("block_interaction.json");

            if (!File.Exists(interactionPath))
            {
                Debug.LogWarning("Block interaction definition not found!");
                flag.Finished = true;
                flag.Failed = true;
                return;
            }
            
            // Load block interaction definitions...
            interactionTable.Clear();
            var interactions = Json.ParseJson(File.ReadAllText(interactionPath, Encoding.UTF8));

            var stateListTable = BlockStatePalette.INSTANCE.StateListTable;
            var statesTable = BlockStatePalette.INSTANCE.StatesTable;

            if (interactions.Properties.ContainsKey("pickable"))
            {
                var entries = interactions.Properties["pickable"].DataArray;

                foreach (var entry in entries) {
                    var blockId = ResourceLocation.fromString(entry.StringValue);

                    if (stateListTable.ContainsKey(blockId)) {
                        foreach (var stateId in stateListTable[blockId]) {
                            interactionTable.Add(stateId, new(BlockInteractionType.Break, ChatParser.TranslateString($"block.{blockId.Namespace}.{blockId.Path}")));

                            //Debug.Log($"Added pickable interaction for blockstate [{stateId}] {statesTable[stateId]}");
                        }
                    }

                }
            }

            if (interactions.Properties.ContainsKey("special"))
            {
                var entries = interactions.Properties["special"].Properties;

                foreach (var entry in entries) {
                    string entryName = entry.Key;

                    var entryCont = entry.Value.Properties;

                    if (entryCont.ContainsKey("action") &&
                        entryCont.ContainsKey("hint") &&
                        entryCont.ContainsKey("predicate") &&
                        entryCont.ContainsKey("triggers"))
                    {
                        var interactionType = entryCont["action"].StringValue switch
                        {
                            "interact" => BlockInteractionType.Interact,
                            "break"    => BlockInteractionType.Break,
                            _          => BlockInteractionType.Interact
                        };

                        var hint = Translations.TryGet(entryCont["hint"].StringValue);
                        var predicate = BlockStatePredicate.fromString(entryCont["predicate"].StringValue);

                        var triggers = entryCont["triggers"].DataArray;

                        foreach (var trigger in triggers)
                        {
                            var blockId = ResourceLocation.fromString(trigger.StringValue);

                            if (stateListTable.ContainsKey(blockId))
                            {
                                foreach (var stateId in stateListTable[blockId])
                                {
                                    if (predicate.check(statesTable[stateId]))
                                    {
                                        interactionTable.Add(stateId, new(interactionType, hint));

                                        //Debug.Log($"Added {entryName} interaction for blockstate [{stateId}] {statesTable[stateId]}");

                                    }
                                }
                            }
                            else
                                Debug.LogWarning($"Unknown interactable block {blockId}");
                        }

                    }
                    else
                        Debug.LogWarning($"Invalid special block interation definition: {entryName}");
                }
            }

            flag.Finished = true;

        }
    }
}