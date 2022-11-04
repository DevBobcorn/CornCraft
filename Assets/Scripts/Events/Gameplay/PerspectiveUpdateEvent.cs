using MinecraftClient.Mapping;

namespace MinecraftClient.Event
{
    public record PerspectiveUpdateEvent : BaseEvent
    {
        public Perspective Perspective { get; }

        public PerspectiveUpdateEvent(Perspective perspective)
        {
            Perspective = perspective;
        }

    }
}