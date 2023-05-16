#nullable enable
using UnityEngine;
using Cinemachine;
using MinecraftClient.Mapping;
using MinecraftClient.Event;

namespace MinecraftClient.Control
{
    public class CameraControllerCinemachine : CameraController
    {
        [SerializeField] private float nearFov = 40F;
        [SerializeField] private float farFov  = 80F;

        [SerializeField] private float cameraZOffsetNear =    2F;
        [SerializeField] private float cameraZOffsetFar  =   20F;
        [SerializeField] private float cameraYOffsetNear =  0.4F;
        [SerializeField] private float cameraYOffsetFar  = -0.4F;
        [SerializeField] private float cameraYOffsetClip =  0.1F;

        [SerializeField] [Range(0F, 20F)] private float scaleSmoothFactor = 4.0F;
        [SerializeField] [Range(0F,  2F)] private float scrollSensitivity = 0.5F;

        [SerializeField] private LayerMask thirdPersonCullingMask;
        [SerializeField] private LayerMask firstPersonCullingMask;

        // Virtual camera and camera components
        private CinemachineVirtualCamera? virtualCameraFollow;
        private CinemachineFramingTransposer? framingTransposer;
        
        private CinemachineVirtualCamera? virtualCameraFixed;

        private CinemachinePOV? followPOV, fixedPOV;

        private float currentTargetDistance;

        public override void EnsureInitialized()
        {
            if (!initialized)
            {
                // Get virtual and render cameras
                var followObj = transform.Find("Follow Virtual");
                virtualCameraFollow = followObj.GetComponent<CinemachineVirtualCamera>();
                followPOV = virtualCameraFollow.GetCinemachineComponent<CinemachinePOV>();
                framingTransposer = virtualCameraFollow.GetCinemachineComponent<CinemachineFramingTransposer>();

                var fixedObj = transform.Find("Fixed Virtual");
                virtualCameraFixed = fixedObj.GetComponent<CinemachineVirtualCamera>();
                fixedPOV = virtualCameraFixed.GetCinemachineComponent<CinemachinePOV>();

                renderCamera = GetComponentInChildren<Camera>();

                if (renderCamera is not null)
                    renderCameraPresent = true;
                else
                    Debug.LogWarning("Render camera not found!");

                // Override input axis to disable input when paused
                CinemachineCore.GetInputAxis = (axisName) => (CornApp.CurrentClient?.IsPaused() ?? true) ? 0F : Input.GetAxis(axisName);

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
            virtualCameraFollow!.m_Lens.FieldOfView = fov;
            virtualCameraFixed!.m_Lens.FieldOfView  = fov;

            SetPerspective(Perspective.ThirdPerson);
        }

        void Update() => ManagedUpdate(Time.deltaTime);

        void ManagedUpdate(float interval)
        {
            // Disable input when game is paused, see EnsureInitialized() above
            if (!client!.MouseScrollAbsorbed())
            {
                float scroll = CinemachineCore.GetInputAxis("Mouse ScrollWheel");

                // Update target camera status according to user input
                if (scroll != 0F)
                    cameraInfo.TargetScale = Mathf.Clamp01(cameraInfo.TargetScale - scroll * scrollSensitivity);
            }

            var curPersp = client!.Perspective;
            
            if (cameraInfo.TargetScale != cameraInfo.CurrentScale)
            {
                cameraInfo.CurrentScale = Mathf.Lerp(cameraInfo.CurrentScale, cameraInfo.TargetScale, interval * scaleSmoothFactor);

                var fov = Mathf.Lerp(nearFov, farFov, cameraInfo.CurrentScale);
                virtualCameraFollow!.m_Lens.FieldOfView = fov;
                virtualCameraFixed!.m_Lens.FieldOfView  = fov;

                if (curPersp == Perspective.ThirdPerson) // Update target local position
                {
                    framingTransposer!.m_TrackedObjectOffset = new(0F, Mathf.Max(cameraYOffsetClip, Mathf.Lerp(cameraYOffsetNear, cameraYOffsetFar, cameraInfo.CurrentScale)), 0F);
                    framingTransposer!.m_CameraDistance = Mathf.Lerp(cameraZOffsetNear, cameraZOffsetFar, cameraInfo.CurrentScale);
                }
                
            }

        }

        public override void SetTarget(Transform target)
        {
            EnsureInitialized();

            virtualCameraFollow!.Follow = target;
            virtualCameraFollow!.LookAt = target;

            virtualCameraFixed!.Follow = target;
        }

        public override Transform? GetTarget()
        {
            EnsureInitialized();
            
            return virtualCameraFollow!.Follow;
        }

        public override float GetYaw() => renderCameraPresent ? renderCamera!.transform.eulerAngles.y : 0F;

        public override void SetYaw(float yaw)
        {
            followPOV!.m_HorizontalAxis.Value = yaw;
            fixedPOV!.m_HorizontalAxis.Value = yaw;
        }

        public override Vector3? GetPosition() => renderCameraPresent ? renderCamera!.transform.position : null;

        public override Transform GetTransform() => renderCameraPresent ? renderCamera!.transform : transform;

        public override void SetPerspective(Perspective perspective)
        {
            EnsureInitialized();

            switch (perspective)
            {
                case Perspective.FirstPerson:
                    EnterFirstPersonMode(client!.Perspective);
                    break;
                case Perspective.ThirdPerson:
                    EnterThirdPersonMode(client!.Perspective);
                    break;
            }

            // Update player data
            client!.Perspective = perspective;

            // Broadcast perspective change
            EventManager.Instance.Broadcast<PerspectiveUpdateEvent>(new(perspective));
        }

        public override bool IsFixed()
        {
            // Camera controller is fixed when player's in first person mode
            return client!.Perspective == Perspective.FirstPerson;
        }

        private void EnterFirstPersonMode(Perspective prevPersp)
        {
            if (prevPersp == Perspective.ThirdPerson)
            {
                // Sync virtual camera rotation
                fixedPOV!.m_HorizontalAxis.Value = followPOV!.m_HorizontalAxis.Value;
                fixedPOV!.m_VerticalAxis.Value   = followPOV!.m_VerticalAxis.Value;

                // Reset virtual camera distance
                framingTransposer!.m_CameraDistance = 0F;
            }

            // Don't render player on this camera
            if (renderCameraPresent)
                renderCamera!.cullingMask = firstPersonCullingMask;
            
            // Update field of view
            var fov = Mathf.Lerp(nearFov, farFov, cameraInfo.CurrentScale);
            virtualCameraFollow!.m_Lens.FieldOfView = fov;
            virtualCameraFixed!.m_Lens.FieldOfView  = fov;
            
            // Make fixed virtual camera the live camera
            virtualCameraFixed!.MoveToTopOfPrioritySubqueue();
        }

        private void EnterThirdPersonMode(Perspective prevPersp)
        {
            if (prevPersp == Perspective.FirstPerson)
            {
                // Sync virtual camera rotation
                followPOV!.m_HorizontalAxis.Value = fixedPOV!.m_HorizontalAxis.Value;
                followPOV!.m_VerticalAxis.Value   = fixedPOV!.m_VerticalAxis.Value;
            }

            // Render player on this camera
            if (renderCameraPresent)
                renderCamera!.cullingMask = thirdPersonCullingMask;
            
            // Update field of view
            var fov = Mathf.Lerp(nearFov, farFov, cameraInfo.CurrentScale);
            virtualCameraFollow!.m_Lens.FieldOfView = fov;
            virtualCameraFixed!.m_Lens.FieldOfView  = fov;

            // Update target offset
            framingTransposer!.m_TrackedObjectOffset = new(0F, Mathf.Max(cameraYOffsetClip, Mathf.Lerp(cameraYOffsetNear, cameraYOffsetFar, cameraInfo.CurrentScale)), 0F);
            framingTransposer!.m_CameraDistance = Mathf.Lerp(cameraZOffsetNear, cameraZOffsetFar, cameraInfo.CurrentScale);

            // Enable follow camera collider
            virtualCameraFollow!.GetComponent<CinemachineCollider>().enabled = true;

            // Make normal virtual camera the live camera
            virtualCameraFollow!.MoveToTopOfPrioritySubqueue();
        }

    }
}