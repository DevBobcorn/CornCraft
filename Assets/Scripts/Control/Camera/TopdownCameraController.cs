using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;

using CraftSharp.Event;

namespace CraftSharp.Control
{
    public class TopdownCameraController : CameraController
    {
        [SerializeField] private float cameraZOffsetNear =   15F;
        [SerializeField] private float cameraZOffsetFar  =   30F;
        
        // Virtual camera and camera components
        private CinemachineVirtualCamera _virtualCameraFollow;
        private CinemachineFramingTransposer _framingTransposer;
        private CinemachinePOV _followPOV;

        private float? _setYawRequest = null;

        public override void EnsureInitialized()
        {
            if (!initialized)
            {
                // Get virtual and render cameras
                var followObj = transform.Find("Follow Virtual");
                _virtualCameraFollow = followObj.GetComponent<CinemachineVirtualCamera>();
                _followPOV = _virtualCameraFollow!.GetCinemachineComponent<CinemachinePOV>();
                _framingTransposer = _virtualCameraFollow.GetCinemachineComponent<CinemachineFramingTransposer>();

                initialized = true;
            }
        }

        void Start()
        {
            EnsureInitialized();

            // Activate virtual camera
            _virtualCameraFollow!.MoveToTopOfPrioritySubqueue();

            // Enable Cinemachine input
            EnableCinemachineInput();

            // Initialize camera scale
            cameraInfo.TargetScale = 0.4F;
            cameraInfo.CurrentScale = cameraInfo.TargetScale - 0.2F;

            // Make sure sprite camera uses same fov as main camera
            spriteRenderCamera!.fieldOfView = _virtualCameraFollow.m_Lens.FieldOfView;

            // Aiming is not enabled by default
            UseAimingCamera(false);
            EventManager.Instance.Broadcast(new CameraAimingEvent(false));
        }

        void Update()
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
            
            if (cameraInfo.TargetScale != cameraInfo.CurrentScale)
            {
                cameraInfo.CurrentScale = Mathf.Lerp(cameraInfo.CurrentScale, cameraInfo.TargetScale, Time.deltaTime * zoomSmoothFactor);
                _framingTransposer!.m_CameraDistance = Mathf.Lerp(cameraZOffsetNear, cameraZOffsetFar, cameraInfo.CurrentScale);
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

        public override void SetTarget(Transform target)
        {
            EnsureInitialized();
            _virtualCameraFollow!.Follow = target;
        }

        public override Transform GetTarget()
        {
            if (_virtualCameraFollow == null)
            {
                return null;
            }

            return _virtualCameraFollow.Follow;
        }

        public override void TeleportByDelta(Vector3 posDelta)
        {
            if (_virtualCameraFollow != null)
            {
                _virtualCameraFollow.OnTargetObjectWarped(_virtualCameraFollow.Follow, posDelta);
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
            return _followPOV!.m_HorizontalAxis.Value;
        }
    }
}