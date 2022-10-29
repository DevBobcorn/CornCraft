#nullable enable
using UnityEngine;

namespace MinecraftClient.Control
{
    public class CameraInfo
    {
        public bool FixedMode = false;

        public Vector3 TargetLocalPosition  = Vector3.zero;
        public Vector3 CurrentVelocity = Vector3.zero;
        public Transform? Target;

        public float CurrentScale = 0.5F;
        public float TargetScale  = 0.5F;

        public float CurrentYaw   = 0F;
        public float CurrentPitch = 0F;
        public float TargetYaw    = 0F;
        public float TargetPitch  = 0F;

    }
}