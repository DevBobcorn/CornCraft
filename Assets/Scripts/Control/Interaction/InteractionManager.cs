#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace CraftSharp.Control
{
    public class InteractionManager
    {
        public static readonly InteractionManager INSTANCE = new();

        private static readonly Dictionary<int, BlockInteractionDefinition> blockInteractionTable = new();

        public Dictionary<int, BlockInteractionDefinition> BlockInteractionTable => blockInteractionTable;

        public void PrepareData(DataLoadFlag flag)
        {
            // Block interactions
            string interactionPath = PathHelper.GetExtraDataFile("block_interaction.json");

            if (!File.Exists(interactionPath))
            {
                Debug.LogWarning("Block interaction definition not found!");
                flag.Finished = true;
                flag.Failed = true;
                return;
            }
            
            // Load block interaction definitions...
            blockInteractionTable.Clear();
            var interactions = Json.ParseJson(File.ReadAllText(interactionPath, Encoding.UTF8));

            var stateListTable = BlockStatePalette.INSTANCE.StateListTable;
            var statesTable = BlockStatePalette.INSTANCE.StatesTable;

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

                        var iconType = InteractionIconType.Dialog;
                        if (entryCont.ContainsKey("icon_type"))
                        {
                            iconType = entryCont["icon_type"].StringValue switch
                            {
                                "interact"       => InteractionIconType.Dialog,
                                "enter_location" => InteractionIconType.EnterLocation,
                                "item_icon"      => InteractionIconType.ItemIcon,
                                _                => InteractionIconType.Dialog
                            };
                        }
                        
                        var hint = entryCont["hint"].StringValue;
                        var predicate = BlockStatePredicate.FromString(entryCont["predicate"].StringValue);

                        var triggers = entryCont["triggers"].DataArray;

                        foreach (var trigger in triggers)
                        {
                            var blockId = ResourceLocation.FromString(trigger.StringValue);

                            if (stateListTable.ContainsKey(blockId))
                            {
                                foreach (var stateId in stateListTable[blockId])
                                {
                                    if (predicate.Check(statesTable[stateId]))
                                    {
                                        blockInteractionTable.Add(stateId, new(interactionType,
                                                iconType, blockId, $"special/{entryName}", hint));

                                        //Debug.Log($"Added {entryName} interaction for blockstate [{stateId}] {statesTable[stateId]}");
                                    }
                                }
                            }
                            //else
                            //    Debug.LogWarning($"Unknown interactable block {blockId}");
                        }

                    }
                    else
                    {
                        Debug.LogWarning($"Invalid special block interation definition: {entryName}");
                    }
                }
            }

            // TODO: Entity interactions

            flag.Finished = true;
        }
    }
}