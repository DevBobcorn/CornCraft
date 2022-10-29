#nullable enable
using UnityEngine;
using MinecraftClient.Mapping;

namespace MinecraftClient.Control
{
    public class CameraController : MonoBehaviour
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

        [SerializeField] private Vector3 fixedOffset = new(0F, 1.6F, 0F);

        public Camera? ActiveCamera { get; set; }

        private CameraInfo cameraInfo = new();
        private readonly CameraUserInputData inputData = new();
        private CameraUserInput? userInput;

        void Awake() => ActiveCamera = Camera.main;

        public void SetPerspective(Perspective perspective)
        {
            switch (perspective)
            {
                case Perspective.FirstPerson:
                    EnableFixedMode();
                    // Don't render player on this camera
                    ActiveCamera!.cullingMask = ActiveCamera.cullingMask & ~(1 << LayerMask.NameToLayer("Player"));
                    break;
                case Perspective.ThirdPerson:
                    DisableFixedMode();
                    // Render player on this camera
                    ActiveCamera!.cullingMask = ActiveCamera.cullingMask | (1 << LayerMask.NameToLayer("Player"));
                    break;
            }
            
        }

        public void SetTarget(Transform target) => cameraInfo.Target = target;

        void Start() {
            // Get user input
            userInput = GetComponent<CameraUserInput>();

            // Initialize camera scale
            cameraInfo.CurrentScale = cameraInfo.TargetScale;

            // Apply default Fov
            ActiveCamera!.fieldOfView = Mathf.Lerp(nearFov, farFov, cameraInfo.CurrentScale);

            if (!cameraInfo.FixedMode)// Apply default offset
                ActiveCamera.transform.localPosition = cameraInfo.TargetLocalPosition = new(0F, 0F, Mathf.Lerp(cameraZOffsetNear, cameraZOffsetFar, cameraInfo.CurrentScale));
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
                // Update Fov
                ActiveCamera!.fieldOfView = Mathf.Lerp(nearFov, farFov, cameraInfo.CurrentScale);
                if (!cameraInfo.FixedMode)// Update target local position
                    cameraInfo.TargetLocalPosition = new(0F, 0F, Mathf.Lerp(cameraZOffsetNear, cameraZOffsetFar, cameraInfo.CurrentScale));
            }

            if (!cameraInfo.FixedMode) // Normal update
            {
                transform.position = Vector3.SmoothDamp(transform.position,
                        cameraInfo.Target.position + Vector3.up * Mathf.Lerp(cameraYOffsetNear, cameraYOffsetFar, cameraInfo.CurrentScale), ref cameraInfo.CurrentVelocity, followSmoothTime);
            
                // Obstacle check
                RaycastHit hitInfo;
                if (Physics.Linecast(transform.position, transform.TransformPoint(cameraInfo.TargetLocalPosition), out hitInfo, obstacleLayer))
                    ActiveCamera!.transform.position = hitInfo.point;
                else
                    ActiveCamera!.transform.localPosition = Vector3.Lerp(ActiveCamera.transform.localPosition, cameraInfo.TargetLocalPosition, interval * obstacleSmoothFactor);
            }
            else // Fixed mode update
                transform.position = cameraInfo.Target.TransformPoint(fixedOffset);
        }

        public float GetCameraYaw() => transform.eulerAngles.y;

        public void EnableFixedMode()
        {
            cameraInfo.FixedMode = true;
            ActiveCamera!.transform.localPosition = cameraInfo.TargetLocalPosition = Vector3.zero;
        }

        public void DisableFixedMode()
        {
            cameraInfo.FixedMode = false;
            ActiveCamera!.transform.localPosition = cameraInfo.TargetLocalPosition = new Vector3(0F, 0F, Mathf.Lerp(cameraZOffsetNear, cameraZOffsetFar, cameraInfo.CurrentScale));
        }

    }

}