#nullable enable
using System.Collections.Generic;
using CraftSharp.Protocol.Handlers;

namespace CraftSharp.Protocol.ProtoDef.NativeTypes
{
    /// <summary>
    /// Type definition for "i8"
    /// </summary>
    public class NumericType_i8 : PacketDefTypeHandler<sbyte>
    {
        public NumericType_i8(ResourceLocation typeId) : base(typeId)
        {

        }

        public override sbyte ReadValueAsType(PacketRecord rec, string parentPath, Queue<byte> cache)
        {
            return DataTypes.ReadNextSByte(cache);
        }
    }
}
