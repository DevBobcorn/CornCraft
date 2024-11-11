#nullable enable
using CraftSharp.Control;

namespace CraftSharp.Event
{
    public record ToolInteractionEvent : BaseEvent
    {
        public int EntityId { get; }
        public Block Block { get; }
        public BlockLoc Location { get; }
        public DiggingStatus Status { get; }
        public float Progress { get; }  // 0 - 1

        public ToolInteractionEvent(int entityId, Block block, BlockLoc location, DiggingStatus status, float progress)
        {
            EntityId = entityId;
            Block = block;
            Location = location;
            Status = status;
            Progress = progress;
        }
    }
}