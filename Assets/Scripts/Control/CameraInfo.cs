#nullable enable
using UnityEngine;

namespace MinecraftClient.Control
{
    public class CameraInfo
    {
        public Vector3 FixedOffset = Vector3.zero;
        public bool FixedMode = false;

        public Vector3 TargetPosition  = Vector3.zero;
        public Vector3 CurrentVelocity = Vector3.zero;
        public Transform? Target;

        public float Scale = 0.5F;

    }
}