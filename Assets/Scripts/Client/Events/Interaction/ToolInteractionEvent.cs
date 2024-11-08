#nullable enable
using CraftSharp.Control;

namespace CraftSharp.Event
{
    public record ToolInteractionEvent : BaseEvent
    {
        public Item? Tool { get; }
        public BlockState BlockState { get; }
        public ToolInteractionInfo Info { get; }

        public ToolInteractionEvent(Item? tool, BlockState blockState, ToolInteractionInfo info)
        {
            Tool = tool;
            BlockState = blockState;
            Info = info;
        }
    }
}