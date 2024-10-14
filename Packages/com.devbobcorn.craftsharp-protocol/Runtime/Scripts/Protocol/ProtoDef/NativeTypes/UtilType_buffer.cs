#nullable enable
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using CraftSharp.Protocol.Handlers;

namespace CraftSharp.Protocol.ProtoDef.NativeTypes
{
    /// <summary>
    /// Type definition for "buffer"
    /// </summary>
    public class UtilType_buffer : TemplateType_namedParameters<byte[]>
    {
        private static readonly string[] PARAM_NAMES = new string[] { "countType", "count", "rest" };

        public UtilType_buffer(ResourceLocation typeId, Dictionary<string, JToken> paramsAsDict,
            UtilType_buffer? inheritedDef) : base(typeId)
        {
            if (InheritParameters(inheritedDef, PARAM_NAMES, paramsAsDict))
            {
                _countType = null;
                _count_as_num = null;
                _count_as_field_name = null;
                _rest = null;

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

            _rest = (int?) ResolvedParameters.GetValueOrDefault("rest");
        }

        /// <summary>
        /// In what type the size if this string is given. e.g. "varint" Alternatively, this can be left null and use set byte counts instead.
        /// </summary>
        private readonly string? _countType;

        /// <summary>
        /// Size of the buffer, used if not reading the size from data.
        /// </summary>
        private readonly int? _count_as_num;
        private readonly string? _count_as_field_name;

        /// <summary>
        /// Size of data after reading. An alternative way of specifying buffer size.
        /// </summary>
        private readonly int? _rest;

        public override byte[] ReadValueAsType(PacketRecord rec, string parentPath, Queue<byte> cache)
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
            else if (_rest is not null)
            {
                count = cache.Count - _rest.Value;
            }
            else
            {
                throw new InvalidDataException("Buffer size is not given in any form!");
            }

            return DataTypes.ReadData(count, cache);
        }
    }
}
