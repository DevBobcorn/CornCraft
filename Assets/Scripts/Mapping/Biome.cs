using Unity.Mathematics;

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

        private int skyColorInt, foliageColorInt, grassColorInt;
        private int fogColorInt, waterColorInt, waterFogColorInt;
        public float3 skyColor, foliageColor, grassColor;
        public float3 fogColor, waterColor, waterFogColor;

        public int SkyColor
        {
            set {
                skyColor = ColorHelper.MC2Float3(value);
                skyColorInt = value;
            }
        }
        public int FoliageColor
        {
            set {
                foliageColor = ColorHelper.MC2Float3(value);
                foliageColorInt = value;
            }
        }
        public int GrassColor
        {
            set {
                grassColor = ColorHelper.MC2Float3(value);
                grassColorInt = value;
            }
        }
        public int FogColor
        {
            set {
                fogColor = ColorHelper.MC2Float3(value);
                fogColorInt = value;
            }
        }
        public int WaterColor
        {
            set {
                waterColor = ColorHelper.MC2Float3(value);
                waterColorInt = value;
            }
        }
        public int WaterFogColor
        {
            set {
                waterFogColor = ColorHelper.MC2Float3(value);
                waterFogColorInt = value;
            }
        }

        public ResourceLocation BiomeId { get; }

        public Biome(int numId, ResourceLocation id)
        {
            NumeralId = numId;
            BiomeId = id;
        }

        public string GetDescription() => $"{BiomeId}\nTemperature: {Temperature:0.00}\tDownfall: {Precipitation} {Downfall:0.00}\n{ColorHelper.GetPreview(skyColorInt)} {ColorHelper.GetPreview(foliageColorInt)} {ColorHelper.GetPreview(grassColorInt)}\n{ColorHelper.GetPreview(fogColorInt)} {ColorHelper.GetPreview(waterColorInt)} {ColorHelper.GetPreview(waterFogColorInt)}";

        public override string ToString() => BiomeId.ToString();
    }
}