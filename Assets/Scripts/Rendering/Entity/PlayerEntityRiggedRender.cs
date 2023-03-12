#nullable enable
using UnityEngine;

using MinecraftClient.Control;
using MinecraftClient.Mapping;
using MinecraftClient.Resource;

namespace MinecraftClient.Rendering
{
    public class PlayerEntityRiggedRender : AnimatorEntityRender
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
        private static readonly int ATTACKING = Animator.StringToHash("Attacking");

        public override void Initialize(EntityType entityType, Entity entity)
        {
            base.Initialize(entityType, entity);

            if (usePlayerSkin)
                UpdateSkinMaterial(entity);
        }

        public override void UpdateStateMachine(PlayerStatus info)
        {
            if (animatorPresent)
            {
                // Update animator parameters
                entityAnimator!.SetBool(GROUNDED, info.Grounded);
                entityAnimator.SetBool(IN_LIQUID, info.InLiquid);
                entityAnimator.SetBool(ON_WALL, info.OnWall);
                entityAnimator.SetBool(MOVING, info.Moving);
                entityAnimator.SetBool(SPRINTING, info.Sprinting);
                entityAnimator.SetBool(WALK_MODE, info.WalkMode);
                entityAnimator.SetFloat(CENTER_DOWN_DIST, info.CenterDownDist);

                entityAnimator.SetBool(ROOT_MOTION, info.PlayingRootMotion);
                entityAnimator.SetBool(ATTACKING, info.Attacking);
            }
        }

        private void UpdateSkinMaterial(Entity player)
        {
            var nameLower = player.Name?.ToLower();

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

    }
}