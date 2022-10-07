#nullable enable
using UnityEngine;

namespace MinecraftClient.Rendering
{
    public class ZombieEntityRender : BipedEntityRender
    {
        public Transform? leftArm, rightArm;

        protected bool armsPresent = false;

        protected override void Initialize()
        {
            base.Initialize();

            if (leftArm is null || rightArm is null)
                Debug.LogWarning("Arms of zombie entity not properly assigned!");
            else
                armsPresent = true;
        }

        public override void ManagedUpdate(Vector3 cameraPos, float tickMilSec)
        {
            base.ManagedUpdate(cameraPos, tickMilSec);

            if (armsPresent)
            {
                leftArm!.localEulerAngles  = new(-90F + currentMovFact * 5F, 0F, 0F);
                rightArm!.localEulerAngles = new(-90F + currentMovFact * 5F, 0F, 0F);
            }

        }

    }
}
