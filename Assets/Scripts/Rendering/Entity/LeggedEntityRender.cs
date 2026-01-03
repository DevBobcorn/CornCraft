using UnityEngine;

namespace CraftSharp.Rendering
{
    public class LeggedEntityRender : LivingEntityRender
    {
        public float cycleTime = 1F;
        public float legAngle = 45F;
        
        protected float currentLegAngle = 0F, currentMovFract = 0F;

        protected void UpdateLegAngle()
        {
            var movFract = Mathf.Clamp01(_visualMovementVelocity.x * _visualMovementVelocity.x + _visualMovementVelocity.z * _visualMovementVelocity.z);
            var cycleMultiplier = Pose.Value == EntityPose.Sneaking ? 3F : 1F;

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (currentMovFract != movFract)
                currentMovFract = Mathf.MoveTowards(currentMovFract, movFract, Time.deltaTime * 3F);

            // Make sure every mob is moving with a different offset
            var refTime = Time.realtimeSinceStartup + _pseudoRandomOffset * cycleTime * cycleMultiplier;

            int fullCnt = (int)(refTime / (cycleTime * cycleMultiplier));
            var movTime = (refTime - fullCnt * (cycleTime * cycleMultiplier)) / (cycleTime * cycleMultiplier);

            // Update leg rotation
            if (movTime <= 0.25F) // 0 ~ 0.25
                currentLegAngle =  (legAngle / cycleMultiplier) * movTime * 4F;
            else if (movTime >= 0.75F) // 0.75 ~ 1
                currentLegAngle = -(legAngle / cycleMultiplier) * (1F - movTime) * 4F;
            else // 0.25 ~ 0.75
                currentLegAngle = -(legAngle / cycleMultiplier) * (movTime - 0.5F) * 4F;
        }
    }
}
