using Unity.Mathematics;

namespace MinecraftClient.Mapping
{
    /// <summary>
    /// Represents a World
    /// </summary>
    public abstract class AbstractWorld
    {
        // Using biome colors of minecraft:plains as default
        // See https://minecraft.fandom.com/wiki/Plains
        public static readonly int DEFAULT_FOLIAGE = 0x77AB2F;
        public static readonly int DEFAULT_GRASS   = 0x91BD59;
        public static readonly int DEFAULT_WATER   = 0x3F76E4;

        public virtual float3 GetFoliageColor(Location loc) => DEFAULT_FOLIAGE;

        public virtual float3 GetGrassColor(Location loc) => DEFAULT_GRASS;

        public virtual float3 GetWaterColor(Location loc) => DEFAULT_WATER;

    }
}