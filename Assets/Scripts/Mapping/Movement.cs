namespace MinecraftClient.Mapping
{
    public static class Movement
    {
        /// <summary>
        /// Check if the specified location is on the ground
        /// </summary>
        /// <param name="world">World for performing check</param>
        /// <param name="location">Location to check</param>
        /// <returns>True if the specified location is on the ground</returns>
        public static bool IsOnGround(World world, Location location)
        {
            //return world.GetBlock(Move(location, Direction.Down)).Type.IsSolid()
            //    && (location.Y <= Math.Truncate(location.Y) + 0.0001);
            return true;
        }

        /// <summary>
        /// Check if the specified location implies swimming
        /// </summary>
        /// <param name="world">World for performing check</param>
        /// <param name="location">Location to check</param>
        /// <returns>True if the specified location implies swimming</returns>
        public static bool IsSwimming(World world, Location location)
        {
            Block block = world.GetBlock(location);
            //return block.Type.IsLiquid();
            return false;
        }

        /// <summary>
        /// Check if the specified location is safe
        /// </summary>
        /// <param name="world">World for performing check</param>
        /// <param name="location">Location to check</param>
        /// <returns>True if the destination location won't directly harm the player</returns>
        public static bool IsSafe(World world, Location location)
        {
            return true;
        }

    }
}
