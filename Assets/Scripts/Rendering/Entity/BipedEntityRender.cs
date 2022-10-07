#nullable enable
using UnityEngine;

namespace MinecraftClient.Rendering
{
    public class BipedEntityRender : LeggedEntityRender
    {
        public Transform? leftLeg, rightLeg;

        void Start()
        {
            if (leftLeg is null || rightLeg is null)
                Debug.LogWarning("Legs of biped entity not properly assigned!");
            else
                legsPresent = true;
        }

        void Update()
        {
            if (legsPresent)
            {
                UpdateLegAngle();
                
                leftLeg!.localEulerAngles  = new( currentLegAngle * currentMovFact, 0F, 0F);
                rightLeg!.localEulerAngles = new(-currentLegAngle * currentMovFact, 0F, 0F);
            }

        }

    }
}
