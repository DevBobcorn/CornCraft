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
        public string Hint { get; } // If this property is left empty, the block's name will be used

        public BlockInteractionDefinition(BlockInteractionType type, string id, string hint)
        {
            Type = type;
            Identifier = id;
            Hint = hint;
        }

        public override int GetHashCode()
        {
            return Identifier.GetHashCode();
        }
    }
}