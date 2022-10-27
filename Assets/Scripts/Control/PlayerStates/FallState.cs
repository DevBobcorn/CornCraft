#nullable enable
using UnityEngine;

namespace MinecraftClient.Control
{
    public class FallState : IPlayerState
    {
        public void UpdatePlayer(float interval, PlayerUserInputData inputData, PlayerStatus info, PlayerAbility ability, Rigidbody rigidbody)
        {
            // Check and constrain fall speed
            if (rigidbody.velocity.y < ability.MaxFallSpeed)
                rigidbody.velocity = new Vector3(rigidbody.velocity.x, ability.MaxFallSpeed, rigidbody.velocity.z);
            
            // Otherwise free fall, leave velocity unchanged
        }

        public bool ShouldEnter(PlayerStatus info)
        {
            if (!info.Grounded && !info.InWater)
                return true;
            return false;
        }

        public bool ShouldExit(PlayerStatus info)
        {
            if (info.Grounded || info.InWater)
                return true;
            return false;
        }

        public override string ToString() => "Fall";

    }
}