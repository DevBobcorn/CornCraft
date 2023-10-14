using System;

namespace CraftSharp
{
    /// <summary>
    /// Represents a block location into a Minecraft world
    /// </summary>
    public struct BlockLoc
    {
        /// <summary>
        /// The X Coordinate
        /// </summary>
        public int X;
        
        /// <summary>
        /// The Y Coordinate (vertical)
        /// </summary>
        public int Y;

        /// <summary>
        /// The Z coordinate
        /// </summary>
        public int Z;

        /// <summary>
        /// Get location with zeroed coordinates
        /// </summary>
        public static BlockLoc Zero
        {
            get
            {
                return new BlockLoc(0, 0, 0);
            }
        }

        /// <summary>
        /// Create a new location
        /// </summary>
        public BlockLoc(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        /// <summary>
        /// Get a squared distance to the specified location
        /// </summary>
        /// <param name="blockLoc">Other location for computing distance</param>
        /// <returns>Distance to the specified location, without using a square root</returns>
        public double DistanceSquared(BlockLoc blockLoc)
        {
            return ((X - blockLoc.X) * (X - blockLoc.X))
                 + ((Y - blockLoc.Y) * (Y - blockLoc.Y))
                 + ((Z - blockLoc.Z) * (Z - blockLoc.Z));
        }

        /// <summary>
        /// Get exact distance to the specified location
        /// </summary>
        /// <param name="blockLoc">Other location for computing distance</param>
        /// <returns>Distance to the specified location, with square root so lower performances</returns>
        public double Distance(BlockLoc blockLoc)
        {
            return Math.Sqrt(DistanceSquared(blockLoc));
        }

        /// <summary>
        /// Compare two locations. Locations are equals if the integer part of their coordinates are equals.
        /// </summary>
        /// <param name="obj">Object to compare to</param>
        /// <returns>TRUE if the locations are equals</returns>
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (obj is BlockLoc)
            {
                return ((int)this.X) == ((int)((BlockLoc)obj).X)
                    && ((int)this.Y) == ((int)((BlockLoc)obj).Y)
                    && ((int)this.Z) == ((int)((BlockLoc)obj).Z);
            }
            return false;
        }

        /// <summary>
        /// Compare two locations. Locations are equals if the integer part of their coordinates are equals.
        /// </summary>
        /// <param name="loc1">First location to compare</param>
        /// <param name="loc2">Second location to compare</param>
        /// <returns>TRUE if the locations are equals</returns>
        public static bool operator ==(BlockLoc loc1, BlockLoc loc2)
        {
            if (loc1 == null && loc2 == null)
                return true;
            if (loc1 == null || loc2 == null)
                return false;
            return loc1.Equals(loc2);
        }

        /// <summary>
        /// Compare two locations. Locations are not equals if the integer part of their coordinates are not equals.
        /// </summary>
        /// <param name="loc1">First location to compare</param>
        /// <param name="loc2">Second location to compare</param>
        /// <returns>TRUE if the locations are equals</returns>
        public static bool operator !=(BlockLoc loc1, BlockLoc loc2)
        {
            if (loc1 == null && loc2 == null)
                return false;
            if (loc1 == null || loc2 == null)
                return true;
            return !loc1.Equals(loc2);
        }

        /// <summary>
        /// Sums two locations and returns the result.
        /// </summary>
        /// <exception cref="NullReferenceException">
        /// Thrown if one of the provided location is null
        /// </exception>
        /// <param name="loc1">First location to sum</param>
        /// <param name="loc2">Second location to sum</param>
        /// <returns>Sum of the two locations</returns>
        public static BlockLoc operator +(BlockLoc loc1, BlockLoc loc2)
        {
            return new BlockLoc
            (
                loc1.X + loc2.X,
                loc1.Y + loc2.Y,
                loc1.Z + loc2.Z
            );
        }

        /// <summary>
        /// Substract a location to another
        /// </summary>
        /// <exception cref="NullReferenceException">
        /// Thrown if one of the provided location is null
        /// </exception>
        /// <param name="loc1">First location</param>
        /// <param name="loc2">Location to substract to the first one</param>
        /// <returns>Sum of the two locations</returns>
        public static BlockLoc operator -(BlockLoc loc1, BlockLoc loc2)
        {
            return new BlockLoc
            (
                loc1.X - loc2.X,
                loc1.Y - loc2.Y,
                loc1.Z - loc2.Z
            );
        }

        /// <summary>
        /// DO NOT USE. Defined to comply with C# requirements requiring a GetHashCode() when overriding Equals() or ==
        /// </summary>
        /// <remarks>
        /// A modulo will be applied if the location is outside the following ranges:
        /// X: -4096 to +4095
        /// Y: -32 to +31
        /// Z: -4096 to +4095
        /// </remarks>
        /// <returns>A simplified version of the location</returns>
        public override int GetHashCode()
        {
            return (X & ~((~0) << 13)) << 19
                 | (Y & ~((~0) << 13)) << 13
                 | (Z & ~((~0) << 06)) << 00;
        }

        /// <summary>
        /// Convert the location into a string representation
        /// </summary>
        /// <returns>String representation of the location</returns>
        public override string ToString()
        {
            return string.Format("X: {0} Y: {1} Z: {2}", X, Y, Z);
        }

        public Location ToLocation()
        {
            return new Location(this.X, this.Y, this.Z);
        }

        public Location ToCenterLocation()
        {
            return new Location(this.X + 0.5F, this.Y + 0.5F, this.Z + 0.5F);
        }

        public double DistanceTo(BlockLoc loc)
        {
            return Math.Sqrt(Math.Pow(this.X - loc.X, 2) + Math.Pow(this.Y - loc.Y, 2) + Math.Pow(this.Z - loc.Z, 2));
        }

        public double SqrDistanceTo(BlockLoc loc)
        {
            return Math.Pow(this.X - loc.X, 2) + Math.Pow(this.Y - loc.Y, 2) + Math.Pow(this.Z - loc.Z, 2);
        }

        public BlockLoc Up()
        {
            return this + new BlockLoc( 0, 1, 0);
        }

        public BlockLoc Down()
        {
            return this + new BlockLoc( 0,-1, 0);
        }

        // MC Z Neg
        public BlockLoc North()
        {
            return this + new BlockLoc( 0, 0,-1);
        }

        // MC Z Pos
        public BlockLoc South()
        {
            return this + new BlockLoc( 0, 0, 1);
        }

        // MC X Pos
        public BlockLoc East()
        {
            return this + new BlockLoc( 1, 0, 0);
        }

        // MC X Neg
        public BlockLoc West()
        {
            return this + new BlockLoc(-1, 0, 0);
        }
    }
}
