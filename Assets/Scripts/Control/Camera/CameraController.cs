#nullable enable
using UnityEngine;

namespace CraftSharp.Control
{
    public abstract class CameraController : MonoBehaviour
    {
        [SerializeField] protected Camera? renderCamera; // Camera used for actual rendering
        protected static readonly Vector3 VIEWPORT_CENTER    = new(0.5F,  0.5F, 0F);
        protected CameraInfo cameraInfo = new();
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

        public virtual Vector3 GetTargetViewportPos()
        {
            var targetPos = GetTarget()?.position;
            if (renderCameraPresent && targetPos is not null)
            {
                return renderCamera!.WorldToViewportPoint(targetPos.Value);
            }
            return VIEWPORT_CENTER;
        }

        public virtual Vector3? GetViewEularAngles() => renderCamera?.transform.eulerAngles;

        public abstract void SetYaw(float yaw);

        public abstract Vector3? GetPosition();

        public abstract Transform GetTransform();

    }
}