namespace MinecraftClient
{
    // Angle in Minecraft data types has a value ranging in [0, 256)
    // See the angle entry at https://wiki.vg/Protocol#Data_types
    public static class AngleConvert
    {
        public static float MC2Unity(float angle) => angle / 256F * 360F;

        public static float Unity2MC(float angle) => angle / 360F * 256F;

        public static float MCYaw2Unity(float angle) => (angle / 256F * 360F) + 90F;

        public static float UnityYaw2MC(float angle) => (angle / 360F - 90F) * 256F;

    }
}