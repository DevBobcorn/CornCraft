#nullable enable
using System.Collections.Generic;
using CraftSharp.Protocol.Handlers;

namespace CraftSharp.Protocol.ProtoDef.NativeTypes
{
    /// <summary>
    /// Type definition for "u64"
    /// </summary>
    public class NumericType_u64 : PacketDefTypeHandler<ulong>
    {
        public NumericType_u64(ResourceLocation typeId) : base(typeId)
        {

        }

        public override ulong ReadValueAsType(PacketRecord rec, string parentPath, Queue<byte> cache)
        {
            return DataTypes.ReadNextULong(cache);
        }
    }
}
