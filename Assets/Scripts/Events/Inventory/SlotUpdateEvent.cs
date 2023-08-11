using MinecraftClient.Inventory;

namespace MinecraftClient.Event
{
    public record SlotUpdateEvent : BaseEvent
    {
        public int InventoryId { get; }
        public int SlotId { get; }
        public ItemStack ItemStack { get; }

        public SlotUpdateEvent(int inventoryId, int slotId, ItemStack itemStack)
        {
            InventoryId = inventoryId;
            SlotId = slotId;
            ItemStack = itemStack;
        }
    }
}