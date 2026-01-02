using System.Collections.Generic;
using UnityEngine;

namespace CraftSharp.Rendering
{
    public class PlayerEntityVanillaRender : BipedEntityRender
    {
        // Player dimensions
        private const float PLAYER_WIDTH  = 0.6F;
        private const float PLAYER_HEIGHT = 1.8F;
        private const float PLAYER_HEIGHT_SNEAKING = 1.5F;

        [SerializeField] private Transform headForCamera;
        [SerializeField] private Transform leftArm, rightArm;
        [SerializeField] private Transform torso;
        [SerializeField] private GameObject[] slimModelObjects = { };
        [SerializeField] private GameObject[] regularModelObjects = { };
        [SerializeField] private AnimationClip standingClip, sneakingClip;
        [SerializeField] private Animation _animation;

        public override void Initialize(EntitySpawnData entitySpawn, Vector3Int originOffset)
        {
            base.Initialize(entitySpawn, originOffset);

            // Apply the initial pose and listen for subsequent pose updates
            Pose.OnValueUpdate += OnPoseChanged;
        }
        
        public override Transform GetAimingRef()
        {
            // Use a separate transform for custom offset when sneaking
            return headForCamera;
        }

        public override void UpdateAnimation(float tickMilSec)
        {
            base.UpdateAnimation(tickMilSec);

            leftArm.localEulerAngles  = new(-currentLegAngle * currentMovFract + torso.localEulerAngles.x, 0F, 0F);
            rightArm.localEulerAngles = new( currentLegAngle * currentMovFract + torso.localEulerAngles.x, 0F, 0F);
        }

        protected override void HandleMaterialUpdate(EntityMaterialManager matManager, ResourceLocation textureId, Material updatedMaterial)
        {
            if (matManager.SkinModels.TryGetValue(textureId, out bool slimModel))
            {
                Debug.Log($"Player {textureId} Slim model: {slimModel}");
                SetSlimModel(slimModel);
            }
        }

        public override Vector2 GetDimensions()
        {
            return new Vector2(PLAYER_WIDTH, Pose.Value == EntityPose.Sneaking ? PLAYER_HEIGHT_SNEAKING : PLAYER_HEIGHT);
        }

        private void SetSlimModel(bool slimModel)
        {
            foreach (var model in slimModelObjects)
            {
                model.SetActive(slimModel);
            }

            foreach (var model in regularModelObjects)
            {
                model.SetActive(!slimModel);
            }
        }

        protected override Dictionary<string, string> GetControlVariables()
        {
            return new Dictionary<string, string>
            {
                ["player_skin"] = $"skin:{UUID}"
            };
        }

        private void OnPoseChanged(EntityPose prevPose, EntityPose newPose)
        {
            ApplyPoseClip(newPose);
        }

        private void ApplyPoseClip(EntityPose pose)
        {
            // Default to standing pose if a dedicated clip is not provided
            var targetClip = pose switch
            {
                EntityPose.Sneaking => sneakingClip,
                _ => standingClip
            };

            if (!targetClip || !_visualTransform)
            {
                return;
            }

            // Sample the first frame to immediately apply the pose to the visual hierarchy
            _animation.clip = targetClip;
            _animation.Play();
        }
    }
}
