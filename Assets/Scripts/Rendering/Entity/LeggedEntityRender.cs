#nullable enable
using UnityEngine;

namespace MinecraftClient.Rendering
{
    public class LeggedEntityRender : EntityRender
    {
        public float cycleTime = 1F;
        public float legAngle = 45F;
        
        protected float currentLegAngle = 0F, currentMovFact = 0F;

        protected bool legsPresent = false;

        protected void UpdateLegAngle()
        {
            var movFact = Mathf.Clamp01(currentVelocity.magnitude);

            if (currentMovFact != movFact)
                currentMovFact = Mathf.MoveTowards(currentMovFact, movFact, Time.deltaTime);

            var movTime = (Time.realtimeSinceStartup % cycleTime) / cycleTime;

            // Update leg rotation
            if (movTime <= cycleTime * 0.25F) // 0 ~ 0.25
                currentLegAngle =  legAngle * (movTime / cycleTime) * 4F;
            else if ((1F - movTime) <= cycleTime * 0.25F) // 0.75 ~ 1
                currentLegAngle = -legAngle * (1F - movTime / cycleTime) * 4F;
            else // 0.25 ~ 0.75
                currentLegAngle = -legAngle * (movTime / cycleTime - 0.5F) * 4F;
        }

    }
}
