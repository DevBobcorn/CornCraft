#nullable enable
using UnityEngine;
using MinecraftClient.Mapping;

namespace MinecraftClient.Control
{
    public abstract class CameraController : MonoBehaviour
    {
        protected static readonly Vector3 VIEWPORT_CENTER    = new(0.5F,  0.5F, 0F);
        protected CameraInfo cameraInfo = new();
        // Camera used for actual rendering
        protected Camera? renderCamera;
        public Camera? RenderCamera => renderCamera;

        protected CornClient? client;
        public void SetClient(CornClient client) => this.client = client;

        // Flag variables
        protected bool initialized = false, renderCameraPresent = false;

        public abstract void SetPerspective(Perspective perspective);

        public virtual Ray? GetViewportCenterRay()
        {
            if (renderCameraPresent)
                return renderCamera!.ViewportPointToRay(VIEWPORT_CENTER);
            return null;
        }

        public abstract void EnsureInitialized();

        public abstract void SetTarget(Transform target);

        public abstract Transform? GetTarget();

        public abstract bool IsFixed();

        public virtual Vector3 GetTargetScreenPos()
        {
            if (IsFixed())
                return renderCameraPresent ? renderCamera!.ViewportToScreenPoint(VIEWPORT_CENTER) : Vector3.zero;
            
            EnsureInitialized();
            
            var targetPos = GetTarget()?.position;
            if (renderCameraPresent && targetPos is not null)
                return renderCamera!.WorldToScreenPoint(targetPos.Value);
            
            return Vector3.zero;
        }

        public virtual Vector3? GetTransfromScreenPos(Transform? t)
        {
            EnsureInitialized();

            var targetPos = t?.position;
            if (renderCameraPresent && targetPos is not null)
            {
                if (renderCamera!.WorldToViewportPoint(targetPos.Value).z > 0F)
                    return renderCamera!.WorldToScreenPoint(targetPos.Value);
            }
            
            return null;
        }

        public abstract float GetYaw();

        public abstract void SetYaw(float yaw);

        public abstract Vector3? GetPosition();

        public abstract Transform GetTransform();

    }
}