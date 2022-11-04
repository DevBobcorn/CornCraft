namespace MinecraftClient.Event
{
    public record NotificationExpireEvent : BaseEvent
    {
        public int Id { get; }
        public int ExpireIndex { get; }

        public NotificationExpireEvent(int id, int index)
        {
            this.Id = id;
            this.ExpireIndex = index;
        }
    }
}