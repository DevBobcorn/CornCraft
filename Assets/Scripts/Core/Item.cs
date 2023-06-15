namespace MinecraftClient.Mapping
{
    public record Item
    {
        public const int DEFAULT_STACK_LIMIT = 64;

        public static readonly ResourceLocation AIR_ID = new("air");

        public static readonly Item UNKNOWN  = new(new ResourceLocation("<missing_no>")); // Unsupported item type (Forge mod custom item...)
        public static readonly Item NULL     = new(new ResourceLocation("<null_item>"));  // Unspecified item type (Used in the network protocol)

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