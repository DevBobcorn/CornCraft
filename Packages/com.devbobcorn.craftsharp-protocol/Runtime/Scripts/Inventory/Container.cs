#nullable enable
using System.Collections.Generic;

namespace CraftSharp.Inventory
{
    /// <summary>
    /// Represents a Minecraft inventory (player inventory, chest, etc.)
    /// </summary>
    public class Container
    {
        /// <summary>
        /// Id of the container on the server
        /// </summary>
        public int Id;

        /// <summary>
        /// Type of container
        /// </summary>
        public ContainerType Type;

        /// <summary>
        /// title of container
        /// </summary>
        public string? Title;

        /// <summary>
        /// state of container
        /// </summary>
        public int StateId;

        /// <summary>
        /// Container Items
        /// </summary>
        public Dictionary<int, ItemStack> Items;

        /// <summary>
        /// Create an empty container
        /// </summary>
        public Container()
        {
            Items = new Dictionary<int, ItemStack>();
        }

        /// <summary>
        /// Create an empty container with ID, Type and Title
        /// </summary>
        /// <param name="id">Container ID</param>
        /// <param name="type">Container Type</param>
        /// <param name="title">Container Title</param>
        public Container(int id, ContainerType type, string title)
        {
            Id = id;
            Type = type;
            Title = title;
            Items = new Dictionary<int, ItemStack>();
        }

        /// <summary>
        /// Create a container with ID, Type, Title and Items
        /// </summary>
        /// <param name="id">Container ID</param>
        /// <param name="type">Container Type</param>
        /// <param name="title">Container Title</param>
        /// <param name="items">Container Items (key: slot ID, value: item info)</param>
        public Container(int id, ContainerType type, string title, Dictionary<int, ItemStack> items)
        {
            Id = id;
            Type = type;
            Title = title;
            Items = items;
        }

        /// <summary>
        /// Create an empty container with ID, Type and Title
        /// </summary>
        /// <param name="id">Container ID</param>
        /// <param name="typeID">Container Type</param>
        /// <param name="title">Container Title</param>
        public Container(int id, int typeID, string title)
        {
            Id = id;
            Type = GetContainerType(typeID);
            Title = title;
            Items = new Dictionary<int, ItemStack>();
        }

        /// <summary>
        /// Create an empty container with Type
        /// </summary>
        /// <param name="type">Container Type</param>
        public Container(ContainerType type)
        {
            Id = -1;
            Type = type;
            Title = null;
            Items = new Dictionary<int, ItemStack>();
        }

        /// <summary>
        /// Create an empty container with T^ype and Items
        /// </summary>
        /// <param name="type">Container Type</param>
        /// <param name="items">Container Items (key: slot ID, value: item info)</param>
        public Container(ContainerType type, Dictionary<int, ItemStack> items)
        {
            Id = -1;
            Type = type;
            Title = null;
            Items = items;
        }

        /// <summary>
        /// Get container type from Type ID
        /// </summary>
        /// <param name="typeID">Container Type ID</param>
        /// <returns>Container Type</returns>
        public static ContainerType GetContainerType(int typeID)
        {
            // https://wiki.vg/Inventory didn't state the inventory ID, assume that list start with 0
            return typeID switch
            {
                0 => ContainerType.Generic_9x1,
                1 => ContainerType.Generic_9x2,
                2 => ContainerType.Generic_9x3,
                3 => ContainerType.Generic_9x4,
                4 => ContainerType.Generic_9x5,
                5 => ContainerType.Generic_9x6,
                6 => ContainerType.Generic_3x3,
                7 => ContainerType.Anvil,
                8 => ContainerType.Beacon,
                9 => ContainerType.BlastFurnace,
                10 => ContainerType.BrewingStand,
                11 => ContainerType.Crafting,
                12 => ContainerType.Enchantment,
                13 => ContainerType.Furnace,
                14 => ContainerType.Grindstone,
                15 => ContainerType.Hopper,
                16 => ContainerType.Lectern,
                17 => ContainerType.Loom,
                18 => ContainerType.Merchant,
                19 => ContainerType.ShulkerBox,
                20 => ContainerType.Smoker,
                21 => ContainerType.Cartography,
                22 => ContainerType.Stonecutter,
                _ => ContainerType.Unknown,
            };
        }

        /// <summary>
        /// Search an item in the container
        /// </summary>
        /// <param name="itemType">The item to search</param>
        /// <returns>An array of slot ID</returns>
        public int[] SearchItem(Item itemType)
        {
            List<int> result = new List<int>();
            if (Items != null)
            {
                foreach (var item in Items)
                {
                    if (item.Value.ItemType == itemType)
                        result.Add(item.Key);
                }
            }
            return result.ToArray();
        }

        /// <summary>
        /// List empty slots in the container
        /// </summary>
        /// <returns>An array of slot ID</returns>
        /// <remarks>Also depending on the container type, some empty slots cannot be used e.g. armor slots. This might cause issues.</remarks>
        public int[] GetEmpytSlots()
        {
            List<int> result = new List<int>();
            for (int i = 0; i < Type.SlotCount(); i++)
            {
                result.Add(i);
            }
            foreach (var item in Items)
            {
                result.Remove(item.Key);
            }
            return result.ToArray();
        }

        /// <summary>
        /// Get the slot ID of first hotbar slot in this container
        /// </summary>
        /// <returns>First hotbar slot in this container</returns>
        public int GetFirstHotbarSlot()
        {
            int hotbarStart = Type.SlotCount() - 9;
            // Remove offhand slot
            if (Type == ContainerType.PlayerInventory)
                hotbarStart--;
            
            return hotbarStart;
        }

        /// <summary>
        /// Check the given slot ID is a hotbar slot and give the hotbar number
        /// </summary>
        /// <param name="slotId">The slot ID to check</param>
        /// <param name="hotbar">Zero-based, 0-8. -1 if not a hotbar</param>
        /// <returns>True if given slot ID is a hotbar slot</returns>
        public bool IsHotbar(int slotId, out int hotbar)
        {
            int hotbarStart = GetFirstHotbarSlot();

            if ((slotId >= hotbarStart) && (slotId <= hotbarStart + 9))
            {
                hotbar = slotId - hotbarStart;
                return true;
            }
            else
            {
                hotbar = -1;
                return false;
            }
        }

        /// <summary>
        /// Check the given slot ID is a hotbar slot and give the hotbar number
        /// </summary>
        /// <param name="slot">Zero-based, 0-8</param>
        /// <returns>True if given slot ID is a hotbar slot</returns>
        public ItemStack? GetHotbarItem(short slot)
        {
            if (slot >= 0 && slot < 9)
            {
                var slotInContainer = slot + GetFirstHotbarSlot();

                if (Items.ContainsKey(slotInContainer))
                {
                    return Items[slotInContainer];
                }
            }

            return null;
        }
    }
}
