namespace MinecraftClient.Event
{
    public record HealthUpdateEvent : BaseEvent
    {
        public float Health { get; }

        public HealthUpdateEvent(float health)
        {
            Health = health;
        }

    }
}