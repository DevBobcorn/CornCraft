#nullable enable
using CraftSharp.Control;

namespace CraftSharp.Event
{
    public record HeldItemChangeEvent : BaseEvent
    {
        public int HotbarSlot { get; }
        public ItemStack? ItemStack { get; }
        public ItemActionType ActionType { get; }

        public HeldItemChangeEvent(int hotbarSlot, ItemStack? itemStack, ItemActionType actionType)
        {
            HotbarSlot = hotbarSlot;
            ItemStack = itemStack;
            ActionType = actionType;
        }
    }
}