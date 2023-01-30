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

        public static byte GetLiquidHeight(BlockState state)
        {
            if (state.InWater || state.InLava)
            {
                if (state.Properties.ContainsKey("level"))
                {
                    return state.Properties["level"] switch {
                        "0"  => 14,
                        "1"  => 12,
                        "2"  => 10,
                        "3"  =>  8,
                        "4"  =>  7,
                        "5"  =>  5,
                        "6"  =>  3,
                        "7"  =>  1,

                        _    => 16
                    };
                }

                return 16;
            }

            return 0;
        }

        private static byte getLiquidHeight(BlockState state)
        {
            if (state.InWater || state.InLava)
            {
                if (state.Properties.ContainsKey("level"))
                {
                    return state.Properties["level"] switch {
                        "0"  => 14,
                        "1"  => 12,
                        "2"  => 10,
                        "3"  =>  8,
                        "4"  =>  7,
                        "5"  =>  5,
                        "6"  =>  3,
                        "7"  =>  1,

                        _    => 16
                    };
                }

                return 16;
            }

            return 0;
        }

        public byte[] GetLiquidHeights(Location location)
        {
            // Height References
            //  NE---E---SE
            //  |         |
            //  N    @    S
            //  |         |
            //  NW---W---SW

            return new byte[] {
                getLiquidHeight(getNEBlock(location).State),    getLiquidHeight(getEastBlock(location).State), getLiquidHeight(getSEBlock(location).State),
                getLiquidHeight(getNorthBlock(location).State), getLiquidHeight(GetBlock(location).State),     getLiquidHeight(getSouthBlock(location).State),
                getLiquidHeight(getNWBlock(location).State),    getLiquidHeight(getWestBlock(location).State), getLiquidHeight(getSWBlock(location).State)
            };
        }

        public int GetCullFlags(Location location, Block self, BlockCheck check)
        {
            int cullFlags = 0;

            if (check(self, getUpBlock(location)))
                cullFlags |= (1 << 0);

            if (check(self, getDownBlock(location)))
                cullFlags |= (1 << 1);
            
            if (check(self, getSouthBlock(location)))
                cullFlags |= (1 << 2);

            if (check(self, getNorthBlock(location)))
                cullFlags |= (1 << 3);
            
            if (check(self, getEastBlock(location)))
                cullFlags |= (1 << 4);

            if (check(self, getWestBlock(location)))
                cullFlags |= (1 << 5);
            
            return cullFlags;
        }

        private Block getUpBlock(Location location) // MC Y Pos
        {
            if (location.ChunkBlockY == Chunk.SizeY - 1)
                return world.GetBlock(location.Up());
            
            return GetBlock(location.Up());
        }

        private Block getDownBlock(Location location) // MC Y Neg
        {
            if (location.ChunkBlockY == 0)
                return world.GetBlock(location.Down());
            
            return GetBlock(location.Down());
        }

        private Block getEastBlock(Location location) // MC X Pos
        {
            if (location.ChunkBlockX == Chunk.SizeX - 1)
                return world.GetBlock(location.East());
            
            return GetBlock(location.East());
        }

        private Block getWestBlock(Location location) // MC X Neg
        {
            if (location.ChunkBlockX == 0)
                return world.GetBlock(location.West());
            
            return GetBlock(location.West());
        }

        private Block getSouthBlock(Location location) // MC Z Pos
        {
            if (location.ChunkBlockZ == Chunk.SizeZ - 1)
                return world.GetBlock(location.South());
            
            return GetBlock(location.South());
        }

        private Block getNorthBlock(Location location) // MC Z Neg
        {
            if (location.ChunkBlockZ == 0)
                return world.GetBlock(location.North());
            
            return GetBlock(location.North());
        }

        private Block getNEBlock(Location location)
        {
            if (location.ChunkBlockZ == 0 || location.ChunkBlockX == Chunk.SizeX - 1)
                return world.GetBlock(location.North().East());
            
            return GetBlock(location.North().East());
        }

        private Block getNWBlock(Location location)
        {
            if (location.ChunkBlockZ == 0 || location.ChunkBlockX == 0)
                return world.GetBlock(location.North().West());
            
            return GetBlock(location.North().West());
        }

        private Block getSEBlock(Location location)
        {
            if (location.ChunkBlockZ == Chunk.SizeZ - 1 || location.ChunkBlockX == Chunk.SizeX - 1)
                return world.GetBlock(location.South().East());
            
            return GetBlock(location.South().East());
        }

        private Block getSWBlock(Location location)
        {
            if (location.ChunkBlockZ == Chunk.SizeZ - 1 || location.ChunkBlockX == 0)
                return world.GetBlock(location.South().West());
            
            return GetBlock(location.South().West());
        }

    }
}
