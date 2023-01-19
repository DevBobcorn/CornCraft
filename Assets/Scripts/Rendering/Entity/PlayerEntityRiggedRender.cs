#nullable enable
using UnityEngine;
using MinecraftClient.Control;
using MinecraftClient.Mapping;
using MinecraftClient.Resource;

namespace MinecraftClient.Rendering
{
    public class PlayerEntityRiggedRender : EntityRender, IPlayerVisual
    {
        [SerializeField] private bool usePlayerSkin = false;

        private static readonly int GROUNDED = Animator.StringToHash("Grounded");
        private static readonly int IN_LIQUID = Animator.StringToHash("InLiquid");
        private static readonly int ON_WALL = Animator.StringToHash("OnWall");
        private static readonly int MOVING = Animator.StringToHash("Moving");
        private static readonly int SPRINTING = Animator.StringToHash("Sprinting");
        private static readonly int WALK_MODE = Animator.StringToHash("WalkMode");
        private static readonly int CENTER_DOWN_DIST = Animator.StringToHash("CenterDownDist");

        private static readonly int ROOT_MOTION = Animator.StringToHash("RootMotion");

        private static readonly int VERTICAL_SPEED = Animator.StringToHash("VerticalSpeed");
        private static readonly int HORIZONTAL_SPEED = Animator.StringToHash("HorizontalSpeed");

        private Animator? playerAnimator;

        private bool animatorPresent = false;

        protected override void Initialize()
        {
            base.Initialize();

            // Get animator component
            playerAnimator = GetComponentInChildren<Animator>();

            if (playerAnimator is not null)
                animatorPresent = true;
            else
                Debug.LogWarning("Player animator not found!");

            if (usePlayerSkin)
                UpdateSkinMaterial();
        }

        public void UpdateEntity(Entity entity) => base.Entity = entity;

        public override void SetVisualMovementVelocity(Vector3 velocity)
        {
            base.SetVisualMovementVelocity(velocity);

            if (animatorPresent)
            {
                playerAnimator!.SetFloat(VERTICAL_SPEED, velocity.y);
                playerAnimator.SetFloat(HORIZONTAL_SPEED, Mathf.Sqrt(velocity.x * velocity.x + velocity.z * velocity.z));
            }
        }

        public void UpdateVelocity(Vector3 velocity) => SetVisualMovementVelocity(velocity);

        public void UpdateStateMachine(PlayerStatus info)
        {
            if (animatorPresent)
            {
                // Update animator parameters
                playerAnimator!.SetBool(GROUNDED, info.Grounded);
                playerAnimator.SetBool(IN_LIQUID, info.InLiquid);
                playerAnimator.SetBool(ON_WALL, info.OnWall);
                playerAnimator.SetBool(MOVING, info.Moving);
                playerAnimator.SetBool(SPRINTING, info.Sprinting);
                playerAnimator.SetBool(WALK_MODE, info.WalkMode);
                playerAnimator.SetFloat(CENTER_DOWN_DIST, info.CenterDownDist);

                playerAnimator.SetBool(ROOT_MOTION, info.PlayingRootMotion);

            }
        }

        private void UpdateSkinMaterial()
        {
            var nameLower = entity!.Name?.ToLower();

            // Find skin and change materials
            if (nameLower is not null && SkinManager.SkinMaterials.ContainsKey(nameLower))
            {
                var visualObj = visual!.gameObject;

                var renderers = visualObj.GetComponentsInChildren<SkinnedMeshRenderer>();
                var mat = SkinManager.SkinMaterials[nameLower];

                foreach (var renderer in renderers)
                    renderer.sharedMaterial = mat;

            }
        }

        public void CrossFadeState(string stateName, float time, int layer, float timeOffset)
        {
            if (animatorPresent)
                playerAnimator!.CrossFade(stateName, time, layer, timeOffset);
        }

    }
}