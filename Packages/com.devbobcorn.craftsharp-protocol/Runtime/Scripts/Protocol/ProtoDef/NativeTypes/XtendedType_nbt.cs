#nullable enable
using System.Collections.Generic;
using CraftSharp.Protocol.Handlers;

namespace CraftSharp.Protocol.ProtoDef.NativeTypes
{
    /// <summary>
    /// Type definition for "nbt", "optionalNbt", "anonymousNbt" and "anonOptionalNbt"
    /// <br/>
    /// The last two are used in 1.20.2+ for sending text-components encoded as NBT.
    /// </summary>
    public class XtendedType_nbt : PacketDefTypeHandler<Dictionary<string, object>>
    {
        public XtendedType_nbt(ResourceLocation typeId) : base(typeId)
        {

        }

        public override Dictionary<string, object> ReadValueAsType(PacketRecord rec, string parentPath, Queue<byte> cache)
        {
            // May be a TAG_END (0) in which case no NBT is present, in this case it'll be an empty dictionary.
            return DataTypes.ReadNextNbt(cache);
        }
    }
}
