using UnityEngine;

namespace CraftSharp.Rendering
{
    public class LivingEntityRender : EntityRender
    {
        public Transform head;
        
        public override Transform GetAimingRef()
        {
            return head;
        }

        public override void UpdateAnimation(float tickMilSec)
        {
            head!.localEulerAngles = new(lastPitch, lastHeadYaw - lastYaw);
        }
    }
}