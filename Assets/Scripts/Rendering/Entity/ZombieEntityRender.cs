using UnityEngine;

namespace CraftSharp.Rendering
{
    public class ZombieEntityRender : BipedEntityRender
    {
        public Transform leftArm, rightArm;

        public override void UpdateAnimation(float tickMilSec)
        {
            base.UpdateAnimation(tickMilSec);

            leftArm!.localEulerAngles  = new(-90F + currentMovFract * 5F, 0F, 0F);
            rightArm!.localEulerAngles = new(-90F + currentMovFract * 5F, 0F, 0F);
        }
    }
}
