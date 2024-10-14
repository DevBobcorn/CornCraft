#nullable enable
using System.Collections.Generic;
using CraftSharp.Protocol.Handlers;

namespace CraftSharp.Protocol.ProtoDef.NativeTypes
{
    /// <summary>
    /// Type definition for "option"
    /// </summary>
    public class ConditionalType_option : PacketDefTypeHandler<object?>
    {
        public ConditionalType_option(ResourceLocation typeId, PacketDefTypeHandlerBase wrappedHandler)
                : base(typeId)
        {
            _wrappedHandler = wrappedHandler;
        }

        protected readonly PacketDefTypeHandlerBase _wrappedHandler;

        public override object? ReadValueAsType(PacketRecord rec, string parentPath, Queue<byte> cache)
        {
            // This type uses a bool value to indicate if the following value is present.
            // See https://wiki.vg/Protocol#Select_Advancements_Tab for an example.
            bool valuePresent = DataTypes.ReadNextBool(cache);

            if (valuePresent)
            {
                return _wrappedHandler.ReadValue(rec, parentPath, cache);
            }
            else
            {
                return null;
            }
        }
    }
}
