namespace CraftSharp.Inventory
{
    public record SuspiciousStewEffect(int TypeId, int Duration)
    {
        public int TypeId { get; } = TypeId;
        public int Duration { get; } = Duration;
    }
}