#nullable enable
using UnityEngine;
using MinecraftClient.Mapping;

namespace MinecraftClient.Control
{
    public abstract class CameraController : MonoBehaviour
    {
        public Camera? ActiveCamera { get; set; }
        protected CameraInfo cameraInfo = new();

        public void SetPerspective(Perspective perspective)
        {
            switch (perspective)
            {
                case Perspective.FirstPerson:
                    EnableFixedMode();
                    // Don't render player on this camera
                    ActiveCamera!.cullingMask = ActiveCamera.cullingMask & ~(1 << LayerMask.NameToLayer("Player"));
                    break;
                case Perspective.ThirdPerson:
                    DisableFixedMode();
                    // Render player on this camera
                    ActiveCamera!.cullingMask = ActiveCamera.cullingMask | (1 << LayerMask.NameToLayer("Player"));
                    break;
            }
            
        }

        public abstract void SetTarget(Transform target);

        public abstract float GetCameraYaw();

        public abstract void EnableFixedMode();

        public abstract void DisableFixedMode();

    }
}