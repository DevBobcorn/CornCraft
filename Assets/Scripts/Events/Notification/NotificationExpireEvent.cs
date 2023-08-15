namespace CraftSharp.Event
{
    public record NotificationExpireEvent : BaseEvent
    {
        public int Id { get; }

        public NotificationExpireEvent(int id)
        {
            this.Id = id;
        }
    }
}