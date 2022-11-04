#nullable enable
using UnityEngine;

namespace MinecraftClient.Control
{
    public class FallState : IPlayerState
    {
        public void UpdatePlayer(float interval, PlayerUserInputData inputData, PlayerStatus info, PlayerAbility ability, Rigidbody rigidbody)
        {
            var moveSpeed = rigidbody.velocity;

            // Check and constrain fall speed
            if (moveSpeed.y < ability.MaxFallSpeed)
                moveSpeed = new(moveSpeed.x, ability.MaxFallSpeed, moveSpeed.z);
            
            // Auto walk up aid
            if (inputData.horInputNormalized != Vector2.zero && Mathf.Abs(info.YawOffset) < 60F) // Trying to moving forward
            {
                if (info.FrontDownDist < -0.03F && info.FrontDownDist > -0.6F)
                    moveSpeed.y = ability.AidSpeedCurve.Evaluate(info.FrontDownDist);
            }

            // Otherwise free fall, leave velocity unchanged

            // Leave stamina value unchanged
        }

        public bool ShouldEnter(PlayerStatus info)
        {
            if (!info.Spectating && !info.Grounded && !info.OnWall && !info.InWater)
                return true;
            return false;
        }

        public bool ShouldExit(PlayerStatus info)
        {
            if (info.Spectating)
                return true;
            
            if (info.Grounded || info.OnWall || info.InWater)
                return true;
            return false;
        }

        public override string ToString() => "Fall";

    }
}