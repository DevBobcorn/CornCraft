using UnityEngine;

namespace MinecraftClient.Control
{
    public class CameraController : MonoBehaviour
    {
        private LayerMask checkLayer;
        private Camera mainCamera;
        private Transform target;
        private Vector3 cameraPositionTarget, currentVelocity = Vector3.zero;
        public float sensitivityX = 5F;
        public float sensitivityY = 3F;
        public float sensitivityWheel = 15F;
        public float near = 40F;
        public float far = 80F;
        public float cameraYOffset = 0.1F;
        public float cameraZOffset = -2F;
        private float fixedYOffset, fixedZOffset;
        private bool fixedMode;

        void Awake()
        {
            checkLayer = ~LayerMask.GetMask("Player", "Ignore Raycast");
            mainCamera = Camera.main;
            mainCamera.fieldOfView = 60F;

            //DisableFixedMode();
            EnableFixedMode(1.8F, 0F);
        }

        public void SetTarget(Transform target)
        {
            this.target = target;
        }

        public void Scroll(float scroll)
        {
            if (scroll != 0)
            {
                mainCamera.fieldOfView = mainCamera.fieldOfView - scroll * sensitivityWheel;
                mainCamera.fieldOfView = Mathf.Clamp(mainCamera.fieldOfView, near, far);
            }
        }

        public void Tick(float interval, float mouseX, float mouseY)
        {
            if (!fixedMode)
            {
                Vector3 orgPivotEuler = transform.rotation.eulerAngles;

                float rotationX = orgPivotEuler.y + mouseX * sensitivityX;
                float rotationY = orgPivotEuler.x - mouseY * sensitivityY;

                if (rotationY < 0F) rotationY += 360F;

                transform.rotation = Quaternion.Euler(
                    rotationY > 180F ? Mathf.Clamp(rotationY, 300F, 360F) : Mathf.Clamp(rotationY, 0F, 60F),
                    rotationX,
                    0F
                );

                CheckCameraPosition();
            }
        }

        public void LateTick(float interval, float mouseX, float mouseY)
        {
            if (!fixedMode)
            {
                transform.position = Vector3.SmoothDamp(transform.position, target.position + Vector3.up, ref currentVelocity, 0.05F);
            }
            else
            {
                // Update position and rotation
                transform.position = target.position;
                Vector3 orgPivotEuler = transform.rotation.eulerAngles;

                float rotationX = orgPivotEuler.y + mouseX * sensitivityX;
                float rotationY = orgPivotEuler.x - mouseY * sensitivityY;

                if (rotationY < 0F) rotationY += 360F;

                transform.rotation = Quaternion.Euler(
                    rotationY > 180F ? Mathf.Clamp(rotationY, 300F, 360F) : Mathf.Clamp(rotationY, 0F, 60F),
                    rotationX,
                    0F
                );
            }
        }

        public float GetCursorRotation()
        {
            return 360F - transform.eulerAngles.y;
        }

        public Vector2 GetPlayerFocusOnScreen()
        {
            return mainCamera.WorldToScreenPoint(target.position + Vector3.up); // TODO
        }

        private void CheckCameraPosition()
        {
            RaycastHit hitInfo;
            if (Physics.Linecast(transform.position, mainCamera.transform.position, out hitInfo, checkLayer))
            {
                mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, hitInfo.point, 0.2F);
            }
            else
            {
                mainCamera.transform.localPosition = Vector3.Lerp(mainCamera.transform.localPosition, cameraPositionTarget, 0.05F);
            }
        }

        public void EnableFixedMode(float fixedY, float fixedZ)
        {
            fixedMode = true;
            fixedYOffset = fixedY;
            fixedZOffset = fixedZ;
            mainCamera.transform.localPosition = cameraPositionTarget = new Vector3(0F, fixedYOffset, fixedZOffset);
        }

        public void DisableFixedMode()
        {
            fixedMode = false;
            mainCamera.transform.localPosition = cameraPositionTarget = new Vector3(0F, cameraYOffset, cameraZOffset);
        }

    }

}