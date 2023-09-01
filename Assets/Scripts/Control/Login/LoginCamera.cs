using UnityEngine;

namespace CraftSharp.Control
{
    public class LoginCamera : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 5F;

        void Update()
        {
            transform.position += Vector3.forward * moveSpeed * Time.deltaTime;
        }
    }
}