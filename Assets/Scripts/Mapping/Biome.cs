using System;

namespace MinecraftClient.Mapping
{
    /// <summary>
    /// Represents a Minecraft Block State
    /// </summary>
    public record Biome
    {


        public int NumeralId { get; }

        public float Temperature = 0F;
        public float Downfall    = 0F;
        public Precipitation Precipitation = Precipitation.None;

        public int SkyColor      = 0xFFFFFF;
        public int FoliageColor  = 0xFFFFFF;
        public int GrassColor    = 0xFFFFFF;
        public int FogColor      = 0xFFFFFF;
        public int WaterColor    = 0xFFFFFF;
        public int WaterFogColor = 0xFFFFFF;


        public ResourceLocation BiomeId { get; }

        public Biome(int numId, ResourceLocation id)
        {
            NumeralId = numId;
            BiomeId = id;
        }

        private static string GetColorPrev(int color)
        {
            var colorCode = $"{color:x}".PadLeft(6, '0');
            return $"<color=#{colorCode}>{colorCode}</color>";
        }

        public string GetDescription() => $"{BiomeId}\nTemperature: {Temperature:0.00}\tDownfall: {Precipitation} {Downfall:0.00}\n{GetColorPrev(SkyColor)} {GetColorPrev(FoliageColor)} {GetColorPrev(GrassColor)}\n{GetColorPrev(FogColor)} {GetColorPrev(WaterColor)} {GetColorPrev(WaterFogColor)}";

        public override string ToString() => BiomeId.ToString();
    }
}