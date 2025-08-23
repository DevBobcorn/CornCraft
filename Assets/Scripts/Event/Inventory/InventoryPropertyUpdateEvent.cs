namespace CraftSharp.Event
{
    public record InventoryPropertyUpdateEvent : BaseEvent
    {
        public int InventoryId { get; }
        public int Property { get; }
        public short Value { get; }

        public InventoryPropertyUpdateEvent(int inventoryId, int property, short value)
        {
            InventoryId = inventoryId;
            Property = property;
            Value = value;
        }
    }
}