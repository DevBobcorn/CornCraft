#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;

namespace CraftSharp.Protocol.ProtoDef.NativeTypes
{
    /// <summary>
    /// Type definition for "array"
    /// </summary>
    public class StructureType_array : TemplateType_namedParameters<object[]>
    {
        private static readonly string[] PARAM_NAMES = new string[] { "countType", "count", "type" };

        public StructureType_array(ResourceLocation typeId, Dictionary<string, JToken> paramsAsDict,
            StructureType_array? inheritedDef, Func<JToken, PacketDefTypeHandlerBase> buildItemHandler) : base(typeId)
        {
            if (InheritParameters(inheritedDef, PARAM_NAMES, paramsAsDict))
            {
                _countType = null;
                _count_as_num = null;
                _count_as_field_name = null;
                _wrappedHandler = null;

                return; // This type definition is not complete. Only usable as preset for defining other types.
            }

            _countType = (string?) ResolvedParameters.GetValueOrDefault("countType");
            var count = ResolvedParameters.GetValueOrDefault("count");
            if (count?.Type == JTokenType.Integer)
            {
                _count_as_num = (int?) count;
                _count_as_field_name = null;
            }
            else
            {
                _count_as_num = 0;
                _count_as_field_name = count?.ToString();
            }

            var wrappedTypeToken = ResolvedParameters.GetValueOrDefault("type")!;
            _wrappedHandler = buildItemHandler(wrappedTypeToken);
        }

        /// <summary>
        /// In what type the size if this array is given. e.g. "varint" Alternatively, this can be left null and use set byte counts instead.
        /// </summary>
        private readonly string? _countType;

        /// <summary>
        /// Size of the array, used if not reading the size from data.
        /// </summary>
        private readonly int? _count_as_num;
        private readonly string? _count_as_field_name;

#nullable disable
        private readonly PacketDefTypeHandlerBase _wrappedHandler;
#nullable enable

        public override object[] ReadValueAsType(PacketRecord rec, string parentPath, Queue<byte> cache)
        {
            CheckIfAllParametersResolvedOrElseThrow();

            int count;

            if (_countType is not null)
            {
                // Get type of this int count number and read the value
                var countNumHandler = GetLoadedHandler(new ResourceLocation(_countType));
                count = (int) countNumHandler.ReadValue(rec, parentPath, cache)!;
            }
            else if (_count_as_num is not null)
            {
                count = _count_as_num.Value;
            }
            else if (!string.IsNullOrEmpty(_count_as_field_name))
            {
                if (rec.TryGetEntryValue(parentPath, _count_as_field_name, out object? entryValue))
                {
                    count = (int) entryValue!;
                }
                else
                {
                    throw new InvalidDataException($"Value for count field [{_count_as_field_name}] is not fount at {parentPath}!");
                }
            }
            else
            {
                throw new InvalidDataException("Array size is not given in any form!");
            }

            var arrayItemValues = new List<object>();

            if (_wrappedHandler is TemplateType_memberContainer nestedDict) // Array element is nested structure
            {
                for (int i = 0; i < count; i++)
                {
                    var containerDict = nestedDict.ReadValueAsType(rec, $"{parentPath}[{i}]", cache);

                    arrayItemValues.Add(containerDict); // Add the whole nested container dict as array element value
                }
            }
            else // Array element is not nested structure
            {
                for (int i = 0; i < count; i++)
                {
                    var arrayItemValue = _wrappedHandler.ReadValue(rec, parentPath, cache)!;
                    arrayItemValues.Add(arrayItemValue);

                    rec.WriteEntry(parentPath, $"[{i}]", _wrappedHandler.TypeId, arrayItemValue);
                }
            }

            return arrayItemValues.ToArray();
        }
    }
}
