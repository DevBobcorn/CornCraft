using System;
using System.Collections.Generic;

namespace CraftSharp.Inventory
{
    public enum InventorySlotType
    {
        Regular,
        Output,
        Helmet,
        Chestplate,
        Leggings,
        Boots,
        Offhand,
        HorseArmor,
        Saddle,
        BeaconActivationItem,
        Bottle,
        BlazePowder,
        LapisLazuli,
        SmithingTemplate,
        Preview
    }

    public static class InventorySlotTypeExtensions
    {
        private static readonly HashSet<ResourceLocation> BEACON_ACTIVATION_ITEM_IDS = new()
        {
            new ResourceLocation("iron_ingot"), new ResourceLocation("gold_ingot"),
            new ResourceLocation("emerald"),    new ResourceLocation("diamond"),
            new ResourceLocation("netherite_ingot")
        };
        
        private static readonly ResourceLocation BLAZE_POWDER_ID = new("blaze_powder");
        
        private static readonly HashSet<ResourceLocation> BREWING_STAND_BOTTLE_ITEM_IDS = new()
        {
            // Water Bottle's id is also minecraft:potion
            new ResourceLocation("potion"), new ResourceLocation("glass_bottle")
        };
        
        private static readonly ResourceLocation LAPIS_LAZULI_ID = new("lapis_lazuli");
        
        private static readonly HashSet<ResourceLocation> CARTOGRAPHY_TABLE_EMPTY_ITEM_IDS = new()
        {
            new ResourceLocation("map"), new ResourceLocation("paper")
        };
        
        private static readonly ResourceLocation CARTOGRAPHY_TABLE_FILLED_ITEM_ID = new("filled_map");
        
        public static Func<ItemStack, bool> GetPlacePredicate(this InventorySlotType slotType)
        {
            // TODO: Also make this data-driven?
            return slotType switch
            {
                InventorySlotType.Regular => _ => true,
                InventorySlotType.Output => _ => false,
                InventorySlotType.Helmet => itemStack => itemStack.ItemType.EquipmentSlot == EquipmentSlot.Head,
                InventorySlotType.Chestplate => itemStack => itemStack.ItemType.EquipmentSlot == EquipmentSlot.Chest,
                InventorySlotType.Leggings => itemStack => itemStack.ItemType.EquipmentSlot == EquipmentSlot.Legs,
                InventorySlotType.Boots => itemStack => itemStack.ItemType.EquipmentSlot == EquipmentSlot.Feet,
                InventorySlotType.Offhand => _ => true, // Offhand slot accepts any item, this slot type is for handling shift-clicks
                InventorySlotType.HorseArmor => _ => false, // TODO: Implement
                InventorySlotType.Saddle => _ => false, // TODO: Implement
                InventorySlotType.BeaconActivationItem => itemStack => BEACON_ACTIVATION_ITEM_IDS.Contains(itemStack.ItemType.ItemId),
                InventorySlotType.Bottle => itemStack => BREWING_STAND_BOTTLE_ITEM_IDS.Contains(itemStack.ItemType.ItemId),
                InventorySlotType.BlazePowder => itemStack => BLAZE_POWDER_ID == itemStack.ItemType.ItemId,
                InventorySlotType.LapisLazuli => itemStack => LAPIS_LAZULI_ID == itemStack.ItemType.ItemId, // TODO: Implement
                InventorySlotType.SmithingTemplate => _ => false, // TODO: Implement
                InventorySlotType.Preview => _ => false,
                _ => _ => false
            };
        }
    }
}