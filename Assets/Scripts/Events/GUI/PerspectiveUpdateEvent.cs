namespace MinecraftClient.Event
{
    public class PerspectiveUpdateEvent : BaseEvent
    {
        public CornClient.Perspective newPerspective;

        public PerspectiveUpdateEvent(CornClient.Perspective perspective)
        {
            newPerspective = perspective;
        }

    }
}