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
        [SerializeField] [Range(0F, 20F)] private float scaleSmoothFactor = 4.0F;
        [SerializeField] [Range(0F,  2F)] private float scrollSensitivity = 0.5F;
        private CinemachinePOV? followPOV;

        // Virtual camera and camera components
        [SerializeField] private CinemachineVirtualCamera? virtualCameraFollow;
        private CinemachineFramingTransposer? framingTransposer;

        public override void EnsureInitialized()
        {
            if (!initialized)
            {
                // Get virtual and render cameras
                var followObj = transform.Find("Follow Virtual");
                virtualCameraFollow = followObj.GetComponent<CinemachineVirtualCamera>();
                followPOV = virtualCameraFollow.GetCinemachineComponent<CinemachinePOV>();
                framingTransposer = virtualCameraFollow.GetCinemachineComponent<CinemachineFramingTransposer>();

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

            SetPerspective(Perspective.ThirdPerson);
        }

        void Update()
        {
            if (client == null) return;

            // Disable input when game is paused, see EnsureInitialized() above
            if (!client!.MouseScrollAbsorbed())
            {
                float scroll = CinemachineCore.GetInputAxis("Mouse ScrollWheel");

                // Update target camera status according to user input
                if (scroll != 0F)
                    cameraInfo.TargetScale = Mathf.Clamp01(cameraInfo.TargetScale - scroll * scrollSensitivity);
            }
            
            if (cameraInfo.TargetScale != cameraInfo.CurrentScale)
            {
                cameraInfo.CurrentScale = Mathf.Lerp(cameraInfo.CurrentScale, cameraInfo.TargetScale, Time.deltaTime * scaleSmoothFactor);
                framingTransposer!.m_CameraDistance = Mathf.Lerp(cameraZOffsetNear, cameraZOffsetFar, cameraInfo.CurrentScale);
            }
        }

        public override Transform? GetTarget() => virtualCameraFollow?.Follow;

        public override Vector3? GetPosition() => renderCameraPresent ? renderCamera!.transform.position : null;

        public override Transform GetTransform() => renderCameraPresent ? renderCamera!.transform : transform;

        public override void SetPerspective(Perspective perspective)
        {
            EnsureInitialized();

            // Only third person perspective is accepted, lock to it and discard the perspective change
            // Update player data
            client!.Perspective = Perspective.ThirdPerson;

            // Broadcast perspective change
            EventManager.Instance.Broadcast<PerspectiveUpdateEvent>(new(Perspective.ThirdPerson));
        }

        public override void SetTarget(Transform target)
        {
            EnsureInitialized();

            virtualCameraFollow!.Follow = target;
        }

        public override void SetYaw(float yaw) { }
    }
}