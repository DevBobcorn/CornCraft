#nullable enable
using UnityEngine;
using MinecraftClient.Mapping;

namespace MinecraftClient.Rendering
{
    public class QuadrupedEntityRender : LeggedEntityRender
    {
        public Transform? frontLeftLeg, frontRightLeg, rearLeftLeg, rearRightLeg;

        public override void Initialize(EntityType entityType, Entity entity)
        {
            base.Initialize(entityType, entity);
            
            if (frontLeftLeg is null || frontRightLeg is null || rearLeftLeg is null || rearRightLeg is null)
                Debug.LogWarning("Legs of quadruped entity not properly assigned!");
            else
                legsPresent = true;
        }

        public override void UpdateAnimation(float tickMilSec)
        {
            base.UpdateAnimation(tickMilSec); 
            
            if (legsPresent)
            {
                UpdateLegAngle();
                
                frontLeftLeg!.localEulerAngles  = new( currentLegAngle * currentMovFract, 0F, 0F);
                frontRightLeg!.localEulerAngles = new(-currentLegAngle * currentMovFract, 0F, 0F);
                rearLeftLeg!.localEulerAngles   = new(-currentLegAngle * currentMovFract, 0F, 0F);
                rearRightLeg!.localEulerAngles  = new( currentLegAngle * currentMovFract, 0F, 0F);
            }

        }

    }
}
