namespace MinecraftClient.Event
{
    public record ChatMessageEvent : BaseEvent
    {
        public string Message { get; }
        
        public ChatMessageEvent(string message)
        {
            this.Message = message;
        }

    }
}
