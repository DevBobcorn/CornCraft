#nullable enable
using System.Collections.Generic;
using CraftSharp.Protocol.Handlers;

namespace CraftSharp.Protocol.ProtoDef.NativeTypes
{
    /// <summary>
    /// Type definition for "f64"
    /// </summary>
    public class NumericType_f64 : PacketDefTypeHandler<double>
    {
        public NumericType_f64(ResourceLocation typeId) : base(typeId)
        {

        }

        public override double ReadValueAsType(PacketRecord rec, string parentPath, Queue<byte> cache)
        {
            return DataTypes.ReadNextDouble(cache);
        }
    }
}
