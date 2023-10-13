using System;
using UnityEngine;

namespace CraftSharp
{
    /// <summary>
    /// Represent a chunk of terrain in a Minecraft world
    /// </summary>
    public class Chunk
    {
        private readonly World world;

        public Chunk(World parent)
        {
            world = parent;
        }

        public const int SIZE = 16;

        /// <summary>
        /// Blocks contained into the chunk
        /// </summary>
        private readonly Block[] blocks = new Block[SIZE * SIZE * SIZE];

        /// <summary>
        /// Read, or set the specified block
        /// </summary>
        /// <param name="blockX">Block X</param>
        /// <param name="blockY">Block Y</param>
        /// <param name="blockZ">Block Z</param>
        /// <returns>chunk at the given location</returns>
        public Block this[int blockX, int blockY, int blockZ]
        {
            get
            {
                if (blockX < 0 || blockX >= SIZE)
                    throw new ArgumentOutOfRangeException(nameof(blockX), $"Must be between 0 and {SIZE - 1} (inclusive), got {blockX}");
                if (blockY < 0 || blockY >= SIZE)
                    throw new ArgumentOutOfRangeException(nameof(blockY), $"Must be between 0 and {SIZE - 1} (inclusive), got {blockY}");
                if (blockZ < 0 || blockZ >= SIZE)
                    throw new ArgumentOutOfRangeException(nameof(blockZ), $"Must be between 0 and {SIZE - 1} (inclusive), got {blockZ}");

                return blocks[(blockY << 8) | (blockZ << 4) | blockX];
            }

            set
            {
                if (blockX < 0 || blockX >= SIZE)
                    throw new ArgumentOutOfRangeException(nameof(blockX), $"Must be between 0 and {SIZE - 1} (inclusive), got {blockX}");
                if (blockY < 0 || blockY >= SIZE)
                    throw new ArgumentOutOfRangeException(nameof(blockY), $"Must be between 0 and {SIZE - 1} (inclusive), got {blockY}");
                if (blockZ < 0 || blockZ >= SIZE)
                    throw new ArgumentOutOfRangeException(nameof(blockZ), $"Must be between 0 and {SIZE - 1} (inclusive), got {blockZ}");

                blocks[(blockY << 8) | (blockZ << 4) | blockX] = value;
            }
        }

        /// <summary>
        /// Used when parsing chunks
        /// </summary>
        /// <param name="blockX">Block X</param>
        /// <param name="blockY">Block Y</param>
        /// <param name="blockZ">Block Z</param>
        /// <param name="block">Block</param>
        public void SetWithoutCheck(int blockX, int blockY, int blockZ, Block block)
        {
            blocks[(blockY << 8) | (blockZ << 4) | blockX] = block;
        }

        /// <summary>
        /// Get block at the specified location
        /// </summary>
        /// <param name="blockLoc">Location, a modulo will be applied</param>
        /// <returns>The block</returns>
        public Block GetBlock(BlockLoc blockLoc)
        {
            return this[blockLoc.GetChunkBlockX(), blockLoc.GetChunkBlockY(), blockLoc.GetChunkBlockZ()];
        }

        public byte[] GetLiquidHeights(BlockLoc blockLoc)
        {
            // Height References
            //  NE---E---SE
            //  |         |
            //  N    @    S
            //  |         |
            //  NW---W---SW

            return new byte[] {
                16, 16, 16,
                16, 16, 16,
                16, 16, 16
            };
        }

        public int GetCullFlags(BlockLoc blockLoc, Block self, BlockNeighborCheck check)
        {
            int cullFlags = 0;

            if (check(self, getUpBlock(blockLoc)))
                cullFlags |= (1 << 0);

            if (check(self, getDownBlock(blockLoc)))
                cullFlags |= (1 << 1);
            
            if (check(self, getSouthBlock(blockLoc)))
                cullFlags |= (1 << 2);

            if (check(self, getNorthBlock(blockLoc)))
                cullFlags |= (1 << 3);
            
            if (check(self, getEastBlock(blockLoc)))
                cullFlags |= (1 << 4);

            if (check(self, getWestBlock(blockLoc)))
                cullFlags |= (1 << 5);
            
            return cullFlags;
        }

        private Block getUpBlock(BlockLoc blockLoc) // MC Y Pos
        {
            if (blockLoc.GetChunkBlockY() == Chunk.SIZE - 1)
                return world.GetBlock(blockLoc.Up());
            
            return GetBlock(blockLoc.Up());
        }

        private Block getDownBlock(BlockLoc blockLoc) // MC Y Neg
        {
            if (blockLoc.GetChunkBlockY() == 0)
                return world.GetBlock(blockLoc.Down());
            
            return GetBlock(blockLoc.Down());
        }

        private Block getEastBlock(BlockLoc blockLoc) // MC X Pos
        {
            if (blockLoc.GetChunkBlockX() == Chunk.SIZE - 1)
                return world.GetBlock(blockLoc.East());
            
            return GetBlock(blockLoc.East());
        }

        private Block getWestBlock(BlockLoc blockLoc) // MC X Neg
        {
            if (blockLoc.GetChunkBlockX() == 0)
                return world.GetBlock(blockLoc.West());
            
            return GetBlock(blockLoc.West());
        }

        private Block getSouthBlock(BlockLoc blockLoc) // MC Z Pos
        {
            if (blockLoc.GetChunkBlockZ() == Chunk.SIZE - 1)
                return world.GetBlock(blockLoc.South());
            
            return GetBlock(blockLoc.South());
        }

        private Block getNorthBlock(BlockLoc blockLoc) // MC Z Neg
        {
            if (blockLoc.GetChunkBlockZ() == 0)
                return world.GetBlock(blockLoc.North());
            
            return GetBlock(blockLoc.North());
        }
    }
}
