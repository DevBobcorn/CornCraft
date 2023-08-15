using System;

namespace CraftSharp
{
    public static class Movement
    {
        private static Location DOWN = new Location(0, -1, 0);

        /// <summary>
        /// Check if the specified location is on the ground
        /// </summary>
        /// <param name="world">World for performing check</param>
        /// <param name="location">Location to check</param>
        /// <returns>True if the specified location is on the ground</returns>
        public static bool IsOnGround(World world, Location location)
        {
            return (!world.GetBlock(location + DOWN).State.LikeAir)
                && (location.Y <= Math.Truncate(location.Y) + 0.0001);
        }

        /// <summary>
        /// Check if the specified location implies swimming
        /// </summary>
        /// <param name="world">World for performing check</param>
        /// <param name="location">Location to check</param>
        /// <returns>True if the specified location implies swimming</returns>
        public static bool IsSwimming(World world, Location location)
        {
            var state = world.GetBlock(location).State;
            return state.InWater || state.InLava;
        }

    }
}
