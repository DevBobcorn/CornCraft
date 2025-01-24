#nullable enable
using CraftSharp.Control;

namespace CraftSharp.Event
{
    public record InteractionAddEvent : BaseEvent
    {
        public int InteractionId { get; }
        public bool AddAndSelect { get; }
        public InteractionInfo Info { get; }

        public InteractionAddEvent(int id, bool addAndSelect, InteractionInfo info)
        {
            InteractionId = id;
            AddAndSelect = addAndSelect;
            Info = info;
        }
    }
}