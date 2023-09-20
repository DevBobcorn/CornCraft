#nullable enable
using System;
using UnityEngine;
using Cinemachine;
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

        [SerializeField] [Range(0F, 20F)] private float scaleSmoothFactor = 4.0F;
        [SerializeField] [Range(0F,  2F)] private float scrollSensitivity = 0.5F;

        [SerializeField] private LayerMask thirdPersonCullingMask;
        [SerializeField] private LayerMask firstPersonCullingMask;

        // Virtual camera and camera components
        [SerializeField] private CinemachineVirtualCamera? virtualCameraFollow;
        private CinemachineFramingTransposer? framingTransposer;
        
        [SerializeField] private CinemachineVirtualCamera? virtualCameraFixed;

        [SerializeField] private CinemachineVirtualCamera? virtualCameraAim;

        private CinemachinePOV? followPOV, fixedPOV;
        private bool isAiming = false;

        public override void EnsureInitialized()
        {
            if (!initialized)
            {
                // Get virtual and render cameras
                followPOV = virtualCameraFollow!.GetCinemachineComponent<CinemachinePOV>();
                framingTransposer = virtualCameraFollow.GetCinemachineComponent<CinemachineFramingTransposer>();
                fixedPOV = virtualCameraFixed!.GetCinemachineComponent<CinemachinePOV>();

                // Override input axis to disable input when paused
                CinemachineCore.GetInputAxis = (axisName) => PlayerUserInputData.Current.Paused ? 0F : Input.GetAxis(axisName);

                initialized = true;
            }
        }

        private Action<CameraAimEvent>? cameraAimCallback;

        void Start()
        {
            EnsureInitialized();

            // Initialize camera scale
            cameraInfo.CurrentScale = cameraInfo.TargetScale;

            // Apply default Fov
            var fov = Mathf.Lerp(nearFov, farFov, cameraInfo.CurrentScale);
            virtualCameraFollow!.m_Lens.FieldOfView = fov;
            virtualCameraFixed!.m_Lens.FieldOfView  = fov;

            // Set perspective to current value to initialize
            SetPerspective(perspective);

            cameraAimCallback = (e) => UseAimCamera(e.Aim, e.AimRef);
            
            EventManager.Instance.Register(cameraAimCallback);
        }

        void OnDestroy()
        {
            if (cameraAimCallback is not null)
                EventManager.Instance.Unregister(cameraAimCallback);
        }

        void Update()
        {
            // Disable input when game is paused, see EnsureInitialized() above
            if (!PlayerUserInputData.Current.MouseScrollAbsorbed)
            {
                float scroll = CinemachineCore.GetInputAxis("Mouse ScrollWheel");

                // Update target camera status according to user input
                if (scroll != 0F)
                    cameraInfo.TargetScale = Mathf.Clamp01(cameraInfo.TargetScale - scroll * scrollSensitivity);
            }
            
            if (cameraInfo.TargetScale != cameraInfo.CurrentScale)
            {
                cameraInfo.CurrentScale = Mathf.Lerp(cameraInfo.CurrentScale, cameraInfo.TargetScale, Time.deltaTime * scaleSmoothFactor);

                var fov = Mathf.Lerp(nearFov, farFov, cameraInfo.CurrentScale);
                virtualCameraFollow!.m_Lens.FieldOfView = fov;
                virtualCameraFixed!.m_Lens.FieldOfView  = fov;

                if (perspective == Perspective.ThirdPerson) // Update target local position
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

        public override Transform? GetTarget() => virtualCameraFollow?.Follow;

        public override void SetAimRef(Transform aimRef)
        {
            EnsureInitialized();

            virtualCameraAim!.Follow = aimRef;
        }

        public override void SetYaw(float yaw)
        {
            followPOV!.m_HorizontalAxis.Value = yaw;
            fixedPOV!.m_HorizontalAxis.Value = yaw;
        }

        public override void SetPerspective(Perspective newPersp)
        {
            EnsureInitialized();

            switch (newPersp)
            {
                case Perspective.FirstPerson:
                    EnterFirstPersonMode();
                    break;
                case Perspective.ThirdPerson:
                    EnterThirdPersonMode();
                    break;
            }

            // Broadcast perspective change
            EventManager.Instance.Broadcast<PerspectiveUpdateEvent>(new(newPersp));
        }

        private bool IsFixed()
        {
            // Camera controller is fixed when player's in first person mode
            return perspective == Perspective.FirstPerson;
        }

        public override Vector3 GetTargetViewportPos()
        {
            if (IsFixed())
                return VIEWPORT_CENTER;
            
            var targetPos = GetTarget()?.position;
            if (renderCamera != null && targetPos is not null)
            {
                return renderCamera!.WorldToViewportPoint(targetPos.Value);
            }
            return VIEWPORT_CENTER;
        }

        private void EnterFirstPersonMode()
        {
            if (perspective == Perspective.ThirdPerson) // Previously third person
            {
                // Sync virtual camera params
                fixedPOV!.m_HorizontalAxis.Value = followPOV!.m_HorizontalAxis.Value;
                fixedPOV!.m_VerticalAxis.Value   = followPOV!.m_VerticalAxis.Value;

                // Reset virtual camera distance
                framingTransposer!.m_CameraDistance = 0F;
            }

            if (!isAiming)
            {
                // Don't render player on this camera
                if (renderCamera != null)
                {
                    renderCamera.cullingMask = firstPersonCullingMask;
                }
                
                // Update field of view
                var fov = Mathf.Lerp(nearFov, farFov, cameraInfo.CurrentScale);
                virtualCameraFollow!.m_Lens.FieldOfView = fov;
                virtualCameraFixed!.m_Lens.FieldOfView  = fov;
                
                // Make fixed virtual camera the live camera
                virtualCameraFixed!.MoveToTopOfPrioritySubqueue();
            }

            perspective = Perspective.FirstPerson;
            EventManager.Instance.Broadcast(new CrosshairEvent(true));
        }

        private void EnterThirdPersonMode()
        {
            if (perspective == Perspective.FirstPerson) // Previously first person
            {
                // Sync virtual camera params
                followPOV!.m_HorizontalAxis.Value = fixedPOV!.m_HorizontalAxis.Value;
                followPOV!.m_VerticalAxis.Value   = fixedPOV!.m_VerticalAxis.Value;
            }

            if (!isAiming)
            {
                // Render player on this camera
                if (renderCamera != null)
                {
                    renderCamera.cullingMask = thirdPersonCullingMask;
                }
                
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

            perspective = Perspective.ThirdPerson;
            EventManager.Instance.Broadcast(new CrosshairEvent(false));
        }

        public void UseAimCamera(bool enable, Transform? aimRef)
        {
            EnsureInitialized();

            isAiming = enable;

            if (enable)
            {
                if (aimRef != null)
                {
                    virtualCameraAim!.Follow = aimRef;
                    virtualCameraAim.MoveToTopOfPrioritySubqueue();
                }

                EventManager.Instance.Broadcast(new CrosshairEvent(true));
            }
            else
            {
                switch (perspective)
                {
                    case Perspective.FirstPerson:
                        EnterFirstPersonMode();
                        break;
                    case Perspective.ThirdPerson:
                        EnterThirdPersonMode();
                        break;
                }
            }
        }
    }
}