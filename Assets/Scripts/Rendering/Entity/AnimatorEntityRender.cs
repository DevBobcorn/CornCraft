#nullable enable
using UnityEngine;

using CraftSharp.Control;

namespace CraftSharp.Rendering
{
    public abstract class AnimatorEntityRender : EntityRender
    {
        protected static readonly int MIRRORED = Animator.StringToHash("Mirrored");

        protected static readonly int VERTICAL_SPEED = Animator.StringToHash("VerticalSpeed");
        protected static readonly int HORIZONTAL_SPEED = Animator.StringToHash("HorizontalSpeed");

        protected Animator? entityAnimator;
        protected AnimatorOverrideController? animatorOverrideController;

        public static GameObject CreateFromModel(GameObject visualPrefab)
        {
            var visualObj = GameObject.Instantiate(visualPrefab);
            visualObj.name = "Visual";

            var renderObj = new GameObject($"Player {visualPrefab.name} Entity");
            var render = renderObj.AddComponent<PlayerEntityRiggedRender>();
            render.VisualTransform = visualObj.transform;

            var infoAnchorObj = new GameObject("Info Anchor");
            infoAnchorObj.transform.SetParent(renderObj.transform, false);
            infoAnchorObj.transform.localPosition = new(0F, 2F, 0F);
            render.InfoAnchor = infoAnchorObj.transform;

            visualObj.transform.SetParent(renderObj.transform, false);

            return renderObj;
        }

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

        public virtual void OverrideState(AnimationClip dummyClip, AnimationClip animationClip)
        {
            // Apply animation clip override
            animatorOverrideController![dummyClip] = animationClip;
        }
    }
}