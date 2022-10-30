namespace MinecraftClient.Event
{
    public class HealthUpdateEvent : BaseEvent
    {
        public float newHealth;

        public HealthUpdateEvent(float health)
        {
            newHealth = health;
        }

    }
}