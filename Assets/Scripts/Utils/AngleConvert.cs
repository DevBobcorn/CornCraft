using UnityEngine;

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

        public static Vector3 GetAxisAlignedDirection(Vector3 original)
        {
            if (Mathf.Abs(original.x) > Mathf.Abs(original.z))
                return original.x > 0F ? Vector3.right   : Vector3.left;
            else
                return original.z > 0F ? Vector3.forward : Vector3.back;
        }

        public static float GetYawFromVector2(Vector2 direction)
        {
            if (direction.y > 0F)
                return Mathf.Atan(direction.x / direction.y) * Mathf.Rad2Deg;
            else if (direction.y < 0F)
                return Mathf.Atan(direction.x / direction.y) * Mathf.Rad2Deg + 180F;
            else
                return direction.x > 0 ? 90F : 270F;
        }

    }
}