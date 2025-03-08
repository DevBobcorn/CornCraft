using CraftSharp.Protocol.Handlers.StructuredComponents.Components._1_20_6;
using CraftSharp.Protocol.Handlers.StructuredComponents.Components.Subcomponents;
using CraftSharp.Protocol.Handlers.StructuredComponents.Components.Subcomponents._1_20_6;
using CraftSharp.Protocol.Handlers.StructuredComponents.Components.Subcomponents._1_21;
using CraftSharp.Protocol.Handlers.StructuredComponents.Core;

namespace CraftSharp.Protocol.Handlers.StructuredComponents.Registries.Subcomponents
{
    public class SubComponentRegistry121 : SubComponentRegistry1206
    {
        public SubComponentRegistry121(DataTypes dataTypes) : base(dataTypes)
        {
            RegisterSubComponent<SoundEventSubComponent>(SubComponents.SoundEvent);
        }
    }
}