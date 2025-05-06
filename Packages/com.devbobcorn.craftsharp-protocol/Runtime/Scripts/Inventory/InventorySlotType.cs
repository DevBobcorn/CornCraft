using System;

namespace CraftSharp.Inventory
{
    public record InventorySlotType
    {
        public static readonly InventorySlotType DUMMY_INVENTORY_SLOT_TYPE = new(ResourceLocation.INVALID, false, _ => false);
        
        public static readonly ResourceLocation SLOT_TYPE_REGULAR_ID = new("regular");
        public static readonly ResourceLocation SLOT_TYPE_OUTPUT_ID = new("output");
        public static readonly ResourceLocation SLOT_TYPE_HEAD_ID = new("head");
        public static readonly ResourceLocation SLOT_TYPE_CHEST_ID = new("chest");
        public static readonly ResourceLocation SLOT_TYPE_LEGS_ID = new("legs");
        public static readonly ResourceLocation SLOT_TYPE_FEET_ID = new("feet");
        public static readonly ResourceLocation SLOT_TYPE_OFFHAND_ID = new("offhand");
        
        public static readonly ResourceLocation SLOT_TYPE_HORSE_ARMOR_ID = new("horse_armor");
        public static readonly ResourceLocation SLOT_TYPE_SADDLE_ID = new("saddle");
        
        public readonly ResourceLocation TypeId;

        public readonly bool Interactable;
        public readonly Func<ItemStack, bool> PlacePredicate;

        public InventorySlotType(ResourceLocation id, bool interactable, Func<ItemStack, bool> placePredicate)
        {
            TypeId = id;
            Interactable = interactable;
            PlacePredicate = placePredicate;
        }
    }
}