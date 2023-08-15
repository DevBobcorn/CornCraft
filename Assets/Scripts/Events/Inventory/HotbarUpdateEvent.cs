#nullable enable
namespace CraftSharp.Event
{
    public record HotbarUpdateEvent : BaseEvent
    {
        public int HotbarSlot { get; }
        public ItemStack? ItemStack { get; }

        public HotbarUpdateEvent(int hotbarSlot, ItemStack? itemStack)
        {
            HotbarSlot = hotbarSlot;
            ItemStack = itemStack;
        }
    }
}