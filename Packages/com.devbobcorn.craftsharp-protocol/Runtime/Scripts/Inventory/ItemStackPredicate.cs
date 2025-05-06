using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CraftSharp.Inventory
{
    public class ItemStackPredicate
    {
        public static readonly ItemStackPredicate EMPTY = new(new());
        
        private readonly Dictionary<string, string> conditions;

        public ItemStackPredicate(Dictionary<string, string> conditions)
        {
            this.conditions = conditions;
        }

        public static ItemStackPredicate FromString(string source)
        {
            if (source is "" or "normal")
                return EMPTY;
            
            var conditions = new Dictionary<string, string>();
            var srcs = source.Split(',');
            foreach (var src in srcs)
            {
                if (src.Contains('='))
                {
                    var keyVal = src.Split('=', 2);
                    conditions.Add(keyVal[0], keyVal[1]);
                }
                else
                {
                    Debug.Log($"Invalid prop condition: <{src}>");
                }
            }
            return new ItemStackPredicate(conditions);
        }

        public bool Check(ItemStack itemStack)
        {
            foreach (var (key, value) in conditions)
            {
                // Multiple allowed values are supported, separated with symbol '|'
                var allowedValues = value.Split('|');

                switch (key)
                {
                    case "id":
                        if (!allowedValues.Select(x => ResourceLocation.FromString(x))
                                .Contains(itemStack.ItemType.ItemId))
                        {
                            return false;
                        }
                        break;
                    case "id_path_starts_with":
                        if (!allowedValues.Any(x => itemStack.ItemType.ItemId.Path.StartsWith(x)))
                        {
                            return false;
                        }
                        break;
                    case "id_path_ends_with":
                        if (!allowedValues.Any(x => itemStack.ItemType.ItemId.Path.EndsWith(x)))
                        {
                            return false;
                        }
                        break;
                    case "equipment_slot":
                        if (!allowedValues.Select(x => EquipmentSlotHelper.GetEquipmentSlot(x))
                                .Contains(itemStack.ItemType.EquipmentSlot))
                        {
                            return false;
                        }
                        break;
                    default:
                        Debug.LogWarning($"Unknown condition key: {key}");
                        return false;
                }
            }

            return true;
        }
    }
}
