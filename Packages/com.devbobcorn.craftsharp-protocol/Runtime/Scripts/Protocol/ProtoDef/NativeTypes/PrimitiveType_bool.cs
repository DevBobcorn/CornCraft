#nullable enable
using System.Collections.Generic;
using CraftSharp.Protocol.Handlers;

namespace CraftSharp.Protocol.ProtoDef.NativeTypes
{
    /// <summary>
    /// Type definition for "bool"
    /// </summary>
    public class PrimitiveType_bool : PacketDefTypeHandler<bool>
    {
        public PrimitiveType_bool(ResourceLocation typeId) : base(typeId)
        {

        }

        public override bool ReadValueAsType(PacketRecord rec, string parentPath, Queue<byte> cache)
        {
            return DataTypes.ReadNextBool(cache);
        }
    }
}
