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

        private static readonly Dictionary<int, InteractionDefinition> interactionTable = new();

        public Dictionary<int, InteractionDefinition> InteractionTable => interactionTable;

        public InteractionDefinition? DefaultHarvestInteraction;

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

            interactionTable.Clear();

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
                            "place"    => InteractionType.Place,
                            "break"    => InteractionType.Break,
                            _          => InteractionType.Interact
                        };

                        // Harvest interaction case
                        ItemActionType? itemActionType = entryCont.TryGetValue("item_action", out var itemAction)
                            ? itemAction.StringValue switch
                            {
                                "axe"     => ItemActionType.Axe,
                                "hoe"     => ItemActionType.Hoe,
                                "pickaxe" => ItemActionType.Pickaxe,
                                "shovel"  => ItemActionType.Shovel,
                                _         => ItemActionType.None,
                            }
                            : null; 

                        // View interaction icon case
                        InteractionIconType iconType = entryCont.TryGetValue("icon_type", out var type)
                            ? type.StringValue switch
                            {
                                "interact"       => InteractionIconType.Dialog,
                                "enter_location" => InteractionIconType.EnterLocation,
                                "item_icon"      => InteractionIconType.ItemIcon,

                                _                => InteractionIconType.Dialog
                            }
                            : InteractionIconType.Dialog;

                        var hintKey = entryCont.TryGetValue("hint", out var hint) ? hint.StringValue : null;

                        var predictor = entryCont.TryGetValue("predicate", out var predicate)
                            ? BlockStatePredicate.FromString(predicate?.StringValue ?? string.Empty)
                            : BlockStatePredicate.EMPTY;

                        foreach (var trigger in triggers.DataArray)
                        {
                            var blockId = ResourceLocation.FromString(trigger.StringValue);

                            if (palette.TryGetAllNumIds(blockId, out var stateIds, x => predictor.Check(x)))
                            {
                                foreach (var stateId in stateIds)
                                {
                                    var inters = new List<Interaction>();

                                    var tag = $"special/{entryName}";
                                    hintKey ??= trigger.StringValue;

                                    if (itemActionType is not null)
                                    {
                                        inters.Add(new HarvestInteraction(itemActionType.Value, interactionType, hintKey, tag));
                                    }
                                    else
                                    {
                                        inters.Add(new ViewInteraction(iconType, blockId, interactionType, hintKey, tag));
                                    }

                                    if (interactionTable.TryGetValue(stateId, out var definition))
                                    {
                                        definition.AddRange(inters);
                                    }
                                    else
                                    {
                                        interactionTable.Add(stateId, new(inters));
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