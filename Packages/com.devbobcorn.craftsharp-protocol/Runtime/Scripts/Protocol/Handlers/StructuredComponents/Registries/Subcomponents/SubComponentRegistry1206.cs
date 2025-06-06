using CraftSharp.Protocol.Handlers.StructuredComponents.Components._1_20_6;
using CraftSharp.Protocol.Handlers.StructuredComponents.Components.Subcomponents;
using CraftSharp.Protocol.Handlers.StructuredComponents.Components.Subcomponents._1_20_6;
using CraftSharp.Protocol.Handlers.StructuredComponents.Core;

namespace CraftSharp.Protocol.Handlers.StructuredComponents.Registries.Subcomponents
{
    public class SubComponentRegistry1206 : SubComponentRegistry
    {
        public SubComponentRegistry1206(IMinecraftDataTypes dataTypes) : base(dataTypes)
        {
            RegisterSubComponent<BlockPredicateSubcomponent>(SubComponents.BlockPredicate);
            RegisterSubComponent<BlockSetSubcomponent>(SubComponents.BlockSet);
            RegisterSubComponent<PropertySubComponent>(SubComponents.Property);
            RegisterSubComponent<AttributeSubComponent>(SubComponents.Attribute);
            RegisterSubComponent<EffectSubComponent>(SubComponents.Effect);
            RegisterSubComponent<PotionEffectSubComponent>(SubComponents.PotionEffect);
            RegisterSubComponent<DetailsSubComponent>(SubComponents.Details);
            RegisterSubComponent<RuleSubComponent>(SubComponents.Rule);
            RegisterSubComponent<FireworkExplosionSubComponent>(SubComponents.FireworkExplosion);
        }
    }
}