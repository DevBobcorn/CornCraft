#nullable enable
using UnityEngine;

namespace CraftSharp.Rendering
{
    public class ProjectileEntityRender : EntityRender
    {
        public override void UpdateTransform(float tickMilSec)
        {
            // Update position
            if ((Position - transform.position).sqrMagnitude > MOVE_THRESHOLD) // Treat as teleport
                transform.position = Position;
            else // Smoothly move to current position
                transform.position = Vector3.SmoothDamp(transform.position, Position, ref visualMovementVelocity, tickMilSec);

            // Update rotation
            if (lastYaw != Yaw || lastPitch != Pitch)
            {
                lastPitch = Pitch;
                lastYaw = Yaw;

                transform.localEulerAngles = new(-lastPitch, 180F - lastYaw);
            }
        }
    }
}