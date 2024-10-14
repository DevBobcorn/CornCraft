#nullable enable
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace CraftSharp.Protocol.ProtoDef.NativeTypes
{
    /// <summary>
    /// Type definition for "topBitSetTerminatedArray"
    /// </summary>
    public class XtendedType_topBitSetTerminatedArray : TemplateType_namedParameters<object?[]>
    {
        private static readonly string[] PARAM_NAMES = new string[] { "type" };

        public XtendedType_topBitSetTerminatedArray(ResourceLocation typeId, Dictionary<string, JToken> paramsAsDict,
            XtendedType_topBitSetTerminatedArray? inheritedDef, Func<JToken, PacketDefTypeHandlerBase> buildItemHandler) : base(typeId)
        {
            if (InheritParameters(inheritedDef, PARAM_NAMES, paramsAsDict))
            {
                _wrappedHandler = null;

                return;
            }

            var wrappedTypeToken = ResolvedParameters.GetValueOrDefault("type")!;
            _wrappedHandler = buildItemHandler(wrappedTypeToken);
        }

#nullable disable
        protected readonly PacketDefTypeHandlerBase _wrappedHandler;
#nullable enable

        public override object?[] ReadValueAsType(PacketRecord rec, string parentPath, Queue<byte> cache)
        {
            CheckIfAllParametersResolvedOrElseThrow();

            // The top bit is set if another entry follows, and otherwise unset if this is
            // the last item in the array. See https://wiki.vg/Protocol#Set_Equipment.
            bool hasNext = (cache.Peek() & (1 << 8)) != 0;

            var equipmentEntries = new List<object?>();

            while (hasNext)
            {
                var metadataValue = _wrappedHandler.ReadValue(rec, parentPath, cache);
                equipmentEntries.Add(metadataValue);
            }

            return equipmentEntries.ToArray();
        }
    }
}
