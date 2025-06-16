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

        public Dictionary<int, InteractionDefinition> InteractionTable { get; } = new();

        private static readonly ResourceLocation defaultHarvestIconTypeId = ResourceLocation.FromString("corncraft:dialog");
        private static readonly string defaultHarvestTag = "special/default_harvest";
        private static readonly string defaultHarvestHintKey = "gameplay.interaction.default_harvest";

        public HarvestInteraction CreateDefaultHarvest(ResourceLocation blockId)
        {
            return new HarvestInteraction(defaultHarvestIconTypeId, blockId, ItemActionType.None, InteractionType.Break, defaultHarvestHintKey, defaultHarvestTag, false);
        }

        public void PrepareData(DataLoadFlag flag)
        {
            // Block interactions
            var interactionPath = PathHelper.GetExtraDataFile("block_interaction.json");

            if (!File.Exists(interactionPath))
            {
                Debug.LogWarning("Block interaction definition not found!");
                flag.Finished = true;
                flag.Failed = true;
                return;
            }

            InteractionTable.Clear();

            var interactions = Json.ParseJson(File.ReadAllText(interactionPath, Encoding.UTF8));

            var palette = BlockStatePalette.INSTANCE;

            if (interactions.Properties.TryGetValue("special", out var specialProperty))
            {
                var entries = specialProperty.Properties;

                foreach (var (entryName, value) in entries)
                {
                    var entryCont = value.Properties;

                    if (entryCont.TryGetValue("action", out var action) &&
                        entryCont.TryGetValue("triggers", out var triggers))
                    {
                        var interactionType = action.StringValue switch
                        {
                            "interact" => InteractionType.Interact,
                            "place" => InteractionType.Place,
                            "break" => InteractionType.Break,
                            _ => InteractionType.Interact
                        };

                        // Harvest interaction case
                        ItemActionType? itemActionType = entryCont.TryGetValue("item_action", out var itemAction)
                            ? itemAction.StringValue switch
                            {
                                "axe" => ItemActionType.Axe,
                                "hoe" => ItemActionType.Hoe,
                                "pickaxe" => ItemActionType.Pickaxe,
                                "shovel" => ItemActionType.Shovel,
                                _ => ItemActionType.None,
                            }
                            : null;

                        // Trigger interaction icon case
                        var iconTypeId = entryCont.TryGetValue("icon_type_id", out var icon) ?
                            ResourceLocation.FromString(icon.StringValue) : ResourceLocation.INVALID;

                        var hintKey = entryCont.TryGetValue("hint", out var hint) ? hint.StringValue : null;

                        var predicate = entryCont.TryGetValue("predicate", out var predicateData)
                            ? BlockStatePredicate.FromString(predicateData?.StringValue ?? string.Empty)
                            : BlockStatePredicate.EMPTY;

                        var reusable = entryCont.TryGetValue("reusable", out var reusableData)
                            && bool.Parse(reusableData?.StringValue!); // false if not specified

                        var showInList = !entryCont.TryGetValue("show_in_list", out var showInListData)
                            || bool.Parse(showInListData?.StringValue!); // true if not specified

                        foreach (var trigger in triggers.DataArray)
                        {
                            var blockId = ResourceLocation.FromString(trigger.StringValue);

                            if (palette.TryGetAllNumIds(blockId, out var stateIds, x => predicate.Check(x)))
                            {
                                foreach (var stateId in stateIds)
                                {
                                    var inters = new List<Interaction>();

                                    var tag = $"special/{entryName}";
                                    hintKey ??= trigger.StringValue;

                                    if (itemActionType is not null)
                                    {
                                        inters.Add(new HarvestInteraction(iconTypeId, blockId, itemActionType.Value, interactionType, hintKey, tag, showInList));
                                    }
                                    else
                                    {
                                        inters.Add(new TriggerInteraction(iconTypeId, blockId, reusable, interactionType, hintKey, tag, showInList));
                                    }

                                    if (InteractionTable.TryGetValue(stateId, out var definition))
                                    {
                                        definition.AddRange(inters);
                                    }
                                    else
                                    {
                                        InteractionTable.Add(stateId, new(inters));
                                    }

                                    //Debug.Log($"Added {entryName} interaction for blockstate [{stateId}] {palette.GetByNumId(stateId)}");
                                }
                            }
                            // else
                            //    Debug.LogWarning($"Unknown interactable block {blockId}");
                        }
                    }
                    // else
                    //    Debug.LogWarning($"Invalid special block interation definition: {entryName}");
                }
            }

            flag.Finished = true;
        }
    }
}