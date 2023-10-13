#nullable enable
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

using CraftSharp.Resource;

namespace CraftSharp
{
    /// <summary>
    /// Represents a Minecraft World
    /// </summary>
    public class World : AbstractWorld
    {
        /// <summary>
        /// The chunks contained into the Minecraft world
        /// </summary>
        private readonly ConcurrentDictionary<int2, ChunkColumn> chunks = new();
        public readonly ConcurrentDictionary<int2, Queue<byte>> LightDataCache = new();
        
        /// <summary>
        /// The dimension info of the world
        /// </summary>
        private static Dimension curDimension = new();
        private static readonly Dictionary<string, Dimension> dimensionList = new();

        public static bool BiomesInitialized { get; private set; } = false;

        /// <summary>
        /// The biomes of the world
        /// </summary>
        private static readonly Dictionary<int, Biome> biomeList = new();

        /// <summary>
        /// Chunk data parsing progress
        /// </summary>
        public int chunkCnt = 0;
        public int chunkLoadNotCompleted = 0;

        /// <summary>
        /// Read, set or unload the specified chunk column
        /// </summary>
        /// <param name="chunkX">ChunkColumn X</param>
        /// <param name="chunkZ">ChunkColumn Z</param>
        /// <returns>chunk at the given location</returns>
        public ChunkColumn? this[int chunkX, int chunkZ]
        {
            get
            {
                chunks.TryGetValue(new(chunkX, chunkZ), out ChunkColumn? chunkColumn);
                return chunkColumn;
            }
            set
            {
                int2 chunkCoord = new(chunkX, chunkZ);
                if (value is null)
                    chunks.TryRemove(chunkCoord, out _);
                else
                    chunks.AddOrUpdate(chunkCoord, value, (_, _) => value);
            }
        }

        // Chunk column data is sent one whole column per time,
        // a whole air chunk is represent by null
        public bool isChunkColumnReady(int chunkX, int chunkZ)
        {
            if (chunks.TryGetValue(new(chunkX, chunkZ), out ChunkColumn? chunkColumn))
                return (chunkColumn is not null && chunkColumn.FullyLoaded && chunkColumn.LightingPresent);
            return false;
        }

        /// <summary>
        /// Storage of all dimensional data - 1.19.1 and above
        /// </summary>
        /// <param name="registryCodec">Registry Codec nbt data</param>
        public static void StoreDimensionList(Dictionary<string, object> registryCodec)
        {
            var dimensionListNbt = (object[])(((Dictionary<string, object>)registryCodec["minecraft:dimension_type"])["value"]);
            foreach (Dictionary<string, object> dimensionNbt in dimensionListNbt)
            {
                string dimensionName = (string)dimensionNbt["name"];
                Dictionary<string, object> dimensionType = (Dictionary<string, object>)dimensionNbt["element"];
                StoreOneDimension(dimensionName, dimensionType);
            }
        }

        /// <summary>
        /// Storage of all dimensional data - 1.19.1 and above
        /// </summary>
        /// <param name="registryCodec">Registry Codec nbt data</param>
        public static void StoreBiomeList(Dictionary<string, object> registryCodec)
        {
            var biomeListNbt = (object[])(((Dictionary<string, object>)registryCodec["minecraft:worldgen/biome"])["value"]);

            var packManager = ResourcePackManager.Instance;
            var grassMap = packManager.GrassColormapPixels;
            var foliageMap = packManager.FoliageColormapPixels;
            var mapSize = packManager.ColormapSize;

            foreach (Dictionary<string, object> biomeNbt in biomeListNbt)
            {
                StoreOneBiome(biomeNbt, mapSize, grassMap, foliageMap);
            }
            BiomesInitialized = true;
        }

        /// <summary>
        /// Store one dimension - Directly used in 1.16.2 to 1.18.2
        /// </summary>
        /// <param name="dimensionName">Dimension name</param>
        /// <param name="dimensionType">Dimension Type nbt data</param>
        public static void StoreOneDimension(string dimensionName, Dictionary<string, object> dimensionType)
        {
            if (dimensionList.ContainsKey(dimensionName))
                dimensionList.Remove(dimensionName);
            dimensionList.Add(dimensionName, new Dimension(dimensionName, dimensionType));
        }

        /// <summary>
        /// Set current dimension - 1.16 and above
        /// </summary>
        /// <param name="name">	The name of the dimension type</param>
        /// <param name="nbt">The dimension type (NBT Tag Compound)</param>
        public static void SetDimension(string name)
        {
            curDimension = dimensionList[name]; // Should not fail
        }

        /// <summary>
        /// Get current dimension
        /// </summary>
        /// <returns>Current dimension</returns>
        public static Dimension GetDimension()
        {
            return curDimension;
        }

        /// <summary>
        /// Store one biome
        /// </summary>
        /// <param name="biomeName">Biome name</param>
        /// <param name="biomeData">Information of this biome</param>
        public static void StoreOneBiome(Dictionary<string, object> biomeData, int mapSize,
                Color32[] grassMap, Color32[] foliageMap)
        {
            var biomeName = (string)biomeData["name"];
            var biomeNumId = (int)biomeData["id"];
            var biomeId = ResourceLocation.FromString(biomeName);

            if (biomeList.ContainsKey(biomeNumId))
                biomeList.Remove(biomeNumId);
            
            //Debug.Log($"Biome registered:\n{Json.Dictionary2Json(biomeData)}");

            int sky = 0, foliage = 0, grass = 0, water = 0, fog = 0, waterFog = 0;
            float temperature = 0F, downfall = 0F, adjustedTemp = 0F, adjustedRain = 0F;
            Precipitation precipitation = Precipitation.None;

            var biomeDef = (Dictionary<string, object>)biomeData["element"];

            if (biomeDef.ContainsKey("downfall"))
                downfall = (float) biomeDef["downfall"];
                            
            if (biomeDef.ContainsKey("temperature"))
                temperature = (float) biomeDef["temperature"];
            
            if (biomeDef.ContainsKey("precipitation"))
            {
                precipitation = ((string) biomeDef["precipitation"]).ToLower() switch
                {
                    "rain" => Precipitation.Rain,
                    "snow" => Precipitation.Snow,
                    "none" => Precipitation.None,

                    _      => Precipitation.Unknown
                };

                if (precipitation == Precipitation.Unknown)
                    Debug.LogWarning($"Unexpected precipitation type: {biomeDef["precipitation"]}");
            }

            if (biomeDef.ContainsKey("effects"))
            {
                var effects = (Dictionary<string, object>)biomeDef["effects"];

                if (effects.ContainsKey("sky_color"))
                    sky = (int) effects["sky_color"];
                
                adjustedTemp = Mathf.Clamp01(temperature);
                adjustedRain = Mathf.Clamp01(downfall) * adjustedTemp;

                int sampleX = (int)((1F - adjustedTemp) * mapSize);
                int sampleY = (int)(adjustedRain * mapSize);

                if (effects.ContainsKey("foliage_color"))
                    foliage = (int)effects["foliage_color"];
                else // Read foliage color from color map. See https://minecraft.fandom.com/wiki/Color
                {
                    var color = foliageMap[sampleY * mapSize + sampleX];
                    foliage = (color.r << 16) | (color.g << 8) | color.b;
                }
                
                if (effects.ContainsKey("grass_color"))
                    grass = (int)effects["grass_color"];
                else // Read grass color from color map. Same as above
                {
                    var color = grassMap[sampleY * mapSize + sampleX];
                    grass = (color.r << 16) | (color.g << 8) | color.b;
                }
                
                if (effects.ContainsKey("fog_color"))
                    fog = (int)effects["fog_color"];
                
                if (effects.ContainsKey("water_color"))
                    water = (int)effects["water_color"];
                
                if (effects.ContainsKey("water_fog_color"))
                    waterFog = (int)effects["water_fog_color"];
            }

            Biome biome = new(biomeId, sky, foliage, grass, water, fog, waterFog)
            {
                Temperature = temperature,
                Downfall = downfall,
                Precipitation = precipitation
            };

            biomeList.Add(biomeNumId, biome);
        }

        /// <summary>
        /// Store chunk at the specified location
        /// </summary>
        /// <param name="chunkX">ChunkColumn X</param>
        /// <param name="chunkY">ChunkColumn Y</param>
        /// <param name="chunkZ">ChunkColumn Z</param>
        /// <param name="chunkColumnSize">ChunkColumn size</param>
        /// <param name="chunk">Chunk data</param>
        public void StoreChunk(int chunkX, int chunkY, int chunkZ, int chunkColumnSize, Chunk? chunk)
        {
            ChunkColumn chunkColumn = chunks.GetOrAdd(new(chunkX, chunkZ), (_) => new(chunkColumnSize));
            chunkColumn[chunkY] = chunk;
        }

        /// <summary>
        /// Create empty chunk column at the specified location
        /// </summary>
        /// <param name="chunkX">ChunkColumn X</param>
        /// <param name="chunkZ">ChunkColumn Z</param>
        /// <param name="chunkColumnSize">ChunkColumn size</param>
        public void CreateEmptyChunkColumn(int chunkX, int chunkZ, int chunkColumnSize)
        {
            chunks.GetOrAdd(new(chunkX, chunkZ), (_) => new(chunkColumnSize));
        }

        /// <summary>
        /// Get chunk column at the specified location
        /// </summary>
        /// <param name="location">Location to retrieve chunk column</param>
        /// <returns>The chunk column</returns>
        public ChunkColumn? GetChunkColumn(BlockLoc location)
        {
            return this[location.GetChunkX(), location.GetChunkZ()];
        }

        public ChunkColumn? GetChunkColumn(int chunkX, int chunkZ)
        {
            return this[chunkX, chunkZ];
        }

        private static readonly Block AIR_INSTANCE = new Block(0);

        /// <summary>
        /// Get block at the specified location
        /// </summary>
        /// <param name="blockLoc">Location to retrieve block from</param>
        /// <returns>Block at specified location or Air if the location is not loaded</returns>
        public Block GetBlock(BlockLoc blockLoc)
        {
            var column = GetChunkColumn(blockLoc);
            if (column != null)
            {
                var chunk = column.GetChunk(blockLoc);
                if (chunk != null)
                    return chunk.GetBlock(blockLoc);
            }
            return AIR_INSTANCE; // Air
        }

        public Biome GetBiome(BlockLoc blockLoc)
        {
            var column = GetChunkColumn(blockLoc);
            if (column != null)
                return biomeList.GetValueOrDefault(column.GetBiomeId(blockLoc), DUMMY_BIOME);
            
            return DUMMY_BIOME; // Not available
        }

        public bool GetIsOpaque(BlockLoc blockLoc)
        {
            var column = GetChunkColumn(blockLoc);
            if (column != null)
                return column.GetIsOpaque(blockLoc);
            
            return false; // Not available
        }

        public byte GetSkyLight(BlockLoc blockLoc)
        {
            var column = GetChunkColumn(blockLoc);
            if (column != null)
                return column.GetSkyLight(blockLoc);
            
            return (byte) 0; // Not available
        }

        public byte GetBlockLight(BlockLoc blockLoc)
        {
            var column = GetChunkColumn(blockLoc);
            if (column != null)
                return column.GetBlockLight(blockLoc);
            
            return (byte) 0; // Not available
        }

        private const int radius = 2;

        public override float3 GetFoliageColor(BlockLoc blockLoc)
        {
            int cnt = 0;
            float3 colorSum = float3.zero;
            for (int x = -radius;x <= radius;x++)
                for (int y = -radius;y <= radius;y++)
                    for (int z = -radius;z < radius;z++)
                    {
                        var b = GetBiome(blockLoc + new BlockLoc(x, y, z));
                        if (b != DUMMY_BIOME)
                        {
                            cnt++;
                            colorSum += b.FoliageColor;
                        }
                    }
            cnt = (cnt == 0) ? 1 : cnt;
            return colorSum / cnt;
        }

        public override float3 GetGrassColor(BlockLoc blockLoc)
        {
            int cnt = 0;
            float3 colorSum = float3.zero;
            for (int x = -radius;x <= radius;x++)
                for (int y = -radius;y <= radius;y++)
                    for (int z = -radius;z < radius;z++)
                    {
                        var b = GetBiome(blockLoc + new BlockLoc(x, y, z));
                        if (b != DUMMY_BIOME)
                        {
                            cnt++;
                            colorSum += b.GrassColor;
                        }
                    }
            cnt = (cnt == 0) ? 1 : cnt;
            return colorSum / cnt;
        }

        public override float3 GetWaterColor(BlockLoc blockLoc)
        {
            int cnt = 0;
            float3 colorSum = float3.zero;
            for (int x = -radius;x <= radius;x++)
                for (int y = -radius;y <= radius;y++)
                    for (int z = -radius;z < radius;z++)
                    {
                        var b = GetBiome(blockLoc + new BlockLoc(x, y, z));
                        if (b != DUMMY_BIOME)
                        {
                            cnt++;
                            colorSum += b.WaterColor;
                        }
                    }
            cnt = (cnt == 0) ? 1 : cnt;
            return colorSum / cnt;
        }

        public bool IsWaterAt(BlockLoc blockLoc)
        {
            var column = GetChunkColumn(blockLoc);
            if (column != null)
            {
                var chunk = column.GetChunk(blockLoc);
                if (chunk != null)
                    return chunk.GetBlock(blockLoc).State.InWater;
            }
            return false;
        }

        public bool IsLavaAt(BlockLoc blockLoc)
        {
            var column = GetChunkColumn(blockLoc);
            if (column != null)
            {
                var chunk = column.GetChunk(blockLoc);
                if (chunk != null)
                    return chunk.GetBlock(blockLoc).State.InLava;
            }
            return false;
        }

        public bool IsLiquidAt(BlockLoc blockLoc)
        {
            var column = GetChunkColumn(blockLoc);
            if (column != null)
            {
                var chunk = column.GetChunk(blockLoc);
                if (chunk != null)
                    return chunk.GetBlock(blockLoc).State.InWater || chunk.GetBlock(blockLoc).State.InLava;
            }
            return false;
        }

        public int GetCullFlags(BlockLoc blockLoc, Block self, BlockNeighborCheck check)
        {
            int cullFlags = 0;
            if (check(self, GetBlock(blockLoc.Up())))
                cullFlags |= (1 << 0);
            if (check(self, GetBlock(blockLoc.Down())))
                cullFlags |= (1 << 1);
            if (check(self, GetBlock(blockLoc.South())))
                cullFlags |= (1 << 2);
            if (check(self, GetBlock(blockLoc.North())))
                cullFlags |= (1 << 3);
            if (check(self, GetBlock(blockLoc.East())))
                cullFlags |= (1 << 4);
            if (check(self, GetBlock(blockLoc.West())))
                cullFlags |= (1 << 5);
            return cullFlags;
        }

        /// <summary>
        /// Set block at the specified location
        /// </summary>
        /// <param name="blockLoc">Location to set block to</param>
        /// <param name="block">Block to set</param>
        public void SetBlock(BlockLoc blockLoc, Block block)
        {
            var column = this[blockLoc.GetChunkX(), blockLoc.GetChunkZ()];
            if (column is not null)
            {
                var chunk = column.GetChunk(blockLoc);
                if (chunk is null)
                    column[blockLoc.GetChunkY()] = chunk = new Chunk(this);
                chunk[blockLoc.GetChunkBlockX(), blockLoc.GetChunkBlockY(), blockLoc.GetChunkBlockZ()] = block;
            }
            else
                UnityEngine.Debug.LogWarning($"Failed to set {block.State} at {blockLoc}: chunk column not loaded");

        }

        /// <summary>
        /// Clear all terrain data from the world
        /// </summary>
        public void Clear()
        {
            chunks.Clear();
            chunkCnt = chunkLoadNotCompleted = 0;
        }
    }
}
