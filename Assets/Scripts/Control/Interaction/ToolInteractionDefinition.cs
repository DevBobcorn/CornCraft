namespace CraftSharp.Control
{
    public record ToolInteractionDefinition
    {
        public ItemActionType Type { get; }

        public ToolInteractionDefinition(ItemActionType type)
        {
            Type = type;
        }

        public override int GetHashCode()
        {
            return Type.GetHashCode();
        }
    }
}