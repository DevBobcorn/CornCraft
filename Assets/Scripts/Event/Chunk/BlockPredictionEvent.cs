namespace CraftSharp.Event
{
    public record BlockPredictionEvent : BaseEvent
    {
        public BlockLoc BlockLoc { get; }
        public int BlockStateId { get; }

        public BlockPredictionEvent(BlockLoc blockLoc, int blockStateId)
        {
            BlockLoc = blockLoc;
            BlockStateId = blockStateId;
        }
    }
}