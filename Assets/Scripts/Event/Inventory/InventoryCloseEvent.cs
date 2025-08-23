using CraftSharp.Inventory;

namespace CraftSharp.Event
{
    public record InventoryCloseEvent : BaseEvent
    {
        public int InventoryId { get; }

        public InventoryCloseEvent(int inventoryId)
        {
            InventoryId = inventoryId;
        }
    }
}