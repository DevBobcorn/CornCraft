#nullable enable

using CraftSharp.Inventory;

namespace CraftSharp.Event
{
    public record HeldItemUpdateEvent : BaseEvent
    {
        public int HotbarSlot { get; }
        public Hand Hand { get; }
        public bool HotbarSlotChanged { get; }
        public ItemStack? ItemStack { get; }
        public ItemActionType ActionType { get; }

        public HeldItemUpdateEvent(int hotbarSlot, Hand hand, bool hotbarSlotChanged, ItemStack? itemStack, ItemActionType actionType)
        {
            HotbarSlot = hotbarSlot;
            Hand = hand;
            HotbarSlotChanged = hotbarSlotChanged;
            ItemStack = itemStack;
            ActionType = actionType;
        }
    }
}