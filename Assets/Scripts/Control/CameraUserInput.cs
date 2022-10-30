using UnityEngine;

namespace MinecraftClient.Control
{
    [RequireComponent(typeof (CameraControllerSimple))]
    public class CameraUserInput : MonoBehaviour
    {
        [SerializeField] private float sensitivityYaw    = 5F;
        [SerializeField] private float sensitivityPitch  = 3F;
        [SerializeField] private float sensitivityScroll = 0.25F;
        [SerializeField] private bool scrollLocked = false;
        private CornClient game;

        void Start() => game = CornClient.Instance;

        public void UpdateInputs(CameraUserInputData inputData)
        {
            if (!game!.IsPaused())
            {
                inputData.mouseDelta = new(
                    Input.GetAxis("Mouse X") * sensitivityYaw,   // Yaw
                    Input.GetAxis("Mouse Y") * sensitivityPitch  // Pitch
                );

                if (!scrollLocked)
                    inputData.scroll = Input.GetAxis("Mouse ScrollWheel") * sensitivityScroll;
            }
            else
            {
                inputData.mouseDelta = Vector2.zero;
                inputData.scroll = 0F;
            }
        }
    }
}
