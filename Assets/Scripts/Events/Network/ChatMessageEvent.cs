using System.Collections.Generic;

namespace CraftSharp.Event
{
    public record ChatMessageEvent : BaseEvent
    {
        public string Message { get; }
        public (string, string, string, string)[] Actions { get; }
        
        public ChatMessageEvent(string message, (string, string, string, string)[] actions)
        {
            this.Message = message;
            this.Actions = actions;
        }
    }
}
