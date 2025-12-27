namespace CraftSharp.Event
{
    public record TargetLiquidLocUpdateEvent : BaseEvent
    {
        public BlockLoc? BlockLoc { get; }

        public TargetLiquidLocUpdateEvent(BlockLoc? blockLoc)
        {
            BlockLoc = blockLoc;
        }
    }
}