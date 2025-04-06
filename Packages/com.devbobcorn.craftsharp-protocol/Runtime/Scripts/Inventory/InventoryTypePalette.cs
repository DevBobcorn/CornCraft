using System;
using System.Globalization;
using System.IO;
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

            var entityTypePath = PathHelper.GetExtraDataFile($"inventories{SP}inventory_types-{dataVersion}.json");

            if (!File.Exists(entityTypePath))
            {
                Debug.LogWarning("Inventory data not complete!");
                flag.Finished = true;
                flag.Failed = true;
                return;
            }

            try
            {
                var inventoryTypes = Json.ParseJson(File.ReadAllText(entityTypePath, Encoding.UTF8));

                foreach (var (key, inventoryDef) in inventoryTypes.Properties)
                {
                    if (int.TryParse(inventoryDef.Properties["protocol_id"].StringValue, out int numId))
                    {
                        var inventoryTypeId = ResourceLocation.FromString(key);

                        var p = inventoryDef.Properties.TryGetValue("prepend_slot_count", out var val) ? byte.Parse(val.StringValue) : (byte) 0;
                        var a = inventoryDef.Properties.TryGetValue("append_slot_count", out val) ? byte.Parse(val.StringValue) : (byte) 0;
                        
                        var w = inventoryDef.Properties.TryGetValue("main_slot_width", out val) ? byte.Parse(val.StringValue) : (byte) 0;
                        var h = inventoryDef.Properties.TryGetValue("main_slot_height", out val) ? byte.Parse(val.StringValue) : (byte) 0;
                        
                        var hb = !inventoryDef.Properties.TryGetValue("has_backpack_slots", out val) || bool.Parse(val.StringValue); // True if not specified
                        var hh = !inventoryDef.Properties.TryGetValue("has_hotbar_slots", out val) || bool.Parse(val.StringValue); // True if not specified
                        
                        var o = inventoryDef.Properties.TryGetValue("output_slot", out val) ? int.Parse(val.StringValue) : -1; // -1 means no output slot

                        AddEntry(inventoryTypeId, numId, new InventoryType(inventoryTypeId, p, w, h, hb, hh, a, o));
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
        }
    }
}
