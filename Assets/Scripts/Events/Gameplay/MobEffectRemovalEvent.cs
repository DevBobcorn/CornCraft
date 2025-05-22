namespace CraftSharp.Event
{
    public class MobEffectRemovalEvent
    {
        public int EffectId { get; }
        
        public MobEffectRemovalEvent(int effectId)
        {
            EffectId = effectId;
        }
    }
}