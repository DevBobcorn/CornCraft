using System;

namespace MinecraftClient.Mapping
{
    /// <summary>
    /// Represents a Minecraft Block State
    /// </summary>
    public record Biome
    {
        public int NumeralId { get; }
        public ResourceLocation BiomeId { get; }

        public Biome(int numId, ResourceLocation id)
        {
            NumeralId = numId;
            BiomeId = id;
        }

        public override string ToString() => BiomeId.ToString();
    }
}