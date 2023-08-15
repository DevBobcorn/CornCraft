#nullable enable
using UnityEngine;

using CraftSharp.Control;

namespace CraftSharp.Rendering
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
                var rb = player.PlayerRigidbody;

                rb.position += animator!.deltaPosition;
            }
        }
    }
}