#nullable enable
using UnityEngine;

namespace CraftSharp.Rendering
{
    public class EntityRagdoll : MonoBehaviour
    {
        [SerializeField] public Rigidbody? mainRigidbody;
        [SerializeField] protected Transform? _visualTransform;
        public Transform Visual => _visualTransform!;

        private void Start()
        {
            if (mainRigidbody == null)
            {
                Debug.LogWarning("Ragdoll main rigidbody not assigned!");
            }

            if (_visualTransform == null)
            {
                Debug.LogWarning("Visual transform for entity render not assigned!");
                _visualTransform = transform;
            }
        }
    }
}