#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace CraftSharp.Rendering
{
    public class PlayerEntityVanillaRender : BipedEntityRender
    {
        [SerializeField] private Transform? leftArm, rightArm;
        [SerializeField] private GameObject[] slimModelObjects = { };
        [SerializeField] private GameObject[] regularModelObjects = { };

        public override void Initialize(EntityData entity, Vector3Int originOffset)
        {
            base.Initialize(entity, originOffset);
        }

        public override void UpdateAnimation(float tickMilSec)
        {
            base.UpdateAnimation(tickMilSec);

            leftArm!.localEulerAngles  = new(-currentLegAngle * currentMovFract, 0F, 0F);
            rightArm!.localEulerAngles = new( currentLegAngle * currentMovFract, 0F, 0F);
        }

        protected override void HandleMaterialUpdate(EntityMaterialManager matManager, ResourceLocation textureId, Material updatedMaterial)
        {
            if (matManager.SkinModels.TryGetValue(textureId, out bool slimModel))
            {
                Debug.Log($"Player {textureId} Slim model: {slimModel}");
                SetSlimModel(slimModel);
            }
        }

        protected override void HandleRagdollMaterialUpdate(EntityMaterialAssigner ragdollMaterialControl, EntityMaterialManager matManager, ResourceLocation textureId, Material updatedMaterial)
        {
            // TODO: Make player ragdoll and update accordingly
        }

        public void SetSlimModel(bool slimModel)
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

        protected override Dictionary<string, string>? GetControlVariables()
        {
            return new Dictionary<string, string>
            {
                ["player_skin"] = $"skin:{UUID}"
            };
        }
    }
}
