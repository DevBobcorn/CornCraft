namespace MinecraftClient.Event
{
    public class ChatMessageEvent : BaseEvent
    {
        public readonly string message;
        
        public ChatMessageEvent(string message)
        {
            this.message = message;
        }

    }
}
