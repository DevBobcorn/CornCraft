#nullable enable
using UnityEngine;
using CraftSharp.Control;

namespace CraftSharp.Rendering
{
    public class PlayerEntityRiggedRender : AnimatorEntityRender
    {
        private static readonly int GROUNDED_HASH = Animator.StringToHash("Grounded");
        private static readonly int FLOATING_HASH = Animator.StringToHash("Floating");
        private static readonly int CLINGING_HASH = Animator.StringToHash("Clinging");
        private static readonly int GLIDING_HASH = Animator.StringToHash("Gliding");
        private static readonly int MOVING_HASH = Animator.StringToHash("Moving");
        private static readonly int SPRINTING_HASH = Animator.StringToHash("Sprinting");
        private static readonly int WALK_MODE_HASH = Animator.StringToHash("WalkMode");
        private static readonly int CENTER_DOWN_DIST_HASH = Animator.StringToHash("CenterDownDist");

        private static readonly int ROOT_MOTION_HASH = Animator.StringToHash("RootMotion");
        private static readonly int ATTACKING_HASH = Animator.StringToHash("Attacking");

        public override void Initialize(EntityType entityType, Entity entity)
        {
            base.Initialize(entityType, entity);

            // Subscribe player controller events
            var playerController = GetComponentInParent<PlayerController>();
            playerController.OnCrossFadeState += this.CrossFadeState;
            playerController.OnOverrideState += this.OverrideState;
            playerController.OnRandomizeMirroredFlag += this.RandomizeMirroredFlag;
            playerController.OnJumpRequest += this.SetJumpFlag;

            var visualObj = _visualTransform!.gameObject;

            // Get player animator
            entityAnimator = visualObj.GetComponent<Animator>();
            // Apply overridden animator controller
            animatorOverrideController = new AnimatorOverrideController(entityAnimator.runtimeAnimatorController);
            entityAnimator.runtimeAnimatorController = animatorOverrideController;

            // Add and initialize player widgets
            var renderWidget = visualObj.AddComponent<PlayerRenderWidget>();
            renderWidget.Initialize();

            var itemMountRef = entityAnimator!.GetBoneTransform(HumanBodyBones.Spine);
            var mainHandRef = entityAnimator.GetBoneTransform(HumanBodyBones.RightHand);
            var offHandRef = entityAnimator.GetBoneTransform(HumanBodyBones.LeftHand);
            renderWidget.SetRefTransforms(mainHandRef, offHandRef, itemMountRef);
        }

        public override void Unload()
        {
            // Unsubscribe player controller events. This is necessary because we
            // cannot effectively clear all subscriptions from the event publisher
            var playerController = GetComponentInParent<PlayerController>();
            playerController.OnCrossFadeState -= this.CrossFadeState;
            playerController.OnOverrideState -= this.OverrideState;
            playerController.OnRandomizeMirroredFlag -= this.RandomizeMirroredFlag;
            playerController.OnJumpRequest -= this.SetJumpFlag;

            var visualObj = _visualTransform!.gameObject;

            // Unload player widgets, allowing it to unsubscribe their events from player controller
            var renderWidget = visualObj.GetComponent<PlayerRenderWidget>();
            renderWidget.Unload();

            // Self destruction
            if (this != null)
            {
                Destroy(this.gameObject);
            }
        }

        public void InitializeActiveItem(ItemStack? itemStack, ItemActionType actionType)
        {
            // Initialize player active item
            var renderWidget = _visualTransform!.GetComponent<PlayerRenderWidget>();
            renderWidget.UpdateActiveItem(itemStack, actionType);
        }

        public override void UpdateAnimatorParams(PlayerStatus info)
        {
            // Update animator parameters
            entityAnimator!.SetBool(GROUNDED_HASH, info.Grounded);
            entityAnimator.SetBool(FLOATING_HASH, info.Floating);
            entityAnimator.SetBool(CLINGING_HASH, info.Clinging);
            entityAnimator.SetBool(GLIDING_HASH, info.Gliding);
            entityAnimator.SetBool(MOVING_HASH, info.Moving);
            entityAnimator.SetBool(SPRINTING_HASH, info.Sprinting);
            entityAnimator.SetBool(WALK_MODE_HASH, info.WalkMode);
            entityAnimator.SetFloat(CENTER_DOWN_DIST_HASH, info.CenterDownDist);

            entityAnimator.SetBool(ROOT_MOTION_HASH, info.PlayingRootMotion);
            entityAnimator.SetBool(ATTACKING_HASH, info.Attacking);
        }
    }
}