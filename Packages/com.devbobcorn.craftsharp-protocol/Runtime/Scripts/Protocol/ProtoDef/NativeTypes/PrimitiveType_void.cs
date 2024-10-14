#nullable enable
using System.Collections.Generic;

namespace CraftSharp.Protocol.ProtoDef.NativeTypes
{
    /// <summary>
    /// Type definition for "void"
    /// </summary>
    public class PrimitiveType_void : PacketDefTypeHandler<object?>
    {
        public PrimitiveType_void(ResourceLocation typeId) : base(typeId)
        {

        }

        public override object? ReadValueAsType(PacketRecord rec, string parentPath, Queue<byte> cache)
        {
            return null;
        }
    }
}
