using System;
using CraftSharp.Inventory;
using UnityEngine;

namespace CraftSharp
{
    public class InventoryTypePalette : IdentifierPalette<InventoryType>
    {
        public static readonly InventoryTypePalette INSTANCE = new();
        protected override string Name => "InventoryType Palette";
        protected override InventoryType UnknownObject => InventoryType.DUMMY_INVENTORY_TYPE;

        /// <summary>
        /// Adds an inventory type directly into the registry.
        /// <br/>
        /// Currently inventory types are not data-driven, so
        /// all inventory types are registered this way.
        /// </summary>
        public void InjectInventoryType(int numId, ResourceLocation identifier, InventoryType type)
        {
            UnfreezeEntries();

            AddEntry(identifier, numId, type);

            FreezeEntries();
        }

        /// <summary>
        /// Load inventory data.
        /// </summary>
        /// <param name="dataVersion">Inventory data version</param>
        /// <param name="flag">Data load flag</param>
        public void PrepareData(string dataVersion, DataLoadFlag flag)
        {
            // Clear loaded stuff...
            ClearEntries();

            try
            {
                switch (dataVersion)
                {
                    case "1.16.5":
                    case "1.17.1":
                    case "1.18.2":
                    case "1.19.2":
                    case "1.19.3":
                        {
                            InjectInventoryType( 0, InventoryType.GENERIC_9x1_ID,   InventoryType.GENERIC_9x1);
                            InjectInventoryType( 1, InventoryType.GENERIC_9x2_ID,   InventoryType.GENERIC_9x2);
                            InjectInventoryType( 2, InventoryType.GENERIC_9x3_ID,   InventoryType.GENERIC_9x3);
                            InjectInventoryType( 3, InventoryType.GENERIC_9x4_ID,   InventoryType.GENERIC_9x4);
                            InjectInventoryType( 4, InventoryType.GENERIC_9x5_ID,   InventoryType.GENERIC_9x5);
                            InjectInventoryType( 5, InventoryType.GENERIC_9x6_ID,   InventoryType.GENERIC_9x6);
                            InjectInventoryType( 6, InventoryType.GENERIC_3x3_ID,   InventoryType.GENERIC_3x3);
                            InjectInventoryType( 7, InventoryType.ANVIL_ID,         InventoryType.ANVIL);
                            InjectInventoryType( 8, InventoryType.BEACON_ID,        InventoryType.BEACON);
                            InjectInventoryType( 9, InventoryType.BLAST_FURNACE_ID, InventoryType.BLAST_FURNACE);
                            InjectInventoryType(10, InventoryType.BREWING_STAND_ID, InventoryType.BREWING_STAND);
                            InjectInventoryType(11, InventoryType.CRAFTING_ID,      InventoryType.CRAFTING);
                            InjectInventoryType(12, InventoryType.ENCHANTMENT_ID,   InventoryType.ENCHANTMENT);
                            InjectInventoryType(13, InventoryType.FURNACE_ID,       InventoryType.FURNACE);
                            InjectInventoryType(14, InventoryType.GRINDSTONE_ID,    InventoryType.GRINDSTONE);
                            InjectInventoryType(15, InventoryType.HOPPER_ID,        InventoryType.HOPPER);
                            InjectInventoryType(16, InventoryType.LECTERN_ID,       InventoryType.LECTERN);
                            InjectInventoryType(17, InventoryType.LOOM_ID,          InventoryType.LOOM);
                            InjectInventoryType(18, InventoryType.MERCHANT_ID,      InventoryType.MERCHANT);
                            InjectInventoryType(19, InventoryType.SHULKER_BOX_ID,   InventoryType.SHULKER_BOX);
                            InjectInventoryType(20, InventoryType.SMITHING_OLD_ID,  InventoryType.SMITHING_OLD); // Added in 1.16
                            InjectInventoryType(21, InventoryType.SMOKER_ID,        InventoryType.SMOKER);
                            InjectInventoryType(22, InventoryType.CARTOGRAPHY_ID,   InventoryType.CARTOGRAPHY);
                            InjectInventoryType(23, InventoryType.STONECUTTER_ID,   InventoryType.STONECUTTER);
                        }
                        break;
                    case "1.19.4":
                        {
                            InjectInventoryType( 0, InventoryType.GENERIC_9x1_ID,   InventoryType.GENERIC_9x1);
                            InjectInventoryType( 1, InventoryType.GENERIC_9x2_ID,   InventoryType.GENERIC_9x2);
                            InjectInventoryType( 2, InventoryType.GENERIC_9x3_ID,   InventoryType.GENERIC_9x3);
                            InjectInventoryType( 3, InventoryType.GENERIC_9x4_ID,   InventoryType.GENERIC_9x4);
                            InjectInventoryType( 4, InventoryType.GENERIC_9x5_ID,   InventoryType.GENERIC_9x5);
                            InjectInventoryType( 5, InventoryType.GENERIC_9x6_ID,   InventoryType.GENERIC_9x6);
                            InjectInventoryType( 6, InventoryType.GENERIC_3x3_ID,   InventoryType.GENERIC_3x3);
                            InjectInventoryType( 7, InventoryType.ANVIL_ID,         InventoryType.ANVIL);
                            InjectInventoryType( 8, InventoryType.BEACON_ID,        InventoryType.BEACON);
                            InjectInventoryType( 9, InventoryType.BLAST_FURNACE_ID, InventoryType.BLAST_FURNACE);
                            InjectInventoryType(10, InventoryType.BREWING_STAND_ID, InventoryType.BREWING_STAND);
                            InjectInventoryType(11, InventoryType.CRAFTING_ID,      InventoryType.CRAFTING);
                            InjectInventoryType(12, InventoryType.ENCHANTMENT_ID,   InventoryType.ENCHANTMENT);
                            InjectInventoryType(13, InventoryType.FURNACE_ID,       InventoryType.FURNACE);
                            InjectInventoryType(14, InventoryType.GRINDSTONE_ID,    InventoryType.GRINDSTONE);
                            InjectInventoryType(15, InventoryType.HOPPER_ID,        InventoryType.HOPPER);
                            InjectInventoryType(16, InventoryType.LECTERN_ID,       InventoryType.LECTERN);
                            InjectInventoryType(17, InventoryType.LOOM_ID,          InventoryType.LOOM);
                            InjectInventoryType(18, InventoryType.MERCHANT_ID,      InventoryType.MERCHANT);
                            InjectInventoryType(19, InventoryType.SHULKER_BOX_ID,   InventoryType.SHULKER_BOX);
                            InjectInventoryType(20, InventoryType.SMITHING_OLD_ID,  InventoryType.SMITHING_OLD); // Added in 1.16
                            InjectInventoryType(21, InventoryType.SMITHING_ID,      InventoryType.SMITHING); // Added in 1.19.4
                            InjectInventoryType(22, InventoryType.SMOKER_ID,        InventoryType.SMOKER);
                            InjectInventoryType(23, InventoryType.CARTOGRAPHY_ID,   InventoryType.CARTOGRAPHY);
                            InjectInventoryType(24, InventoryType.STONECUTTER_ID,   InventoryType.STONECUTTER);
                        }
                        break;
                    case "1.20.1":
                    case "1.20.2":
                        {
                            InjectInventoryType( 0, InventoryType.GENERIC_9x1_ID,   InventoryType.GENERIC_9x1);
                            InjectInventoryType( 1, InventoryType.GENERIC_9x2_ID,   InventoryType.GENERIC_9x2);
                            InjectInventoryType( 2, InventoryType.GENERIC_9x3_ID,   InventoryType.GENERIC_9x3);
                            InjectInventoryType( 3, InventoryType.GENERIC_9x4_ID,   InventoryType.GENERIC_9x4);
                            InjectInventoryType( 4, InventoryType.GENERIC_9x5_ID,   InventoryType.GENERIC_9x5);
                            InjectInventoryType( 5, InventoryType.GENERIC_9x6_ID,   InventoryType.GENERIC_9x6);
                            InjectInventoryType( 6, InventoryType.GENERIC_3x3_ID,   InventoryType.GENERIC_3x3);
                            InjectInventoryType( 7, InventoryType.ANVIL_ID,         InventoryType.ANVIL);
                            InjectInventoryType( 8, InventoryType.BEACON_ID,        InventoryType.BEACON);
                            InjectInventoryType( 9, InventoryType.BLAST_FURNACE_ID, InventoryType.BLAST_FURNACE);
                            InjectInventoryType(10, InventoryType.BREWING_STAND_ID, InventoryType.BREWING_STAND);
                            InjectInventoryType(11, InventoryType.CRAFTING_ID,      InventoryType.CRAFTING);
                            InjectInventoryType(12, InventoryType.ENCHANTMENT_ID,   InventoryType.ENCHANTMENT);
                            InjectInventoryType(13, InventoryType.FURNACE_ID,       InventoryType.FURNACE);
                            InjectInventoryType(14, InventoryType.GRINDSTONE_ID,    InventoryType.GRINDSTONE);
                            InjectInventoryType(15, InventoryType.HOPPER_ID,        InventoryType.HOPPER);
                            InjectInventoryType(16, InventoryType.LECTERN_ID,       InventoryType.LECTERN);
                            InjectInventoryType(17, InventoryType.LOOM_ID,          InventoryType.LOOM);
                            InjectInventoryType(18, InventoryType.MERCHANT_ID,      InventoryType.MERCHANT);
                            InjectInventoryType(19, InventoryType.SHULKER_BOX_ID,   InventoryType.SHULKER_BOX);
                            InjectInventoryType(20, InventoryType.SMITHING_ID,      InventoryType.SMITHING); // Added in 1.19.4
                            InjectInventoryType(21, InventoryType.SMOKER_ID,        InventoryType.SMOKER);
                            InjectInventoryType(22, InventoryType.CARTOGRAPHY_ID,   InventoryType.CARTOGRAPHY);
                            InjectInventoryType(23, InventoryType.STONECUTTER_ID,   InventoryType.STONECUTTER);
                        }
                        break;
                    case "1.20.4":
                    case "1.20.6":
                        {
                            InjectInventoryType( 0, InventoryType.GENERIC_9x1_ID,   InventoryType.GENERIC_9x1);
                            InjectInventoryType( 1, InventoryType.GENERIC_9x2_ID,   InventoryType.GENERIC_9x2);
                            InjectInventoryType( 2, InventoryType.GENERIC_9x3_ID,   InventoryType.GENERIC_9x3);
                            InjectInventoryType( 3, InventoryType.GENERIC_9x4_ID,   InventoryType.GENERIC_9x4);
                            InjectInventoryType( 4, InventoryType.GENERIC_9x5_ID,   InventoryType.GENERIC_9x5);
                            InjectInventoryType( 5, InventoryType.GENERIC_9x6_ID,   InventoryType.GENERIC_9x6);
                            InjectInventoryType( 6, InventoryType.GENERIC_3x3_ID,   InventoryType.GENERIC_3x3);
                            InjectInventoryType( 7, InventoryType.CRAFTER_3x3_ID,   InventoryType.CRAFTER_3x3); // Added in 1.20.3
                            InjectInventoryType( 8, InventoryType.ANVIL_ID,         InventoryType.ANVIL);
                            InjectInventoryType( 9, InventoryType.BEACON_ID,        InventoryType.BEACON);
                            InjectInventoryType(10, InventoryType.BLAST_FURNACE_ID, InventoryType.BLAST_FURNACE);
                            InjectInventoryType(11, InventoryType.BREWING_STAND_ID, InventoryType.BREWING_STAND);
                            InjectInventoryType(12, InventoryType.CRAFTING_ID,      InventoryType.CRAFTING);
                            InjectInventoryType(13, InventoryType.ENCHANTMENT_ID,   InventoryType.ENCHANTMENT);
                            InjectInventoryType(14, InventoryType.FURNACE_ID,       InventoryType.FURNACE);
                            InjectInventoryType(15, InventoryType.GRINDSTONE_ID,    InventoryType.GRINDSTONE);
                            InjectInventoryType(16, InventoryType.HOPPER_ID,        InventoryType.HOPPER);
                            InjectInventoryType(17, InventoryType.LECTERN_ID,       InventoryType.LECTERN);
                            InjectInventoryType(18, InventoryType.LOOM_ID,          InventoryType.LOOM);
                            InjectInventoryType(19, InventoryType.MERCHANT_ID,      InventoryType.MERCHANT);
                            InjectInventoryType(20, InventoryType.SHULKER_BOX_ID,   InventoryType.SHULKER_BOX);
                            InjectInventoryType(21, InventoryType.SMITHING_ID,      InventoryType.SMITHING); // Added in 1.19.4
                            InjectInventoryType(22, InventoryType.SMOKER_ID,        InventoryType.SMOKER);
                            InjectInventoryType(23, InventoryType.CARTOGRAPHY_ID,   InventoryType.CARTOGRAPHY);
                            InjectInventoryType(24, InventoryType.STONECUTTER_ID,   InventoryType.STONECUTTER);
                        }
                        break;
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
