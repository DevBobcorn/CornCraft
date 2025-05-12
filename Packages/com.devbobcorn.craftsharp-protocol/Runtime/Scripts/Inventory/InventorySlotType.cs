using System;

namespace CraftSharp.Inventory
{
    public record InventorySlotType
    {
        public static readonly InventorySlotType DUMMY_INVENTORY_SLOT_TYPE = new(ResourceLocation.INVALID, false, 0, _ => false);
        
        public static readonly ResourceLocation SLOT_TYPE_REGULAR_ID = new("regular");
        public static readonly ResourceLocation SLOT_TYPE_OUTPUT_ID = new("output");
        public static readonly ResourceLocation SLOT_TYPE_HEAD_ITEM_ID = new("head_item");
        public static readonly ResourceLocation SLOT_TYPE_CHEST_ITEM_ID = new("chest_item");
        public static readonly ResourceLocation SLOT_TYPE_LEGS_ITEM_ID = new("legs_item");
        public static readonly ResourceLocation SLOT_TYPE_FEET_ITEM_ID = new("feet_item");
        public static readonly ResourceLocation SLOT_TYPE_OFFHAND_ITEM_ID = new("offhand_item");
        
        public static readonly ResourceLocation SLOT_TYPE_HORSE_ARMOR_ID = new("horse_armor");
        public static readonly ResourceLocation SLOT_TYPE_SADDLE_ID = new("saddle");
        
        public readonly ResourceLocation TypeId;

        public readonly bool Interactable;
        public readonly int MaxCount; // Should be int.MaxValue for regular slots
        public readonly Func<ItemStack, bool> PlacePredicate;

        public InventorySlotType(ResourceLocation id, bool interactable, int maxCount, Func<ItemStack, bool> placePredicate)
        {
            TypeId = id;
            Interactable = interactable;
            MaxCount = maxCount;
            PlacePredicate = placePredicate;
        }
    }
}