using UnityEngine;
using MinecraftClient.UI;

namespace MinecraftClient.Control
{
    [RequireComponent(typeof (CameraController))]
    [AddComponentMenu("Unicorn/Control/Camera User Control")]
    public class CameraUserControl : MonoBehaviour
    {
        CameraController camControl;
        private bool scrollLocked = false;

        void Start()
        {
            camControl = GetComponent<CameraController>();
        }

        void Update()
        {
            if (CornClient.Instance.IsPaused() || Cursor.lockState != CursorLockMode.Locked) return;

            float x = Input.GetAxis("Mouse X");
            float y = Input.GetAxis("Mouse Y");
            camControl.Tick(Time.deltaTime, x, y);
            if (!scrollLocked)
            {
                ScrollCamera(Input.GetAxis("Mouse ScrollWheel"));
            }
        }

        void LateUpdate()
        {
            if (CornClient.Instance.IsPaused() || Cursor.lockState != CursorLockMode.Locked) return;

            camControl.LateTick(Time.deltaTime);
        }

        public void ScrollCamera(float scroll)
        {
            if (!camControl) return;
            camControl.Scroll(scroll);
        }
    }
}
