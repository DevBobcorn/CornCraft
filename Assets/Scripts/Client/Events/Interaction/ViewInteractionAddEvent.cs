#nullable enable
using CraftSharp.Control;

namespace CraftSharp.Event
{
    public record ViewInteractionAddEvent : BaseEvent
    {
        public int InteractionId { get; }
        public ViewInteractionInfo Info { get; }

        public ViewInteractionAddEvent(int id, ViewInteractionInfo info)
        {
            InteractionId = id;
            Info = info;
        }
    }
}