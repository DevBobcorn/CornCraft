using CraftSharp.Inventory;

namespace CraftSharp.Event
{
    public record OpenInventoryEvent : BaseEvent
    {
        public int InventoryId { get; }
        public InventoryData InventoryData { get; }

        public OpenInventoryEvent(int inventoryId, InventoryData inventoryData)
        {
            InventoryId = inventoryId;
            InventoryData = inventoryData;
        }
    }
}