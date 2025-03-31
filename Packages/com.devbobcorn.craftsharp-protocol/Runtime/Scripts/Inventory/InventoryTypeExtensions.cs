namespace CraftSharp.Inventory
{
    public static class InventoryTypeExtensions
    {
        /// <summary>
        /// Get the slot count of the inventory
        /// </summary>
        /// <param name="c"></param>
        /// <returns>Slot count of the inventory</returns>
        public static int SlotCount(this InventoryType c)
        {
            return c switch
            {
                InventoryType.PlayerInventory => 46,
                InventoryType.Generic_9x3 => 63,
                InventoryType.Generic_9x6 => 90,
                InventoryType.Generic_3x3 => 45,
                InventoryType.Crafting => 46,
                InventoryType.BlastFurnace => 39,
                InventoryType.Furnace => 39,
                InventoryType.Smoker => 39,
                InventoryType.Enchantment => 38,
                InventoryType.BrewingStand => 41,
                InventoryType.Merchant => 39,
                InventoryType.Beacon => 37,
                InventoryType.Anvil => 39,
                InventoryType.Hopper => 41,
                InventoryType.ShulkerBox => 63,
                InventoryType.Loom => 40,
                InventoryType.Stonecutter => 38,
                InventoryType.Lectern => 37,
                InventoryType.Cartography => 39,
                InventoryType.Grindstone => 39,
                InventoryType.Unknown => 0,
                _ => 0
            };
        }
    }
}
