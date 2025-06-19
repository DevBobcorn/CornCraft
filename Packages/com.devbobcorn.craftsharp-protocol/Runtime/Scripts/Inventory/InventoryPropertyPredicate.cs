using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CraftSharp.Inventory
{
    public class InventoryPropertyPredicate
    {
        public enum Operator
        {
            EQUAL,
            NOT_EQUAL,
            LESS,
            GREATER,
            LESS_EQUAL,
            GREATER_EQUAL
        }

        private static bool CheckOperator(Operator op, short propVal, short targetVal)
        {
            return op switch
            {
                Operator.EQUAL => propVal == targetVal,
                Operator.NOT_EQUAL => propVal != targetVal,
                Operator.LESS => propVal < targetVal,
                Operator.GREATER => propVal > targetVal,
                Operator.LESS_EQUAL => propVal <= targetVal,
                Operator.GREATER_EQUAL => propVal >= targetVal,
                _ => throw new InvalidDataException($"Operator {op} cannot be checked!")
            };
        }
        
        private readonly Dictionary<string, (Operator, string)> conditions;

        public InventoryPropertyPredicate(Dictionary<string, (Operator, string)> conditions)
        {
            this.conditions = conditions;
        }

        public static InventoryPropertyPredicate FromString(string source)
        {
            var conditions = new Dictionary<string, (Operator, string)>();
            var srcs = source.Split(',');
            foreach (var src in srcs)
            {
                string[] keyVal;

                if (src.Contains(">="))
                {
                    keyVal = src.Split(">=", 2);
                    conditions.Add(keyVal[0], (Operator.GREATER_EQUAL, keyVal[1]));
                }
                else if (src.Contains("<="))
                {
                    keyVal = src.Split("<=", 2);
                    conditions.Add(keyVal[0], (Operator.LESS_EQUAL, keyVal[1]));
                }
                else if (src.Contains('='))
                {
                    keyVal = src.Split('=', 2);
                    conditions.Add(keyVal[0], (Operator.EQUAL, keyVal[1]));
                }
                else if (src.Contains("=="))
                {
                    keyVal = src.Split("==", 2);
                    conditions.Add(keyVal[0], (Operator.EQUAL, keyVal[1]));
                }
                else if (src.Contains("!="))
                {
                    keyVal = src.Split("!=", 2);
                    conditions.Add(keyVal[0], (Operator.NOT_EQUAL, keyVal[1]));
                }
                else if (src.Contains('>'))
                {
                    keyVal = src.Split('>', 2);
                    conditions.Add(keyVal[0], (Operator.GREATER, keyVal[1]));
                }
                else if (src.Contains('<'))
                {
                    keyVal = src.Split('<', 2);
                    conditions.Add(keyVal[0], (Operator.LESS, keyVal[1]));
                }
                else
                {
                    Debug.Log($"Invalid prop condition: <{src}>");
                }
            }
            return new InventoryPropertyPredicate(conditions);
        }

        public bool Check(Dictionary<string, short> propertyTable)
        {
            foreach (var (key, (op, target)) in conditions)
            {
                if (!propertyTable.TryGetValue(key, out var propVal))
                {
                    return false; // Property is not present
                }

                if (!short.TryParse(target, out var targetValue))
                {
                    if (!propertyTable.TryGetValue(target, out targetValue))
                    {
                        return false; // Target is neither a short integer literal, nor a property name
                    }
                }

                if (!CheckOperator(op, propVal, targetValue))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
