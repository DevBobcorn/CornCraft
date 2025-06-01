using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

using CraftSharp.Event;

namespace CraftSharp.Control
{
    public class TopdownCameraController : CameraController
    {
        [SerializeField] private float cameraZOffsetNear =   15F;
        [SerializeField] private float cameraZOffsetFar  =   30F;
        
        // Virtual camera and camera components
        [SerializeField] private CinemachineCamera virtualCameraFollow;
        private CinemachinePositionComposer _positionComposer;
        private CinemachinePanTilt _followPOV;

        private float? _setYawRequest;

        public override void EnsureInitialized()
        {
            if (!initialized)
            {
                // Get virtual and render cameras
                _followPOV = (CinemachinePanTilt) virtualCameraFollow!.GetCinemachineComponent(CinemachineCore.Stage.Aim);
                _positionComposer = (CinemachinePositionComposer) virtualCameraFollow.GetCinemachineComponent(CinemachineCore.Stage.Body);

                initialized = true;
            }
        }

        private void Start()
        {
            EnsureInitialized();

            // Activate virtual camera
            virtualCameraFollow!.Prioritize();

            // Enable Cinemachine input
            EnableCinemachineInput();

            // Initialize camera scale
            cameraInfo.TargetScale = 0.4F;
            cameraInfo.CurrentScale = cameraInfo.TargetScale - 0.2F;

            // Make sure sprite camera uses same fov as main camera
            spriteRenderCamera!.fieldOfView = virtualCameraFollow.Lens.FieldOfView;

            // Aiming is not enabled by default
            UseAimingCamera(false);
            EventManager.Instance.Broadcast(new CameraAimingEvent(false));
        }

        private void Update()
        {
            if (_setYawRequest != null)
            {
                SetYaw(_setYawRequest.Value);
            }

            var mouseScroll = Mouse.current.scroll.value.y;
            if (mouseScroll != 0F && Keyboard.current.shiftKey.IsPressed())
            {
                // Update target camera status according to user input
                cameraInfo.TargetScale = Mathf.Clamp01(cameraInfo.TargetScale - mouseScroll * zoomSensitivity);
            }
            
            if (!Mathf.Approximately(cameraInfo.TargetScale, cameraInfo.CurrentScale))
            {
                cameraInfo.CurrentScale = Mathf.Lerp(cameraInfo.CurrentScale, cameraInfo.TargetScale, Time.deltaTime * zoomSmoothFactor);
                _positionComposer!.CameraDistance = Mathf.Lerp(cameraZOffsetNear, cameraZOffsetFar, cameraInfo.CurrentScale);
            }
        }

        protected override void EnableAimingCamera()
        {
            EnsureInitialized();

            EventManager.Instance.Broadcast(new CameraAimingEvent(true));
        }

        protected override void DisableAimingCamera()
        {
            EnsureInitialized();

            EventManager.Instance.Broadcast(new CameraAimingEvent(false));
        }

        public override void SetTargets(Transform followTarget, Transform aimingTarget)
        {
            EnsureInitialized();
            virtualCameraFollow!.Follow = followTarget;
        }

        protected override Transform GetFollowTarget()
        {
            return !virtualCameraFollow ? null : virtualCameraFollow.Follow;
        }

        public override void TeleportByDelta(Vector3 posDelta)
        {
            if (virtualCameraFollow)
            {
                virtualCameraFollow.OnTargetObjectWarped(virtualCameraFollow.Follow, posDelta);
            }
        }

        public override float GetPitch()
        {
            return _followPOV!.TiltAxis.Value;
        }

        public override void SetYaw(float yaw)
        {
            if (!_followPOV)
            {
                _setYawRequest = yaw;
            }
            else
            {
                _followPOV.PanAxis.Value = yaw;
                _setYawRequest = null;
            }
        }

        public override float GetYaw()
        {
            return _followPOV!.PanAxis.Value;
        }
    }
}