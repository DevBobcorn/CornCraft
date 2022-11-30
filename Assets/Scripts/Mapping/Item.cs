namespace MinecraftClient.Mapping
{
    public class Item
    {
        public static readonly ResourceLocation AIR_ID = new ResourceLocation("air");
        public static readonly Item UNKNOWN  = new Item(new("<missing_no>"));
        public static readonly Item AIR_ITEM = new Item(new ResourceLocation("air"));

        public readonly ResourceLocation itemId; // Something like 'minecraft:grass_block'

        public Item(ResourceLocation itemId)
        {
            this.itemId = itemId;
        }

        public override string ToString()
        {
            return itemId.ToString();
        }
    }
}