#nullable enable
using UnityEngine;

namespace MinecraftClient.Rendering
{
    public class QuadrupedEntityRender : LeggedEntityRender
    {
        public Transform? frontLeftLeg, frontRightLeg, rearLeftLeg, rearRightLeg;

        protected override void Initialize()
        {
            base.Initialize();
            
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
                
                frontLeftLeg!.localEulerAngles  = new( currentLegAngle * currentMovFact, 0F, 0F);
                frontRightLeg!.localEulerAngles = new(-currentLegAngle * currentMovFact, 0F, 0F);
                rearLeftLeg!.localEulerAngles   = new(-currentLegAngle * currentMovFact, 0F, 0F);
                rearRightLeg!.localEulerAngles  = new( currentLegAngle * currentMovFact, 0F, 0F);
            }

        }

    }
}
