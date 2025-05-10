#nullable enable

namespace CraftSharp.Event
{
    public record HeldItemUpdateEvent : BaseEvent
    {
        public int HotbarSlot { get; }
        public bool HotbarSlotChanged { get; }
        public ItemStack? ItemStack { get; }
        public ItemActionType ActionType { get; }

        public HeldItemUpdateEvent(int hotbarSlot, bool hotbarSlotChanged, ItemStack? itemStack, ItemActionType actionType)
        {
            HotbarSlot = hotbarSlot;
            HotbarSlotChanged = hotbarSlotChanged;
            ItemStack = itemStack;
            ActionType = actionType;
        }
    }
}