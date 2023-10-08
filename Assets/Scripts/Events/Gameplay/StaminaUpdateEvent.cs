namespace CraftSharp.Event
{
    public record StaminaUpdateEvent : BaseEvent
    {
        public float Stamina { get; }
        public float MaxStamina { get; }

        public StaminaUpdateEvent(float stamina, float maxStamina)
        {
            Stamina = stamina;
            MaxStamina = maxStamina;
        }
    }
}