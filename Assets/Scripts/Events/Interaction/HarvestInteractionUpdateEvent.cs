#nullable enable
using CraftSharp.Control;

namespace CraftSharp.Event
{
    public record HarvestInteractionUpdateEvent : BaseEvent
    {
        public int InteractionId { get; }

        public int EntityId { get; }
        public Block Block { get; }
        public BlockLoc Location { get; }
        public DiggingStatus Status { get; }
        public float Progress { get; }  // 0 - 1

        public HarvestInteractionUpdateEvent(int id, int entityId, Block block, BlockLoc location, DiggingStatus status, float progress)
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