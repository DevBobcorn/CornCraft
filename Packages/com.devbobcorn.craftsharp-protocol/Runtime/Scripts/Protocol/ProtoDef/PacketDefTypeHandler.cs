#nullable enable
using System;
using System.Collections.Generic;

namespace CraftSharp.Protocol.ProtoDef
{
    public abstract class PacketDefTypeHandler<T> : PacketDefTypeHandlerBase
    {
        protected PacketDefTypeHandler(ResourceLocation typeId) : base(typeId)
        {

        }

        public override Type GetValueType()
        {
            return typeof (T);
        }

        public abstract T ReadValueAsType(PacketRecord rec, string parentPath, Queue<byte> cache);

        public override object? ReadValue(PacketRecord rec, string parentPath, Queue<byte> cache)
        {
            return ReadValueAsType(rec, parentPath, cache);
        }
    }
}
