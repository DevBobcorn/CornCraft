using Unity.Mathematics;

namespace CraftSharp
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

        public static readonly Biome DUMMY_BIOME = new(ResourceLocation.INVALID,
                0, DEFAULT_FOLIAGE, DEFAULT_GRASS, DEFAULT_WATER, 0, 0);

        public virtual float3 GetFoliageColor(Location loc) => DUMMY_BIOME.FoliageColor;

        public virtual float3 GetGrassColor(Location loc) => DUMMY_BIOME.GrassColor;

        public virtual float3 GetWaterColor(Location loc) => DUMMY_BIOME.WaterColor;

    }
}