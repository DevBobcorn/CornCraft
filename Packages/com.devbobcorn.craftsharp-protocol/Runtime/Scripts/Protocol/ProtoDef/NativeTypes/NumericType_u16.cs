#nullable enable
using System.Collections.Generic;
using CraftSharp.Protocol.Handlers;

namespace CraftSharp.Protocol.ProtoDef.NativeTypes
{
    /// <summary>
    /// Type definition for "u16"
    /// </summary>
    public class NumericType_u16 : PacketDefTypeHandler<ushort>
    {
        public NumericType_u16(ResourceLocation typeId) : base(typeId)
        {

        }

        public override ushort ReadValueAsType(PacketRecord rec, string parentPath, Queue<byte> cache)
        {
            return DataTypes.ReadNextUShort(cache);
        }
    }
}
