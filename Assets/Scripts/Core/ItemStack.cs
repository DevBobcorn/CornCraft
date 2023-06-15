using System;
using System.Collections.Generic;
using System.Text;

using MinecraftClient.Mapping;

namespace MinecraftClient.Inventory
{
    /// <summary>
    /// Represents an item stack
    /// </summary>
    public class ItemStack
    {
        /// <summary>
        /// Item Type
        /// </summary>
        public Item Type;

        /// <summary>
        /// Item Count
        /// </summary>
        public int Count;

        #nullable enable
        /// <summary>
        /// Item Metadata
        /// </summary>
        public Dictionary<string, object>? NBT;

        /// <summary>
        /// Create an item with ItemType, Count and Metadata
        /// </summary>
        /// <param name="itemType">Type of the item</param>
        /// <param name="count">Item Count</param>
        /// <param name="nbt">Item Metadata</param>
        public ItemStack(Item itemType, int count, Dictionary<string, object>? nbt)
        {
            this.Type = itemType;
            this.Count = count;
            this.NBT = nbt;
        }
        #nullable disable

        /// <summary>
        /// Check if the item slot is empty
        /// </summary>
        /// <returns>TRUE if the item is empty</returns>
        public bool IsEmpty
        {
            get
            {
                return Type.ItemId == Item.AIR_ID || Count == 0;
            }
        }

        /// <summary>
        /// Retrieve item display name from NBT properties. NULL if no display name is defined.
        /// </summary>
        public string DisplayName
        {
            get
            {
                if (NBT != null && NBT.ContainsKey("display"))
                {
                    var displayProperties = NBT["display"] as Dictionary<string, object>;
                    if (displayProperties != null && displayProperties.ContainsKey("Name"))
                    {
                        string displayName = displayProperties["Name"] as string;
                        if (!String.IsNullOrEmpty(displayName))
                            return displayProperties["Name"].ToString();
                    }
                }
                return null;
            }
        }
        
        /// <summary>
        /// Retrieve item lores from NBT properties. Returns null if no lores is defined.
        /// </summary>
        public object[] Lores
        {
            get
            {
                List<string> lores = new List<string>();
                if (NBT != null && NBT.ContainsKey("display"))
                {
                    var displayProperties = NBT["display"] as Dictionary<string, object>;
                    if (displayProperties != null && displayProperties.ContainsKey("Lore"))
                    {
                        return displayProperties["Lore"] as object[];
                    }
                }
                return null;
            }
        }
        
        /// <summary>
        /// Retrieve item damage from NBT properties. Returns 0 if no damage is defined.
        /// </summary>
        public int Damage
        {
            get
            {
                if (NBT != null && NBT.ContainsKey("Damage"))
                {
                    object damage = NBT["Damage"];
                    if (damage != null)
                    {
                        return int.Parse(damage.ToString());
                    }
                }
                return 0;
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("x{0,-2} {1}", Count, Type.ToString());
            string displayName = DisplayName;
            if (!String.IsNullOrEmpty(displayName))
            {
                sb.AppendFormat(" - {0}§8", displayName);
            }
            int damage = Damage;
            if (damage != 0)
            {
                sb.AppendFormat(" | Damage: {1}", damage);
            }
            return sb.ToString();
        }
    }
}
