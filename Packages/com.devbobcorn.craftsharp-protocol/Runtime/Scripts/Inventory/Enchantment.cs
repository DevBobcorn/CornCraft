#nullable enable

namespace CraftSharp.Inventory
{
    public record Enchantment(Enchantments Type, int Level)
    {
        public Enchantments Type { get; } = Type;
        public int Level { get; } = Level;
    }
}