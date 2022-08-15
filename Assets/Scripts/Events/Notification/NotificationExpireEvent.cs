namespace MinecraftClient.Event
{
    public class NotificationExpireEvent : BaseEvent
    {
        public readonly int id;
        public readonly int pos;
        public NotificationExpireEvent(int id, int pos)
        {
            this.id = id;
            this.pos = pos;
        }
    }
}