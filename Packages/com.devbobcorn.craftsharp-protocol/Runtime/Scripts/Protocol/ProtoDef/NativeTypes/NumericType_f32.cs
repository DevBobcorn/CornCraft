#nullable enable
using System.Collections.Generic;
using CraftSharp.Protocol.Handlers;

namespace CraftSharp.Protocol.ProtoDef.NativeTypes
{
    /// <summary>
    /// Type definition for "f32"
    /// </summary>
    public class NumericType_f32 : PacketDefTypeHandler<float>
    {
        public NumericType_f32(ResourceLocation typeId) : base(typeId)
        {
            
        }

        public override float ReadValueAsType(PacketRecord rec, string parentPath, Queue<byte> cache)
        {
            return DataTypes.ReadNextFloat(cache);
        }
    }
}
