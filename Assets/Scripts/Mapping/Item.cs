namespace MinecraftClient.Mapping
{
    public class Item
    {
        public const int DEFAULT_STACK_LIMIT = 64;

        public static readonly ResourceLocation AIR_ID = new ResourceLocation("air");
        public static readonly Item UNKNOWN  = new Item(new("<missing_no>")); // Unsupported item type (Forge mod custom item...)
        public static readonly Item NULL     = new Item(new("<null_item>"));  // Unspecified item type (Used in the network protocol)
        public static readonly Item AIR_ITEM = new Item(new ResourceLocation("air")) { StackLimit = 1 };

        public readonly ResourceLocation ItemId; // Something like 'minecraft:grass_block'
        public int StackLimit = DEFAULT_STACK_LIMIT;
        public ItemRarity Rarity = ItemRarity.Common;

        public bool IsStackable => StackLimit > 1;

        public Item(ResourceLocation itemId)
        {
            this.ItemId = itemId;
        }

        public override string ToString()
        {
            return ItemId.ToString();
        }
    }
}