#nullable enable
using UnityEngine;
using MinecraftClient.Mapping;

namespace MinecraftClient.Rendering
{
    public class PlayerEntityVanillaRender : BipedEntityRender
    {
        public Transform? leftArm, rightArm;

        protected bool armsPresent = false;

        public override void Initialize(EntityType entityType, Entity entity)
        {
            base.Initialize(entityType, entity);

            if (leftArm is null || rightArm is null)
                Debug.LogWarning("Arms of player entity not properly assigned!");
            else
                armsPresent = true;
            
            UpdateSkinMaterial();
        }

        public override void UpdateAnimation(float tickMilSec)
        {
            base.UpdateAnimation(tickMilSec);

            if (armsPresent)
            {
                leftArm!.localEulerAngles  = new(-currentLegAngle * currentMovFract, 0F, 0F);
                rightArm!.localEulerAngles = new( currentLegAngle * currentMovFract, 0F, 0F);
            }

        }

        private void UpdateSkinMaterial()
        {
            var nameLower = entity!.Name?.ToLower();
            var skinMats = CornApp.CurrentClient?.MaterialManager?.SkinMaterials;

            // Find skin and change materials
            if (nameLower is not null && skinMats is not null && skinMats.ContainsKey(nameLower))
            {
                var renderers = visual!.gameObject.GetComponentsInChildren<MeshRenderer>();
                var mat = skinMats[nameLower];

                foreach (var renderer in renderers)
                    renderer.sharedMaterial = mat;

            }
        }

    }
}
