#nullable enable
namespace CraftSharp.Event
{
    public record TriggerInteractionRemoveEvent : BaseEvent
    {
        public int InteractionId { get; }

        public TriggerInteractionRemoveEvent(int id)
        {
            InteractionId = id;
        }
    }
}