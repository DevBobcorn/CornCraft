using MinecraftClient.Mapping;

namespace MinecraftClient.Event
{
    public record BlockUpdateEvent : BaseEvent
    {
        public Location Location { get; }

        public BlockUpdateEvent(Location loc)
        {
            this.Location = loc;
        }
    }
}
