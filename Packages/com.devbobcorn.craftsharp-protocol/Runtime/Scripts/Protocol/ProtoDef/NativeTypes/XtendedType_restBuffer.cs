#nullable enable
using System.Collections.Generic;
using CraftSharp.Protocol.Handlers;

namespace CraftSharp.Protocol.ProtoDef.NativeTypes
{
    /// <summary>
    /// Type definition for "restBuffer"
    /// <br>
    /// Read any byte that's still left in the packet.
    /// </summary>
    public class XtendedType_restBuffer : PacketDefTypeHandler<RawByteArray>
    {
        public XtendedType_restBuffer(ResourceLocation typeId) : base(typeId)
        {

        }

        public override RawByteArray ReadValueAsType(PacketRecord rec, string parentPath, Queue<byte> cache)
        {
            int restLength = cache.Count;

            return new RawByteArray(DataTypes.ReadData(restLength, cache));
        }
    }
}
