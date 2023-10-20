#nullable enable
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

        [SerializeField] private LayerMask thirdPersonCullingMask;
        [SerializeField] private LayerMask firstPersonCullingMask;
        // Virtual camera and camera components
        [SerializeField] private CinemachineVirtualCamera? virtualCameraFollow;
        private CinemachineFramingTransposer? framingTransposer;        
        [SerializeField] private CinemachineVirtualCamera? virtualCameraFixed;
        [SerializeField] private CinemachineVirtualCamera? virtualCameraAim;

        private CinemachinePOV? followPOV, fixedPOV, aimingPOV;

        public override void EnsureInitialized()
        {
            if (!initialized)
            {
                // Get virtual and render cameras
                followPOV = virtualCameraFollow!.GetCinemachineComponent<CinemachinePOV>();
                framingTransposer = virtualCameraFollow.GetCinemachineComponent<CinemachineFramingTransposer>();
                fixedPOV = virtualCameraFixed!.GetCinemachineComponent<CinemachinePOV>();

                aimingPOV = virtualCameraAim!.GetCinemachineComponent<CinemachinePOV>();

                initialized = true;
            }
        }

        void Start()
        {
            EnsureInitialized();

            // Initialize camera scale
            zoomInput!.action.Enable();
            cameraInfo.CurrentScale = cameraInfo.TargetScale;

            // Apply default Fov
            var fov = Mathf.Lerp(nearFov, farFov, cameraInfo.CurrentScale);
            virtualCameraFollow!.m_Lens.FieldOfView = fov;
            virtualCameraFixed!.m_Lens.FieldOfView  = fov;

            // Set perspective to current value to initialize
            SetPerspective(perspective);
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

            virtualCameraAim!.Follow = target;
        }

        public override Transform? GetTarget()
        {
            return virtualCameraFollow?.Follow;
        }

        public override void EnableAimingCamera(bool enable)
        {
            EnsureInitialized();
            IsAiming = enable;

            if (enable)
            {
                switch (perspective)
                {
                    case Perspective.FirstPerson:
                        aimingPOV!.m_HorizontalAxis.Value = fixedPOV!.m_HorizontalAxis.Value;
                        aimingPOV!.m_VerticalAxis.Value = followPOV!.m_VerticalAxis.Value;
                        break;
                    case Perspective.ThirdPerson:
                        aimingPOV!.m_HorizontalAxis.Value = followPOV!.m_HorizontalAxis.Value;
                        aimingPOV!.m_VerticalAxis.Value = followPOV!.m_VerticalAxis.Value;
                        //Debug.Log($"D: {followPOV!.m_VerticalAxis.Value} {aimingPOV!.m_VerticalAxis.Value}");
                        break;
                }

                virtualCameraAim!.MoveToTopOfPrioritySubqueue();
                EventManager.Instance.Broadcast(new CrosshairEvent(true));
            }
            else
            {
                switch (perspective)
                {
                    case Perspective.FirstPerson:
                        fixedPOV!.m_HorizontalAxis.Value = aimingPOV!.m_HorizontalAxis.Value;
                        fixedPOV!.m_VerticalAxis.Value = aimingPOV!.m_VerticalAxis.Value;
                        EnterFirstPersonMode();
                        break;
                    case Perspective.ThirdPerson:
                        followPOV!.m_HorizontalAxis.Value = aimingPOV!.m_HorizontalAxis.Value;
                        followPOV!.m_VerticalAxis.Value = aimingPOV!.m_VerticalAxis.Value;
                        //Debug.Log($"T: {followPOV!.m_VerticalAxis.Value} {aimingPOV!.m_VerticalAxis.Value}");
                        EnterThirdPersonMode();
                        break;
                }
            }
        }

        public override void SetYaw(float yaw)
        {
            followPOV!.m_HorizontalAxis.Value = yaw;
            fixedPOV!.m_HorizontalAxis.Value = yaw;
        }

        public override float GetYaw()
        {
            if (IsAiming)
                return aimingPOV!.m_HorizontalAxis.Value;
            
            return perspective == Perspective.FirstPerson ?
                    fixedPOV!.m_HorizontalAxis.Value : followPOV!.m_HorizontalAxis.Value;
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
            if (IsFixed()) // Use screen center if camera is fixed
            {
                return VIEWPORT_CENTER;
            }
            
            return base.GetTargetViewportPos();
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

            if (!IsAiming)
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

            if (!IsAiming)
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
    }
}