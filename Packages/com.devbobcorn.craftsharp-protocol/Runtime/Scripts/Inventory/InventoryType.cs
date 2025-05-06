using System.Collections.Generic;
using UnityEngine;

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
        public static readonly ResourceLocation SMITHING_OLD_ID  = new("legacy_smithing");   // Smithing Table in 1.19.4 with 'update_1_20' feature flag disabled
        public static readonly ResourceLocation SMITHING_ID      = new("smithing");          // Smithing Table in 1.16~1.19.3 and 1.20+, and in 1.19.4 with 'update_1_20' feature flag enabled
        public static readonly ResourceLocation SMOKER_ID        = new("smoker");            // Smoker
        public static readonly ResourceLocation CARTOGRAPHY_ID   = new("cartography_table"); // Cartography Table
        public static readonly ResourceLocation STONECUTTER_ID   = new("stonecutter");       // Stonecutter

        public static readonly InventoryType DUMMY_INVENTORY_TYPE = new(ResourceLocation.INVALID,
            0, 0, 0, false, false, 0, new(), new());
        
        public readonly ResourceLocation TypeId;
        
        public bool HasBackpackSlots { get; } // 9x3 slots from player backpack
        public bool HasHotbarSlots { get; } // 9x1 hotbar slots
        public int MainSlotWidth { get; }
        public int MainSlotHeight { get; }
        
        // Count of special slots (e.g. Crafting output slot or Beacon item slot). Doesn't include offhand slot(it's appended after backpack slots)
        public int PrependSlotCount { get; }
        // Offhand slot, etc.
        public int AppendSlotCount { get; }

        public int SlotCount => PrependSlotCount + MainSlotWidth * MainSlotHeight +
                                (HasBackpackSlots ? 27 : 0) + (HasHotbarSlots ? 9 : 0) + AppendSlotCount;

        private readonly Dictionary<int, InventorySlotInfo> extraSlotInfo;
        
        private readonly List<InventorySlotInfo> slotInfo;
        
        public readonly List<InventorySpriteInfo> spriteInfo;

        public Dictionary<int, string> PropertyNames = new();
        public Dictionary<string, int> PropertySlots = new();

        public record InventorySlotInfo(float PosX, float PosY, InventorySlotType Type,
            ItemStack PreviewItemStack, ResourceLocation? PlaceholderTypeId)
        {
            public float PosX { get; } = PosX;
            public float PosY { get; } = PosY;
            public InventorySlotType Type { get; } = Type;
            public ItemStack PreviewItemStack { get; } = PreviewItemStack;
            public ResourceLocation? PlaceholderTypeId { get; } = PlaceholderTypeId;
        }
        
        public record InventorySpriteInfo(float PosX, float PosY, int Width, int Height,
            ResourceLocation TypeId, string CurFillProperty = null, string MaxFillProperty = null)
        {
            public float PosX { get; } = PosX;
            public float PosY { get; } = PosY;
            public int Width { get; } = Width;
            public int Height { get; } = Height;
            public ResourceLocation TypeId { get; } = TypeId;
            public string CurFillProperty { get; set; } = CurFillProperty;
            public string MaxFillProperty { get; set; } = MaxFillProperty;
        }

        public Vector2 GetInventorySlotPos(int slot)
        {
            return extraSlotInfo.TryGetValue(slot, out var info) ? new(info.PosX, info.PosY) : Vector2.zero;
        }
        
        public ItemStack GetInventorySlotPreviewItem(int slot)
        {
            return extraSlotInfo.TryGetValue(slot, out var info) ? info.PreviewItemStack : null;
        }
        
        public ResourceLocation? GetInventorySlotPlaceholderSpriteTypeId(int slot)
        {
            return extraSlotInfo.TryGetValue(slot, out var info) ? info.PlaceholderTypeId : null;
        }
        
        public InventorySlotType GetInventorySlotType(int slot)
        {
            return extraSlotInfo.TryGetValue(slot, out var info) ?
                info.Type : InventorySlotTypePalette.INSTANCE.GetById(InventorySlotType.SLOT_TYPE_REGULAR_ID);
        }
        
        // UI Layout settings
        public int WorkPanelHeight { get; set; } = 3;
        public int ListPanelWidth { get; set; } = 0;
        public float MainPosX { get; set; } = 0;
        public float MainPosY { get; set; } = 0;
        
        public InventoryType(ResourceLocation id, int prependSlotCount, int mainSlotWidth, int mainSlotHeight,
            bool hasBackpackSlots, bool hasHotbarSlots, int appendSlotCount,
            Dictionary<int, InventorySlotInfo> extraSlotInfo, List<InventorySpriteInfo> spriteInfo)
        {
            TypeId = id;
            
            PrependSlotCount = prependSlotCount;
            MainSlotWidth = mainSlotWidth;
            MainSlotHeight = mainSlotHeight;
            HasBackpackSlots = hasBackpackSlots;
            HasHotbarSlots = hasHotbarSlots;
            AppendSlotCount = appendSlotCount;
            this.extraSlotInfo = extraSlotInfo;
            this.spriteInfo = spriteInfo;
        }

        public override string ToString()
        {
            return TypeId.ToString();
        }
    }
}
