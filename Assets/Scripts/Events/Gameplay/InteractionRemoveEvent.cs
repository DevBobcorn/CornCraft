#nullable enable

namespace CraftSharp.Event
{
    public record InteractionRemoveEvent : BaseEvent
    {
        public int InteractionId { get; }

        public InteractionRemoveEvent(int id)
        {
            InteractionId = id;
        }

    }
}