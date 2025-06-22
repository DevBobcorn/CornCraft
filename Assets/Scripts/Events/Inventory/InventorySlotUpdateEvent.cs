namespace CraftSharp.Event
{
    public record InventorySlotUpdateEvent : BaseEvent
    {
        public int InventoryId { get; }
        public int SlotId { get; }
        public ItemStack ItemStack { get; }
        public bool FromClient { get; }

        public InventorySlotUpdateEvent(int inventoryId, int slotId, ItemStack itemStack, bool fromClient)
        {
            InventoryId = inventoryId;
            SlotId = slotId;
            ItemStack = itemStack;
            FromClient = fromClient;
        }
    }
}