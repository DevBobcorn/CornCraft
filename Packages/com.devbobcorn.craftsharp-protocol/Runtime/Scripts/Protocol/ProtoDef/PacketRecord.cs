#nullable enable
using System;
using System.Collections.Generic;

namespace CraftSharp.Protocol.ProtoDef
{
    public class PacketRecord
    {
        private readonly Dictionary<string, object?> entryPath2Value = new();
        private readonly Dictionary<string, ResourceLocation> entryPath2TypeId = new();

        private int anonymousItemCount = 0;

        /// <summary>
        /// Used for assigning a unique id for anonymous container items within this packet record.
        /// <br/>
        /// For nested anonymous containers do NOT use this naming, instead just unwrap their fields
        /// into the parent container.
        /// </summary>
        public string GetNextAnonymousContainerItemName()
        {
            var name = $"anonymous_container_item_#{anonymousItemCount}";
            anonymousItemCount += 1;

            return name;
        }

        public static string GetAbsolutePath(string parentPath, string entryName)
        {
            if (entryName.StartsWith('/')) // Root path
            {
                entryName = entryName[1..];
                parentPath = string.Empty;
            }
            else
            {
                while (entryName.StartsWith("../")) // Go up one level
                {
                    entryName = entryName[3..];
                    int pos = parentPath.LastIndexOf('/');
                    parentPath = (pos == -1) ? string.Empty : parentPath[..pos];
                }
            }

            return string.IsNullOrEmpty(parentPath) ? entryName : $"{parentPath}/{entryName}";
        }

        public static string GetValueAsString(object? value)
        {
            if (value is bool valueIsTrue)
            {
                return valueIsTrue ? "true" : "false"; // Lower case
            }
            else
            {
                return value?.ToString() ?? "<null>";
            }
        }

        public void WriteEntry(string parentPath, string entryName, ResourceLocation entryValueTypeId, object? entryValue)
        {
            string entryPath = GetAbsolutePath(parentPath, entryName);

            entryPath2Value.Add(entryPath, entryValue);
            entryPath2TypeId.Add(entryPath, entryValueTypeId);

            Console.WriteLine($"[{entryPath}]: {GetValueAsString(entryValue)} ({entryValueTypeId})");
        }

        public bool TryGetEntryValue(string parentPath, string entryName, out object? entryValue)
        {
            string entryPath = GetAbsolutePath(parentPath, entryName);

            return entryPath2Value.TryGetValue(entryPath, out entryValue);
        }

        public bool TryGetEntryType(string parentPath, string entryName, out ResourceLocation entryValue)
        {
            string entryPath = GetAbsolutePath(parentPath, entryName);

            return entryPath2TypeId.TryGetValue(entryPath, out entryValue);
        }
    }
}
