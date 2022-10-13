#nullable enable
using UnityEngine;

namespace MinecraftClient.Rendering
{
    public class BipedEntityRender : LeggedEntityRender
    {
        public Transform? leftLeg, rightLeg;

        protected override void Initialize()
        {
            base.Initialize();

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
