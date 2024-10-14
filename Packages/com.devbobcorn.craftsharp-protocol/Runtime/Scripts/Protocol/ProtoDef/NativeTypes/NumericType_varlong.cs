#nullable enable
using System.Collections.Generic;
using CraftSharp.Protocol.Handlers;

namespace CraftSharp.Protocol.ProtoDef.NativeTypes
{
    /// <summary>
    /// Type definition for "varlong"
    /// </summary>
    public class NumericType_varlong : PacketDefTypeHandler<long>
    {
        public NumericType_varlong(ResourceLocation typeId) : base(typeId)
        {

        }

        public override long ReadValueAsType(PacketRecord rec, string parentPath, Queue<byte> cache)
        {
            return DataTypes.ReadNextVarLong(cache);
        }
    }
}
