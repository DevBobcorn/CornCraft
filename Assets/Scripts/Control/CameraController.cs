using UnityEngine;
using MinecraftClient.Mapping;

namespace MinecraftClient.Control
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private LayerMask obstacleLayer;
        [SerializeField] private float sensitivityX = 5F;
        [SerializeField] private float sensitivityY = 3F;
        [SerializeField] private float sensitivityScroll = 15F;
        [SerializeField] private float nearFov = 40F;
        [SerializeField] private float farFov  = 80F;
        [SerializeField] private float cameraZOffsetNear =  -3F;
        [SerializeField] private float cameraZOffsetFar  =  -9F;
        [SerializeField] private float cameraYOffsetNear = 0.1F;
        [SerializeField] private float cameraYOffsetFar  = 0.5F;
        [SerializeField] private float smoothTime = 0.15F;

        private static readonly Vector3 DEFAULT_FIXED_OFFSET = new(0F, 1.6F, 0F);

        public Camera ActiveCamera { get; set; }

        private CameraInfo cameraInfo = new();

        void Awake() => ActiveCamera = Camera.main;

        public void SetPerspective(Perspective perspective)
        {
            switch (perspective)
            {
                case Perspective.FirstPerson:
                    EnableFixedMode(DEFAULT_FIXED_OFFSET);
                    // Don't render player on this camera
                    ActiveCamera.cullingMask = ActiveCamera.cullingMask & ~(1 << LayerMask.NameToLayer("Player"));
                    break;
                case Perspective.ThirdPerson:
                    DisableFixedMode();
                    // Render player on this camera
                    ActiveCamera.cullingMask = ActiveCamera.cullingMask | (1 << LayerMask.NameToLayer("Player"));
                    break;
            }
            
        }

        public void SetTarget(Transform target) => cameraInfo.Target = target;

        public void Scroll(float scroll)
        {
            if (scroll != 0)
            {
                cameraInfo.Scale = Mathf.Clamp01(cameraInfo.Scale - scroll * sensitivityScroll);

                // Update Fov
                ActiveCamera.fieldOfView = Mathf.Lerp(nearFov, farFov, cameraInfo.Scale);

                // Update offset
                ActiveCamera.transform.localPosition = cameraInfo.TargetPosition = new Vector3(0F, 0F, Mathf.Lerp(cameraZOffsetNear, cameraZOffsetFar, cameraInfo.Scale));
            }
        }

        public void ManagedUpdate(float interval, float mouseX, float mouseY)
        {
            if (!cameraInfo.FixedMode)
            {
                Vector3 orgPivotEuler = transform.rotation.eulerAngles;

                float camYaw   = orgPivotEuler.y + mouseX * sensitivityX;
                float camPitch = orgPivotEuler.x - mouseY * sensitivityY;

                camPitch %= 360F;

                transform.rotation = Quaternion.Euler(
                    camPitch,
                    camYaw,
                    0F
                );

                CheckCameraPosition();
            }
        }

        public void LateTick(float interval, float mouseX, float mouseY)
        {
            if (!cameraInfo.FixedMode)
                transform.position = Vector3.SmoothDamp(transform.position,
                        cameraInfo.Target.position + Vector3.up * Mathf.Lerp(cameraYOffsetNear, cameraYOffsetFar, cameraInfo.Scale), ref cameraInfo.CurrentVelocity, smoothTime);
            else
            {
                // Update position and rotation
                transform.position = cameraInfo.Target.TransformPoint(cameraInfo.FixedOffset);
                Vector3 orgPivotEuler = transform.rotation.eulerAngles;

                float camYaw   = orgPivotEuler.y + mouseX * sensitivityX;
                float camPitch = orgPivotEuler.x - mouseY * sensitivityY;

                while (camPitch < 0F)
                    camPitch += 360F;

                transform.rotation = Quaternion.Euler(
                    camPitch > 180F ? Mathf.Clamp(camPitch, 271F, 360F) : Mathf.Clamp(camPitch, 0F, 89F),
                    camYaw,
                    0F
                );

                CheckCameraPosition();
            }
        }

        public float GetCameraYaw() => transform.eulerAngles.y;

        private void CheckCameraPosition()
        {
            RaycastHit hitInfo;
            if (Physics.Linecast(transform.position, ActiveCamera.transform.position, out hitInfo, obstacleLayer))
                ActiveCamera.transform.position = Vector3.Lerp(ActiveCamera.transform.position, hitInfo.point, Time.unscaledDeltaTime * 30F);
            else
                ActiveCamera.transform.localPosition = Vector3.Lerp(ActiveCamera.transform.localPosition, cameraInfo.TargetPosition, Time.unscaledDeltaTime * 5F);
            
        }

        public void EnableFixedMode(Vector3 fixedOffset)
        {
            cameraInfo.FixedMode = true;
            cameraInfo.FixedOffset = fixedOffset;
            ActiveCamera.transform.localPosition = cameraInfo.TargetPosition = Vector3.zero;
        }

        public void DisableFixedMode()
        {
            cameraInfo.FixedMode = false;
            ActiveCamera.transform.localPosition = cameraInfo.TargetPosition = new Vector3(0F, 0F, Mathf.Lerp(cameraZOffsetNear, cameraZOffsetFar, cameraInfo.Scale));
        }

    }

}