#nullable enable

namespace CraftSharp
{
    public static class BlockLocExtension
    {
        /// <summary>
        /// Get the X index of the corresponding chunk in the world
        /// </summary>
        public static int GetChunkX(this BlockLoc loc)
        {
            return loc.X >> 4;
        }

        /// <summary>
        /// Get the Y index of the corresponding chunk in the world
        /// </summary>
        public static int GetChunkY(this BlockLoc loc)
        {
            return (loc.Y - World.GetDimension().minY) >> 4;
        }

        /// <summary>
        /// Get the Z index of the corresponding chunk in the world
        /// </summary>
        public static int GetChunkZ(this BlockLoc loc)
        {
            return loc.Z >> 4;
        }

        /// <summary>
        /// Get the X index of the corresponding block in the corresponding chunk of the world
        /// </summary>
        public static int GetChunkBlockX(this BlockLoc loc)
        {
            return ((loc.X % Chunk.SIZE) + Chunk.SIZE) % Chunk.SIZE;
        }

        /// <summary>
        /// Get the Y index of the corresponding block in the corresponding chunk of the world
        /// </summary>
        public static int GetChunkBlockY(this BlockLoc loc)
        {
            return ((loc.Y % Chunk.SIZE) + Chunk.SIZE) % Chunk.SIZE;
        }

        /// <summary>
        /// Get the Z index of the corresponding block in the corresponding chunk of the world
        /// </summary>
        public static int GetChunkBlockZ(this BlockLoc loc)
        {
            return ((loc.Z % Chunk.SIZE) + Chunk.SIZE) % Chunk.SIZE;
        }
    }
}