#nullable enable
using System;
using UnityEngine;

namespace CraftSharp
{
    /// <summary>
    /// Represent a column of chunks of terrain in a Minecraft world
    /// </summary>
    public class ChunkColumn
    {
        public readonly int ColumnSize;
        public readonly int MinimumY;

        public bool FullyLoaded = false;

        /// <summary>
        /// Blocks contained into the chunk
        /// </summary>
        private readonly Chunk?[] chunks;

        public bool ChunkIsEmpty(int chunkY)
        {
            if (chunkY >= 0 && chunkY < ColumnSize)
                return chunks[chunkY] is null;
            return true;
        }

        private readonly short[] biomes;
        private readonly byte[] skyLight, blockLight;
        private readonly bool[] aoSolid;

        private bool lightingPresent = false;
        public bool LightingPresent => lightingPresent;

        /// <summary>
        /// Create a new ChunkColumn
        /// </summary>
        public ChunkColumn(int size = 16)
        {
            ColumnSize = size;
            MinimumY = World.GetDimension().minY;

            chunks = new Chunk?[size];
            biomes = new short[64 * size];
            skyLight = new byte[4096 * (size + 2)];
            blockLight = new byte[4096 * (size + 2)];

            aoSolid = new bool[4096 * size];
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
        /// <param name="blockLoc">Location, a modulo will be applied</param>
        /// <returns>The chunk, or null if not loaded</returns>
        public Chunk? GetChunk(BlockLoc blockLoc)
        {
            try
            {
                return this[blockLoc.GetChunkY()];
            }
            catch (IndexOutOfRangeException)
            {
                return null;
            }
        }

        public void SetBiomeIds(short[] biomes)
        {
            if (biomes.Length == this.biomes.Length)
                Array.Copy(biomes, this.biomes, biomes.Length);
            else
                Debug.LogWarning($"Biomes data length inconsistent: {biomes.Length} {this.biomes.Length}");
        }

        public short GetBiomeId(BlockLoc blockLoc)
        {
            int index = (((blockLoc.Y - MinimumY) >> 2) << 4) | ((blockLoc.GetChunkBlockZ() >> 2) << 2) | (blockLoc.GetChunkBlockX() >> 2);

            if (index < 0 || index >= biomes.Length)
                return (short) -1;

            return biomes[index];
        }

        public void SetLights(byte[] skyLight, byte[] blockLight)
        {
            if (skyLight.Length == this.skyLight.Length && blockLight.Length == this.blockLight.Length)
            {
                Array.Copy(skyLight,   this.skyLight,   skyLight.Length);
                Array.Copy(blockLight, this.blockLight, blockLight.Length);

                lightingPresent = true;
            }
            else
                Debug.LogWarning($"Lighting data length inconsistent: Sky Light: {skyLight.Length} {this.skyLight.Length} Block Light: {blockLight.Length} {this.blockLight.Length}");
        }

        public byte GetSkyLight(BlockLoc blockLoc)
        {
            // Move up by one chunk
            int index = ((blockLoc.Y - MinimumY + Chunk.SIZE) << 8) | (blockLoc.GetChunkBlockZ() << 4) | blockLoc.GetChunkBlockX();
            
            if (index < 0 || index >= skyLight.Length)
                return (byte) 0;
            
            return lightingPresent ? skyLight[index] : (byte) 0;
        }

        public byte GetBlockLight(BlockLoc blockLoc)
        {
            // Move up by one chunk
            int index = ((blockLoc.Y - MinimumY + Chunk.SIZE) << 8) | (blockLoc.GetChunkBlockZ() << 4) | blockLoc.GetChunkBlockX();
            
            if (index < 0 || index >= blockLight.Length)
                return (byte) 0;

            return lightingPresent ? blockLight[index] : (byte) 0;
        }

        public void RefreshAOSolid()
        {
            for (int ci = 0; ci < ColumnSize; ci++)
            {
                int firstIndex = ci * 4096;
                var chunk = chunks[ci];

                if (chunk is null) // Empty chunk, no opaque blocks
                {
                    Array.Fill(aoSolid, false, firstIndex, 4096);
                }
                else
                {
                    for (int x = 0; x < 16; x++) for (int y = 0; y < 16; y++) for (int z = 0; z < 16; z++)
                    {
                        aoSolid[firstIndex + (y << 8) + (z << 4) + x] = chunk[x, y, z].State.AmbientOcclusionSolid;
                    }
                }
            }
        }

        public bool GetIsOpaque(BlockLoc blockLoc)
        {
            int index = ((blockLoc.Y - MinimumY) << 8) | (blockLoc.GetChunkBlockZ() << 4) | blockLoc.GetChunkBlockX();
            
            if (index < 0 || index >= aoSolid.Length)
                return false;

            return aoSolid[index];
        }
    }
}
