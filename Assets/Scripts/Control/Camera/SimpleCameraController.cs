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
        private CinemachineFramingTransposer? framingTransposer;
        [SerializeField] private CinemachineVirtualCamera? virtualCameraAim;

        private CinemachinePOV? followPOV, aimingPOV;

        public override void EnsureInitialized()
        {
            if (!initialized)
            {
                // Get virtual and render cameras
                followPOV = virtualCameraFollow!.GetCinemachineComponent<CinemachinePOV>();
                framingTransposer = virtualCameraFollow.GetCinemachineComponent<CinemachineFramingTransposer>();

                aimingPOV = virtualCameraAim!.GetCinemachineComponent<CinemachinePOV>();

                initialized = true;
            }
        }

        void Start()
        {
            EnsureInitialized();

            // Initialize camera scale
            zoomInput!.action.Enable();
            cameraInfo.CurrentScale = cameraInfo.TargetScale;

            // Apply default Fov
            var fov = Mathf.Lerp(nearFov, farFov, cameraInfo.CurrentScale);
            virtualCameraFollow!.m_Lens.FieldOfView = fov;
            spriteRenderCamera!.fieldOfView = fov;
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

                framingTransposer!.m_TrackedObjectOffset = new(0F, Mathf.Max(cameraYOffsetClip, Mathf.Lerp(cameraYOffsetNear, cameraYOffsetFar, cameraInfo.CurrentScale)), 0F);
                framingTransposer!.m_CameraDistance = Mathf.Lerp(cameraZOffsetNear, cameraZOffsetFar, cameraInfo.CurrentScale);
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
            return virtualCameraFollow?.Follow;
        }

        public override void EnableAimingCamera(bool enable)
        {
            EnsureInitialized();
            IsAiming = enable;

            if (enable)
            {
                aimingPOV!.m_HorizontalAxis.Value = followPOV!.m_HorizontalAxis.Value;
                aimingPOV!.m_VerticalAxis.Value = followPOV!.m_VerticalAxis.Value;

                virtualCameraAim!.MoveToTopOfPrioritySubqueue();
                EventManager.Instance.Broadcast(new CrosshairEvent(true));
            }
            else
            {
                followPOV!.m_HorizontalAxis.Value = aimingPOV!.m_HorizontalAxis.Value;
                followPOV!.m_VerticalAxis.Value = aimingPOV!.m_VerticalAxis.Value;

                virtualCameraFollow!.MoveToTopOfPrioritySubqueue();
                EventManager.Instance.Broadcast(new CrosshairEvent(false));
            }
        }

        public override void SetYaw(float yaw)
        {
            followPOV!.m_HorizontalAxis.Value = yaw;
        }

        public override float GetYaw()
        {
            if (IsAiming)
                return aimingPOV!.m_HorizontalAxis.Value;
            
            return followPOV!.m_HorizontalAxis.Value;
        }

        private bool IsFixed() => IsAiming;

        public override Vector3 GetTargetViewportPos()
        {
            if (IsFixed()) // Use screen center if camera is fixed
            {
                return VIEWPORT_CENTER;
            }
            
            return base.GetTargetViewportPos();
        }
    }
}