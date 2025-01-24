#nullable enable
using CraftSharp.Control;

namespace CraftSharp.Event
{
    public record InteractionAddEvent : BaseEvent
    {
        public int InteractionId { get; }
        public bool AddAndSelect { get; }
        public bool UseProgress { get; }
        public InteractionInfo Info { get; }

        public InteractionAddEvent(int id, bool addAndSelect, bool useProgress, InteractionInfo info)
        {
            InteractionId = id;
            AddAndSelect = addAndSelect;
            UseProgress = useProgress;
            Info = info;
        }
    }
}