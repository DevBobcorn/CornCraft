#nullable enable
using System;
using UnityEngine;

namespace MinecraftClient.Mapping
{
    /// <summary>
    /// Represent a column of chunks of terrain in a Minecraft world
    /// </summary>
    public class ChunkColumn
    {
        public int ColumnSize;

        public bool FullyLoaded = false;

        private World world;
        public int ChunkMask;

        /// <summary>
        /// Blocks contained into the chunk
        /// </summary>
        private readonly Chunk?[] chunks;

        private readonly short[] biomes;
        private readonly byte[] lighting;

        private bool lightingPresent = false;
        public bool LightingPresent => lightingPresent;

        /// <summary>
        /// Create a new ChunkColumn
        /// </summary>
        public ChunkColumn(World parent, int size = 16)
        {
            world = parent;
            ColumnSize = size;
            chunks = new Chunk?[size];
            biomes = new short[64 * size];
            lighting = new byte[4096 * (size + 2)];
            Array.Fill<byte>(lighting, 0xFF);
        }

        /// <summary>
        /// Get or set the specified chunk
        /// </summary>
        /// <param name="chunkY">Chunk Y</param>
        /// <returns>chunk at the given location</returns>
        public Chunk? this[int chunkY]
        {
            get
            {
                return chunks[chunkY];
            }
            set
            {
                chunks[chunkY] = value;
            }
        }

        /// <summary>
        /// Get chunk at the specified location
        /// </summary>
        /// <param name="location">Location, a modulo will be applied</param>
        /// <returns>The chunk, or null if not loaded</returns>
        public Chunk? GetChunk(Location location)
        {
            try
            {
                return this[location.ChunkY];
            }
            catch (IndexOutOfRangeException)
            {
                return null;
            }
        }

        public void SetBiomes(short[] biomes)
        {
            if (biomes.Length == this.biomes.Length)
                Array.Copy(biomes, this.biomes, biomes.Length);
            else
                Debug.LogWarning($"Biomes data length inconsistent: {biomes.Length} {this.biomes.Length}");
        }

        public Biome GetBiome(Location location)
        {
            int index = (location.ChunkBlockY << 4) | (location.ChunkBlockZ << 2) | location.ChunkBlockX;
            return index < biomes.Length ? BiomePalette.INSTANCE.FromId(biomes[index]) : BiomePalette.EMPTY;
        }

        public short GetBiomeId(Location location)
        {
            int index = (location.ChunkBlockY << 4) | (location.ChunkBlockZ << 2) | location.ChunkBlockX;
            return index < biomes.Length ? biomes[index] : (short) -1;
        }

        public void SetLighting(byte[] lighting)
        {
            if (lighting.Length == this.lighting.Length)
                Array.Copy(lighting, this.lighting, lighting.Length);
            else
                Debug.LogWarning($"Lighting data length inconsistent: {lighting.Length} {this.lighting.Length}");
        }
    }
}
