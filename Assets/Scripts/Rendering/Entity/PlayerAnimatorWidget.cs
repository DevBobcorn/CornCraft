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

        public void Hit() { }

        void Start()
        {
            player = GetComponentInParent<PlayerController>();
            animator = GetComponent<Animator>();

        }

        void OnAnimatorMove()
        {
            if (player is not null && player.UseRootMotion)
            {
                Vector3 newPosition = transform.parent.position;

                newPosition += animator!.deltaPosition;

                transform.parent.position = newPosition;
            }
        }
    }
}