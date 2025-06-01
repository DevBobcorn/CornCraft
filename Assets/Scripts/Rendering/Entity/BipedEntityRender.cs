using UnityEngine;

namespace CraftSharp.Rendering
{
    public class BipedEntityRender : LeggedEntityRender
    {
        public Transform leftLeg, rightLeg;

        public override void UpdateAnimation(float tickMilSec)
        {
            base.UpdateAnimation(tickMilSec);
            
            UpdateLegAngle();
            
            leftLeg!.localEulerAngles  = new( currentLegAngle * currentMovFract, 0F, 0F);
            rightLeg!.localEulerAngles = new(-currentLegAngle * currentMovFract, 0F, 0F);
        }
    }
}
