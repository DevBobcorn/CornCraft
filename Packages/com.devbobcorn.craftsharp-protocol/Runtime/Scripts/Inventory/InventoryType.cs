#nullable enable
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
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
                0, 0, 0, false, false, 0, new(new(), new(), new(), new(), new()));
        
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

        public readonly InventoryLayoutInfo WorkPanelLayout;

        public Dictionary<int, string> PropertyNames = new();
        public Dictionary<string, int> PropertySlots = new();

        public record InventoryLayoutInfo(
            List<InventorySpriteInfo>? SpriteInfo,
            Dictionary<int, InventorySlotInfo>? SlotInfo,
            Dictionary<int, InventoryInputInfo>? InputInfo,
            List<InventoryLabelInfo>? LabelInfo,
            Dictionary<int, InventoryButtonInfo>? ButtonInfo,
            Dictionary<int, InventoryScrollViewInfo>? ScrollViewInfo = null) : InventoryFragmentInfo
        {
            public List<InventorySpriteInfo>? SpriteInfo { get; } = SpriteInfo;
            public Dictionary<int, InventorySlotInfo>? SlotInfo { get; } = SlotInfo;
            public Dictionary<int, InventoryInputInfo>? InputInfo { get; } = InputInfo;
            public List<InventoryLabelInfo>? LabelInfo { get; } = LabelInfo;
            public Dictionary<int, InventoryButtonInfo>? ButtonInfo { get; } = ButtonInfo;
            public Dictionary<int, InventoryScrollViewInfo>? ScrollViewInfo { get; } = ScrollViewInfo;

            public static InventoryLayoutInfo FromJson(Json.JSONData data)
            {
                List<InventorySpriteInfo>? spriteInfo = null;
                Dictionary<int, InventorySlotInfo>? slotInfo = null;
                Dictionary<int, InventoryInputInfo>? inputInfo = null;
                List<InventoryLabelInfo>? labelInfo = null;
                Dictionary<int, InventoryButtonInfo>? buttonInfo = null;
                Dictionary<int, InventoryScrollViewInfo>? scrollViewInfo = null;
                
                if (data.Properties.TryGetValue("sprites", out var val))
                {
                    spriteInfo = val.DataArray.Select(InventorySpriteInfo.FromJson).ToList();
                }

                if (data.Properties.TryGetValue("slots", out val))
                {
                    slotInfo = val.Properties.ToDictionary(x => int.Parse(x.Key),
                        x => InventorySlotInfo.FromJson(x.Value));
                }

                if (data.Properties.TryGetValue("inputs", out val))
                {
                    inputInfo = val.Properties.ToDictionary(x => int.Parse(x.Key),
                        x => InventoryInputInfo.FromJson(x.Value));
                }

                if (data.Properties.TryGetValue("labels", out val))
                {
                    labelInfo = val.DataArray.Select(InventoryLabelInfo.FromJson).ToList();
                }

                if (data.Properties.TryGetValue("buttons", out val))
                {
                    buttonInfo = val.Properties.ToDictionary(x => int.Parse(x.Key),
                        x => InventoryButtonInfo.FromJson(x.Value));
                }
                
                if (data.Properties.TryGetValue("scroll_views", out val))
                {
                    scrollViewInfo = val.Properties.ToDictionary(x => int.Parse(x.Key),
                        x => InventoryScrollViewInfo.FromJson(x.Value));
                }

                var layoutInfo = new InventoryLayoutInfo(spriteInfo, slotInfo, inputInfo, labelInfo, buttonInfo, scrollViewInfo);
                layoutInfo.ReadBaseInfo(data);

                return layoutInfo;
            }
        }

        public enum PredicateType
        {
            Visible,
            Enabled,
            Selected
        }

        public record InventoryFragmentInfo
        {
            public readonly Dictionary<PredicateType, InventoryPropertyPredicate> Predicates = new();
            public string? HintTranslationKey { get; private set; }

            protected void ReadBaseInfo(Json.JSONData data)
            {
                if (data.Properties.TryGetValue("visible_predicate", out var predicate))
                {
                    Predicates.Add(PredicateType.Visible,
                        InventoryPropertyPredicate.FromString(predicate.StringValue));
                }
                if (data.Properties.TryGetValue("enabled_predicate", out predicate))
                {
                    Predicates.Add(PredicateType.Enabled,
                        InventoryPropertyPredicate.FromString(predicate.StringValue));
                }
                if (data.Properties.TryGetValue("selected_predicate", out predicate))
                {
                    Predicates.Add(PredicateType.Selected,
                        InventoryPropertyPredicate.FromString(predicate.StringValue));
                }
                if (data.Properties.TryGetValue("hint_translation_key", out var val))
                {
                    HintTranslationKey = val.StringValue;
                }
            }
        }

        public record InventorySlotInfo(float PosX, float PosY, ResourceLocation TypeId,
            ItemStack? PreviewItemStack, ResourceLocation? PlaceholderTypeId) : InventoryFragmentInfo
        {
            public float PosX { get; } = PosX;
            public float PosY { get; } = PosY;
            public ResourceLocation TypeId { get; } = TypeId;
            public ItemStack? PreviewItemStack { get; } = PreviewItemStack;
            public ResourceLocation? PlaceholderTypeId { get; } = PlaceholderTypeId;

            public static InventorySlotInfo FromJson(Json.JSONData data)
            {
                var typeId = data.Properties.TryGetValue("type_id", out var val) ?
                    ResourceLocation.FromString(val.StringValue) : InventorySlotType.SLOT_TYPE_REGULAR_ID;
                
                var x = data.Properties.TryGetValue("pos_x", out val) ?
                    float.Parse(val.StringValue, CultureInfo.InvariantCulture.NumberFormat) : 0;
                var y = data.Properties.TryGetValue("pos_y", out val) ?
                    float.Parse(val.StringValue, CultureInfo.InvariantCulture.NumberFormat) : 0;

                ItemStack? previewItem = data.Properties.TryGetValue("preview_item", out val)
                                        ? ItemStack.FromJson(val) : null;
                
                ResourceLocation? placeholderTypeId = data.Properties.TryGetValue("placeholder_type_id", out val) ?
                    ResourceLocation.FromString(val.StringValue) : null;

                var slotInfo = new InventorySlotInfo(x, y, typeId, previewItem, placeholderTypeId);
                slotInfo.ReadBaseInfo(data);

                return slotInfo;
            }
        }
        
        public record InventorySpriteInfo(float PosX, float PosY, float Width,
            float Height, ResourceLocation TypeId, string? CurFillProperty = null,
            string? MaxFillProperty = null, string? FlipIdxProperty = null) : InventoryFragmentInfo
        {
            public float PosX { get; } = PosX;
            public float PosY { get; } = PosY;
            public float Width { get; } = Width;
            public float Height { get; } = Height;
            public ResourceLocation TypeId { get; } = TypeId;
            public string? CurFillProperty { get; set; } = CurFillProperty;
            public string? MaxFillProperty { get; set; } = MaxFillProperty;
            public string? FlipIdxProperty { get; set; } = FlipIdxProperty;

            public static InventorySpriteInfo FromJson(Json.JSONData data)
            {
                var typeId = data.Properties.TryGetValue("type_id", out var val) ?
                    ResourceLocation.FromString(val.StringValue) : ResourceLocation.INVALID;
                
                var x = data.Properties.TryGetValue("pos_x", out val) ?
                    float.Parse(val.StringValue, CultureInfo.InvariantCulture.NumberFormat) : 0;
                var y = data.Properties.TryGetValue("pos_y", out val) ?
                    float.Parse(val.StringValue, CultureInfo.InvariantCulture.NumberFormat) : 0;
                var w = data.Properties.TryGetValue("width", out val) ?
                    float.Parse(val.StringValue, CultureInfo.InvariantCulture.NumberFormat) : 1;
                var h = data.Properties.TryGetValue("height", out val) ?
                    float.Parse(val.StringValue, CultureInfo.InvariantCulture.NumberFormat) : 1;

                var spriteInfo = new InventorySpriteInfo(x, y, w, h, typeId);

                if (data.Properties.TryGetValue("cur_value_property", out val))
                    spriteInfo.CurFillProperty = val.StringValue;
                
                if (data.Properties.TryGetValue("max_value_property", out val))
                    spriteInfo.MaxFillProperty = val.StringValue;
                
                if (data.Properties.TryGetValue("flipbook_index_property", out val))
                    spriteInfo.FlipIdxProperty = val.StringValue;
                
                spriteInfo.ReadBaseInfo(data);
                
                return spriteInfo;
            }
        }

        public record InventoryInputInfo(float PosX, float PosY, float Width,
            string? PlaceholderTranslationKey) : InventoryFragmentInfo
        {
            public float PosX { get; } = PosX;
            public float PosY { get; } = PosY;
            public float Width { get; } = Width;
            public string? PlaceholderTranslationKey { get; } = PlaceholderTranslationKey;

            public static InventoryInputInfo FromJson(Json.JSONData data)
            {
                var translationKey = data.Properties.TryGetValue("translation_key", out var val) ?
                    val.StringValue : null;

                var x = data.Properties.TryGetValue("pos_x", out val) ?
                    float.Parse(val.StringValue, CultureInfo.InvariantCulture.NumberFormat) : 0;
                var y = data.Properties.TryGetValue("pos_y", out val) ?
                    float.Parse(val.StringValue, CultureInfo.InvariantCulture.NumberFormat) : 0;
                var w = data.Properties.TryGetValue("width", out val) ?
                    float.Parse(val.StringValue, CultureInfo.InvariantCulture.NumberFormat) : 5;
                
                var inputInfo = new InventoryInputInfo(x, y, w, translationKey);
                inputInfo.ReadBaseInfo(data);

                return inputInfo;
            }
        }

        public enum LabelAlignment
        {
            Left, Center, Right
        }

        public static LabelAlignment GetLabelAlignment(string alignmentName)
        {
            return alignmentName switch
            {
                "left" => LabelAlignment.Left,
                "center" => LabelAlignment.Center,
                "right" => LabelAlignment.Right,
                _ => throw new InvalidDataException($"Label alignment {alignmentName} is not defined!")
            };
        }

        public record InventoryLabelInfo(float PosX, float PosY, float Width, float Height,
            LabelAlignment Alignment, string? TextTranslationKey, string? ContentProperty) : InventoryFragmentInfo
        {
            public float PosX { get; } = PosX;
            public float PosY { get; } = PosY;
            public float Width { get; } = Width;
            public float Height { get; } = Height;
            public LabelAlignment Alignment { get; } = Alignment;
            public string? TextTranslationKey { get; } = TextTranslationKey;
            public string? ContentProperty { get; } = ContentProperty;

            public static InventoryLabelInfo FromJson(Json.JSONData data)
            {
                var translationKey = data.Properties.TryGetValue("translation_key", out var val) ?
                    val.StringValue : null;

                var x = data.Properties.TryGetValue("pos_x", out val) ?
                    float.Parse(val.StringValue, CultureInfo.InvariantCulture.NumberFormat) : 0;
                var y = data.Properties.TryGetValue("pos_y", out val) ?
                    float.Parse(val.StringValue, CultureInfo.InvariantCulture.NumberFormat) : 0;
                var w = data.Properties.TryGetValue("width", out val) ?
                    float.Parse(val.StringValue, CultureInfo.InvariantCulture.NumberFormat) : 6;
                var h = data.Properties.TryGetValue("height", out val) ?
                    float.Parse(val.StringValue, CultureInfo.InvariantCulture.NumberFormat) : 1;
                var alignment = data.Properties.TryGetValue("alignment", out val) ?
                    GetLabelAlignment(val.StringValue) : LabelAlignment.Left;
                var contentProperty = data.Properties.TryGetValue("content_property", out val) ?
                    val.StringValue : null;
                
                var labelInfo = new InventoryLabelInfo(x, y, w, h, alignment, translationKey, contentProperty);
                labelInfo.ReadBaseInfo(data);

                return labelInfo;
            }
        }

        public record InventoryButtonInfo(float PosX, float PosY, float Width,
            float Height, InventoryLayoutInfo LayoutInfo) : InventoryFragmentInfo
        {
            public float PosX { get; } = PosX;
            public float PosY { get; } = PosY;
            public float Width { get; } = Width;
            public float Height { get; } = Height;
            public InventoryLayoutInfo LayoutInfo { get; } = LayoutInfo;

            public static InventoryButtonInfo FromJson(Json.JSONData data)
            {
                var buttonLayout = InventoryLayoutInfo.FromJson(data);
                
                var x = data.Properties.TryGetValue("pos_x", out var val) ?
                    float.Parse(val.StringValue, CultureInfo.InvariantCulture.NumberFormat) : 0;
                var y = data.Properties.TryGetValue("pos_y", out val) ?
                    float.Parse(val.StringValue, CultureInfo.InvariantCulture.NumberFormat) : 0;
                var w = data.Properties.TryGetValue("width", out val) ?
                    float.Parse(val.StringValue, CultureInfo.InvariantCulture.NumberFormat) : 1;
                var h = data.Properties.TryGetValue("height", out val) ?
                    float.Parse(val.StringValue, CultureInfo.InvariantCulture.NumberFormat) : 1;
                
                var buttonInfo = new InventoryButtonInfo(x, y, w, h, buttonLayout);
                buttonInfo.ReadBaseInfo(data);

                return buttonInfo;
            }
        }

        public record InventoryScrollViewInfo(float PosX, float PosY, float Width,
            float Height) : InventoryFragmentInfo
        {
            public float PosX { get; } = PosX;
            public float PosY { get; } = PosY;
            public float Width { get; } = Width;
            public float Height { get; } = Height;

            public static InventoryScrollViewInfo FromJson(Json.JSONData data)
            {
                var x = data.Properties.TryGetValue("pos_x", out var val) ?
                    float.Parse(val.StringValue, CultureInfo.InvariantCulture.NumberFormat) : 0;
                var y = data.Properties.TryGetValue("pos_y", out val) ?
                    float.Parse(val.StringValue, CultureInfo.InvariantCulture.NumberFormat) : 0;
                var w = data.Properties.TryGetValue("width", out val) ?
                    float.Parse(val.StringValue, CultureInfo.InvariantCulture.NumberFormat) : 1;
                var h = data.Properties.TryGetValue("height", out val) ?
                    float.Parse(val.StringValue, CultureInfo.InvariantCulture.NumberFormat) : 1;
                
                var scrollViewInfo = new InventoryScrollViewInfo(x, y, w, h);
                scrollViewInfo.ReadBaseInfo(data);

                return scrollViewInfo;
            }
        }
        
        public InventorySlotInfo GetWorkPanelSlotInfo(int slot)
        {
            return WorkPanelLayout.SlotInfo.TryGetValue(slot, out var info) ? info :
                // ReSharper disable once PossibleLossOfFraction
                new(slot % 9, slot / 9, InventorySlotType.SLOT_TYPE_REGULAR_ID, null, null);
        }
        
        // UI Layout settings
        public int WorkPanelHeight { get; set; } = 3;
        public int ListPanelWidth { get; set; } = 0;
        public float MainPosX { get; set; } = 0;
        public float MainPosY { get; set; } = 0;
        
        public InventoryType(ResourceLocation id, int prependSlotCount, int mainSlotWidth, int mainSlotHeight,
            bool hasBackpackSlots, bool hasHotbarSlots, int appendSlotCount, InventoryLayoutInfo workPanelLayout)
        {
            TypeId = id;
            
            PrependSlotCount = prependSlotCount;
            MainSlotWidth = mainSlotWidth;
            MainSlotHeight = mainSlotHeight;
            HasBackpackSlots = hasBackpackSlots;
            HasHotbarSlots = hasHotbarSlots;
            AppendSlotCount = appendSlotCount;
            WorkPanelLayout = workPanelLayout;
        }

        public override string ToString()
        {
            return TypeId.ToString();
        }
    }
}
