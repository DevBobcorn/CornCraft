#nullable enable
using UnityEngine;

namespace MinecraftClient.Rendering
{
    public class EntityRagdoll : MonoBehaviour
    {
        [SerializeField] public Rigidbody? mainRigidbody;

        void Start()
        {
            if (mainRigidbody == null)
            {
                Debug.LogWarning("Ragdoll main rigidbody not assigned!");
            }
            
        }
    }
}