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