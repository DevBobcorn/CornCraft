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

            string biomesPath = PathHelper.GetExtraDataFile($"biomes-{dataVersion}.json");

            Json.JSONData biomeList = Json.ParseJson(File.ReadAllText(biomesPath, Encoding.UTF8));

            foreach (var biome in biomeList.Properties)
            {
                int numId;
                if (int.TryParse(biome.Key, out numId))
                {
                    biomesTable.TryAdd(numId, new(numId, ResourceLocation.fromString(biome.Value.StringValue)));
                    //Debug.Log($"Loading biome {numId} {biome.Value.StringValue}");
                }
                else
                    Debug.LogWarning($"Invalid numeral biome key [{biome.Key}].");
                
            }

        }
    }
}