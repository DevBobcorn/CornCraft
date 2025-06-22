using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CraftSharp.Inventory;
using UnityEngine;

namespace CraftSharp
{
    public class InventoryTypePalette : IdentifierPalette<InventoryType>
    {
        private static readonly char SP = Path.DirectorySeparatorChar;
        
        public static readonly InventoryTypePalette INSTANCE = new();
        protected override string Name => "InventoryType Palette";
        protected override InventoryType UnknownObject => InventoryType.DUMMY_INVENTORY_TYPE;

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
                new InventoryType.InventoryLayoutInfo(
                    new()
                    {
                        new(7, 2, 1, 1, ResourceLocation.FromString("corncraft:arrow"))
                    },
                    new()
                    {
                        [0] = new(8, 2, InventorySlotType.SLOT_TYPE_OUTPUT_ID, null, null),
                        [1] = new(5, 2.5F, InventorySlotType.SLOT_TYPE_REGULAR_ID, null, null),
                        [2] = new(6, 2.5F, InventorySlotType.SLOT_TYPE_REGULAR_ID, null, null),
                        [3] = new(5, 1.5F, InventorySlotType.SLOT_TYPE_REGULAR_ID, null, null),
                        [4] = new(6, 1.5F, InventorySlotType.SLOT_TYPE_REGULAR_ID, null, null),
                        [5] = new(0, 3, InventorySlotType.SLOT_TYPE_HEAD_ITEM_ID, null,
                            ResourceLocation.FromString("corncraft:empty_armor_slot_helmet")),
                        [6] = new(0, 2, InventorySlotType.SLOT_TYPE_CHEST_ITEM_ID, null,
                            ResourceLocation.FromString("corncraft:empty_armor_slot_chestplate")),
                        [7] = new(0, 1, InventorySlotType.SLOT_TYPE_LEGS_ITEM_ID, null,
                            ResourceLocation.FromString("corncraft:empty_armor_slot_leggings")),
                        [8] = new(0, 0, InventorySlotType.SLOT_TYPE_FEET_ITEM_ID, null,
                            ResourceLocation.FromString("corncraft:empty_armor_slot_boots")),

                        [45] = new(4, 0, InventorySlotType.SLOT_TYPE_OFFHAND_ITEM_ID, null,
                            ResourceLocation.FromString("corncraft:empty_armor_slot_shield"))
                    }, null, null, null
                ))
            {
                WorkPanelHeight = 4
            };

            HORSE_REGULAR = new(InventoryType.HORSE_ID,
                2, 0, 0, true, true, 0,
                new InventoryType.InventoryLayoutInfo(
                    null, new()
                    {
                        [0] = new(0, 2, InventorySlotType.SLOT_TYPE_HORSE_ARMOR_ID, null, null),
                        [1] = new(0, 2, InventorySlotType.SLOT_TYPE_SADDLE_ID, null, null)
                    }, null, null, null
                ))
            {
                MainPosX = 4,
                MainPosY = 0,
            };

            HORSE_CHESTED = new(InventoryType.HORSE_ID,
                2, 5, 3, true, true, 0,
                new InventoryType.InventoryLayoutInfo(
                    null, new()
                    {
                        [0] = new(0, 2, InventorySlotType.SLOT_TYPE_HORSE_ARMOR_ID, null, null),
                        [1] = new(0, 2, InventorySlotType.SLOT_TYPE_SADDLE_ID, null, null)
                    }, null, null, null
                ))
            {
                MainPosX = 4,
                MainPosY = 0
            };
        }

        /// <summary>
        /// Load inventory data from external files.
        /// </summary>
        /// <param name="dataVersion">Inventory data version</param>
        /// <param name="flag">Data load flag</param>
        public void PrepareData(string dataVersion, DataLoadFlag flag)
        {
            // Clear loaded stuff...
            ClearEntries();

            var inventoryTypePath = PathHelper.GetExtraDataFile($"inventories{SP}inventory_types-{dataVersion}.json");

            if (!File.Exists(inventoryTypePath))
            {
                Debug.LogWarning("Inventory data not complete!");
                flag.Finished = true;
                flag.Failed = true;
                return;
            }

            try
            {
                var inventoryTypes = Json.ParseJson(File.ReadAllText(inventoryTypePath, Encoding.UTF8));

                foreach (var (key, inventoryDef) in inventoryTypes.Properties)
                {
                    if (int.TryParse(inventoryDef.Properties["protocol_id"].StringValue, out int numId))
                    {
                        var inventoryTypeId = ResourceLocation.FromString(key);

                        var p = inventoryDef.Properties.TryGetValue("prepend_slot_count", out var val) ? int.Parse(val.StringValue) : 0;
                        var a = inventoryDef.Properties.TryGetValue("append_slot_count", out val) ? int.Parse(val.StringValue) : 0;
                        
                        var w = inventoryDef.Properties.TryGetValue("main_slot_width", out val) ? int.Parse(val.StringValue) : 0;
                        var h = inventoryDef.Properties.TryGetValue("main_slot_height", out val) ? int.Parse(val.StringValue) : 0;
                        
                        var hb = !inventoryDef.Properties.TryGetValue("has_backpack_slots", out val) || bool.Parse(val.StringValue); // True if not specified
                        var hh = !inventoryDef.Properties.TryGetValue("has_hotbar_slots", out val) || bool.Parse(val.StringValue); // True if not specified

                        var workPanelLayout = InventoryType.InventoryLayoutInfo.FromJson(inventoryDef);
                        
                        var t = new InventoryType(inventoryTypeId, p, w, h, hb, hh, a, workPanelLayout);
                        
                        if (inventoryDef.Properties.TryGetValue("work_panel_height", out val))
                            t.WorkPanelHeight = int.Parse(val.StringValue);
                        
                        if (inventoryDef.Properties.TryGetValue("list_panel_width", out val))
                            t.ListPanelWidth = int.Parse(val.StringValue);
                        
                        if (inventoryDef.Properties.TryGetValue("main_pos_x", out val))
                            t.MainPosX = int.Parse(val.StringValue);
                        
                        if (inventoryDef.Properties.TryGetValue("main_pos_y", out val))
                            t.MainPosY = int.Parse(val.StringValue);

                        if (inventoryDef.Properties.TryGetValue("properties", out val))
                        {
                            t.PropertyNames = val.Properties.ToDictionary(
                                x => int.Parse(x.Key), x => x.Value.StringValue);
                            t.PropertySlots = val.Properties.ToDictionary(
                                x => x.Value.StringValue, x => int.Parse(x.Key));
                        }
                        
                        AddEntry(inventoryTypeId, numId, t);
                    }
                    else
                    {
                        Debug.LogWarning($"Invalid numeral inventory type key [{key}]");
                    }
                }

                // Register builtin inventory types
                RegisterBuiltinInventoryTypes();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error loading inventory types: {e.Message}");
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
