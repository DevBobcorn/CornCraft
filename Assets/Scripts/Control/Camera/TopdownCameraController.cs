#nullable enable
using UnityEngine;
using Cinemachine;

using CraftSharp.Event;

namespace CraftSharp.Control
{
    public class TopdownCameraController : CameraController
    {
        [SerializeField] private float cameraZOffsetNear =   15F;
        [SerializeField] private float cameraZOffsetFar  =   30F;
        private CinemachinePOV? followPOV;

        // Virtual camera and camera components
        private CinemachineVirtualCamera? virtualCameraFollow;
        private CinemachineFramingTransposer? framingTransposer;

        public override void EnsureInitialized()
        {
            if (!initialized)
            {
                // Get virtual and render cameras
                var followObj = transform.Find("Follow Virtual");
                virtualCameraFollow = followObj.GetComponent<CinemachineVirtualCamera>();
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

            SetPerspective(Perspective.ThirdPerson);
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

        public override void SetPerspective(Perspective newPersp)
        {
            EnsureInitialized();

            // Only third person perspective is accepted, lock to it and discard the perspective change
            perspective = Perspective.ThirdPerson;

            // Broadcast perspective change
            EventManager.Instance.Broadcast<PerspectiveUpdateEvent>(new(Perspective.ThirdPerson));
        }
    }
}