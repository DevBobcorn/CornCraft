#nullable enable
using System.Collections.Generic;
using CraftSharp.Protocol.Handlers;

namespace CraftSharp.Protocol.ProtoDef.NativeTypes
{
    /// <summary>
    /// Type definition for "u8"
    /// </summary>
    public class NumericType_u8 : PacketDefTypeHandler<byte>
    {
        public NumericType_u8(ResourceLocation typeId) : base(typeId)
        {

        }

        public override byte ReadValueAsType(PacketRecord rec, string parentPath, Queue<byte> cache)
        {
            return DataTypes.ReadNextByte(cache);
        }
    }
}
