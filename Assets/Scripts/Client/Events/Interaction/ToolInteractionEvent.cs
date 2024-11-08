using CraftSharp.Control;

namespace CraftSharp.Event
{
    public record ToolInteractionEvent : BaseEvent
    {
        public ToolInteractionInfo Info { get; }

        public ToolInteractionEvent(ToolInteractionInfo info)
        {
            Info = info;
        }
    }
}