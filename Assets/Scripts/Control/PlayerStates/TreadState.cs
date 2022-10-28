#nullable enable
using UnityEngine;

namespace MinecraftClient.Control
{
    public class TreadState : IPlayerState
    {
        public void UpdatePlayer(float interval, PlayerUserInputData inputData, PlayerStatus info, PlayerAbility ability, Rigidbody rigidbody)
        {
            if (inputData.horInputNormalized != Vector2.zero || inputData.ascend || inputData.descend) // Start moving
                info.Moving = true;
            else
            {
                info.Moving = false;

                Vector3 moveVelocity;

                if (!info.Grounded) // Floating in water
                {
                    // Gradually reduce velocity to zero
                    moveVelocity = CoordConvert.ApproachOrigin(rigidbody.velocity, interval * 4F);
                    // Restore y velocity (to validate rigidbody gravity)
                    moveVelocity.y = Mathf.Max(ability.MaxWaterFallSpeed, rigidbody.velocity.y);
                }
                else  // Idle in water
                    moveVelocity = Vector3.zero;

                // Clamp velocity magnitude
                if (moveVelocity.magnitude > ability.MaxWaterMoveSpeed)
                    moveVelocity *= (ability.MaxWaterMoveSpeed / moveVelocity.magnitude);
                
                rigidbody.velocity = moveVelocity;
            }
            
        }

        public bool ShouldEnter(PlayerStatus info)
        {
            if (!info.Spectating && info.InWater && !info.Moving)
                return true;
            return false;
        }

        public bool ShouldExit(PlayerStatus info)
        {
            if (info.Spectating)
                return true;
            
            if (!info.InWater || info.Moving)
                return true;
            return false;
        }

        public override string ToString() => "Tread";

    }
}