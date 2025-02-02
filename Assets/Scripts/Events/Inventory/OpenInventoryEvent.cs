using CraftSharp.Inventory;

namespace CraftSharp.Event
{
    public record OpenInventoryEvent : BaseEvent
    {
        public int InventoryId { get; }
        public Container Inventory { get; }

        public OpenInventoryEvent(int inventoryId, Container inventory)
        {
            InventoryId = inventoryId;
            Inventory = inventory;
        }
    }
}