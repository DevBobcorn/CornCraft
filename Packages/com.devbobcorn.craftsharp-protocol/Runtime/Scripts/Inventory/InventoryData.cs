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
        public readonly Dictionary<int, ItemStack> Items;

        /// <summary>
        /// Inventory properties
        /// Used for Furnaces, Enchanting Table, Beacon, Brewing Stand, Stone Cutter, Loom and Lectern
        /// </summary>
        public readonly Dictionary<int, short> Properties;

        /// <summary>
        /// Create an empty inventory with Id, Type and Title
        /// </summary>
        /// <param name="id">Inventory Id</param>
        /// <param name="type">Inventory Type</param>
        /// <param name="title">Inventory Title</param>
        public InventoryData(int id, InventoryType type, string? title)
        {
            Id = id;
            Type = type;
            Title = title;
            Items = new Dictionary<int, ItemStack>();
            Properties = new Dictionary<int, short>();
        }
        
        public static bool CheckStackable(ItemStack stackA, ItemStack stackB)
        {
            return stackA.ItemType == stackB.ItemType &&
                   DictionaryUtil.DeepCompareDictionaries(stackA.NBT, stackB.NBT);
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
        /// Get the slot Id of first backpack slot in this inventory
        /// </summary>
        /// <returns>First backpack slot in this inventory</returns>
        public int GetFirstBackpackSlot()
        {
            // Reduce backpack count, hotbar count and append slot count
            int backpackStart = Type.SlotCount - 36 - Type.AppendSlotCount;
            
            return backpackStart;
        }
        
        /// <summary>
        /// Check the given slot Id is a backpack slot and give the backpack number
        /// </summary>
        /// <param name="slotId">The slot Id to check</param>
        /// <param name="backpack">Zero-based, 0-26. -1 if not a backpack</param>
        /// <returns>True if given slot Id is a backpack slot</returns>
        public bool IsBackpack(int slotId, out int backpack)
        {
            if (!Type.HasBackpackSlots)
            {
                backpack = -1;
                return false;
            }
            
            int backpackStart = GetFirstBackpackSlot();

            if (slotId >= backpackStart && slotId <= backpackStart + 27)
            {
                backpack = slotId - backpackStart;
                return true;
            }
            
            backpack = -1;
            return false;
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
            if (!Type.HasHotbarSlots)
            {
                hotbar = -1;
                return false;
            }
            
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
        
        public override string ToString()
        {
            return "Inventory " + Id + " (" + Type + ")";
        }
    }
}
