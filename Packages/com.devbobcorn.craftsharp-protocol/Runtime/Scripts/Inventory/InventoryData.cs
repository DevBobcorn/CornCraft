#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

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
            /*
            return stackA.ItemType == stackB.ItemType &&
                   DictionaryUtil.DeepCompareDictionaries(stackA.NBT, stackB.NBT);
            */
            return stackA.ItemType == stackB.ItemType;
        }

        /// <summary>
        /// Search an item in the inventory
        /// </summary>
        /// <param name="itemType">The item to search</param>
        /// <returns>An array of slot Id</returns>
        public int[] SearchItem(Item itemType)
        {
            return (from x in Items where x.Value.ItemType == itemType select x.Key).ToArray();
        }
        
        /// <summary>
        /// Match an item stack in the inventory
        /// </summary>
        /// <param name="itemStack">The item stack to match</param>
        /// <returns>An array of slot Id</returns>
        public int[] MatchItemStack(ItemStack itemStack)
        {
            return (from x in Items where CheckStackable(x.Value, itemStack) select x.Key).ToArray();
        }
        
        /// <summary>
        /// Match an item stack in the inventory hotbar
        /// </summary>
        /// <param name="itemStack">The item stack to match</param>
        /// <returns>Hotbar Slot Id if found, -1 if not</returns>
        public short MatchItemStackInHotbar(ItemStack itemStack)
        {
            try
            {
                return (short) (Items.First(x => CheckStackable(x.Value, itemStack)).Key - GetFirstHotbarSlot());
            }
            catch (InvalidOperationException)
            {
                return -1;
            }
        }
        
        /// <summary>
        /// Match an item stack in the inventory hotbar
        /// </summary>
        /// <param name="itemStack">The item stack to match</param>
        /// <returns>(Hotbar + Backpack) Slot Id if found, -1 if not</returns>
        public short MatchItemStackInHotbarAndBackpack(ItemStack itemStack)
        {
            // 0-8: Hotbar slots (slots are 36-44 in player inventory)
            // 9-35: Backpack slots (slots are 9-35 in player inventory)
            var hotbarStart = GetFirstHotbarSlot();
            var backpackStart = GetFirstBackpackSlot();
                
            var range = Enumerable.Range(hotbarStart, 9)
                .Concat(Enumerable.Range(backpackStart, 27)).ToArray();

            for (short i = 0; i < 36; i++)
            {
                if (Items.TryGetValue(range[i], out var candidate) && CheckStackable(candidate, itemStack))
                {
                    return i;
                }
            }
            
            return -1;
        }

        /// <summary>
        /// List empty slots in the inventory
        /// </summary>
        /// <returns>An array of slot Id</returns>
        /// <remarks>Also depending on the inventory type, some empty slots cannot be used e.g. armor slots. This might cause issues.</remarks>
        public int[] GetEmptySlots(int start, int count)
        {
            return Enumerable.Range(start, count).Where(x => !Items.ContainsKey(x)).ToArray();
        }
        
        /// <summary>
        /// Get first empty hotbar slot, or first slot not containing an enchanted item, otherwise an empty slot in backpack
        /// </summary>
        /// <returns>Slot Id</returns>
        /// <remarks>See https://minecraft.wiki/w/Java_Edition_protocol?oldid=2772660#Pick_Item</remarks>
        public int GetSuitableSlotInHotbar(int currentHotbarSlot)
        {
            var hotbarStart = GetFirstHotbarSlot();
            
            var range = currentHotbarSlot == 0 ? Enumerable.Range(0, 9).ToList() :
                Enumerable.Range(currentHotbarSlot, 9 - currentHotbarSlot)
                    .Concat(Enumerable.Range(0, currentHotbarSlot)).ToList();

            try // Try get first empty slot, starting from current slot
            {
                return range.First(x => !Items.ContainsKey(hotbarStart + x));
            }
            catch (InvalidOperationException)
            {
                try // Try get first slot not containing an enchanted item, starting from current slot
                {
                    return range.First(x => !Items[hotbarStart + x].IsEnchanted);
                }
                catch (InvalidOperationException)
                {
                    return currentHotbarSlot;
                }
            }
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
