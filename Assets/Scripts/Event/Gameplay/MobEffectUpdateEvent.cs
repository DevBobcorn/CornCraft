namespace CraftSharp.Event
{
    public record MobEffectUpdateEvent
    {
        public MobEffectInstance Effect { get; }
        
        public MobEffectUpdateEvent(MobEffectInstance effect)
        {
            Effect = effect;
        }
    }
}