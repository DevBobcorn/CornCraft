#nullable enable
using UnityEngine;

using MinecraftClient.Control;
using MinecraftClient.Mapping;

namespace MinecraftClient.Rendering
{
    public class AnimatorEntityRender : EntityRender
    {
        protected static readonly int MIRRORED = Animator.StringToHash("Mirrored");

        protected static readonly int VERTICAL_SPEED = Animator.StringToHash("VerticalSpeed");
        protected static readonly int HORIZONTAL_SPEED = Animator.StringToHash("HorizontalSpeed");

        [SerializeField] protected Animator? entityAnimator;

        public override void SetVisualMovementVelocity(Vector3 velocity)
        {
            base.SetVisualMovementVelocity(velocity);

            entityAnimator!.SetFloat(VERTICAL_SPEED, velocity.y);
            entityAnimator.SetFloat(HORIZONTAL_SPEED, Mathf.Sqrt(velocity.x * velocity.x + velocity.z * velocity.z));
        }

        public virtual void UpdateStateMachine(PlayerStatus info) { }

        public virtual void SetMirroredFlag(bool flag)
        {
            entityAnimator!.SetBool(MIRRORED, flag);
        }

        public virtual void CrossFadeState(string stateName, float time, int layer, float timeOffset)
        {
            entityAnimator!.CrossFade(stateName, time, layer, timeOffset);
        }
    }
}