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
        public static readonly float3 DEFAULT_FOLIAGE = new float3(119, 171, 47) / 255F;
        public static readonly float3 DEFAULT_GRASS   = new float3(145, 189, 89) / 255F;
        public static readonly float3 DEFAULT_WATER   = new float3(63, 118, 228) / 255F;

        public virtual float3 GetFoliageColor(Location loc) => DEFAULT_FOLIAGE;

        public virtual float3 GetGrassColor(Location loc) => DEFAULT_GRASS;

        public virtual float3 GetWaterColor(Location loc) => DEFAULT_WATER;

    }
}