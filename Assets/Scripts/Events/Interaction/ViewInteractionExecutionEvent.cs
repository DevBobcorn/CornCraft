namespace CraftSharp.Event
{
    public record ViewInteractionExecutionEvent : BaseEvent
    {
        public int InteractionId { get; }

        public ViewInteractionExecutionEvent(int id)
        {
            InteractionId = id;
        }
    }
}