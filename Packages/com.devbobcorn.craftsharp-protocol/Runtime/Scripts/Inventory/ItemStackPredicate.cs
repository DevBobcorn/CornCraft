using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CraftSharp.Inventory
{
    public class ItemStackPredicate
    {
        public enum Operator
        {
            EQUAL,
            NOT_EQUAL
        }
        
        public static readonly ItemStackPredicate EMPTY = new(new());
        
        private readonly Dictionary<string, (Operator, string)> conditions;

        public ItemStackPredicate(Dictionary<string, (Operator, string)> conditions)
        {
            this.conditions = conditions;
        }

        public static ItemStackPredicate FromString(string source)
        {
            if (source is "" or "normal")
                return EMPTY;
            
            var conditions = new Dictionary<string, (Operator, string)>();
            var srcs = source.Split(',');
            foreach (var src in srcs)
            {
                string[] keyVal;
                
                if (src.Contains("=="))
                {
                    keyVal = src.Split("==", 2);
                    conditions.Add(keyVal[0], (Operator.EQUAL, keyVal[1]));
                }
                else if (src.Contains("!="))
                {
                    keyVal = src.Split("!=", 2);
                    conditions.Add(keyVal[0], (Operator.NOT_EQUAL, keyVal[1]));
                }
                else if (src.Contains('='))
                {
                    keyVal = src.Split('=', 2);
                    conditions.Add(keyVal[0], (Operator.EQUAL, keyVal[1]));
                }
                else
                {
                    Debug.Log($"Invalid prop condition: <{src}>");
                }
            }
            return new ItemStackPredicate(conditions);
        }

        private static bool CheckIsCondition(string itemPredicate, ItemStack itemStack)
        {
            return itemPredicate switch
            {
                "damageable" => itemStack.IsDamageable,
                "stackable" => itemStack.IsStackable,
                "enchanted" => itemStack.IsEnchanted,
                "empty" => itemStack.IsEmpty,
                _ => throw new System.IO.InvalidDataException($"Undefined item predicate: {itemPredicate}")
            };
        }

        public bool Check(ItemStack itemStack)
        {
            foreach (var (key, (op, value)) in conditions)
            {
                // Multiple allowed values are supported, separated with symbol '|'
                var allowedValues = value.Split('|');
                bool conditionMet = false;

                switch (key)
                {
                    case "id":
                        {
                            var itemId = itemStack.ItemType.ItemId.ToString();
                            if (op == Operator.EQUAL)
                                conditionMet = allowedValues.Any(x => x == itemId);
                            if (op == Operator.NOT_EQUAL)
                                conditionMet = allowedValues.All(x => x != itemId);
                        }
                        break;
                    case "is":
                        {
                            if (op == Operator.EQUAL)
                                conditionMet = allowedValues.Any(x => CheckIsCondition(x, itemStack));
                            if (op == Operator.NOT_EQUAL)
                                conditionMet = allowedValues.All(x => !CheckIsCondition(x, itemStack));
                        }
                        break;
                    case "id_path_starts_with":
                        {
                            var itemIdPath = itemStack.ItemType.ItemId.Path;
                            if (op == Operator.EQUAL)
                                conditionMet = allowedValues.Any(x => itemIdPath.StartsWith(x));
                            if (op == Operator.NOT_EQUAL)
                                conditionMet = allowedValues.All(x => !itemIdPath.StartsWith(x));
                        }
                        break;
                    case "id_path_ends_with":
                        {
                            var itemIdPath = itemStack.ItemType.ItemId.Path;
                            if (op == Operator.EQUAL)
                                conditionMet = allowedValues.Any(x => itemIdPath.EndsWith(x));
                            if (op == Operator.NOT_EQUAL)
                                conditionMet = allowedValues.All(x => !itemIdPath.EndsWith(x));

                            var o = op switch
                            {
                                Operator.EQUAL => "ends",
                                Operator.NOT_EQUAL => "does not end",
                                _ => "WTF"
                            };
                            Debug.Log($"Checking if {itemIdPath} {o} with any of {string.Join(", ", allowedValues)}. Result: {conditionMet}");
                        }
                        break;
                    case "equipment_slot":
                    {
                        if (op == Operator.EQUAL)
                            conditionMet = allowedValues.Select(EquipmentSlotHelper.GetEquipmentSlot)
                                .Any(x => x == itemStack.ItemType.EquipmentSlot);
                        if (op == Operator.NOT_EQUAL)
                            conditionMet = allowedValues.Select(EquipmentSlotHelper.GetEquipmentSlot)
                                .All(x => x != itemStack.ItemType.EquipmentSlot);
                    }
                        break;
                    default:
                        Debug.LogWarning($"Unknown condition key: {key}");
                        break;
                }
                
                if (!conditionMet) // This condition is not met
                    return false;
            }

            // All conditions are met
            return true;
        }
    }
}
