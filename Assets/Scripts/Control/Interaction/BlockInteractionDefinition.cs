namespace CraftSharp.Control
{
    public enum BlockInteractionType
    {
        Break = 0, Interact = 1
    }

    public record BlockInteractionDefinition
    {
        public BlockInteractionType Type { get; }

        public string Hint { get; } // If this property is left empty, the block's name will be used

        public BlockInteractionDefinition(BlockInteractionType type, string hint)
        {
            Type = type;
            Hint = hint;
        }

        public override int GetHashCode()
        {
            return Hint.GetHashCode();
        }
    }
}