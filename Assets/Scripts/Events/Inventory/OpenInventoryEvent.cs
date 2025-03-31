using CraftSharp.Inventory;

namespace CraftSharp.Event
{
    public record OpenInventoryEvent : BaseEvent
    {
        public int InventoryId { get; }
        public BaseInventory Inventory { get; }

        public OpenInventoryEvent(int inventoryId, BaseInventory inventory)
        {
            InventoryId = inventoryId;
            Inventory = inventory;
        }
    }
}