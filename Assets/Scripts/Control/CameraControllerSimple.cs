#nullable enable
using UnityEngine;

namespace MinecraftClient.Control
{
    public class CameraControllerSimple : CameraController
    {
        [SerializeField] private LayerMask obstacleLayer;
        [SerializeField] private float nearFov = 40F;
        [SerializeField] private float farFov  = 80F;
        [SerializeField] private float cameraZOffsetNear =  -3F;
        [SerializeField] private float cameraZOffsetFar  =  -9F;
        [SerializeField] private float cameraYOffsetNear = 0.1F;
        [SerializeField] private float cameraYOffsetFar  = 0.5F;
        [SerializeField] private float followSmoothTime   = 0.25F;
        [SerializeField] private float scaleSmoothFactor    =  5F;
        [SerializeField] private float rotateSmoothFactor   =  5F;
        [SerializeField] private float obstacleSmoothFactor =  2F;

        private readonly CameraUserInputData inputData = new();
        private CameraUserInput? userInput;

        public override void EnsureInitialized()
        {
            if (!initialized)
            {
                // Get render camera
                renderCamera = GetComponentInChildren<Camera>();

                if (renderCamera is not null)
                    renderCameraPresent = true;
                else
                    Debug.LogWarning("Render camera not found!");

                // Get user input
                userInput = GetComponent<CameraUserInput>();

                initialized = true;
            }
        }

        void Start()
        {
            EnsureInitialized();

            // Initialize camera scale
            cameraInfo.CurrentScale = cameraInfo.TargetScale;

            if (renderCameraPresent)
            {
                // Apply default Fov
                renderCamera!.fieldOfView = Mathf.Lerp(nearFov, farFov, cameraInfo.CurrentScale);

                if (!cameraInfo.FixedMode)// Apply default offset
                    renderCamera.transform.localPosition = cameraInfo.TargetLocalPosition = new(0F, 0F, Mathf.Lerp(cameraZOffsetNear, cameraZOffsetFar, cameraInfo.CurrentScale));
                else
                    renderCamera!.transform.localPosition = cameraInfo.TargetLocalPosition = Vector3.zero;
            }
        }

        void Update() => ManagedUpdate(Time.deltaTime);

        public void ManagedUpdate(float interval)
        {
            // Update user input
            userInput!.UpdateInputs(inputData);

            // Update target camera status according to user input
            if (inputData.scroll != 0F)
                cameraInfo.TargetScale = Mathf.Clamp01(cameraInfo.TargetScale - inputData.scroll);
            
            if (inputData.mouseDelta != Vector2.zero)
            {
                // Update yaw (unclamped)
                cameraInfo.TargetYaw = cameraInfo.TargetYaw + inputData.mouseDelta.x;

                // Update pitch (invert pitch input)
                cameraInfo.TargetPitch = cameraInfo.TargetPitch - inputData.mouseDelta.y;
                cameraInfo.TargetPitch = Mathf.Clamp(cameraInfo.TargetPitch, -85F, 85F);
            }
            
        }

        void LateUpdate() => ManagedLateUpdate(Time.deltaTime);

        public void ManagedLateUpdate(float interval)
        {
            if (cameraInfo.Target is null)
                return;

            if (cameraInfo.TargetYaw != cameraInfo.CurrentYaw || cameraInfo.TargetPitch != cameraInfo.CurrentPitch)
            {
                if (cameraInfo.FixedMode)
                {
                    cameraInfo.CurrentYaw   = cameraInfo.TargetYaw;
                    cameraInfo.CurrentPitch = cameraInfo.TargetPitch;
                }
                else
                {
                    cameraInfo.CurrentYaw   = Mathf.Lerp(cameraInfo.CurrentYaw, cameraInfo.TargetYaw, interval * rotateSmoothFactor);
                    cameraInfo.CurrentPitch = Mathf.Lerp(cameraInfo.CurrentPitch, cameraInfo.TargetPitch, interval * rotateSmoothFactor);
                }

                transform.rotation = Quaternion.Euler(
                    cameraInfo.CurrentPitch,
                    cameraInfo.CurrentYaw,
                    0F
                );
            }

            if (cameraInfo.TargetScale != cameraInfo.CurrentScale)
            {
                cameraInfo.CurrentScale = Mathf.Lerp(cameraInfo.CurrentScale, cameraInfo.TargetScale, interval * scaleSmoothFactor);
                
                if (renderCameraPresent) // Update Fov
                    renderCamera!.fieldOfView = Mathf.Lerp(nearFov, farFov, cameraInfo.CurrentScale);
                
                if (!cameraInfo.FixedMode)// Update target local position
                    cameraInfo.TargetLocalPosition = new(0F, 0F, Mathf.Lerp(cameraZOffsetNear, cameraZOffsetFar, cameraInfo.CurrentScale));
            }

            if (!cameraInfo.FixedMode) // Normal update
            {
                transform.position = Vector3.SmoothDamp(transform.position,
                        cameraInfo.Target.position + Vector3.up * Mathf.Lerp(cameraYOffsetNear, cameraYOffsetFar, cameraInfo.CurrentScale), ref cameraInfo.CurrentVelocity, followSmoothTime);

                if (renderCameraPresent)
                {
                    // Obstacle check
                    RaycastHit hitInfo;
                    if (Physics.Linecast(transform.position, transform.TransformPoint(cameraInfo.TargetLocalPosition), out hitInfo, obstacleLayer))
                        renderCamera!.transform.position = hitInfo.point;
                    else
                        renderCamera!.transform.localPosition = Vector3.Lerp(renderCamera.transform.localPosition, cameraInfo.TargetLocalPosition, interval * obstacleSmoothFactor);
                }
            }
            else // Fixed mode update
                transform.position = cameraInfo.Target.position;
        }

        public override void SetTarget(Transform target) => cameraInfo.Target = target;

        public override float GetYaw() => transform.eulerAngles.y;

        public override Vector3? GetPosition() => renderCameraPresent ? renderCamera!.transform.position : null;

        public override Transform GetTransform() => renderCameraPresent ? renderCamera!.transform : transform;

        public override void EnableFixedMode()
        {
            EnsureInitialized();
            cameraInfo.FixedMode = true;

            if (renderCameraPresent)
            {
                renderCamera!.transform.localPosition = cameraInfo.TargetLocalPosition = Vector3.zero;

                // Don't render player on this camera
                renderCamera!.cullingMask = renderCamera.cullingMask & ~(1 << LayerMask.NameToLayer("Player"));
            }
        }

        public override void DisableFixedMode()
        {
            EnsureInitialized();
            cameraInfo.FixedMode = false;

            if (renderCameraPresent)
            {
                renderCamera!.transform.localPosition = cameraInfo.TargetLocalPosition = new Vector3(0F, 0F, Mathf.Lerp(cameraZOffsetNear, cameraZOffsetFar, cameraInfo.CurrentScale));

                // Render player on this camera
                renderCamera!.cullingMask = renderCamera.cullingMask | (1 << LayerMask.NameToLayer("Player"));
            }
        }

    }

}