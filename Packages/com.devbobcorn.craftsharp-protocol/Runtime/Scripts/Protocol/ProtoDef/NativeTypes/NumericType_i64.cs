#nullable enable
using System.Collections.Generic;
using CraftSharp.Protocol.Handlers;

namespace CraftSharp.Protocol.ProtoDef.NativeTypes
{
    /// <summary>
    /// Type definition for "i64"
    /// </summary>
    public class NumericType_i64 : PacketDefTypeHandler<long>
    {
        public NumericType_i64(ResourceLocation typeId) : base(typeId)
        {

        }

        public override long ReadValueAsType(PacketRecord rec, string parentPath, Queue<byte> cache)
        {
            return DataTypes.ReadNextLong(cache);
        }
    }
}
