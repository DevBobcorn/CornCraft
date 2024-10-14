#nullable enable
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace CraftSharp.Protocol.ProtoDef.NativeTypes
{
    /// <summary>
    /// Type definition for "entityMetadataLoop"
    /// </summary>
    public class XtendedType_entityMetadataLoop : TemplateType_namedParameters<object?[]>
    {
        private static readonly string[] PARAM_NAMES = new string[] { "endVal", "type" };

        public XtendedType_entityMetadataLoop(ResourceLocation typeId, Dictionary<string, JToken> paramsAsDict,
            XtendedType_entityMetadataLoop? inheritedDef, Func<JToken, PacketDefTypeHandlerBase> buildItemHandler) : base(typeId)
        {
            if (InheritParameters(inheritedDef, PARAM_NAMES, paramsAsDict))
            {
                _endVal = 0;
                _wrappedHandler = null;

                return;
            }

            var wrappedTypeToken = ResolvedParameters.GetValueOrDefault("type")!;
            _wrappedHandler = buildItemHandler(wrappedTypeToken);
        }

        protected readonly byte _endVal;
#nullable disable
        protected readonly PacketDefTypeHandlerBase _wrappedHandler;
#nullable enable

        public override object?[] ReadValueAsType(PacketRecord rec, string parentPath, Queue<byte> cache)
        {
            CheckIfAllParametersResolvedOrElseThrow();

            // This type uses a end value to indicate if a following value is present.
            // See https://wiki.vg/Entity_metadata#Entity_Metadata_Format for an example.
            bool hasNext = cache.Peek() != _endVal;

            var metadataEntries = new List<object?>();

            while (hasNext)
            {
                var metadataValue = _wrappedHandler.ReadValue(rec, parentPath, cache);
                metadataEntries.Add(metadataValue);
            }

            return metadataEntries.ToArray();
        }
    }
}
