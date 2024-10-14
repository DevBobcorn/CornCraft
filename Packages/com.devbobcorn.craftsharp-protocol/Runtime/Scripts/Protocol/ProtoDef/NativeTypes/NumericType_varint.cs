#nullable enable
using System.Collections.Generic;
using CraftSharp.Protocol.Handlers;

namespace CraftSharp.Protocol.ProtoDef.NativeTypes
{
    /// <summary>
    /// Type definition for "varint"
    /// </summary>
    public class NumericType_varint : PacketDefTypeHandler<int>
    {
        public NumericType_varint(ResourceLocation typeId) : base(typeId)
        {

        }

        public override int ReadValueAsType(PacketRecord rec, string parentPath, Queue<byte> cache)
        {
            return DataTypes.ReadNextVarInt(cache);
        }
    }
}
