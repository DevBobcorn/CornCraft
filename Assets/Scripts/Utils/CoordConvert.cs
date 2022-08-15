using UnityEngine;

using MinecraftClient.Mapping;

namespace MinecraftClient
{
    // Unity has a different coodinate system than Minecraft,
    // therefore we need to convert them in some occasions...
    // See https://minecraft.fandom.com/wiki/Coordinates
    public class CoordConvert
    {
        // Swap X and Z axis...
        public static Vector3 MC2Unity(float x, float y, float z)
        {
            
            return new Vector3(z, y, x);
        }

        public static Vector3 MC2Unity(Location loc)
        {
            return new Vector3((float)loc.Z, (float)loc.Y, (float)loc.X);
        }

        public static Vector3 MC2Unity(int x, int y, int z)
        {
            return new Vector3(z, y, x);
        }

        public static Vector3 MC2Unity(Vector3 vec)
        {
            return new Vector3(vec.z, vec.y, vec.x);
        }

        public static Location Unity2MC(Vector3 vec)
        {
            return new Location(vec.z, vec.y, vec.x);
        }
    }
}