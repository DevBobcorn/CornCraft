#nullable enable
using UnityEngine;
using MinecraftClient.Mapping;
using MinecraftClient.Resource;

namespace MinecraftClient.Rendering
{
    public class PlayerEntityVanillaRender : BipedEntityRender
    {
        public Transform? leftArm, rightArm;

        protected bool armsPresent = false;

        protected override void Initialize()
        {
            base.Initialize();

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

            // Find skin and change materials
            if (nameLower is not null && SkinManager.SkinMaterials.ContainsKey(nameLower))
            {
                var renderers = visual!.gameObject.GetComponentsInChildren<MeshRenderer>();
                var mat = SkinManager.SkinMaterials[nameLower];

                foreach (var renderer in renderers)
                    renderer.sharedMaterial = mat;

            }
        }

    }
}
