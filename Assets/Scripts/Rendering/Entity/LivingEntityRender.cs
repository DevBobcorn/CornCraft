#nullable enable
using UnityEngine;

namespace MinecraftClient.Rendering
{
    public class LivingEntityRender : EntityRender
    {
        public Transform? head;
        protected bool headPresent = false;

        protected override void Initialize()
        {
            base.Initialize();

            headPresent = head is not null;
        }

        public override void UpdateAnimation(float tickMilSec)
        {
            if (headPresent)
                head!.localEulerAngles = new(lastPitch, lastHeadYaw - lastYaw);

        }

    }
}