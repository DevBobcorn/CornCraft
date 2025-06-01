using UnityEngine;

namespace CraftSharp.Rendering
{
    public class QuadrupedEntityRender : LeggedEntityRender
    {
        public Transform frontLeftLeg, frontRightLeg, rearLeftLeg, rearRightLeg;

        public override void UpdateAnimation(float tickMilSec)
        {
            base.UpdateAnimation(tickMilSec); 

            UpdateLegAngle();
            
            frontLeftLeg!.localEulerAngles  = new( currentLegAngle * currentMovFract, 0F, 0F);
            frontRightLeg!.localEulerAngles = new(-currentLegAngle * currentMovFract, 0F, 0F);
            rearLeftLeg!.localEulerAngles   = new(-currentLegAngle * currentMovFract, 0F, 0F);
            rearRightLeg!.localEulerAngles  = new( currentLegAngle * currentMovFract, 0F, 0F);
        }
    }
}
