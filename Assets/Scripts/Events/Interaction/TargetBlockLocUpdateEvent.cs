namespace CraftSharp.Event
{
    public record TargetBlockLocUpdateEvent : BaseEvent
    {
        public BlockLoc? BlockLoc { get; }

        public TargetBlockLocUpdateEvent(BlockLoc? blockLoc)
        {
            BlockLoc = blockLoc;
        }
    }
}