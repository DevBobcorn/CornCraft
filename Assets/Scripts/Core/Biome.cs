using Unity.Mathematics;

namespace MinecraftClient.Mapping
{
    /// <summary>
    /// Represents a Minecraft Biome
    /// </summary>
    public record Biome
    {
        public float Temperature = 0F;
        public float Downfall    = 0F;
        
        public Precipitation Precipitation = Precipitation.None;

        public readonly int SkyColorInt, FoliageColorInt, GrassColorInt;
        public readonly int FogColorInt, WaterColorInt, WaterFogColorInt;
        public readonly float3 SkyColor, FoliageColor, GrassColor;
        public readonly float3 FogColor, WaterColor, WaterFogColor;
        private readonly string colorsText;

        private static float3 GetFloat3Color(int color)
        {
            float r = ((color & 0xFF0000) >> 16) / 255F;
            float g = ((color &   0xFF00) >>  8) / 255F;
            float b = ((color &     0xFF))       / 255F;

            return new(r, g, b);
        }

        public ResourceLocation BiomeId { get; }

        public Biome(ResourceLocation id, int sky, int foliage, int grass, int water, int fog, int waterFog)
        {
            BiomeId = id;

            // Set biome colors
            SkyColor = GetFloat3Color(sky);
            SkyColorInt = sky;
            FoliageColor = GetFloat3Color(foliage);
            FoliageColorInt = foliage;
            GrassColor = GetFloat3Color(grass);
            GrassColorInt = grass;
            WaterColor = GetFloat3Color(water);
            WaterColorInt = water;
            FogColor = GetFloat3Color(fog);
            FogColorInt = fog;
            WaterFogColor = GetFloat3Color(waterFog);
            WaterFogColorInt = waterFog;

            string colorText(int color)
            {
                var colorCode = $"{color:x}".PadLeft(6, '0');
                return $"<color=#{colorCode}>{colorCode}</color>";
            };

            colorsText = $"{colorText(SkyColorInt)} {colorText(FoliageColorInt)} {colorText(GrassColorInt)}\n" +
                    $"{colorText(FogColorInt)} {colorText(WaterColorInt)} {colorText(WaterFogColorInt)}";
        }

        public string GetDescription()
        {
            return $"{BiomeId}\nTemperature: {Temperature:0.00}\tDownfall: {Precipitation} {Downfall:0.00}\n{colorsText}";
        }

        public override string ToString() => BiomeId.ToString();
    }
}