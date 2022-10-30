#nullable enable
using UnityEngine;
using Cinemachine;

namespace MinecraftClient.Control
{
    [RequireComponent(typeof (CinemachineVirtualCamera))]
    public class CameraControllerCinemachine : CameraController
    {
        [SerializeField] private float cameraZOffsetNear =  -3F;
        [SerializeField] private float cameraZOffsetFar  =  -9F;
        [SerializeField] private float cameraYOffsetNear = 0.1F;
        [SerializeField] private float cameraYOffsetFar  = 0.5F;
        [SerializeField] [Range(0F, 20F)] private float scaleSmoothFactor = 4F;
        [SerializeField] [Range(0F, 20F)] private float scrollSensitivity = 1F;

        // Virtual camera and camera components
        private CinemachineVirtualCamera? virtualCamera;
        private CinemachineFramingTransposer? framingTransposer;
        private CinemachineHardLockToTarget? hardLockToTarget;

        private CinemachineCollider? cameraCollider;

        private float currentTargetDistance;

        // flag indicating whether framing transposer is initialized
        private bool initialized = false;

        public void EnsureInitialized()
        {
            if (!initialized)
            {
                virtualCamera = GetComponent<CinemachineVirtualCamera>();
                cameraCollider = GetComponent<CinemachineCollider>();

                // Override input axis to disable input when paused
                CinemachineCore.GetInputAxis = (axisName) => {
                    return CornClient.Instance.IsPaused() ? 0F : Input.GetAxis(axisName);
                };

                initialized = true;
            }
        }

        void Start()
        {
            EnsureInitialized();

            // Initialize camera scale
            cameraInfo.CurrentScale = cameraInfo.TargetScale;

            if (!cameraInfo.FixedMode) // Apply default offset
                DisableFixedMode();
            else
                EnableFixedMode();
        }

        void Update() => ManagedUpdate(Time.deltaTime);

        void ManagedUpdate(float interval)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel") * scrollSensitivity;

            // Update target camera status according to user input
            if (scroll != 0F)
                cameraInfo.TargetScale = Mathf.Clamp01(cameraInfo.TargetScale - scroll);
            
            if (cameraInfo.TargetScale != cameraInfo.CurrentScale)
            {
                cameraInfo.CurrentScale = Mathf.Lerp(cameraInfo.CurrentScale, cameraInfo.TargetScale, interval * scaleSmoothFactor);
                
                if (!cameraInfo.FixedMode) // Update target local position
                {
                    framingTransposer!.m_TrackedObjectOffset = new(0F, Mathf.Lerp(cameraYOffsetNear, cameraYOffsetFar, cameraInfo.CurrentScale), 0F);
                    framingTransposer!.m_CameraDistance = Mathf.Lerp(cameraZOffsetNear, cameraZOffsetFar, cameraInfo.CurrentScale);
                }
            }

        }

        public override void SetTarget(Transform target)
        {
            EnsureInitialized();

            virtualCamera!.Follow = target;
            virtualCamera!.LookAt = target;
        }

        public override float GetCameraYaw() => transform.eulerAngles.y;

        public override void EnableFixedMode()
        {
            EnsureInitialized();
            cameraInfo.FixedMode = true;

            hardLockToTarget = virtualCamera!.AddCinemachineComponent<CinemachineHardLockToTarget>();

            var pov = virtualCamera!.AddCinemachineComponent<CinemachinePOV>();
            pov.m_VerticalAxis   = new( -80,  80, false, false, 600F, 0F, 0F, "Mouse Y",  true);
            pov.m_HorizontalAxis = new(-180, 180,  true, false, 600F, 0F, 0F, "Mouse X", false);

            // Disable obstacle avoiding
            cameraCollider!.enabled = false;
        }

        public override void DisableFixedMode()
        {
            EnsureInitialized();
            cameraInfo.FixedMode = false;
            
            framingTransposer = virtualCamera!.AddCinemachineComponent<CinemachineFramingTransposer>();
            framingTransposer!.m_SoftZoneWidth  = 0.3F;
            framingTransposer!.m_SoftZoneHeight = 0.5F;

            framingTransposer!.m_TrackedObjectOffset = new(0F, Mathf.Lerp(cameraYOffsetNear, cameraYOffsetFar, cameraInfo.CurrentScale), 0F);
            framingTransposer!.m_CameraDistance = Mathf.Lerp(cameraZOffsetNear, cameraZOffsetFar, cameraInfo.CurrentScale);

            var pov = virtualCamera!.AddCinemachineComponent<CinemachinePOV>();
            pov.m_VerticalAxis   = new( -89,  89, false, false, 300F, 0.1F, 0.1F, "Mouse Y",  true);
            pov.m_HorizontalAxis = new(-180, 180,  true, false, 300F, 0.1F, 0.1F, "Mouse X", false);

            // Disable obstacle avoiding
            cameraCollider!.enabled = true;
        }

    }
}