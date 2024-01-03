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
        private CinemachineVirtualCamera? virtualCameraFollow;
        private CinemachineFramingTransposer? framingTransposer;
        private CinemachinePOV? followPOV;

        public override void EnsureInitialized()
        {
            if (!initialized)
            {
                // Get virtual and render cameras
                var followObj = transform.Find("Follow Virtual");
                virtualCameraFollow = followObj.GetComponent<CinemachineVirtualCamera>();
                followPOV = virtualCameraFollow!.GetCinemachineComponent<CinemachinePOV>();
                framingTransposer = virtualCameraFollow.GetCinemachineComponent<CinemachineFramingTransposer>();

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
                framingTransposer!.m_CameraDistance = Mathf.Lerp(cameraZOffsetNear, cameraZOffsetFar, cameraInfo.CurrentScale);
            }
        }

        public override void SetTarget(Transform target)
        {
            EnsureInitialized();
            virtualCameraFollow!.Follow = target;
        }

        public override Transform? GetTarget() => virtualCameraFollow?.Follow;

        public override void SetYaw(float yaw)
        {
            followPOV!.m_HorizontalAxis.Value = yaw;
        }
    }
}