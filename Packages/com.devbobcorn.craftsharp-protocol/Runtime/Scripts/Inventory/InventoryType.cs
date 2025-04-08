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

        public static readonly InventoryType PLAYER = new(PLAYER_ID,
            9, 0, 0, true, true, 1,
            new()
            {
                [0] = new(8, 1, InventorySlotType.Output),
                [1] = new(5, 2, InventorySlotType.Regular),
                [2] = new(6, 2, InventorySlotType.Regular),
                [3] = new(5, 1, InventorySlotType.Regular),
                [4] = new(6, 1, InventorySlotType.Regular),
                [5] = new(0, 3, InventorySlotType.Helmet),
                [6] = new(0, 2, InventorySlotType.Chestplate),
                [7] = new(0, 1, InventorySlotType.Leggings),
                [8] = new(0, 0, InventorySlotType.Boots),
                
                [45] = new(4, 0, InventorySlotType.Offhand)
            })
        {
            WorkPanelHeight = 4
        };
        
        public static readonly InventoryType HORSE_REGULAR = new(HORSE_ID,
            2, 0, 0, true, true, 0,
            new()
            {
                [0] = new(0, 2, InventorySlotType.HorseArmor),
                [1] = new(0, 2, InventorySlotType.Saddle)
            })
        {
            MainPosX = 4,
            MainPosY = 0,
        };
        public static readonly InventoryType HORSE_CHESTED = new(HORSE_ID,
            2, 5, 3, true, true, 0,
            new()
            {
                [0] = new(0, 2, InventorySlotType.HorseArmor),
                [1] = new(0, 2, InventorySlotType.Saddle)
            })
        {
            MainPosX = 4,
            MainPosY = 0
        };
        
        public static readonly InventoryType DUMMY_INVENTORY_TYPE = new(ResourceLocation.INVALID,
            0, 0, 0, false, false, 0, new());

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
        
        public readonly List<InventorySpriteInfo> spriteInfo = new();

        public record InventorySlotInfo(int PosX, int PosY, InventorySlotType Type)
        {
            public int PosX { get; } = PosX;
            public int PosY { get; } = PosY;
            public InventorySlotType Type { get; } = Type;
        }
        
        public record InventorySpriteInfo(int PosX, int PosY, int Width, int Height, ResourceLocation TypeId)
        {
            public int PosX { get; } = PosX;
            public int PosY { get; } = PosY;
            public int Width { get; } = Width;
            public int Height { get; } = Height;
            public ResourceLocation TypeId { get; } = TypeId;
        }

        public Vector2Int GetInventorySlotPos(int slot)
        {
            return extraSlotInfo.TryGetValue(slot, out var slotInfo) ? new(slotInfo.PosX, slotInfo.PosY): Vector2Int.zero;
        }
        
        public InventorySlotType GetInventorySlotType(int slot)
        {
            return extraSlotInfo.TryGetValue(slot, out var slotInfo) ? slotInfo.Type : InventorySlotType.Regular;
        }
        
        // UI Layout settings
        public int WorkPanelHeight { get; set; } = 3;
        public int ListPanelWidth { get; set; } = 0;
        public int MainPosX { get; set; } = 0;
        public int MainPosY { get; set; } = 0;
        
        public InventoryType(ResourceLocation id, int prependSlotCount, int mainSlotWidth, int mainSlotHeight,
            bool hasBackpackSlots, bool hasHotbarSlots, int appendSlotCount, Dictionary<int, InventorySlotInfo> extraSlotInfo)
        {
            TypeId = id;
            
            PrependSlotCount = prependSlotCount;
            MainSlotWidth = mainSlotWidth;
            MainSlotHeight = mainSlotHeight;
            HasBackpackSlots = hasBackpackSlots;
            HasHotbarSlots = hasHotbarSlots;
            AppendSlotCount = appendSlotCount;
            this.extraSlotInfo = extraSlotInfo;
        }

        public override string ToString()
        {
            return TypeId.ToString();
        }
    }
}
