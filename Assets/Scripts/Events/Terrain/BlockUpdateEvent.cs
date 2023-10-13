namespace CraftSharp.Event
{
    public record BlockUpdateEvent : BaseEvent
    {
        public BlockLoc Location { get; }

        public BlockUpdateEvent(BlockLoc location)
        {
            this.Location = location;
        }
    }
}
