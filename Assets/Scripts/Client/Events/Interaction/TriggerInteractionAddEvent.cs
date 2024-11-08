#nullable enable
using CraftSharp.Control;

namespace CraftSharp.Event
{
    public record TriggerInteractionAddEvent : BaseEvent
    {
        public int InteractionId { get; }
        public TriggerInteractionInfo Info { get; }

        public TriggerInteractionAddEvent(int id, TriggerInteractionInfo info)
        {
            InteractionId = id;
            Info = info;
        }
    }
}