namespace CraftSharp
{
    /// <summary>
    /// Represents a Minecraft BlockEntity Type
    /// </summary>
    public record BlockEntityType
    {
        public static readonly BlockEntityType DUMMY_BLOCK_ENTITY_TYPE = new(0, ResourceLocation.INVALID);

        public int NumeralId { get; }

        public ResourceLocation BlockEntityId { get; }

        public BlockEntityType(int numId, ResourceLocation id)
        {
            NumeralId = numId;
            BlockEntityId = id;
        }

        public override string ToString()
        {
            return BlockEntityId.ToString();
        }
    }
}