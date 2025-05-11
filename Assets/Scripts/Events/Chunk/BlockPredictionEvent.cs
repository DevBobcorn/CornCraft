namespace CraftSharp.Event
{
    public record BlockPredictionEvent : BaseEvent
    {
        public BlockLoc BlockLoc { get; }
        public ushort BlockStateId { get; }

        public BlockPredictionEvent(BlockLoc blockLoc, ushort blockStateId)
        {
            BlockLoc = blockLoc;
            BlockStateId = blockStateId;
        }
    }
}