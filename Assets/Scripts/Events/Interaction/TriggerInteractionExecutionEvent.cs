namespace CraftSharp.Event
{
    public record TriggerInteractionExecutionEvent : BaseEvent
    {
        public int InteractionId { get; }

        public TriggerInteractionExecutionEvent(int id)
        {
            InteractionId = id;
        }
    }
}