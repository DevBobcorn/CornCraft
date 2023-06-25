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

        private int skyColorInt, foliageColorInt, grassColorInt;
        private int fogColorInt, waterColorInt, waterFogColorInt;
        public float3 skyColor, foliageColor, grassColor;
        public float3 fogColor, waterColor, waterFogColor;

        private static float3 GetFloat3Color(int color)
        {
            float r = ((color & 0xFF0000) >> 16) / 255F;
            float g = ((color &   0xFF00) >>  8) / 255F;
            float b = ((color &     0xFF))       / 255F;

            return new(r, g, b);
        }

        public int SkyColor
        {
            set {
                skyColor = GetFloat3Color(value);
                skyColorInt = value;
            }
        }
        public int FoliageColor
        {
            set {
                foliageColor = GetFloat3Color(value);
                foliageColorInt = value;
            }
        }
        public int GrassColor
        {
            set {
                grassColor = GetFloat3Color(value);
                grassColorInt = value;
            }
        }
        public int FogColor
        {
            set {
                fogColor = GetFloat3Color(value);
                fogColorInt = value;
            }
        }
        public int WaterColor
        {
            set {
                waterColor = GetFloat3Color(value);
                waterColorInt = value;
            }
        }
        public int WaterFogColor
        {
            set {
                waterFogColor = GetFloat3Color(value);
                waterFogColorInt = value;
            }
        }

        public ResourceLocation BiomeId { get; }

        public Biome(ResourceLocation id)
        {
            BiomeId = id;
        }

        private static string GetColorText(int color)
        {
            var colorCode = $"{color:x}".PadLeft(6, '0');
            return $"<color=#{colorCode}>{colorCode}</color>";
        }

        public string GetDescription()
        {
            return $"{BiomeId}\nTemperature: {Temperature:0.00}\tDownfall: {Precipitation} {Downfall:0.00}\n{GetColorText(skyColorInt)} {GetColorText(foliageColorInt)} {GetColorText(grassColorInt)}\n{GetColorText(fogColorInt)} {GetColorText(waterColorInt)} {GetColorText(waterFogColorInt)}";
        }

        public override string ToString() => BiomeId.ToString();
    }
}