#nullable enable
using System;

namespace CraftSharp
{
    public static class LocationExtension
    {
        /// <summary>
        /// Get the X index of the corresponding chunk in the world
        /// </summary>
        public static int GetChunkX(this Location loc)
        {
            return (int)Math.Floor(loc.X / Chunk.SizeX);
        }

        /// <summary>
        /// Get the Y index of the corresponding chunk in the world
        /// </summary>
        public static int GetChunkY(this Location loc)
        {
            return (int)Math.Floor((loc.Y - World.GetDimension().minY) / Chunk.SizeY);
        }

        /// <summary>
        /// Get the Z index of the corresponding chunk in the world
        /// </summary>
        public static int GetChunkZ(this Location loc)
        {
            return (int)Math.Floor(loc.Z / Chunk.SizeZ);
        }

        /// <summary>
        /// Get the X index of the corresponding block in the corresponding chunk of the world
        /// </summary>
        public static int GetChunkBlockX(this Location loc)
        {
            return ((int)Math.Floor(loc.X) % Chunk.SizeX + Chunk.SizeX) % Chunk.SizeX;
        }

        /// <summary>
        /// Get the Y index of the corresponding block in the corresponding chunk of the world
        /// </summary>
        public static int GetChunkBlockY(this Location loc)
        {
            return ((int)Math.Floor(loc.Y) % Chunk.SizeY + Chunk.SizeY) % Chunk.SizeY;
        }

        /// <summary>
        /// Get the Z index of the corresponding block in the corresponding chunk of the world
        /// </summary>
        public static int GetChunkBlockZ(this Location loc)
        {
            return ((int)Math.Floor(loc.Z) % Chunk.SizeZ + Chunk.SizeZ) % Chunk.SizeZ;
        }

    }
}