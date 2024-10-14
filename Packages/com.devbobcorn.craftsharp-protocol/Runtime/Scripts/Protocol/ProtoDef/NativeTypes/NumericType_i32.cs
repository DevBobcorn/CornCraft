#nullable enable
using System.Collections.Generic;
using CraftSharp.Protocol.Handlers;

namespace CraftSharp.Protocol.ProtoDef.NativeTypes
{
    /// <summary>
    /// Type definition for "i32"
    /// </summary>
    public class NumericType_i32 : PacketDefTypeHandler<int>
    {
        public NumericType_i32(ResourceLocation typeId) : base(typeId)
        {

        }

        public override int ReadValueAsType(PacketRecord rec, string parentPath, Queue<byte> cache)
        {
            return DataTypes.ReadNextInt(cache);
        }
    }
}
