namespace CraftSharp.Inventory
{
    /// <summary>
    /// Represents a Minecraft Inventory Type
    /// </summary>
    public record InventoryType
    {
        // See https://minecraft.wiki/w/Minecraft_Wiki:Projects/wiki.vg_merge/Inventory
        public static readonly ResourceLocation PLAYER_ID        = new("inventory");         // Player backpack, including armor slots & 2x2 crafter
        public static readonly ResourceLocation HORSE_ID         = new("horse");             // Horse & Donkey & Llama & Camel
        
        public static readonly ResourceLocation GENERIC_9x1_ID   = new("generic_9x1");       // [Unused]
        public static readonly ResourceLocation GENERIC_9x2_ID   = new("generic_9x2");       // [Unused]
        public static readonly ResourceLocation GENERIC_9x3_ID   = new("generic_9x3");       // Chest(& Minecart) & Ender Chest & Barrel
        public static readonly ResourceLocation GENERIC_9x4_ID   = new("generic_9x4");       // [Unused]
        public static readonly ResourceLocation GENERIC_9x5_ID   = new("generic_9x5");       // [Unused]
        public static readonly ResourceLocation GENERIC_9x6_ID   = new("generic_9x6");       // Large Chest
        public static readonly ResourceLocation GENERIC_3x3_ID   = new("generic_3x3");       // Dispenser & Dropper
        public static readonly ResourceLocation CRAFTER_3x3_ID   = new("crafter_3x3");       // Crafter
        public static readonly ResourceLocation ANVIL_ID         = new("anvil");             // Anvil
        public static readonly ResourceLocation BEACON_ID        = new("beacon");            // Beacon
        public static readonly ResourceLocation BLAST_FURNACE_ID = new("blast_furnace");     // Blast Furnace
        public static readonly ResourceLocation BREWING_STAND_ID = new("brewing_stand");     // Brewing Stand
        public static readonly ResourceLocation CRAFTING_ID      = new("crafting");          // Crafting Table
        public static readonly ResourceLocation ENCHANTMENT_ID   = new("enchantment");       // Enchanting Table
        public static readonly ResourceLocation FURNACE_ID       = new("furnace");           // Furnace
        public static readonly ResourceLocation GRINDSTONE_ID    = new("grindstone");        // Grindstone
        public static readonly ResourceLocation HOPPER_ID        = new("hopper");            // Hopper(& Minecart)
        public static readonly ResourceLocation LECTERN_ID       = new("lectern");           // Lectern
        public static readonly ResourceLocation LOOM_ID          = new("loom");              // Loom
        public static readonly ResourceLocation MERCHANT_ID      = new("merchant");          // Villager & Wandering Trader
        public static readonly ResourceLocation SHULKER_BOX_ID   = new("shulker_box");       // Shulker Box
        public static readonly ResourceLocation SMITHING_OLD_ID  = new("legacy_smithing");   // Smithing Table in 1.16-1.19.3, and in 1.19.4 with 'update_1_20' feature flag disabled
        public static readonly ResourceLocation SMITHING_ID      = new("smithing");          // Smithing Table in 1.20+, and in 1.19.4 with 'update_1_20' feature flag enabled
        public static readonly ResourceLocation SMOKER_ID        = new("smoker");            // Smoker
        public static readonly ResourceLocation CARTOGRAPHY_ID   = new("cartography_table"); // Cartography Table
        public static readonly ResourceLocation STONECUTTER_ID   = new("stonecutter");       // Stonecutter

        public static readonly InventoryType PLAYER        = new(PLAYER_ID,        0, 0, 9, true, 1, 0); // Append offhand slot (slot 45)
        public static readonly InventoryType HORSE_REGULAR = new(HORSE_ID,         0, 0, 2, true, 2, 0);
        public static readonly InventoryType HORSE_CHESTED = new(HORSE_ID,         5, 3, 2, true, 2, 0);
        public static readonly InventoryType GENERIC_9x1   = new(GENERIC_9x1_ID,   9, 1);
        public static readonly InventoryType GENERIC_9x2   = new(GENERIC_9x2_ID,   9, 2);
        public static readonly InventoryType GENERIC_9x3   = new(GENERIC_9x3_ID,   9, 3);
        public static readonly InventoryType GENERIC_9x4   = new(GENERIC_9x4_ID,   9, 4);
        public static readonly InventoryType GENERIC_9x5   = new(GENERIC_9x5_ID,   9, 5);
        public static readonly InventoryType GENERIC_9x6   = new(GENERIC_9x6_ID,   9, 6);
        public static readonly InventoryType GENERIC_3x3   = new(GENERIC_3x3_ID,   3, 3);
        public static readonly InventoryType CRAFTER_3x3   = new(CRAFTER_3x3_ID,   3, 3, 0, true, 1, 45); // Append output preview slot (slot 45)
        public static readonly InventoryType ANVIL         = new(ANVIL_ID,         0, 0, 3, true, 0, 2);
        public static readonly InventoryType BEACON        = new(BEACON_ID,        0, 0, 1);
        public static readonly InventoryType BLAST_FURNACE = new(BLAST_FURNACE_ID, 0, 0, 3, true, 0, 2);
        public static readonly InventoryType BREWING_STAND = new(BREWING_STAND_ID, 0, 0, 5);
        public static readonly InventoryType CRAFTING      = new(CRAFTING_ID,      3, 3, 1, true, 0, 0);
        public static readonly InventoryType ENCHANTMENT   = new(ENCHANTMENT_ID,   0, 0, 2);
        public static readonly InventoryType FURNACE       = new(FURNACE_ID,       0, 0, 3, true, 0, 2);
        public static readonly InventoryType GRINDSTONE    = new(GRINDSTONE_ID,    0, 0, 3, true, 0, 2);
        public static readonly InventoryType HOPPER        = new(HOPPER_ID,        5, 1);
        public static readonly InventoryType LECTERN       = new(LECTERN_ID,       0, 0);
        public static readonly InventoryType LOOM          = new(LOOM_ID,          0, 0, 4, true, 0, 3);
        public static readonly InventoryType MERCHANT      = new(MERCHANT_ID,      0, 0, 3, true, 0, 2);
        public static readonly InventoryType SHULKER_BOX   = new(SHULKER_BOX_ID,   9, 3); // Basically the same as GENERIC_9x3
        public static readonly InventoryType SMITHING_OLD  = new(SMITHING_OLD_ID,  0, 0, 3, true, 0, 2);
        public static readonly InventoryType SMITHING      = new(SMITHING_ID,      0, 0, 4, true, 0, 3);
        public static readonly InventoryType SMOKER        = new(SMOKER_ID,        0, 0, 3, true, 0, 2);
        public static readonly InventoryType CARTOGRAPHY   = new(CARTOGRAPHY_ID,   0, 0, 3, true, 0, 2);
        public static readonly InventoryType STONECUTTER   = new(STONECUTTER_ID,   0, 0, 2, true, 0, 1);
        
        public static readonly InventoryType DUMMY_INVENTORY_TYPE = new(ResourceLocation.INVALID, 0, 0, 0, false);

        public readonly ResourceLocation TypeId;
        
        public bool HasBackpackSlots { get; private set; } // 9x3 slots from player backpack + 9x1 hotbar slots
        public byte MainSlotWidth { get; private set; }
        public byte MainSlotHeight { get; private set; }
        
        // Count of special slots (e.g. Crafting output slot or Beacon item slot). Doesn't include offhand slot(it's appended after backpack slots)
        public byte PrependSlotCount { get; private set; }
        // Offhand slot, etc.
        public byte AppendSlotCount { get; private set; }
        
        public int OutputSlot { get; private set; }
        
        public int SlotCount => PrependSlotCount + MainSlotWidth * MainSlotHeight + (HasBackpackSlots ? 36 : 0) + AppendSlotCount;
        
        public bool HasOutputSlot => OutputSlot >= 0;
        
        private InventoryType(ResourceLocation id, byte mainSlotWidth, byte mainSlotHeight,
            byte prependSlotCount = 0, bool hasBackpackSlots = true, byte appendSlotCount = 0, int outputSlot = -1)
        {
            TypeId = id;
            HasBackpackSlots = hasBackpackSlots;
            AppendSlotCount = appendSlotCount;
            MainSlotWidth = mainSlotWidth;
            MainSlotHeight = mainSlotHeight;
            PrependSlotCount = prependSlotCount;
            OutputSlot = outputSlot;
        }

        public override string ToString()
        {
            return TypeId.ToString();
        }   
    }
}
