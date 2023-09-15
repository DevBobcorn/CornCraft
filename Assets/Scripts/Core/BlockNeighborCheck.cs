namespace CraftSharp
{
    public delegate bool BlockNeighborCheck(Block self, Block neighbor);

    public class BlockNeighborChecks
    {
        public static readonly BlockNeighborCheck WATER_SURFACE = new((self, neighbor)
                => { return !(neighbor.State.InWater || neighbor.State.FaceOcclusionSolid); });
        public static readonly BlockNeighborCheck LAVA_SURFACE  = new((self, neighbor)
                => { return !(neighbor.State.InLava  || neighbor.State.FaceOcclusionSolid); });

        public static readonly BlockNeighborCheck NON_FULL_SOLID = new((self, neighbor)
                => { return !neighbor.State.FaceOcclusionSolid; });
        
    }
}