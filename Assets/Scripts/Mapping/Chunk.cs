using System;
using System.Threading;

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
        private readonly Block[,,] blocks  = new Block[SizeX, SizeY, SizeZ];

        /// <summary>
        /// Lock for thread safety
        /// </summary>
        private readonly ReaderWriterLockSlim blockLock = new ReaderWriterLockSlim();

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
                    throw new ArgumentOutOfRangeException("blockX", "Must be between 0 and " + (SizeX - 1) + " (inclusive)");
                if (blockY < 0 || blockY >= SizeY)
                    throw new ArgumentOutOfRangeException("blockY", "Must be between 0 and " + (SizeY - 1) + " (inclusive)");
                if (blockZ < 0 || blockZ >= SizeZ)
                    throw new ArgumentOutOfRangeException("blockZ", "Must be between 0 and " + (SizeZ - 1) + " (inclusive)");

                blockLock.EnterReadLock();
                try
                {
                    return blocks[blockX, blockY, blockZ];
                }
                finally
                {
                    blockLock.ExitReadLock();
                }
            }

            set
            {
                if (blockX < 0 || blockX >= SizeX)
                    throw new ArgumentOutOfRangeException("blockX", "Must be between 0 and " + (SizeX - 1) + " (inclusive)");
                if (blockY < 0 || blockY >= SizeY)
                    throw new ArgumentOutOfRangeException("blockY", "Must be between 0 and " + (SizeY - 1) + " (inclusive)");
                if (blockZ < 0 || blockZ >= SizeZ)
                    throw new ArgumentOutOfRangeException("blockZ", "Must be between 0 and " + (SizeZ - 1) + " (inclusive)");

                blockLock.EnterWriteLock();
                try
                {
                    blocks[blockX, blockY, blockZ]              = value;
                }
                finally
                {
                    blockLock.ExitWriteLock();
                }
            }
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

        public delegate bool BlockCheck(Block bloc);

        public int GetCullFlags(Location location, BlockCheck check)
        {
            int cullFlags = 0;

            if (hasUpFace(location,    check))
                cullFlags |= (1 << 0);

            if (hasDownFace(location,  check))
                cullFlags |= (1 << 1);
            
            if (hasSouthFace(location, check))
                cullFlags |= (1 << 2);

            if (hasNorthFace(location, check))
                cullFlags |= (1 << 3);
            
            if (hasEastFace(location,  check))
                cullFlags |= (1 << 4);

            if (hasWestFace(location,  check))
                cullFlags |= (1 << 5);
            
            return cullFlags;

        }

        private bool hasUpFace(Location location, BlockCheck check) // MC Y Pos
        {
            if (location.ChunkBlockY == Chunk.SizeY - 1)
                return check(world.GetBlock(location.Up()));
            
            return check(GetBlock(location.Up()));
        }

        private bool hasDownFace(Location location, BlockCheck check) // MC Y Neg
        {
            if (location.ChunkBlockY == 0)
                return check(world.GetBlock(location.Down()));
            
            return check(GetBlock(location.Down()));
        }

        private bool hasEastFace(Location location, BlockCheck check) // MC X Pos
        {
            if (location.ChunkBlockX == Chunk.SizeX - 1)
                return check(world.GetBlock(location.East()));
            
            return check(GetBlock(location.East()));
        }

        private bool hasWestFace(Location location, BlockCheck check) // MC X Neg
        {
            if (location.ChunkBlockX == 0)
                return check(world.GetBlock(location.West()));
            
            return check(GetBlock(location.West()));
        }

        private bool hasSouthFace(Location location, BlockCheck check) // MC Z Pos
        {
            if (location.ChunkBlockZ == Chunk.SizeZ - 1)
                return check(world.GetBlock(location.South()));
            
            return check(GetBlock(location.South()));
        }

        private bool hasNorthFace(Location location, BlockCheck check) // MC Z Neg
        {
            if (location.ChunkBlockZ == 0)
                return check(world.GetBlock(location.North()));
            
            return check(GetBlock(location.North()));
        }

    }
}
