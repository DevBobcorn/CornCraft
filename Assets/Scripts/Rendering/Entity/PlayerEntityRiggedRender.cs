using System.Collections.Generic;
using UnityEngine;
using CraftSharp.Control;

namespace CraftSharp.Rendering
{
    public class PlayerEntityRiggedRender : AnimatorEntityRender
    {
        private static readonly int GLIDING_HASH = Animator.StringToHash("Gliding");
        private static readonly int MOVING_HASH = Animator.StringToHash("Moving");
        private static readonly int SPRINTING_HASH = Animator.StringToHash("Sprinting");
        private static readonly int WALK_MODE_HASH = Animator.StringToHash("WalkMode");
        private static readonly int CENTER_DOWN_DIST_HASH = Animator.StringToHash("CenterDownDist");

        private static readonly int ROOT_MOTION_HASH = Animator.StringToHash("RootMotion");
        private static readonly int ATTACKING_HASH = Animator.StringToHash("Attacking");

        private PlayerRiggedRenderWidget playerRenderWidget;

        [SerializeField] private GameObject[] slimModelObjects = { };
        [SerializeField] private GameObject[] regularModelObjects = { };

        public override void Initialize(EntityData entity, Vector3Int originOffset)
        {
            base.Initialize(entity, originOffset);

            // Subscribe player controller events
            var playerController = GetComponentInParent<PlayerController>();
            playerController.OnCrossFadeState += CrossFadeState;
            playerController.OnOverrideState += OverrideState;
            playerController.OnRandomizeMirroredFlag += RandomizeMirroredFlag;

            var visualObj = _visualTransform!.gameObject;

            // Get player animator
            entityAnimator = visualObj.GetComponent<Animator>();
            // Apply overridden animator controller
            animatorOverrideController = new AnimatorOverrideController(entityAnimator!.runtimeAnimatorController);
            entityAnimator.runtimeAnimatorController = animatorOverrideController;

            // Add and initialize player widgets
            playerRenderWidget = visualObj.GetComponent<PlayerRiggedRenderWidget>();
            playerRenderWidget.Initialize();

            var itemMountRef = entityAnimator!.GetBoneTransform(HumanBodyBones.Spine);
            var mainHandRef = entityAnimator.GetBoneTransform(HumanBodyBones.RightHand);
            var offHandRef = entityAnimator.GetBoneTransform(HumanBodyBones.LeftHand);

            playerRenderWidget.SetRefTransforms(mainHandRef, offHandRef, itemMountRef);
        }

        public override Transform GetAimingRef()
        {
            return playerRenderWidget ? playerRenderWidget.m_AimingRef : base.GetAimingRef();
        }

        public Vector2 GetClimbOverOffset()
        {
            return playerRenderWidget ? playerRenderWidget.m_ClimbOverOffset : Vector2.zero;
        }

        public override void Unload()
        {
            // Unsubscribe player controller events. This is necessary because we
            // cannot effectively clear all subscriptions from the event publisher
            var playerController = GetComponentInParent<PlayerController>();
            playerController.OnCrossFadeState -= CrossFadeState;
            playerController.OnOverrideState -= OverrideState;
            playerController.OnRandomizeMirroredFlag -= RandomizeMirroredFlag;

            var visualObj = _visualTransform!.gameObject;

            // Unload player widgets, allowing it to unsubscribe their events from player controller
            var renderWidget = visualObj.GetComponent<PlayerRiggedRenderWidget>();
            renderWidget.Unload();

            // Self destruction
            if (this)
            {
                Destroy(gameObject);
            }
        }

        #nullable enable
        public void InitializeActiveItem(ItemStack? itemStack, ItemActionType actionType)
        {
            // Initialize player active item
            var renderWidget = _visualTransform!.GetComponent<PlayerRiggedRenderWidget>();
            renderWidget.UpdateActiveItem(itemStack, actionType);
        }
        #nullable disable

        public override void UpdateAnimator(PlayerStatus info)
        {
            if (!entityAnimator) return;
            
            // Update animator parameters
            entityAnimator.SetBool(GLIDING_HASH, info.Gliding);
            entityAnimator.SetBool(MOVING_HASH, info.Moving);
            entityAnimator.SetBool(SPRINTING_HASH, info.Sprinting);
            entityAnimator.SetBool(WALK_MODE_HASH, info.WalkMode);
            entityAnimator.SetFloat(CENTER_DOWN_DIST_HASH, info.CenterDownDist);

            entityAnimator.SetBool(ROOT_MOTION_HASH, info.PlayingRootMotion);
            entityAnimator.SetBool(ATTACKING_HASH, info.Attacking);

            // Update animator widget
            playerRenderWidget.ManagedUpdate();
        }

        protected override Dictionary<string, string> GetControlVariables()
        {
            return new Dictionary<string, string>
            {
                ["player_skin"] = $"skin:{UUID}"
            };
        }
    }
}