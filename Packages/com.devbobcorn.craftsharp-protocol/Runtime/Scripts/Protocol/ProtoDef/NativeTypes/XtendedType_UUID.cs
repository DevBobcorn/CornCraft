#nullable enable
using System;
using System.Collections.Generic;
using CraftSharp.Protocol.Handlers;

namespace CraftSharp.Protocol.ProtoDef.NativeTypes
{
    /// <summary>
    /// Type definition for "UUID"
    /// </summary>
    public class XtendedType_UUID : PacketDefTypeHandler<Guid>
    {
        public XtendedType_UUID(ResourceLocation typeId) : base(typeId)
        {

        }

        public override Guid ReadValueAsType(PacketRecord rec, string parentPath, Queue<byte> cache)
        {
            return DataTypes.ReadNextUUID(cache);
        }
    }
}
