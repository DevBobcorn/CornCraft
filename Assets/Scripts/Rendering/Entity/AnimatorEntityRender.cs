#nullable enable
using UnityEngine;

using CraftSharp.Control;

namespace CraftSharp.Rendering
{
    public abstract class AnimatorEntityRender : EntityRender
    {
        protected static readonly int MIRRORED_HASH = Animator.StringToHash("Mirrored");
        protected static readonly int JUMP_HASH = Animator.StringToHash("Jump");

        protected static readonly int VERTICAL_SPEED_HASH = Animator.StringToHash("VerticalSpeed");
        protected static readonly int HORIZONTAL_SPEED_HASH = Animator.StringToHash("HorizontalSpeed");

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

        public override void SetVisualMovementVelocity(Vector3 velocity, Vector3 upDirection)
        {
            _visualMovementVelocity = velocity;

            // Get vertical velocity
            var vertical = Vector3.Project(velocity, upDirection);
            var upward = Vector3.Dot(velocity, upDirection) > 0F;

            // Assign vertical speed and horizontal speed
            entityAnimator!.SetFloat(VERTICAL_SPEED_HASH, upward ? vertical.magnitude : -vertical.magnitude);
            entityAnimator.SetFloat(HORIZONTAL_SPEED_HASH, (velocity - vertical).magnitude);
        }

        public virtual void UpdateAnimator(PlayerStatus info) { }

        public virtual void RandomizeMirroredFlag()
        {
            var mirrored = Time.frameCount % 2 == 0;
            entityAnimator!.SetBool(MIRRORED_HASH, mirrored);
        }

        public virtual void SetJumpFlag()
        {
            entityAnimator!.SetTrigger(JUMP_HASH);
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