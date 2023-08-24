#nullable enable
using UnityEngine;

namespace CraftSharp.Rendering
{
    public class PlayerEntityVanillaRender : BipedEntityRender
    {
        [SerializeField] private Renderer[] playerSkinRenderers = { };
        [SerializeField] private Transform? leftArm, rightArm;

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
            if (playerSkinRenderers.Length == 0)
            {
                // No render in this model uses player skin, no need to update
                return;
            }

            var nameLower = entity!.Name?.ToLower();
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
        }
    }
}
