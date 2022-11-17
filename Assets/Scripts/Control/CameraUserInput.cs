using UnityEngine;

namespace MinecraftClient.Control
{
    [RequireComponent(typeof (CameraControllerSimple))]
    public class CameraUserInput : MonoBehaviour
    {
        [SerializeField] private float sensitivityYaw    = 5F;
        [SerializeField] private float sensitivityPitch  = 3F;
        [SerializeField] private float sensitivityScroll = 0.25F;
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

                if (!game.MouseScrollAbsorbed())
                    inputData.scroll = Input.GetAxis("Mouse ScrollWheel") * sensitivityScroll;
                else
                    inputData.scroll = 0;
            }
            else
            {
                inputData.mouseDelta = Vector2.zero;
                inputData.scroll = 0F;
            }
        }
    }
}
