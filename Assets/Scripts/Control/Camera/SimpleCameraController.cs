using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

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
        [SerializeField] private CinemachineCamera virtualCameraFollow;
        private CinemachinePositionComposer _positionComposer;
        [SerializeField] private CinemachineCamera virtualCameraAim;

        private CinemachinePanTilt _followPOV, _aimingPOV;

        private float? _setYawRequest;

        public override void EnsureInitialized()
        {
            if (!initialized)
            {
                // Get virtual and render cameras
                _followPOV = (CinemachinePanTilt) virtualCameraFollow!.GetCinemachineComponent(CinemachineCore.Stage.Aim);
                _positionComposer = (CinemachinePositionComposer) virtualCameraFollow.GetCinemachineComponent(CinemachineCore.Stage.Body);

                _aimingPOV = (CinemachinePanTilt) virtualCameraAim!.GetCinemachineComponent(CinemachineCore.Stage.Aim);

                initialized = true;
            }
        }

        private void Start()
        {
            EnsureInitialized();

            // Enable Cinemachine input
            EnableCinemachineInput();

            // Initialize camera scale
            cameraInfo.TargetScale = 0.7F;
            cameraInfo.CurrentScale = cameraInfo.TargetScale - 0.2F;

            // Make sure sprite camera uses same fov as main camera
            var fov = Mathf.Lerp(nearFov, farFov, cameraInfo.CurrentScale);
            virtualCameraFollow!.Lens.FieldOfView = fov;
            spriteRenderCamera!.fieldOfView = fov;

            // Aiming is not enabled by default
            virtualCameraFollow!.Prioritize();
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

                var fov = Mathf.Lerp(nearFov, farFov, cameraInfo.CurrentScale);
                virtualCameraFollow!.Lens.FieldOfView = fov;
                spriteRenderCamera!.fieldOfView = fov;

                _positionComposer!.TargetOffset = new(0F, Mathf.Max(cameraYOffsetClip, Mathf.Lerp(cameraYOffsetNear, cameraYOffsetFar, cameraInfo.CurrentScale)), 0F);
                _positionComposer!.CameraDistance = Mathf.Lerp(cameraZOffsetNear, cameraZOffsetFar, cameraInfo.CurrentScale);
            }
        }

        protected override void EnableAimingCamera()
        {
            EnsureInitialized();

            _aimingPOV!.PanAxis.Value = _followPOV!.PanAxis.Value;
            _aimingPOV!.TiltAxis.Value = _followPOV!.TiltAxis.Value;

            virtualCameraAim!.Prioritize();
            EventManager.Instance.Broadcast(new CameraAimingEvent(true));
        }

        protected override void DisableAimingCamera()
        {
            EnsureInitialized();

            _followPOV!.PanAxis.Value = _aimingPOV!.PanAxis.Value;
            _followPOV!.TiltAxis.Value = _aimingPOV!.TiltAxis.Value;

            virtualCameraFollow!.Prioritize();
            EventManager.Instance.Broadcast(new CameraAimingEvent(false));
        }

        public override void SetTarget(Transform target)
        {
            EnsureInitialized();

            virtualCameraFollow!.Follow = target;
            virtualCameraFollow!.LookAt = target;

            virtualCameraAim!.Follow = target;
        }

        public override Transform GetTarget()
        {
            return virtualCameraFollow == null ? null : virtualCameraFollow.Follow;
        }

        public override void TeleportByDelta(Vector3 posDelta)
        {
            if (virtualCameraFollow)
            {
                virtualCameraFollow.OnTargetObjectWarped(virtualCameraFollow.Follow, posDelta);
            }

            if (virtualCameraAim)
            {
                virtualCameraAim.OnTargetObjectWarped(virtualCameraAim.Follow, posDelta);
            }
        }

        public override Vector3 GetTargetViewportPos(Vector3 offset)
        {
            return IsFixed() ? // Use screen center if camera is fixed
                VIEWPORT_CENTER : base.GetTargetViewportPos(offset);
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
                _aimingPOV!.PanAxis.Value = yaw;
                _setYawRequest = null;
            }
        }

        public override float GetYaw()
        {
            return IsAimingOrLocked ? _aimingPOV!.PanAxis.Value : _followPOV!.PanAxis.Value;
        }

        private bool IsFixed() => IsAimingOrLocked;
    }
}