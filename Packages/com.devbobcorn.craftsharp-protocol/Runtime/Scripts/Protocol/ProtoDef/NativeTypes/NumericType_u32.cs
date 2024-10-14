#nullable enable
using System.Collections.Generic;
using CraftSharp.Protocol.Handlers;

namespace CraftSharp.Protocol.ProtoDef.NativeTypes
{
    /// <summary>
    /// Type definition for "u32"
    /// </summary>
    public class NumericType_u32 : PacketDefTypeHandler<uint>
    {
        public NumericType_u32(ResourceLocation typeId) : base(typeId)
        {

        }

        public override uint ReadValueAsType(PacketRecord rec, string parentPath, Queue<byte> cache)
        {
            return DataTypes.ReadNextUInt(cache);
        }
    }
}
