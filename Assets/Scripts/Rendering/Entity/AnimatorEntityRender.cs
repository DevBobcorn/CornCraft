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

        protected Animator? entityAnimator;
        protected bool animatorPresent = false;

        public override void Initialize(EntityType entityType, Entity entity)
        {
            base.Initialize(entityType, entity);

            // Get animator component
            entityAnimator = GetComponentInChildren<Animator>();

            if (entityAnimator is not null)
            {
                animatorPresent = true;
                Debug.Log("Meow!");
            }
            else
                Debug.LogWarning("Player animator not found!");
        }

        public override void SetVisualMovementVelocity(Vector3 velocity)
        {
            base.SetVisualMovementVelocity(velocity);

            if (animatorPresent)
            {
                entityAnimator!.SetFloat(VERTICAL_SPEED, velocity.y);
                entityAnimator.SetFloat(HORIZONTAL_SPEED, Mathf.Sqrt(velocity.x * velocity.x + velocity.z * velocity.z));
            }
        }

        public virtual void UpdateStateMachine(PlayerStatus info) { }

        public virtual void SetMirroredFlag(bool flag)
        {
            if (animatorPresent)
            {
                entityAnimator!.SetBool(MIRRORED, flag);
            }
        }

        public virtual void CrossFadeState(string stateName, float time, int layer, float timeOffset)
        {
            if (animatorPresent)
                entityAnimator!.CrossFade(stateName, time, layer, timeOffset);
        }
    }
}