#nullable enable
using UnityEngine;
using MinecraftClient.Mapping;

namespace MinecraftClient.Rendering
{
    public class BipedEntityRender : LeggedEntityRender
    {
        public Transform? leftLeg, rightLeg;

        public override void Initialize(EntityType entityType, Entity entity)
        {
            base.Initialize(entityType, entity);

            if (leftLeg is null || rightLeg is null)
                Debug.LogWarning("Legs of biped entity not properly assigned!");
            else
                legsPresent = true;
        }

        public override void UpdateAnimation(float tickMilSec)
        {
            base.UpdateAnimation(tickMilSec);
            
            if (legsPresent)
            {
                UpdateLegAngle();
                
                leftLeg!.localEulerAngles  = new( currentLegAngle * currentMovFract, 0F, 0F);
                rightLeg!.localEulerAngles = new(-currentLegAngle * currentMovFract, 0F, 0F);
            }

        }

    }
}
