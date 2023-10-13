using System;

namespace CraftSharp
{
    /// <summary>
    /// Represent a chunk of terrain in a Minecraft world
    /// </summary>
    public class Chunk
    {
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
    }
}
