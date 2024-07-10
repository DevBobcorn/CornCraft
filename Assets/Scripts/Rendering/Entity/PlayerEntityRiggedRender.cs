#nullable enable
using UnityEngine;
using CraftSharp.Control;

namespace CraftSharp.Rendering
{
    public class PlayerEntityRiggedRender : AnimatorEntityRender
    {
        [SerializeField] private Renderer[] playerSkinRenderers = { };

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

            // Subscribe player events
            var playerController = GetComponentInParent<PlayerController>();
            playerController.OnCrossFadeState += this.CrossFadeState;
            playerController.OnOverrideState += this.OverrideState;
            playerController.OnRandomizeMirroredFlag += () => {
                var mirrored = Time.frameCount % 2 == 0;
                SetMirroredFlag(mirrored);
            };
            playerController.OnJumpRequest += this.SetJumpFlag;

            var visualObj = _visualTransform!.gameObject;

            // Get player animator
            entityAnimator = visualObj.GetComponent<Animator>();
            // Apply overridden animator controller
            animatorOverrideController = new AnimatorOverrideController(entityAnimator.runtimeAnimatorController);
            entityAnimator.runtimeAnimatorController = animatorOverrideController;

            // Add and initialize player widgets
            var accessoryWidget = visualObj.AddComponent<PlayerAccessoryWidget>();
            accessoryWidget.Initialize();

            var itemMountRef = entityAnimator!.GetBoneTransform(HumanBodyBones.Spine);
            var mainHandRef = entityAnimator.GetBoneTransform(HumanBodyBones.RightHand);
            var offHandRef = entityAnimator.GetBoneTransform(HumanBodyBones.LeftHand);
            accessoryWidget.SetRefTransforms(mainHandRef, offHandRef, itemMountRef);

            UpdateSkinMaterial();
        }

        public void InitializeActiveItem(ItemStack? itemStack, ItemActionType actionType)
        {
            // Initialize player active item
            var accessoryWidget = _visualTransform!.GetComponent<PlayerAccessoryWidget>();
            accessoryWidget.UpdateActiveItem(itemStack, actionType);
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

        private void UpdateSkinMaterial()
        {
            if (playerSkinRenderers.Length == 0)
            {
                // No render in this model uses player skin, no need to update
                return;
            }

            /*
            var nameLower = Name?.ToLower();
            var skinMats = CornApp.CurrentClient!.MaterialManager!.SkinMaterials;

            // Find skin and change materials
            if (nameLower is not null && skinMats.ContainsKey(nameLower))
            {
                var mat = skinMats[nameLower];

                foreach (var renderer in playerSkinRenderers)
                    renderer.sharedMaterial = mat;

                Debug.Log($"Skin applied to {nameLower}");
            }
            else
            {
                Debug.LogWarning($"Failed to apply skin for {nameLower}");
            }
            */
        }
    }
}