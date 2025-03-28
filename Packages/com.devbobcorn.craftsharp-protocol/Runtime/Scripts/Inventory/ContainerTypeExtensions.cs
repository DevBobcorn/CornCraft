namespace CraftSharp.Inventory
{
    public static class ContainerTypeExtensions
    {
        /// <summary>
        /// Get the slot count of the container
        /// </summary>
        /// <param name="c"></param>
        /// <returns>Slot count of the container</returns>
        public static int SlotCount(this ContainerType c)
        {
            return c switch
            {
                ContainerType.PlayerInventory => 46,
                ContainerType.Generic_9x3 => 63,
                ContainerType.Generic_9x6 => 90,
                ContainerType.Generic_3x3 => 45,
                ContainerType.Crafting => 46,
                ContainerType.BlastFurnace => 39,
                ContainerType.Furnace => 39,
                ContainerType.Smoker => 39,
                ContainerType.Enchantment => 38,
                ContainerType.BrewingStand => 41,
                ContainerType.Merchant => 39,
                ContainerType.Beacon => 37,
                ContainerType.Anvil => 39,
                ContainerType.Hopper => 41,
                ContainerType.ShulkerBox => 63,
                ContainerType.Loom => 40,
                ContainerType.Stonecutter => 38,
                ContainerType.Lectern => 37,
                ContainerType.Cartography => 39,
                ContainerType.Grindstone => 39,
                ContainerType.Unknown => 0,
                _ => 0
            };
        }
    }
}
