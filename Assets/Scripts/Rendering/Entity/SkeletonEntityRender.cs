#nullable enable
using UnityEngine;

namespace CraftSharp.Rendering
{
    public class SkeletonEntityRender : BipedEntityRender
    {
        public Transform? leftArm, rightArm;

        public override void UpdateAnimation(float tickMilSec)
        {
            base.UpdateAnimation(tickMilSec);

            leftArm!.localEulerAngles  = new(-90F, 30F, 0F);
            rightArm!.localEulerAngles = new(-90F,  0F, 0F);
        }
    }
}
