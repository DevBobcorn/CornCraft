#nullable enable
using UnityEngine;

using MinecraftClient.Control;

namespace MinecraftClient.Rendering
{
    public class PlayerAnimatorWidget : MonoBehaviour
    {
        private PlayerController? player;
        private Animator? animator;

        public void FootL() { }

        public void FootR() { }

        void Awake()
        {
            player = GetComponentInParent<PlayerController>();
            animator = GetComponent<Animator>();

        }

        void OnAnimatorMove()
        {
            if (player!.UseRootMotion)
            {
                Vector3 newPosition = transform.parent.position;

                newPosition -= animator!.deltaPosition;
                Debug.Log($"Animator deltaPosition: {animator!.deltaPosition}");
                //animator!.ApplyBuiltinRootMotion();

                transform.transform.position = newPosition;
            }
        }
    }
}