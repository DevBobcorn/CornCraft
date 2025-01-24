#nullable enable
using CraftSharp.Control;

namespace CraftSharp.Event
{
    public record ToolInteractionUpdateEvent : BaseEvent
    {
        public int InteractionId { get; }

        public int EntityId { get; }
        public Block Block { get; }
        public BlockLoc Location { get; }
        public DiggingStatus Status { get; }
        public float Progress { get; }  // 0 - 1

        public ToolInteractionUpdateEvent(int id, int entityId, Block block, BlockLoc location, DiggingStatus status, float progress)
        {
            InteractionId = id;
            
            EntityId = entityId;
            Block = block;
            Location = location;
            Status = status;
            Progress = progress;
        }
    }
}