#nullable enable
using System.Collections.Generic;

namespace CraftSharp.Inventory
{
    /// <summary>
    /// Represents a Minecraft inventory (player inventory, chest, etc.)
    /// </summary>
    public class InventoryData
    {
        /// <summary>
        /// Id of the inventory on the server
        /// </summary>
        public readonly int Id;

        /// <summary>
        /// Type of inventory
        /// </summary>
        public readonly InventoryType Type;

        /// <summary>
        /// Title of inventory
        /// </summary>
        public readonly string? Title;

        /// <summary>
        /// State of inventory
        /// </summary>
        public int StateId;

        /// <summary>
        /// Inventory items
        /// </summary>
        public Dictionary<int, ItemStack> Items;

        /// <summary>
        /// Inventory properties
        /// Used for Furnaces, Enchanting Table, Beacon, Brewing stand, Stone cutter, Loom and Lectern
        /// </summary>
        public readonly Dictionary<int, short> Properties;

        /// <summary>
        /// Create an empty inventory with Id, Type and Title
        /// </summary>
        /// <param name="id">Inventory Id</param>
        /// <param name="type">Inventory Type</param>
        /// <param name="title">Inventory Title</param>
        public InventoryData(int id, InventoryType type, string title)
        {
            Id = id;
            Type = type;
            Title = title;
            Items = new Dictionary<int, ItemStack>();
            Properties = new Dictionary<int, short>();
        }

        /// <summary>
        /// Create a inventory with Id, Type, Title and Items
        /// </summary>
        /// <param name="id">Inventory Id</param>
        /// <param name="type">Inventory Type</param>
        /// <param name="title">Inventory Title</param>
        /// <param name="items">Inventory Items (key: slot Id, value: item info)</param>
        public InventoryData(int id, InventoryType type, string title, Dictionary<int, ItemStack> items)
        {
            Id = id;
            Type = type;
            Title = title;
            Items = items;
            Properties = new Dictionary<int, short>();
        }

        /// <summary>
        /// Create an empty inventory with Type
        /// </summary>
        /// <param name="type">Inventory Type</param>
        public InventoryData(InventoryType type)
        {
            Id = -1;
            Type = type;
            Title = null;
            Items = new Dictionary<int, ItemStack>();
            Properties = new Dictionary<int, short>();
        }

        /// <summary>
        /// Create an empty inventory with Type and Items
        /// </summary>
        /// <param name="type">Inventory Type</param>
        /// <param name="items">Inventory Items (key: slot Id, value: item info)</param>
        public InventoryData(InventoryType type, Dictionary<int, ItemStack> items)
        {
            Id = -1;
            Type = type;
            Title = null;
            Items = items;
            Properties = new Dictionary<int, short>();
        }

        /// <summary>
        /// Search an item in the inventory
        /// </summary>
        /// <param name="itemType">The item to search</param>
        /// <returns>An array of slot Id</returns>
        public int[] SearchItem(Item itemType)
        {
            var result = new List<int>();
            foreach (var item in Items)
            {
                if (item.Value.ItemType == itemType)
                    result.Add(item.Key);
            }
            return result.ToArray();
        }

        /// <summary>
        /// List empty slots in the inventory
        /// </summary>
        /// <returns>An array of slot Id</returns>
        /// <remarks>Also depending on the inventory type, some empty slots cannot be used e.g. armor slots. This might cause issues.</remarks>
        public int[] GetEmptySlots()
        {
            var result = new List<int>();
            for (int i = 0; i < Type.SlotCount; i++)
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
        /// Get the slot Id of first hotbar slot in this inventory
        /// </summary>
        /// <returns>First hotbar slot in this inventory</returns>
        public int GetFirstHotbarSlot()
        {
            // Reduce hotbar count and append slot count
            int hotbarStart = Type.SlotCount - 9 - Type.AppendSlotCount;
            
            return hotbarStart;
        }

        /// <summary>
        /// Check the given slot Id is a hotbar slot and give the hotbar number
        /// </summary>
        /// <param name="slotId">The slot Id to check</param>
        /// <param name="hotbar">Zero-based, 0-8. -1 if not a hotbar</param>
        /// <returns>True if given slot Id is a hotbar slot</returns>
        public bool IsHotbar(int slotId, out int hotbar)
        {
            int hotbarStart = GetFirstHotbarSlot();

            if (slotId >= hotbarStart && slotId <= hotbarStart + 9)
            {
                hotbar = slotId - hotbarStart;
                return true;
            }
            
            hotbar = -1;
            return false;
        }

        /// <summary>
        /// Check if the given slot Id is a hotbar slot and return the hotbar number
        /// </summary>
        /// <param name="slot">Zero-based, 0-8</param>
        /// <returns>True if given slot Id is a hotbar slot</returns>
        public ItemStack? GetHotbarItem(short slot)
        {
            if (slot is >= 0 and < 9)
            {
                var slotInInventory = slot + GetFirstHotbarSlot();

                if (Items.TryGetValue(slotInInventory, out var item))
                {
                    return item;
                }
            }

            return null;
        }
    }
}
