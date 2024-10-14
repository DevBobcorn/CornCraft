#nullable enable
using System.Collections.Generic;

namespace CraftSharp.Protocol.ProtoDef.NativeTypes
{
    /// <summary>
    /// Template for type definitions like "container" or "bitfield"
    /// </summary>
    public abstract class TemplateType_memberContainer : PacketDefTypeHandler<Dictionary<string, object?>>
    {
        protected TemplateType_memberContainer(ResourceLocation typeId) : base(typeId)
        {

        }
    }
}
