#nullable enable

namespace CraftSharp.Event
{
    public record HeldItemUpdateEvent : BaseEvent
    {
        public int HotbarSlot { get; }
        public ItemStack? ItemStack { get; }
        public ItemActionType ActionType { get; }

        public HeldItemUpdateEvent(int hotbarSlot, ItemStack? itemStack, ItemActionType actionType)
        {
            HotbarSlot = hotbarSlot;
            ItemStack = itemStack;
            ActionType = actionType;
        }
    }
}