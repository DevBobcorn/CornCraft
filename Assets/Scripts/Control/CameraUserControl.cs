using UnityEngine;

namespace MinecraftClient.Control
{
    [RequireComponent(typeof (CameraController))]
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
            if (CornClient.Instance.IsPaused()) return;

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
            if (CornClient.Instance.IsPaused()) return;

            float x = Input.GetAxis("Mouse X");
            float y = Input.GetAxis("Mouse Y");
            camControl.LateTick(Time.deltaTime, x, y);
        }

        public void ScrollCamera(float scroll)
        {
            if (!camControl) return;
            camControl.Scroll(scroll);
        }
    }
}
