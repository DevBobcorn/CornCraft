#nullable enable
using System.Collections; // Required for non-generic IDictionary
using System.Collections.Generic;
using System.Linq; // Required for potential IEnumerable comparison extension

namespace CraftSharp.Inventory
{
    public static class DictionaryUtil
    {
        /// <summary>
        /// Performs a deep comparison of two dictionaries to determine if they are logically equal.
        /// This method handles nested dictionaries and uses object.Equals for value comparisons,
        /// respecting overridden Equals methods or IEquatable<T> implementations.
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
        /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
        /// <param name="dict1">The first dictionary to compare.</param>
        /// <param name="dict2">The second dictionary to compare.</param>
        /// <returns>True if the dictionaries are deeply equal, false otherwise.</returns>
        /// <remarks>
        /// - Null dictionaries are considered equal only if both are null.
        /// - Reference equality is checked first for performance.
        /// - Compares counts, then keys, then deeply compares values.
        /// - Nested dictionaries (of any key/value type) are compared recursively.
        /// - Does not handle cyclic references (may cause StackOverflowException).
        /// - Assumes keys (TKey) can be correctly compared using the dictionary's default comparer.
        /// </remarks>
        public static bool DeepCompareDictionaries<TKey, TValue>(
            IDictionary<TKey, TValue>? dict1,
            IDictionary<TKey, TValue>? dict2)
        {
            // Empty dictionaries should be treated as null
            if (dict1?.Count == 0) dict1 = null;
            if (dict2?.Count == 0) dict2 = null;
            
            // 1. Null checks
            if (dict1 == null && dict2 == null) return true;
            if (dict1 == null || dict2 == null) return false;
    
            // 2. Reference equality check (optimization)
            if (ReferenceEquals(dict1, dict2)) return true;
    
            // 3. Count check
            if (dict1.Count != dict2.Count) return false;
    
            // 4. Key and Value Comparison
            // We must ensure that every key in dict1 exists in dict2 and that their values are deeply equal.
            // Since counts are equal, we don't need to check the other way around (if key in dict2 exists in dict1).
            foreach (KeyValuePair<TKey, TValue> pair in dict1)
            {
                // Check if the key exists in the second dictionary.
                // TValue might be a reference type, default is null.
                if (!dict2.TryGetValue(pair.Key, out TValue? value2))
                {
                    // Key not found in dict2
                    return false;
                }
    
                // Key found, now deeply compare the values
                if (!DeepCompareObjects(pair.Value, value2))
                {
                    // Values are not deeply equal
                    return false;
                }
            }
    
            // All checks passed, dictionaries are deeply equal
            return true;
        }
    
        /// <summary>
        /// Helper method to deeply compare two objects.
        /// Handles nulls, reference equality, dictionaries, (optionally) other collections,
        /// and falls back to object.Equals for other types.
        /// </summary>
        private static bool DeepCompareObjects(object? obj1, object? obj2)
        {
            // 1. Null checks
            if (obj1 == null && obj2 == null) return true;
            if (obj1 == null || obj2 == null) return false;
    
            // 2. Reference equality check (optimization)
            if (ReferenceEquals(obj1, obj2)) return true;
    
            // 3. Type check (optional, but can sometimes prevent Equals from comparing incompatible types)
            // Note: Be cautious if inheritance hierarchies should be considered equal.
            // if (obj1.GetType() != obj2.GetType()) return false;
    
            // 4. Check if objects are non-generic Dictionaries
            // We use the non-generic IDictionary interface to handle nested dictionaries
            // regardless of their specific key/value types.
            if (obj1 is IDictionary dict1 && obj2 is IDictionary dict2)
            {
                if (dict1.Count != dict2.Count) return false;
    
                // Create a temporary list of keys from dict2 for efficient lookup checks
                var dict2Keys = new HashSet<object>(dict2.Keys.Cast<object>());
    
                foreach (var key in dict1.Keys)
                {
                    // Ensure dict2 also contains this key
                    if (!dict2Keys.Contains(key)) return false;
    
                    // Recursively compare values associated with the key
                    if (!DeepCompareObjects(dict1[key], dict2[key]))
                    {
                        return false;
                    }
                }
                return true; // Dictionaries are equal
            }
    
            // 5. OPTIONAL: Add handling for other collection types like lists/arrays if needed
            // Example for IEnumerable (treats them as ordered sequences):
            /*
            if (obj1 is IEnumerable enum1 && obj2 is IEnumerable enum2)
            {
                var list1 = enum1.Cast<object>().ToList();
                var list2 = enum2.Cast<object>().ToList();
    
                if (list1.Count != list2.Count) return false;
    
                for (int i = 0; i < list1.Count; i++)
                {
                    // Recursively compare elements
                    if (!DeepCompareObjects(list1[i], list2[i]))
                    {
                        return false;
                    }
                }
                return true; // Enumerables are equal
            }
            */
    
            // 6. Fallback to standard equality comparison for any other types.
            // This respects overridden Equals methods and IEquatable<T>.
            return object.Equals(obj1, obj2);
        }
    }
}