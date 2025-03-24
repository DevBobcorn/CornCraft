using UnityEngine;
using UnityEngine.InputSystem;

namespace AnimeSkybox
{
    public class SimpleCameraRotate : MonoBehaviour
    {
        public float sensitivity = 0.5f;
        private float rotationX = 0.0f;
        private float rotationY = 0.0f;

        private void Start()
        {
            Vector3 rot = transform.localRotation.eulerAngles;
            rotationX = rot.x;
            rotationY = rot.y;
        }

        private void Update()
        {
            if (Mouse.current.leftButton.isPressed)
            {
                rotationY += Mouse.current.delta.x.value * sensitivity;
                rotationX -= Mouse.current.delta.y.value * sensitivity;

                rotationX = Mathf.Clamp(rotationX, -90, 90);

                Quaternion localRotation = Quaternion.Euler(rotationX, rotationY, 0.0f);
                transform.rotation = localRotation;
            }
        }
    }
}