#nullable enable

namespace CraftSharp.Event
{
    public record ViewInteractionRemoveEvent : BaseEvent
    {
        public int InteractionId { get; }

        public ViewInteractionRemoveEvent(int id)
        {
            InteractionId = id;
        }
    }
}