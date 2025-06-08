using CraftSharp.Protocol.Handlers.StructuredComponents.Components.Subcomponents;

namespace CraftSharp.Protocol.Handlers.StructuredComponents.Registries.Subcomponents
{
    public class SubComponentRegistry1211 : SubComponentRegistry1206
    {
        public SubComponentRegistry1211(IMinecraftDataTypes dataTypes) : base(dataTypes)
        {
            RegisterSubComponent<SoundEventSubComponent>(SubComponents.SoundEvent);
        }
    }
}