#nullable enable
using System.Collections.Generic;

namespace CraftSharp.Event
{
    public record InventoryItemsUpdateEvent : BaseEvent
    {
        public int InventoryId { get; }
        public Dictionary<int, ItemStack?> Items { get; }

        public InventoryItemsUpdateEvent(int inventoryId, Dictionary<int, ItemStack?> itemList)
        {
            InventoryId = inventoryId;
            Items = itemList;
        }
    }
}