#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CraftSharp.Protocol.ProtoDef.NativeTypes
{
    /// <summary>
    /// Type definition for "container"
    /// <br>
    /// Some ("optional") fields in result might be null depending on whether they should be present
    /// </summary>
    public class StructureType_container : TemplateType_memberContainer
    {
        public StructureType_container(ResourceLocation typeId,
                (bool a, string? n, PacketDefTypeHandlerBase h)[] itemArray) : base(typeId)
        {
            containerEntries = itemArray.Select(a =>
                    new ContainerEntry(a.a, a.n, a.h)).ToArray();
        }

        class ContainerEntry
        {
            public ContainerEntry(bool anonymous, string? name, PacketDefTypeHandlerBase wrappedHandler)
            {
                Anonymous = anonymous;
                Name = name;
                WrappedHandler = wrappedHandler;

                if (!anonymous && string.IsNullOrEmpty(Name))
                {
                    throw new InvalidDataException("Container entry neither marked as anonymous nor have a name!");
                }
            }

            public readonly bool Anonymous;
            public readonly string? Name;
            public readonly PacketDefTypeHandlerBase WrappedHandler;
        }

        private readonly ContainerEntry[] containerEntries;

        public override Dictionary<string, object?> ReadValueAsType(PacketRecord rec, string parentPath, Queue<byte> cache)
        {
            // Field name -> field value
            var containerDict = new Dictionary<string, object?>();

            for (int i = 0; i < containerEntries.Length; i++)
            {
                var entry = containerEntries[i];
                var containerItemHandler = entry.WrappedHandler;
                
                if (containerItemHandler is TemplateType_memberContainer nestedDict) // Container entry is nested structure
                {
                    if (entry.Anonymous || string.IsNullOrEmpty(entry.Name)) // Read entries of this anonymous dict into the current path
                    {
                        var anonDictValue = nestedDict.ReadValueAsType(rec, parentPath, cache);

                        foreach (var anonPair in anonDictValue)
                        {
                            containerDict.Add(anonPair.Key, anonPair.Value); // Add each as own field
                        }
                    }
                    else // Read entries of this named dict into its own path
                    {
                        var nestedDictValue = nestedDict.ReadValueAsType(rec,
                            string.IsNullOrEmpty(parentPath) ? entry.Name : $"{parentPath}/{entry.Name}", cache);

                        // Add the whole nested dict as own field
                        containerDict.Add(entry.Name, nestedDictValue);
                    }
                }
                else // Container entry is not nested structure
                {
                    var itemValue = containerItemHandler.ReadValue(rec, parentPath, cache);

                    var entryNameOrAnonymousName = (entry.Anonymous || string.IsNullOrEmpty(entry.Name)) ?
                        rec.GetNextAnonymousContainerItemName() : entry.Name;
                    containerDict.Add(entryNameOrAnonymousName, itemValue);

                    rec.WriteEntry(parentPath, entryNameOrAnonymousName, containerItemHandler.TypeId, itemValue);
                }
            }

            return containerDict;
        }
    }
}
