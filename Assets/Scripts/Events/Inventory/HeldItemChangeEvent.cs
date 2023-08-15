namespace CraftSharp.Event
{
    public record HeldItemChangeEvent : BaseEvent
    {
        public int HotbarSlot { get; }

        public HeldItemChangeEvent(int hotbarSlot)
        {
            HotbarSlot = hotbarSlot;
        }
    }
}