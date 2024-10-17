namespace CraftSharp.Event
{
    public record HealthUpdateEvent : BaseEvent
    {
        public float Health { get; }
        public bool  UpdateMaxHealth { get; }

        public HealthUpdateEvent(float health)
        {
            Health = health;
            UpdateMaxHealth = false;
        }

        public HealthUpdateEvent(float health, bool updateMax)
        {
            Health = health;
            UpdateMaxHealth = updateMax;
        }
    }
}