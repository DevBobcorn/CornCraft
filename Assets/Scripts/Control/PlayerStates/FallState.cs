#nullable enable
using UnityEngine;

namespace MinecraftClient.Control
{
    public class FallState : IPlayerState
    {
        public void UpdatePlayer(float interval, PlayerUserInputData inputData, PlayerStatus info, PlayerAbility ability, Rigidbody rigidbody)
        {
            info.Sprinting = false;
            
            var moveSpeed = rigidbody.velocity;

            // Check and constrain fall speed
            if (moveSpeed.y < ability.MaxFallSpeed)
                moveSpeed = new(moveSpeed.x, ability.MaxFallSpeed, moveSpeed.z);
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