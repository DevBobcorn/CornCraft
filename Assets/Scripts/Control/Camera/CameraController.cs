#nullable enable
using UnityEngine;
using Cinemachine;
using UnityEngine.InputSystem;

namespace CraftSharp.Control
{
    public abstract class CameraController : MonoBehaviour
    {
        /// <summary>
        /// Camera used for actual rendering
        /// </summary>
        [SerializeField] protected Camera? renderCamera;

        /// <summary>
        /// Camera used for rendering sprites, without post processing.
        /// The clear depth flag of this camera should be set to false
        /// </summary>
        [SerializeField] protected Camera? spriteRenderCamera;

        [SerializeField] protected InputActionReference? zoomInput;
        [SerializeField] [Range(0F, 20F)] protected float zoomSmoothFactor = 4.0F;
        [SerializeField] [Range(0F,  2F)] protected float zoomSensitivity = 0.5F;

        protected static readonly Vector3 VIEWPORT_CENTER    = new(0.5F,  0.5F, 0F);
        protected CameraInfo cameraInfo = new();
        public Camera? RenderCamera => renderCamera;

        public virtual void EnableInput()
        {
            foreach (var input in GetComponentsInChildren<CinemachineInputProvider>())
            {
                input.enabled = true;
            }
        }

        public virtual void DisableInput()
        {
            foreach (var input in GetComponentsInChildren<CinemachineInputProvider>())
            {
                input.enabled = false;
            }
        }

        public virtual void EnableZoom()
        {
            zoomInput!.action.Enable();
        }

        public virtual void DisableZoom()
        {
            zoomInput!.action.Disable();
        }

        // Flag variables
        protected bool initialized = false;
        public bool IsAiming { get; protected set; } = false;

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

        /// <summary>
        /// Used when updating world origin offset to seamlessly teleport the camera
        /// and maintain its relative position to following target
        /// </summary>
        public abstract void TeleportByDelta(Vector3 posDelta);

        public virtual void EnableAimingCamera(bool enable)
        {
            EnsureInitialized();
            IsAiming = enable;
        }

        public virtual Vector3 GetTargetViewportPos(Vector3 offset)
        {
            var target = GetTarget();
            if (target != null)
            {
                var pos = renderCamera!.WorldToViewportPoint(target.TransformPoint(offset));
                pos.z = 0F;
                return pos;
            }
            return VIEWPORT_CENTER;
        }

        public virtual Transform GetTransform()
        {
            return renderCamera!.transform;
        }

        public abstract float GetYaw();

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
            return $"Aiming: {IsAiming}\nCamYaw: {GetYaw()}";
        }
    }
}