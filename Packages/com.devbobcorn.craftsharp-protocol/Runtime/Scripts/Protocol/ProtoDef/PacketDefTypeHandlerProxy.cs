#nullable enable
using System;
using System.Collections.Generic;

namespace CraftSharp.Protocol.ProtoDef
{
    public class PacketDefTypeHandlerProxy : PacketDefTypeHandler<object?>
    {
        private readonly ResourceLocation _proxiedHandlerTypeId;

        public PacketDefTypeHandlerProxy(ResourceLocation typeId, ResourceLocation proxiedHandlerTypeId) : base(typeId)
        {
            _proxiedHandlerTypeId = proxiedHandlerTypeId;
        }

        public override Type GetValueType()
        {
            var proxiedHandler = PacketDefTypeHandlerBase.GetLoadedHandler(_proxiedHandlerTypeId);

            return proxiedHandler.GetValueType();
        }

        public override object? ReadValueAsType(PacketRecord rec, string parentPath, Queue<byte> cache)
        {
            var proxiedHandler = PacketDefTypeHandlerBase.GetLoadedHandler(_proxiedHandlerTypeId);

            return proxiedHandler.ReadValue(rec, parentPath, cache);
        }

        public override object? ReadValue(PacketRecord rec, string parentPath, Queue<byte> cache)
        {
            return ReadValueAsType(rec, parentPath, cache);
        }
    }
}