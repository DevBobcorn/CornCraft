#nullable enable
using System.Collections.Generic;
using CraftSharp.Protocol.Handlers;

namespace CraftSharp.Protocol.ProtoDef.NativeTypes
{
    /// <summary>
    /// Type definition for "i16"
    /// </summary>
    public class NumericType_i16 : PacketDefTypeHandler<short>
    {
        public NumericType_i16(ResourceLocation typeId) : base(typeId)
        {

        }

        public override short ReadValueAsType(PacketRecord rec, string parentPath, Queue<byte> cache)
        {
            return DataTypes.ReadNextShort(cache);
        }
    }
}
