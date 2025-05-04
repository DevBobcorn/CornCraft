namespace CraftSharp.Event
{
    public record InventorySlotUpdateEvent : BaseEvent
    {
        public int InventoryId { get; }
        public int Slot { get; }
        public ItemStack ItemStack { get; }
        public bool FromClient { get; }

        public InventorySlotUpdateEvent(int inventoryId, int slot, ItemStack itemStack, bool fromClient)
        {
            InventoryId = inventoryId;
            Slot = slot;
            ItemStack = itemStack;
            FromClient = fromClient;
        }
    }
}