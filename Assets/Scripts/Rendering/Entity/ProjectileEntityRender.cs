#nullable enable
using UnityEngine;

namespace MinecraftClient.Rendering
{
    public class ProjectileEntityRender : EntityRender
    {
        public override void UpdateTransform(float tickMilSec)
        {
            // Update position
            if (lastPosition is not null && targetPosition is not null)
            {
                if ((targetPosition.Value - transform.position).sqrMagnitude > MOVE_THRESHOLD) // Treat as teleport
                    transform.position = targetPosition.Value;
                else // Smoothly move to current position
                    transform.position = Vector3.SmoothDamp(transform.position, targetPosition.Value, ref visualMovementVelocity, tickMilSec);

            }

            // Update rotation
            if (lastYaw != targetYaw || lastPitch != targetPitch)
            {
                lastPitch = targetPitch;
                lastYaw = targetYaw;

                transform.localEulerAngles = new(-lastPitch, 180F - lastYaw);
            }

        }
    }
}