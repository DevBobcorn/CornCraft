namespace CraftSharp.Event
{
    public record StaminaUpdateEvent : BaseEvent
    {
        public float Stamina { get; }
        public bool  IsStaminaFull { get; }

        public StaminaUpdateEvent(float stamina, bool isFull)
        {
            Stamina = stamina;
            IsStaminaFull = isFull;
        }
    }
}