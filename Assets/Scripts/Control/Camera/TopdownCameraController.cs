#nullable enable
using UnityEngine;
using Cinemachine;

namespace CraftSharp.Control
{
    public class TopdownCameraController : CameraController
    {
        [SerializeField] private float cameraZOffsetNear =   15F;
        [SerializeField] private float cameraZOffsetFar  =   30F;
        

        // Virtual camera and camera components
        private CinemachineVirtualCamera? _virtualCameraFollow;
        private CinemachineFramingTransposer? _framingTransposer;
        private CinemachinePOV? _followPOV;

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

            // Initialize camera scale
            zoomInput!.action.Enable();
            cameraInfo.CurrentScale = cameraInfo.TargetScale;

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
                _framingTransposer!.m_CameraDistance = Mathf.Lerp(cameraZOffsetNear, cameraZOffsetFar, cameraInfo.CurrentScale);
            }
        }

        public override void SetTarget(Transform target)
        {
            EnsureInitialized();
            _virtualCameraFollow!.Follow = target;
        }

        public override Transform? GetTarget()
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