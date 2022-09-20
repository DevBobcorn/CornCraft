using UnityEngine;

namespace MinecraftClient.Control
{
    public class CameraController : MonoBehaviour
    {
        private const float EYE_HEIGHT = 1.62F;

        private LayerMask checkLayer;
        public Camera ActiveCamera { get; set; }
        private Transform target;
        private Vector3 cameraPositionTarget, currentVelocity = Vector3.zero;
        public float sensitivityX = 5F;
        public float sensitivityY = 3F;
        public float sensitivityWheel = 15F;
        public float near = 40F;
        public float far = 80F;
        public float cameraYOffset = 0.5F, cameraZOffset = -5F;
        public float smoothTime = 0.15F;
        private float fixedYOffset, fixedZOffset;
        private bool fixedMode;

        void Awake()
        {
            checkLayer = LayerMask.GetMask("Entity", "Movement");
            ActiveCamera = Camera.main;
            ActiveCamera.fieldOfView = 60F;
        }

        public void SetPerspective(CornClient.Perspective perspective)
        {
            switch (perspective)
            {
                case CornClient.Perspective.FirstPerson:
                    EnableFixedMode(EYE_HEIGHT, 0F);
                    // Don't render player on this camera
                    ActiveCamera.cullingMask = ActiveCamera.cullingMask & ~(1 << LayerMask.NameToLayer("Player"));
                    break;
                case CornClient.Perspective.ThirdPerson:
                    DisableFixedMode();
                    // Render player on this camera
                    ActiveCamera.cullingMask = ActiveCamera.cullingMask | (1 << LayerMask.NameToLayer("Player"));
                    break;
            }
            
        }

        public void SetTarget(Transform target)
        {
            this.target = target;
        }

        public void Scroll(float scroll)
        {
            if (scroll != 0)
            {
                ActiveCamera.fieldOfView = ActiveCamera.fieldOfView - scroll * sensitivityWheel;
                ActiveCamera.fieldOfView = Mathf.Clamp(ActiveCamera.fieldOfView, near, far);
            }
        }

        public void Tick(float interval, float mouseX, float mouseY)
        {
            if (!fixedMode)
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
            if (!fixedMode)
            {
                transform.position = Vector3.SmoothDamp(transform.position, target.position + Vector3.up * cameraYOffset, ref currentVelocity, smoothTime);
            }
            else
            {
                // Update position and rotation
                transform.position = target.position + target.up * fixedYOffset - target.forward * fixedZOffset;
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

        public float GetCursorRotation()
        {
            return 360F - transform.eulerAngles.y;
        }

        public Vector2 GetPlayerFocusOnScreen()
        {
            return ActiveCamera.WorldToScreenPoint(target.position + Vector3.up * cameraYOffset);
        }

        private void CheckCameraPosition()
        {
            RaycastHit hitInfo;
            if (Physics.Linecast(transform.position, ActiveCamera.transform.position, out hitInfo, checkLayer))
            {
                ActiveCamera.transform.position = Vector3.Lerp(ActiveCamera.transform.position, hitInfo.point, 0.2F);
            }
            else
            {
                ActiveCamera.transform.localPosition = Vector3.Lerp(ActiveCamera.transform.localPosition, cameraPositionTarget, 0.05F);
            }
        }

        public void EnableFixedMode(float fixedY, float fixedZ)
        {
            fixedMode = true;
            fixedYOffset = fixedY;
            fixedZOffset = fixedZ;
            ActiveCamera.transform.localPosition = cameraPositionTarget = Vector3.zero;
        }

        public void DisableFixedMode()
        {
            fixedMode = false;
            ActiveCamera.transform.localPosition = cameraPositionTarget = new Vector3(0F, 0F, cameraZOffset);
        }

    }

}