#nullable enable
using UnityEngine;
using MinecraftClient.Mapping;

namespace MinecraftClient.Rendering
{
    public class LivingEntityRender : EntityRender
    {
        public Transform? head;
        protected bool headPresent = false;

        public override void Initialize(EntityType entityType, Entity entity)
        {
            base.Initialize(entityType, entity);

            headPresent = head is not null;
        }

        public override void UpdateAnimation(float tickMilSec)
        {
            if (headPresent)
                head!.localEulerAngles = new(lastPitch, lastHeadYaw - lastYaw);

        }

    }
}