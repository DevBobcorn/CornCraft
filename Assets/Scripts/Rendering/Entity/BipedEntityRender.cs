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

        public override void ManagedUpdate(Vector3 cameraPos, float tickMilSec)
        {
            base.ManagedUpdate(cameraPos, tickMilSec);
            
            if (legsPresent)
            {
                UpdateLegAngle();
                
                leftLeg!.localEulerAngles  = new( currentLegAngle * currentMovFact, 0F, 0F);
                rightLeg!.localEulerAngles = new(-currentLegAngle * currentMovFact, 0F, 0F);
            }

        }

    }
}
