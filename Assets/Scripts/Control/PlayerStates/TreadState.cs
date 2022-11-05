#nullable enable
using UnityEngine;

namespace MinecraftClient.Control
{
    public class TreadState : IPlayerState
    {
        public void UpdatePlayer(float interval, PlayerUserInputData inputData, PlayerStatus info, PlayerAbility ability, Rigidbody rigidbody)
        {
            info.Sprinting = false;
            
            if (inputData.horInputNormalized != Vector2.zero || inputData.ascend || inputData.descend) // Start moving
                info.Moving = true;
            else
            {
                info.Moving = false;

                Vector3 moveVelocity;

                bool onLiquidSurface = info.LiquidDist > PlayerStatusUpdater.SURFING_LIQUID_DIST_THERSHOLD;
                
                if (!onLiquidSurface)
                {
                    // Gradually reduce velocity to zero
                    moveVelocity = CoordConvert.ApproachOrigin(rigidbody.velocity, interval * 4F);

                    // Restore y velocity (to validate rigidbody gravity)
                    moveVelocity.y = Mathf.Max(ability.MaxWaterFallSpeed, rigidbody.velocity.y);
                }
                else // On water surface, preserve velocity
                {
                    moveVelocity = rigidbody.velocity;
                }

                // Clamp velocity magnitude
                if (moveVelocity.magnitude > ability.MaxWaterMoveSpeed)
                    moveVelocity *= (ability.MaxWaterMoveSpeed / moveVelocity.magnitude);
                
                rigidbody.velocity = moveVelocity;

                // Leave stamina value unchanged
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