#nullable enable
namespace CraftSharp.Event
{
    public record HeldItemChangeEvent : BaseEvent
    {
        public int HotbarSlot { get; }
        public ItemStack? ItemStack { get; }

        public HeldItemChangeEvent(int hotbarSlot, ItemStack? itemStack)
        {
            HotbarSlot = hotbarSlot;
            ItemStack = itemStack;
        }
    }
}