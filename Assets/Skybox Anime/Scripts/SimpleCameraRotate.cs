using UnityEngine;

namespace AnimeSkybox
{
    public class SimpleCameraRotate : MonoBehaviour
    {
        public float sensitivity = 0.5f;
        private float rotationX = 0.0f;
        private float rotationY = 0.0f;

        void Start()
        {
            Vector3 rot = transform.localRotation.eulerAngles;
            rotationX = rot.x;
            rotationY = rot.y;
        }

        void Update()
        {
            if (Input.GetMouseButton(0))
            {
                //Debug.Log("Mouse button down");
                rotationY += Input.GetAxis("Mouse X") * sensitivity;
                rotationX -= Input.GetAxis("Mouse Y") * sensitivity;

                rotationX = Mathf.Clamp(rotationX, -90, 90);

                Quaternion localRotation = Quaternion.Euler(rotationX, rotationY, 0.0f);
                transform.rotation = localRotation;
            }
        }
    }
}