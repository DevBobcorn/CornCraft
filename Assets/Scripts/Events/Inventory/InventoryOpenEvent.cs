#nullable enable
using CraftSharp.Inventory;

namespace CraftSharp.Event
{
    public record InventoryOpenEvent : BaseEvent
    {
        public int InventoryId { get; }
        public InventoryData InventoryData { get; }

        public InventoryOpenEvent(int inventoryId, InventoryData inventoryData)
        {
            InventoryId = inventoryId;
            InventoryData = inventoryData;
        }
    }
}