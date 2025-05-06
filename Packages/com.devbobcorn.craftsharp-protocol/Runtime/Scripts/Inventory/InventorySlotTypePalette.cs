using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace CraftSharp.Inventory
{
    public class InventorySlotTypePalette : IdentifierPalette<InventorySlotType>
    {
        public static readonly InventorySlotTypePalette INSTANCE = new();
        protected override string Name => "InventorySlotType Palette";
        protected override InventorySlotType UnknownObject => InventorySlotType.DUMMY_INVENTORY_SLOT_TYPE;

        private static readonly Func<ItemStack, bool> ALWAYS_PREDICATE = _ => true;
        private static readonly Func<ItemStack, bool> NEVER_PREDICATE = _ => false;

        // Builtin inventory types
        public InventoryType PLAYER { get; private set; } = InventoryType.DUMMY_INVENTORY_TYPE;
        public InventoryType HORSE_REGULAR { get; private set; } = InventoryType.DUMMY_INVENTORY_TYPE;
        public InventoryType HORSE_CHESTED { get; private set; } = InventoryType.DUMMY_INVENTORY_TYPE;

        /// <summary>
        /// Called after inventory slot types are loaded
        /// </summary>
        private void RegisterBuiltinInventoryTypes()
        {
            PLAYER = new(InventoryType.PLAYER_ID,
                9, 0, 0, true, true, 1,
                new()
                {
                    [0] = new(8, 2, getType(InventorySlotType.SLOT_TYPE_OUTPUT_ID), null, null),
                    [1] = new(5, 2.5F, getType(InventorySlotType.SLOT_TYPE_REGULAR_ID), null, null),
                    [2] = new(6, 2.5F, getType(InventorySlotType.SLOT_TYPE_REGULAR_ID), null, null),
                    [3] = new(5, 1.5F, getType(InventorySlotType.SLOT_TYPE_REGULAR_ID), null, null),
                    [4] = new(6, 1.5F, getType(InventorySlotType.SLOT_TYPE_REGULAR_ID), null, null),
                    [5] = new(0, 3, getType(InventorySlotType.SLOT_TYPE_HEAD_ID), null,
                        ResourceLocation.FromString("corncraft:empty_armor_slot_helmet")),
                    [6] = new(0, 2, getType(InventorySlotType.SLOT_TYPE_CHEST_ID), null,
                        ResourceLocation.FromString("corncraft:empty_armor_slot_chestplate")),
                    [7] = new(0, 1, getType(InventorySlotType.SLOT_TYPE_LEGS_ID), null,
                        ResourceLocation.FromString("corncraft:empty_armor_slot_leggings")),
                    [8] = new(0, 0, getType(InventorySlotType.SLOT_TYPE_FEET_ID), null,
                        ResourceLocation.FromString("corncraft:empty_armor_slot_boots")),

                    [45] = new(4, 0, getType(InventorySlotType.SLOT_TYPE_OFFHAND_ID), null,
                        ResourceLocation.FromString("corncraft:empty_armor_slot_shield"))
                },
                new()
                {
                    new(7, 2, 1, 1, ResourceLocation.FromString("corncraft:arrow"))
                })
            {
                WorkPanelHeight = 4
            };

            HORSE_REGULAR = new(InventoryType.HORSE_ID,
                2, 0, 0, true, true, 0,
                new()
                {
                    [0] = new(0, 2, getType(InventorySlotType.SLOT_TYPE_HORSE_ARMOR_ID), null, null),
                    [1] = new(0, 2, getType(InventorySlotType.SLOT_TYPE_SADDLE_ID), null, null)
                },
                new())
            {
                MainPosX = 4,
                MainPosY = 0,
            };
            
            HORSE_CHESTED = new(InventoryType.HORSE_ID,
                2, 5, 3, true, true, 0,
                new()
                {
                    [0] = new(0, 2, getType(InventorySlotType.SLOT_TYPE_HORSE_ARMOR_ID), null, null),
                    [1] = new(0, 2, getType(InventorySlotType.SLOT_TYPE_SADDLE_ID), null, null)
                },
                new())
            {
                MainPosX = 4,
                MainPosY = 0
            };

            return;
            
            static InventorySlotType getType(ResourceLocation typeId)
            {
                return InventorySlotTypePalette.INSTANCE.GetById(typeId);
            }
        }

        /// <summary>
        /// Load inventory slot data from external files.
        /// </summary>
        /// <param name="flag">Data load flag</param>
        public void PrepareData(DataLoadFlag flag)
        {
            // Clear loaded stuff...
            ClearEntries();

            var inventorySlotTypePath = PathHelper.GetExtraDataFile($"inventory_slot_types.json");

            if (!File.Exists(inventorySlotTypePath))
            {
                Debug.LogWarning("Inventory slot data not complete!");
                flag.Finished = true;
                flag.Failed = true;
                return;
            }

            try
            {
                var inventorySlotTypes = Json.ParseJson(File.ReadAllText(inventorySlotTypePath, Encoding.UTF8));

                int numId = 0; // Numeral id doesn't matter, it's a client-side only thing

                foreach (var (key, inventorySlotDef) in inventorySlotTypes.Properties)
                {
                    var inventorySlotTypeId = ResourceLocation.FromString(key);
                    var interactable = !inventorySlotDef.Properties.TryGetValue("interactable", out var val) || bool.Parse(val.StringValue); // True if not specified

                    Debug.Log($"{inventorySlotTypeId} interactable: {interactable}");

                    var placePredicateStr = inventorySlotDef.Properties.TryGetValue("place_predicate", out val) ? val.StringValue : "never";
                    
                    Func<ItemStack, bool> placePredicate = placePredicateStr switch
                    {
                        "always" => ALWAYS_PREDICATE,
                        "never" => NEVER_PREDICATE,
                        _ => ItemStackPredicate.FromString(placePredicateStr).Check
                    };

                    var t = new InventorySlotType(inventorySlotTypeId, interactable, placePredicate);
                    
                    AddEntry(inventorySlotTypeId, numId++, t);
                }
                
                // Register builtin inventory types
                RegisterBuiltinInventoryTypes();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error loading inventory slot types: {e.Message}");
                flag.Failed = true;
            }
            finally
            {
                FreezeEntries();
                flag.Finished = true;
            }
        }

    }
}