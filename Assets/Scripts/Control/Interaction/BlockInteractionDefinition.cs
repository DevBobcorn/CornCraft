namespace CraftSharp.Control
{
    public enum BlockInteractionType
    {
        Break = 0, Interact = 1
    }

    public record BlockInteractionDefinition
    {
        public BlockInteractionType Type { get; }
        public InteractionIconType IconType { get; }
        public ResourceLocation IconItemId { get; }
        public string Identifier { get; }
        public string HintKey { get; } // If this property is left empty, the block's name will be used

        public BlockInteractionDefinition(BlockInteractionType type, InteractionIconType iconType, ResourceLocation iconItem, string id, string hintKey)
        {
            Type = type;
            IconType = iconType;
            IconItemId = iconItem;
            Identifier = id;
            HintKey = hintKey;
        }

        public override int GetHashCode()
        {
            return Identifier.GetHashCode();
        }
    }
}