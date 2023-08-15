#nullable enable
using UnityEngine;

namespace CraftSharp.Rendering
{
    public class EntityRagdoll : MonoBehaviour
    {
        [SerializeField] public Rigidbody? mainRigidbody;
        [SerializeField] protected Transform? visual;
        public Transform Visual => visual!;

        void Start()
        {
            if (mainRigidbody == null)
            {
                Debug.LogWarning("Ragdoll main rigidbody not assigned!");
            }

            if (visual == null)
            {
                Debug.LogWarning("Visual transform for entity render not assigned!");
                visual = transform;
            }
            
        }
    }
}