#nullable enable
using UnityEngine;
using Cinemachine;

using CraftSharp.Event;

namespace CraftSharp.Control
{
    public class SimpleCameraController : CameraController
    {
        [SerializeField] private float nearFov = 40F;
        [SerializeField] private float farFov  = 80F;

        [SerializeField] private float cameraZOffsetNear =    2F;
        [SerializeField] private float cameraZOffsetFar  =   20F;
        [SerializeField] private float cameraYOffsetNear =  0.4F;
        [SerializeField] private float cameraYOffsetFar  = -0.4F;
        [SerializeField] private float cameraYOffsetClip =  0.1F;

        // Virtual camera and camera components
        [SerializeField] private CinemachineVirtualCamera? virtualCameraFollow;
        private CinemachineFramingTransposer? _framingTransposer;
        [SerializeField] private CinemachineVirtualCamera? virtualCameraAim;

        private CinemachinePOV? _followPOV, _aimingPOV;

        private float? _setYawRequest = null;

        public override void EnsureInitialized()
        {
            if (!initialized)
            {
                // Get virtual and render cameras
                _followPOV = virtualCameraFollow!.GetCinemachineComponent<CinemachinePOV>();
                _framingTransposer = virtualCameraFollow.GetCinemachineComponent<CinemachineFramingTransposer>();

                _aimingPOV = virtualCameraAim!.GetCinemachineComponent<CinemachinePOV>();

                initialized = true;
            }
        }

        void Start()
        {
            EnsureInitialized();

            // Initialize camera scale
            zoomInput!.action.Enable();
            cameraInfo.TargetScale = 0.8F;
            cameraInfo.CurrentScale = cameraInfo.TargetScale - 0.2F;

            // Aiming is not enabled by default
            EnableAimingCamera(false);
        }

        void Update()
        {
            var zoom = zoomInput!.action.ReadValue<float>();
            if (zoom != 0F)
            {
                // Update target camera status according to user input
                cameraInfo.TargetScale = Mathf.Clamp01(cameraInfo.TargetScale - zoom * zoomSensitivity);
            }
            
            if (cameraInfo.TargetScale != cameraInfo.CurrentScale)
            {
                cameraInfo.CurrentScale = Mathf.Lerp(cameraInfo.CurrentScale, cameraInfo.TargetScale, Time.deltaTime * zoomSmoothFactor);

                var fov = Mathf.Lerp(nearFov, farFov, cameraInfo.CurrentScale);
                virtualCameraFollow!.m_Lens.FieldOfView = fov;
                spriteRenderCamera!.fieldOfView = fov;

                _framingTransposer!.m_TrackedObjectOffset = new(0F, Mathf.Max(cameraYOffsetClip, Mathf.Lerp(cameraYOffsetNear, cameraYOffsetFar, cameraInfo.CurrentScale)), 0F);
                _framingTransposer!.m_CameraDistance = Mathf.Lerp(cameraZOffsetNear, cameraZOffsetFar, cameraInfo.CurrentScale);
            }

            if (_setYawRequest != null)
            {
                SetYaw(_setYawRequest.Value);
            }
        }

        public override void SetTarget(Transform target)
        {
            EnsureInitialized();

            virtualCameraFollow!.Follow = target;
            virtualCameraFollow!.LookAt = target;

            virtualCameraAim!.Follow = target;
        }

        public override Transform? GetTarget()
        {
            return virtualCameraFollow == null ? null : virtualCameraFollow.Follow;
        }

        public override void TeleportByDelta(Vector3 posDelta)
        {
            if (virtualCameraFollow != null)
            {
                virtualCameraFollow.OnTargetObjectWarped(virtualCameraFollow.Follow, posDelta);
            }

            if (virtualCameraAim != null)
            {
                virtualCameraAim.OnTargetObjectWarped(virtualCameraAim.Follow, posDelta);
            }
        }

        public override Vector3 GetTargetViewportPos(Vector3 offset)
        {
            if (IsFixed()) // Use screen center if camera is fixed
            {
                return VIEWPORT_CENTER;
            }
            
            return base.GetTargetViewportPos(offset);
        }

        public override void EnableAimingCamera(bool enable)
        {
            EnsureInitialized();
            IsAiming = enable;

            if (enable)
            {
                _aimingPOV!.m_HorizontalAxis.Value = _followPOV!.m_HorizontalAxis.Value;
                _aimingPOV!.m_VerticalAxis.Value = _followPOV!.m_VerticalAxis.Value;

                virtualCameraAim!.MoveToTopOfPrioritySubqueue();
                EventManager.Instance.Broadcast(new CrosshairEvent(true));
            }
            else
            {
                _followPOV!.m_HorizontalAxis.Value = _aimingPOV!.m_HorizontalAxis.Value;
                _followPOV!.m_VerticalAxis.Value = _aimingPOV!.m_VerticalAxis.Value;

                virtualCameraFollow!.MoveToTopOfPrioritySubqueue();
                EventManager.Instance.Broadcast(new CrosshairEvent(false));
            }
        }

        public override void SetYaw(float yaw)
        {
            if (_followPOV == null)
            {
                _setYawRequest = yaw;
            }
            else
            {
                _followPOV.m_HorizontalAxis.Value = yaw;
                _setYawRequest = null;
            }
        }

        public override float GetYaw()
        {
            if (IsAiming)
            {
                return _aimingPOV!.m_HorizontalAxis.Value;
            }
            
            return _followPOV!.m_HorizontalAxis.Value;
        }

        private bool IsFixed() => IsAiming;
    }
}