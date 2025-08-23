namespace CraftSharp.Event
{
    public record MobEffectRemovalEvent
    {
        public int EffectId { get; }
        
        public MobEffectRemovalEvent(int effectId)
        {
            EffectId = effectId;
        }
    }
}