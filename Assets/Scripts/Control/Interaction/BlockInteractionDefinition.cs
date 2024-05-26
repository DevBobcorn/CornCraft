namespace CraftSharp.Control
{
    public enum BlockInteractionType
    {
        Break = 0, Interact = 1
    }

    public record BlockInteractionDefinition
    {
        public BlockInteractionType Type { get; }
        public string Identifier { get; }
        public string HintKey { get; } // If this property is left empty, the block's name will be used

        public BlockInteractionDefinition(BlockInteractionType type, string id, string hintKey)
        {
            Type = type;
            Identifier = id;
            HintKey = hintKey;
        }

        public override int GetHashCode()
        {
            return Identifier.GetHashCode();
        }
    }
}