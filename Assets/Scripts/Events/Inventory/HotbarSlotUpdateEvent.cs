#nullable enable
namespace CraftSharp.Event
{
    public record HotbarSlotUpdateEvent : BaseEvent
    {
        public int HotbarSlot { get; }
        public ItemStack? ItemStack { get; }

        public HotbarSlotUpdateEvent(int hotbarSlot, ItemStack? itemStack)
        {
            HotbarSlot = hotbarSlot;
            ItemStack = itemStack;
        }
    }
}