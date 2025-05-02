namespace CraftSharp.Event
{
    public record InventoryPropertyUpdateEvent : BaseEvent
    {
        public int InventoryId { get; }
        public int Property { get; }
        public int Value { get; }

        public InventoryPropertyUpdateEvent(int inventoryId, int property, int value)
        {
            InventoryId = inventoryId;
            Property = property;
            Value = value;
        }
    }
}