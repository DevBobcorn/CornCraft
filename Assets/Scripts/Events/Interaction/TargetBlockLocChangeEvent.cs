namespace CraftSharp.Event
{
    public record TargetBlockLocChangeEvent : BaseEvent
    {
        public BlockLoc? BlockLoc { get; }

        public TargetBlockLocChangeEvent(BlockLoc? blockLoc)
        {
            BlockLoc = blockLoc;
        }
    }
}