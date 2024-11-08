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

        private static readonly Dictionary<int, TriggerInteractionDefinition> blockInteractionTable = new();

        private static readonly Dictionary<int, ToolInteractionDefinition> toolInteractionTable = new();

        public Dictionary<int, TriggerInteractionDefinition> BlockInteractionTable => blockInteractionTable;

        public Dictionary<int, ToolInteractionDefinition> ToolInteractionTable => toolInteractionTable;

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

            var palette = BlockStatePalette.INSTANCE;

            PrepareSpecialData(interactions, palette);

            PrepareToolData(interactions, palette);

            flag.Finished = true;
        }

        private void PrepareSpecialData(Json.JSONData interactions, BlockStatePalette palette)
        {
            if (interactions.Properties.TryGetValue("special", out var specialProperty))
            {
                var entries = specialProperty.Properties;

                foreach (var (entryName, value) in entries)
                {
                    var entryCont = value.Properties;

                    if (entryCont.ContainsKey("action") &&
                        entryCont.ContainsKey("hint") &&
                        entryCont.ContainsKey("predicate") &&
                        entryCont.ContainsKey("triggers"))
                    {
                        var interactionType = entryCont["action"].StringValue switch
                        {
                            "interact" => TriggerInteractionType.Interact,
                            "break"    => TriggerInteractionType.Break,
                            _          => TriggerInteractionType.Interact
                        };

                        var iconType = InteractionIconType.Dialog;
                        if (entryCont.TryGetValue("icon_type", out var type))
                        {
                            iconType = type.StringValue switch
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

                            if (palette.TryGetAllNumIds(blockId, out int[] stateIds, x => predicate.Check(x)))
                            {
                                foreach (var stateId in stateIds)
                                {
                                    blockInteractionTable.Add(stateId, new(interactionType, iconType, blockId, $"special/{entryName}", hint));
                                    //Debug.Log($"Added {entryName} interaction for blockstate [{stateId}] {palette.GetByNumId(stateId)}");
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
        }

        private void PrepareToolData(Json.JSONData interactions, BlockStatePalette palette)
        {
            if (interactions.Properties.TryGetValue("diggable", out var mineableProperty))
            {
                var entries = mineableProperty.Properties;

                foreach (var (entryName, value) in entries)
                {
                    ItemActionType actionType = entryName switch
                    {
                        "axe" => ItemActionType.Axe,
                        "hoe" => ItemActionType.Hoe,
                        "pickaxe" => ItemActionType.Pickaxe,
                        "shovel" => ItemActionType.Shovel,
                        _ => ItemActionType.None,
                    };

                    foreach (var type in value.DataArray)
                    {
                        var blockId = ResourceLocation.FromString(type.StringValue);

                        if (palette.TryGetAllNumIds(blockId, out int[] stateIds))
                        {
                            foreach (var stateId in stateIds)
                            {
                                toolInteractionTable.Add(stateId, new(actionType));
                                Debug.Log($"Added {entryName} best tool for blockstate [{stateId}] {palette.GetByNumId(stateId)}");
                            }
                        }
                    }
                }
            }
        }
    }
}