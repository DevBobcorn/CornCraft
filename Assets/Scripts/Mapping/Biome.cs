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

        public string GetDescription() => $"{BiomeId}\nTemperature: {Temperature:0.00}\tDownfall: {Precipitation} {Downfall:0.00}\n{ColorHelper.GetPreview(SkyColor)} {ColorHelper.GetPreview(FoliageColor)} {ColorHelper.GetPreview(GrassColor)}\n{ColorHelper.GetPreview(FogColor)} {ColorHelper.GetPreview(WaterColor)} {ColorHelper.GetPreview(WaterFogColor)}";

        public override string ToString() => BiomeId.ToString();
    }
}