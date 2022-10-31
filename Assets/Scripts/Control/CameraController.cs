#nullable enable
using UnityEngine;
using MinecraftClient.Mapping;

namespace MinecraftClient.Control
{
    public abstract class CameraController : MonoBehaviour
    {
        protected static readonly Vector3 VIEWPORT_CENTER = new(0.5F, 0.5F, 0F);
        protected CameraInfo cameraInfo = new();
        // Camera used for actual rendering
        protected Camera? renderCamera;
        // Flag variables
        
        protected bool initialized = false, renderCameraPresent = false;

        public virtual void SetPerspective(Perspective perspective)
        {
            switch (perspective)
            {
                case Perspective.FirstPerson:
                    EnableFixedMode();
                    break;
                case Perspective.ThirdPerson:
                    DisableFixedMode();
                    break;
            }
        }

        public virtual Ray? GetViewportCenterRay()
        {
            if (renderCameraPresent)
                return renderCamera!.ViewportPointToRay(VIEWPORT_CENTER);
            return null;
        }

        public abstract void EnsureInitialized();

        public abstract void SetTarget(Transform target);

        public abstract float GetYaw();

        public abstract Vector3? GetPosition();

        public abstract Transform GetTransform();

        public abstract void EnableFixedMode();

        public abstract void DisableFixedMode();

    }
}