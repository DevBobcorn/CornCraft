namespace CraftSharp.Event
{
    public class MobEffectUpdateEvent
    {
        public int EffectId { get; }
        public int Amplifier { get; }
        public int DurationTicks { get; }
        public bool IsAmbient { get; }
        public bool ShowParticles { get; }
        public bool ShowIcon { get; }
        
        public MobEffectUpdateEvent(int effectId, int amplifier, int durationTicks,
            bool isAmbient, bool showParticles, bool showIcon)
        {
            EffectId = effectId;
            Amplifier = amplifier;
            DurationTicks = durationTicks;
            IsAmbient = isAmbient;
            ShowParticles = showParticles;
            ShowIcon = showIcon;
        }
    }
}