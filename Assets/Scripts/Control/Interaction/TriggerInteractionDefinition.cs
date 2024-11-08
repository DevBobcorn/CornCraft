namespace CraftSharp.Control
{
    public enum TriggerInteractionType
    {
        Break = 0,
        Interact = 1
    }

    public enum InteractionIconType
    {
        Dialog,
        EnterLocation,
        Ride,
        ItemIcon
    }

    public record TriggerInteractionDefinition
    {
        public TriggerInteractionType Type { get; }
        public InteractionIconType IconType { get; }
        public ResourceLocation IconItemId { get; }
        public string Identifier { get; }
        public string HintKey { get; } // If this property is left empty, the block's name will be used

        public TriggerInteractionDefinition(TriggerInteractionType type, InteractionIconType iconType, ResourceLocation iconItem, string id, string hintKey)
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