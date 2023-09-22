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
        protected Perspective perspective = Perspective.ThirdPerson;

        // Flag variables
        protected bool initialized = false;
        public bool IsAiming { get; protected set; } = false;

        public abstract void SetPerspective(Perspective newPersp);

        public void SwitchPerspective()
        {
            // Switch to next perspective
            var newPersp = perspective switch
            {
                Perspective.FirstPerson    => Perspective.ThirdPerson,
                Perspective.ThirdPerson    => Perspective.FirstPerson,

                _                          => Perspective.ThirdPerson
            };

            SetPerspective(newPersp);
        }

        public Perspective GetPerspective()
        {
            return perspective;
        }

        public virtual Ray? GetViewportCenterRay()
        {
            if (renderCamera != null)
            {
                return renderCamera.ViewportPointToRay(VIEWPORT_CENTER);
            }
            return null;
        }

        public abstract void EnsureInitialized();

        public abstract void SetTarget(Transform target);

        public abstract Transform? GetTarget();

        public virtual void EnableAimingCamera(bool enable)
        {
            EnsureInitialized();
            IsAiming = enable;
        }

        public virtual Vector3 GetTargetViewportPos()
        {
            var targetPos = GetTarget()?.position;
            if (renderCamera != null && targetPos is not null)
            {
                return renderCamera.WorldToViewportPoint(targetPos.Value);
            }
            return VIEWPORT_CENTER;
        }

        public virtual Transform GetTransform()
        {
            return renderCamera?.transform ?? transform;
        }

        public virtual float GetYaw()
        {
            return GetTransform().eulerAngles.y;
        }

        public abstract void SetYaw(float yaw);

        public virtual Vector3 GetPosition()
        {
            return GetTransform().position;
        }

        public virtual Vector3 GetEularAngles()
        {
            return GetTransform().eulerAngles;
        }

        public virtual string GetDebugInfo()
        {
            return $"Aiming: {IsAiming}\ncamYaw: {GetYaw()}";
        }
    }
}