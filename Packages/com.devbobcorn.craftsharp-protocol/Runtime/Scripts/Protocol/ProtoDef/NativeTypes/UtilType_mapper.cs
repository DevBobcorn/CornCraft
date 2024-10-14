#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace CraftSharp.Protocol.ProtoDef.NativeTypes
{
    /// <summary>
    /// Type definition for "mapper"
    /// </summary>
    public class UtilType_mapper : TemplateType_namedParameters<string?>
    {
        private static readonly string[] PARAM_NAMES = new string[] { "type", "mappings" };

        public UtilType_mapper(ResourceLocation typeId, Dictionary<string, JToken> paramsAsDict,
            UtilType_mapper? inheritedDef, Func<JToken, PacketDefTypeHandlerBase> buildItemHandler) : base(typeId)
        {
            if (InheritParameters(inheritedDef, PARAM_NAMES, paramsAsDict))
            {
                _inputHandler = null;
                _mappings = null;

                return; // This type definition is not complete. Only usable as preset for defining other types.
            }

            var inputType = ResolvedParameters.GetValueOrDefault("type")!;
            _inputHandler = buildItemHandler(inputType);

            var inputCsType = _inputHandler.GetValueType();
            var inputCsTypeIsInteger =
                    (inputCsType == typeof(sbyte)) || (inputCsType == typeof(short))  ||
                    (inputCsType == typeof(int))   || (inputCsType == typeof(long))   ||
                    (inputCsType == typeof(byte))  || (inputCsType == typeof(ushort)) ||
                    (inputCsType == typeof(uint))  || (inputCsType == typeof(ulong));

            _mappings = (ResolvedParameters.GetValueOrDefault("mappings")! as JObject)!.Properties()
                // The string might be in hexadecimal for integers, so unify them as regular integer strings
                .ToDictionary(x => inputCsTypeIsInteger ? Convert.ToInt64(x.Name, 16).ToString() : x.Name,
                    x => x.Value.ToString());
        }

#nullable disable
        protected readonly PacketDefTypeHandlerBase _inputHandler;
        protected readonly Dictionary<string, string> _mappings;
#nullable enable

        public override string? ReadValueAsType(PacketRecord rec, string parentPath, Queue<byte> cache)
        {
            CheckIfAllParametersResolvedOrElseThrow();

            var inputValue = _inputHandler.ReadValue(rec, parentPath, cache);

            string inputValueAsString = PacketRecord.GetValueAsString(inputValue);

            if (_mappings.TryGetValue(inputValueAsString, out string? mappedString))
            {
                return mappedString;
            }
            else
            {
                //throw new InvalidDataException($"Mapper does not have a mapping for {inputValueAsString} ({_inputHandler.TypeId}). Accepted values are: {string.Join(", ", _mappings.Keys)}.");
                Console.WriteLine($"Mapper does not have a mapping for {inputValueAsString} ({_inputHandler.TypeId}). Accepted values are: {string.Join(", ", _mappings.Keys)}.");
                // Well sometimes nothing is given. Just do nothing.

                return null;
            }
        }
    }
}
