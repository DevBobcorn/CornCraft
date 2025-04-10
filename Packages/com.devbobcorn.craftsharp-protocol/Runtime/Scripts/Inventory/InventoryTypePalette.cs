using System;
using System.Collections.Generic;
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

                        var slotInfo = new Dictionary<int, InventoryType.InventorySlotInfo>();

                        if (inventoryDef.Properties.TryGetValue("slots", out val))
                        {
                            foreach (var (key2, data) in val.Properties)
                            {
                                slotInfo[int.Parse(key2)] = getSlotInfo(data);
                            }
                        }

                        var t = new InventoryType(inventoryTypeId, p, w, h, hb, hh, a, slotInfo);
                        
                        if (inventoryDef.Properties.TryGetValue("sprites", out val))
                        {
                            foreach (var info in val.DataArray.Select(getSpriteInfo))
                            {
                                t.spriteInfo.Add(info);
                            }
                        }
                        
                        if (inventoryDef.Properties.TryGetValue("work_panel_height", out val))
                            t.WorkPanelHeight = int.Parse(val.StringValue);
                        
                        if (inventoryDef.Properties.TryGetValue("list_panel_width", out val))
                            t.ListPanelWidth = int.Parse(val.StringValue);
                        
                        if (inventoryDef.Properties.TryGetValue("main_pos_x", out val))
                            t.MainPosX = int.Parse(val.StringValue);
                        
                        if (inventoryDef.Properties.TryGetValue("main_pos_y", out val))
                            t.MainPosY = int.Parse(val.StringValue);
                        
                        AddEntry(inventoryTypeId, numId, t);
                    }
                    else
                    {
                        Debug.LogWarning($"Invalid numeral inventory type key [{key}]");
                    }
                }
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

            return;

            static InventoryType.InventorySlotInfo getSlotInfo(Json.JSONData data)
            {
                var typeStr = data.Properties.TryGetValue("type", out var val) ? val.StringValue : "regular";
                var type = typeStr switch
                {
                    "regular" => InventorySlotType.Regular,
                    "output" => InventorySlotType.Output,
                    "helmet" => InventorySlotType.Helmet,
                    "chestplate" => InventorySlotType.Chestplate,
                    "leggings" => InventorySlotType.Leggings,
                    "boots" => InventorySlotType.Boots,
                    "offhand" => InventorySlotType.Offhand,
                    "horse_armor" => InventorySlotType.HorseArmor,
                    "saddle" => InventorySlotType.Saddle,
                    "beacon_activation_item" => InventorySlotType.BeaconActivationItem,
                    "bottle" => InventorySlotType.Bottle,
                    "smithing_template" => InventorySlotType.SmithingTemplate,
                    
                    _ => InventorySlotType.Regular
                };
                
                var x = data.Properties.TryGetValue("pos_x", out val) ? int.Parse(val.StringValue) : 0;
                var y = data.Properties.TryGetValue("pos_y", out val) ? int.Parse(val.StringValue) : 0;

                return new(x, y, type);
            }
            
            static InventoryType.InventorySpriteInfo getSpriteInfo(Json.JSONData data)
            {
                var typeId = data.Properties.TryGetValue("type_id", out var val) ?
                    ResourceLocation.FromString(val.StringValue) : ResourceLocation.INVALID;
                
                var x = data.Properties.TryGetValue("pos_x", out val) ? int.Parse(val.StringValue) : 0;
                var y = data.Properties.TryGetValue("pos_y", out val) ? int.Parse(val.StringValue) : 0;
                var w = data.Properties.TryGetValue("width", out val) ? int.Parse(val.StringValue) : 1;
                var h = data.Properties.TryGetValue("height", out val) ? int.Parse(val.StringValue) : 1;

                return new(x, y, w, h, typeId);
            }
        }
    }
}
