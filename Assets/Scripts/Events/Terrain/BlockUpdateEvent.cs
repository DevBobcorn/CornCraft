using MinecraftClient.Mapping;

namespace MinecraftClient.Event
{
    public class BlockUpdateEvent : BaseEvent
    {
        public readonly Location location;

        public BlockUpdateEvent(Location loc)
        {
            this.location = loc;
        }
    }
}
