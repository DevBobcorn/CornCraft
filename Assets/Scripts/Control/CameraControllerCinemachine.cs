#nullable enable
using UnityEngine;
using Cinemachine;

namespace MinecraftClient.Control
{
    public class CameraControllerCinemachine : CameraController
    {
        [SerializeField] private float nearFov = 40F;
        [SerializeField] private float farFov  = 80F;
        [SerializeField] private float cameraZOffsetNear =   2F;
        [SerializeField] private float cameraZOffsetFar  =  20F;
        [SerializeField] private float cameraYOffsetNear = 0.1F;
        [SerializeField] private float cameraYOffsetFar  = 0.5F;
        [SerializeField] [Range(0F, 20F)] private float scaleSmoothFactor = 4.0F;
        [SerializeField] [Range(0F,  2F)] private float scrollSensitivity = 0.5F;

        // Virtual camera and camera components
        private CinemachineVirtualCamera? virtualCameraNormal;
        private CinemachineFramingTransposer? framingTransposer;
        private CinemachineVirtualCamera? virtualCameraFixed;

        private CinemachinePOV? normalPOV, fixedPOV;

        private float currentTargetDistance;

        public override void EnsureInitialized()
        {
            if (!initialized)
            {
                // Get virtual and render cameras
                var normalObj = transform.Find("Normal Virtual");
                virtualCameraNormal = normalObj.GetComponent<CinemachineVirtualCamera>();
                normalPOV = virtualCameraNormal.GetCinemachineComponent<CinemachinePOV>();
                framingTransposer = virtualCameraNormal.GetCinemachineComponent<CinemachineFramingTransposer>();

                var fixedObj = transform.Find("Fixed Virtual");
                virtualCameraFixed = fixedObj.GetComponent<CinemachineVirtualCamera>();
                fixedPOV = virtualCameraFixed.GetCinemachineComponent<CinemachinePOV>();

                renderCamera = GetComponentInChildren<Camera>();

                if (renderCamera is not null)
                    renderCameraPresent = true;
                else
                    Debug.LogWarning("Render camera not found!");

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

            // Apply default Fov
            var fov = Mathf.Lerp(nearFov, farFov, cameraInfo.CurrentScale);
            virtualCameraNormal!.m_Lens.FieldOfView = fov;
            virtualCameraFixed!.m_Lens.FieldOfView  = fov;

            if (!cameraInfo.FixedMode) // Apply default offset
                DisableFixedMode();
            else
                EnableFixedMode();
        }

        void Update() => ManagedUpdate(Time.deltaTime);

        void ManagedUpdate(float interval)
        {
            // Disable input when game is paused, see EnsureInitialized() above
            float scroll = CinemachineCore.GetInputAxis("Mouse ScrollWheel") * scrollSensitivity;

            // Update target camera status according to user input
            if (scroll != 0F)
                cameraInfo.TargetScale = Mathf.Clamp01(cameraInfo.TargetScale - scroll);
            
            if (cameraInfo.TargetScale != cameraInfo.CurrentScale)
            {
                cameraInfo.CurrentScale = Mathf.Lerp(cameraInfo.CurrentScale, cameraInfo.TargetScale, interval * scaleSmoothFactor);
                
                var fov = Mathf.Lerp(nearFov, farFov, cameraInfo.CurrentScale);
                virtualCameraNormal!.m_Lens.FieldOfView = fov;
                virtualCameraFixed!.m_Lens.FieldOfView  = fov;

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

            virtualCameraNormal!.Follow = target;
            virtualCameraNormal!.LookAt = target;

            virtualCameraFixed!.Follow = target;
        }

        public override float GetYaw() => renderCameraPresent ? renderCamera!.transform.eulerAngles.y : 0F;

        public override Vector3? GetPosition() => renderCameraPresent ? renderCamera!.transform.position : null;

        public override Transform GetTransform() => renderCameraPresent ? renderCamera!.transform : transform;

        public override void EnableFixedMode()
        {
            EnsureInitialized();
            cameraInfo.FixedMode = true;

            // Sync virtual camera rotation
            fixedPOV!.m_HorizontalAxis.Value = normalPOV!.m_HorizontalAxis.Value;
            fixedPOV!.m_VerticalAxis.Value   = normalPOV!.m_VerticalAxis.Value;

            // Make fixed virtual camera the live camera
            virtualCameraFixed!.MoveToTopOfPrioritySubqueue();

            // Don't render player on this camera
            if (renderCameraPresent)
                renderCamera!.cullingMask = renderCamera.cullingMask & ~(1 << LayerMask.NameToLayer("Player"));
        }

        public override void DisableFixedMode()
        {
            EnsureInitialized();
            cameraInfo.FixedMode = false;

            // Sync virtual camera rotation
            normalPOV!.m_HorizontalAxis.Value = fixedPOV!.m_HorizontalAxis.Value;
            normalPOV!.m_VerticalAxis.Value   = fixedPOV!.m_VerticalAxis.Value;
            
            // Make normal virtual camera the live camera
            virtualCameraNormal!.MoveToTopOfPrioritySubqueue();

            // Render player on this camera
            if (renderCameraPresent)
                renderCamera!.cullingMask = renderCamera.cullingMask | (1 << LayerMask.NameToLayer("Player"));
        }

    }
}