using System.Collections.Generic;
using System.IO;
using System.Text;

using UnityEngine;

namespace MinecraftClient.Mapping
{
    public class BiomePalette
    {
        public static readonly BiomePalette INSTANCE = new();
        public static readonly Biome EMPTY = new(-1, ResourceLocation.INVALID);

        private readonly Dictionary<int, Biome> biomesTable = new();

        public Biome FromId(int biomeId) => biomesTable.GetValueOrDefault(biomeId, EMPTY);

        public void PrepareData(string dataVersion)
        {
            // Clear loaded stuff...
            biomesTable.Clear();

            biomesTable.Add(-1, EMPTY);

            var biomeListPath = PathHelper.GetExtraDataFile($"biomes-{dataVersion}.json");

            if (File.Exists(biomeListPath))
            {
                var biomeList = Json.ParseJson(File.ReadAllText(biomeListPath, Encoding.UTF8));

                foreach (var biome in biomeList.Properties)
                {
                    int numId;
                    if (int.TryParse(biome.Key, out numId))
                    {
                        int sky = 0, foliage = 0, grass = 0, water = 0, fog = 0, waterFog = 0;
                        float temp = 0F, rain = 0F;
                        Precipitation prec = Precipitation.None;

                        var biomeId = ResourceLocation.fromString(biome.Value.StringValue);
                        var biomeDefPath = PathHelper.GetExtraDataFile($"biome_defs-{dataVersion}\\{biomeId.path}.json");

                        if (File.Exists(biomeDefPath))
                        {
                            var biomeDef = Json.ParseJson(File.ReadAllText(biomeDefPath));

                            if (biomeDef.Properties.ContainsKey("effects"))
                            {
                                var effects = biomeDef.Properties["effects"].Properties;

                                if (effects.ContainsKey("sky_color"))
                                    int.TryParse(effects["sky_color"].StringValue, out sky);
                                
                                if (effects.ContainsKey("foliage_color"))
                                    int.TryParse(effects["foliage_color"].StringValue, out foliage);
                                
                                if (effects.ContainsKey("grass_color"))
                                    int.TryParse(effects["grass_color"].StringValue, out grass);
                                
                                if (effects.ContainsKey("fog_color"))
                                    int.TryParse(effects["fog_color"].StringValue, out fog);
                                
                                if (effects.ContainsKey("water_color"))
                                    int.TryParse(effects["water_color"].StringValue, out water);
                                
                                if (effects.ContainsKey("water_fog_color"))
                                    int.TryParse(effects["water_fog_color"].StringValue, out waterFog);

                            }

                            if (biomeDef.Properties.ContainsKey("downfall"))
                                float.TryParse(biomeDef.Properties["downfall"].StringValue, out rain);
                            
                            if (biomeDef.Properties.ContainsKey("temperature"))
                                float.TryParse(biomeDef.Properties["temperature"].StringValue, out temp);
                            
                            if (biomeDef.Properties.ContainsKey("precipitation"))
                            {
                                prec = biomeDef.Properties["precipitation"].StringValue.ToLower() switch
                                {
                                    "rain" => Precipitation.Rain,
                                    "snow" => Precipitation.Snow,
                                    "none" => Precipitation.None,

                                    _      => Precipitation.Unknown
                                };

                                if (prec == Precipitation.Unknown)
                                    Debug.LogWarning($"Unexpected precipitation type: {biomeDef.Properties["precipitation"].StringValue}");
                            }

                        }
                        else
                            Debug.LogWarning($"Biome definition for {biomeId} not found at {biomeDefPath}");

                        Biome newBiome = new(numId, biomeId)
                        {
                            Temperature = temp,
                            Downfall = rain,
                            Precipitation = prec,

                            SkyColor = sky,
                            FoliageColor = foliage,
                            GrassColor = grass,
                            WaterColor = water,
                            FogColor = fog,
                            WaterFogColor = waterFog
                        };

                        biomesTable.TryAdd(numId, newBiome);
                        //Debug.Log($"Loading biome {numId} {biome.Value.StringValue}");
                    }
                    else
                        Debug.LogWarning($"Invalid numeral biome key [{biome.Key}].");
                    
                }
            }
            else
                Debug.LogWarning("Biome dictionary not found!");

        }
    }
}