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
            var movFact = Mathf.Clamp01(currentVelocity.x * currentVelocity.x + currentVelocity.z * currentVelocity.z);

            if (currentMovFact != movFact)
                currentMovFact = Mathf.MoveTowards(currentMovFact, movFact, Time.deltaTime * 3F);

            // Make sure every mob is moving with a different offset
            var refTime = Time.realtimeSinceStartup + pseudoRandomOffset * cycleTime;

            int fullCnt = (int)(refTime / cycleTime);
            var movTime = (refTime - fullCnt * cycleTime) / cycleTime;

            // Update leg rotation
            if (movTime <= 0.25F) // 0 ~ 0.25
                currentLegAngle =  legAngle * movTime * 4F;
            else if (movTime >= 0.75F) // 0.75 ~ 1
                currentLegAngle = -legAngle * (1F - movTime) * 4F;
            else // 0.25 ~ 0.75
                currentLegAngle = -legAngle * (movTime - 0.5F) * 4F;
            
            //nameText!.text = ((int)currentLegAngle).ToString();

        }

    }
}
