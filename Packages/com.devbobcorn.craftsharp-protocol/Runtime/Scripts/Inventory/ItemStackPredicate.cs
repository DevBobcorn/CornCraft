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
                        if (!allowedValues.Select(ResourceLocation.FromString)
                                .Contains(itemStack.ItemType.ItemId))
                        {
                            return false;
                        }
                        break;
                    case "is":
                        return allowedValues.Any(x =>
                        {
                            var res = x switch
                            {
                                "damageable" => itemStack.IsDamageable,
                                "stackable" => itemStack.IsStackable,
                                "enchanted" => itemStack.IsEnchanted,
                                "empty" => itemStack.IsEmpty,
                                _ => throw new System.IO.InvalidDataException($"Undefined item predicate: {x}")
                            };
                            
                            Debug.Log($"{x}: {res}");

                            return res;
                        });
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
                        if (!allowedValues.Select(EquipmentSlotHelper.GetEquipmentSlot)
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
