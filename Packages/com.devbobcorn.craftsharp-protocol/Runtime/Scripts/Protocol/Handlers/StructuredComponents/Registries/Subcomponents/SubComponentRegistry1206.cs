using CraftSharp.Protocol.Handlers.StructuredComponents.Components.Subcomponents;
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
            RegisterSubComponent<AttributeModifierSubComponent>(SubComponents.Attribute);
            RegisterSubComponent<EffectSubComponent>(SubComponents.Effect);
            RegisterSubComponent<PotionEffectSubComponent>(SubComponents.PotionEffect);
            RegisterSubComponent<DetailsSubComponent>(SubComponents.Details);
            RegisterSubComponent<RuleSubComponent>(SubComponents.Rule);
            RegisterSubComponent<FireworkExplosionSubComponent>(SubComponents.FireworkExplosion);
        }
    }
}