namespace CraftSharp.Event
{
    public record CameraAimingEvent : BaseEvent
    {
        public bool Aiming { get; }

        public CameraAimingEvent(bool show)
        {
            Aiming = show;
        }
    }
}