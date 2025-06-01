using UnityEngine;

namespace CraftSharp.Rendering
{
    public class EntityRagdoll : MonoBehaviour
    {
        [SerializeField] public Rigidbody mainRigidbody;
        [SerializeField] protected Transform _visualTransform;
        public Transform Visual => _visualTransform;

        private void Start()
        {
            if (!mainRigidbody)
            {
                Debug.LogWarning("Ragdoll main rigidbody not assigned!");
            }

            if (!_visualTransform)
            {
                Debug.LogWarning("Visual transform for entity render not assigned!");
                _visualTransform = transform;
            }
        }
    }
}