using System;

namespace MinecraftClient.Mapping
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

        public const int SizeX = 16;
        public const int SizeY = 16;
        public const int SizeZ = 16;

        /// <summary>
        /// Blocks contained into the chunk
        /// </summary>
        private readonly Block[] blocks = new Block[SizeY * SizeZ * SizeX];

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
                if (blockX < 0 || blockX >= SizeX)
                    throw new ArgumentOutOfRangeException(nameof(blockX), "Must be between 0 and " + (SizeX - 1) + " (inclusive)");
                if (blockY < 0 || blockY >= SizeY)
                    throw new ArgumentOutOfRangeException(nameof(blockY), "Must be between 0 and " + (SizeY - 1) + " (inclusive)");
                if (blockZ < 0 || blockZ >= SizeZ)
                    throw new ArgumentOutOfRangeException(nameof(blockZ), "Must be between 0 and " + (SizeZ - 1) + " (inclusive)");

                return blocks[(blockY << 8) | (blockZ << 4) | blockX];
            }

            set
            {
                if (blockX < 0 || blockX >= SizeX)
                    throw new ArgumentOutOfRangeException(nameof(blockX), "Must be between 0 and " + (SizeX - 1) + " (inclusive)");
                if (blockY < 0 || blockY >= SizeY)
                    throw new ArgumentOutOfRangeException(nameof(blockY), "Must be between 0 and " + (SizeY - 1) + " (inclusive)");
                if (blockZ < 0 || blockZ >= SizeZ)
                    throw new ArgumentOutOfRangeException(nameof(blockZ), "Must be between 0 and " + (SizeZ - 1) + " (inclusive)");

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
        /// <param name="location">Location, a modulo will be applied</param>
        /// <returns>The block</returns>
        public Block GetBlock(Location location)
        {
            return this[location.ChunkBlockX, location.ChunkBlockY, location.ChunkBlockZ];
        }

        public Block GetBlockFromWorld(Location location)
        {
            return world.GetBlock(location);
        }

        public delegate bool BlockCheck(Block self, Block neighbor);

        public int GetCullFlags(Location location, Block self, BlockCheck check)
        {
            int cullFlags = 0;

            if (hasUpFace(location,    self, check))
                cullFlags |= (1 << 0);

            if (hasDownFace(location,  self, check))
                cullFlags |= (1 << 1);
            
            if (hasSouthFace(location, self, check))
                cullFlags |= (1 << 2);

            if (hasNorthFace(location, self, check))
                cullFlags |= (1 << 3);
            
            if (hasEastFace(location,  self, check))
                cullFlags |= (1 << 4);

            if (hasWestFace(location,  self, check))
                cullFlags |= (1 << 5);
            
            return cullFlags;

        }

        private bool hasUpFace(Location location, Block self, BlockCheck check) // MC Y Pos
        {
            if (location.ChunkBlockY == Chunk.SizeY - 1)
                return check(self, world.GetBlock(location.Up()));
            
            return check(self, GetBlock(location.Up()));
        }

        private bool hasDownFace(Location location, Block self, BlockCheck check) // MC Y Neg
        {
            if (location.ChunkBlockY == 0)
                return check(self, world.GetBlock(location.Down()));
            
            return check(self, GetBlock(location.Down()));
        }

        private bool hasEastFace(Location location, Block self, BlockCheck check) // MC X Pos
        {
            if (location.ChunkBlockX == Chunk.SizeX - 1)
                return check(self, world.GetBlock(location.East()));
            
            return check(self, GetBlock(location.East()));
        }

        private bool hasWestFace(Location location, Block self, BlockCheck check) // MC X Neg
        {
            if (location.ChunkBlockX == 0)
                return check(self, world.GetBlock(location.West()));
            
            return check(self, GetBlock(location.West()));
        }

        private bool hasSouthFace(Location location, Block self, BlockCheck check) // MC Z Pos
        {
            if (location.ChunkBlockZ == Chunk.SizeZ - 1)
                return check(self, world.GetBlock(location.South()));
            
            return check(self, GetBlock(location.South()));
        }

        private bool hasNorthFace(Location location, Block self, BlockCheck check) // MC Z Neg
        {
            if (location.ChunkBlockZ == 0)
                return check(self, world.GetBlock(location.North()));
            
            return check(self, GetBlock(location.North()));
        }

    }
}
