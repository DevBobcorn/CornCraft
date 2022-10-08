#nullable enable
using UnityEngine;

namespace MinecraftClient.Rendering
{
    public class PlayerEntityRender : BipedEntityRender
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
        }

        public override void UpdateAnimation(float tickMilSec)
        {
            base.UpdateAnimation(tickMilSec);

            if (armsPresent)
            {
                leftArm!.localEulerAngles  = new(-currentLegAngle * currentMovFact, 0F, 0F);
                rightArm!.localEulerAngles = new( currentLegAngle * currentMovFact, 0F, 0F);
            }

        }

    }
}
