namespace CraftSharp.Event
{
    public record BlockActionEvent : BaseEvent
    {
        public readonly BlockLoc BlockLoc;
        public readonly byte ActionId;
        public readonly byte ActionParam;

        public BlockActionEvent(BlockLoc blockLoc, byte actionId, byte actionParam)
        {
            BlockLoc = blockLoc;
            ActionId = actionId;
            ActionParam = actionParam;
        }
    }
}