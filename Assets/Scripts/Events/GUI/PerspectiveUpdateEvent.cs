using MinecraftClient.Mapping;

namespace MinecraftClient.Event
{
    public class PerspectiveUpdateEvent : BaseEvent
    {
        public Perspective newPerspective;

        public PerspectiveUpdateEvent(Perspective perspective)
        {
            newPerspective = perspective;
        }

    }
}