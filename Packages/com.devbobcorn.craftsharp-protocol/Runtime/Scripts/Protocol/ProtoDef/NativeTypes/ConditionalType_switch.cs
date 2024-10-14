#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace CraftSharp.Protocol.ProtoDef.NativeTypes
{
    /// <summary>
    /// Type definition for "switch"
    /// </summary>
    public class ConditionalType_switch : TemplateType_namedParameters<object?>
    {
        private static readonly string[] PARAM_NAMES = new string[] { "compareTo", "fields", "default" };

        public ConditionalType_switch(ResourceLocation typeId, Dictionary<string, JToken> paramsAsDict,
            ConditionalType_switch? inheritedDef, Func<JToken, PacketDefTypeHandlerBase> buildItemHandler) : base(typeId)
        {
            if (InheritParameters(inheritedDef, PARAM_NAMES, paramsAsDict))
            {
                _compareTo = null;
                _fields = null;
                _defaultHandler = null;

                return; // This type definition is not complete. Only usable as preset for defining other types.
            }

            _compareTo = (string) ResolvedParameters.GetValueOrDefault("compareTo")!;
            _fields = (ResolvedParameters.GetValueOrDefault("fields")! as JObject)!.Properties()
                .ToDictionary(x => x.Name, x => buildItemHandler(x.Value));

            _defaultHandler = null;

            if (ResolvedParameters.TryGetValue("default", out JToken? defaultType))
            {
                _defaultHandler = buildItemHandler(defaultType);
            }
        }

#nullable disable
        protected readonly string _compareTo;
        protected readonly Dictionary<string, PacketDefTypeHandlerBase> _fields;
#nullable enable
        protected readonly PacketDefTypeHandlerBase? _defaultHandler;

        public override object? ReadValueAsType(PacketRecord rec, string parentPath, Queue<byte> cache)
        {
            CheckIfAllParametersResolvedOrElseThrow();

            //var compareToPath = PacketRecord.GetAbsolutePath(parentPath, _compareTo);

            if (rec.TryGetEntryValue(parentPath, _compareTo, out object? entryValue))
            {
                string entryValueAsString = PacketRecord.GetValueAsString(entryValue);

                if (_fields.TryGetValue(entryValueAsString, out PacketDefTypeHandlerBase? chosenHandler))
                {
                    //Console.WriteLine($"[{compareToPath}] is {entryValueAsString}. Handler {chosenHandler.GetType().Name}");
                    return chosenHandler.ReadValue(rec, parentPath, cache);
                }
                else if (_defaultHandler is not null)
                {
                    //Console.WriteLine($"[{compareToPath}] is {entryValueAsString}. Defualt handler {_defaultHandler.GetType().Name}");
                    return _defaultHandler.ReadValue(rec, parentPath, cache);
                }
                else
                {
                    //throw new InvalidDataException($"Compare field {_compareTo} with value {entryValueAsString} has no matching and no default type is given!");
                    Console.WriteLine($"Compare field [{_compareTo}] with value {entryValueAsString} has no matching and no default type is given!");
                    // Well sometimes nothing is given. Just do nothing.

                    return null;
                }
            }
            else
            {
                throw new InvalidDataException($"Value for compare field [{_compareTo}] is not fount at {parentPath}!");
            }
        }
    }
}
