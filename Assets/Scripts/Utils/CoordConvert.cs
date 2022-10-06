using UnityEngine;

using MinecraftClient.Mapping;

namespace MinecraftClient
{
    // Unity has a different coodinate system than Minecraft,
    // therefore we need to convert them in some occasions...
    // See https://minecraft.fandom.com/wiki/Coordinates
    public static class CoordConvert
    {
        // Swap X and Z axis...
        public static Vector3 MC2Unity(float x, float y, float z) => new(z, y, x);

        public static Vector3 MC2Unity(Location loc) => new((float)loc.Z, (float)loc.Y, (float)loc.X);

        public static Vector3 MC2Unity(int x, int y, int z) => new(z, y, x);

        public static Vector3 MC2Unity(Vector3 vec) => new(vec.z, vec.y, vec.x);

        public static Location Unity2MC(Vector3 vec) => new(vec.z, vec.y, vec.x);

    }
}