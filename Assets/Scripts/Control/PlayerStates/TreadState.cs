#nullable enable
using UnityEngine;

namespace CraftSharp.Control
{
    public class TreadState : IPlayerState
    {
        public void UpdatePlayer(float interval, PlayerActions inputData, PlayerStatus info, Rigidbody rigidbody, PlayerController player)
        {
            var ability = player.Ability;
            
            info.Sprinting = false;
            info.Gliding = false;
            info.Moving = false;

            // Whether gravity should be reduced by liquid, and whether the player can move around by swimming
            bool soaked = info.LiquidDist <= SwimState.SOAKED_LIQUID_DIST_THERSHOLD;

            if (soaked) // Use no gravity
            {
                info.GravityScale = 0F;
            }
            else // Still in air, use reduced gravity
            {
                info.GravityScale = SwimState.SURFING_GRAVITY_SCALE;
            }

            // Apply new velocity to rigidbody
            info.MoveVelocity = rigidbody.velocity * ability.LiquidMoveMultiplier;
            
            if (info.Grounded) // Restore stamina
                info.StaminaLeft = Mathf.MoveTowards(info.StaminaLeft, ability.MaxStamina, interval * ability.StaminaRestore);
        }

        public bool ShouldEnter(PlayerActions inputData, PlayerStatus info)
        {
            if (inputData.Gameplay.Movement.IsPressed() ||
                    inputData.Gameplay.Ascend.IsPressed() ||
                    inputData.Gameplay.Descend.IsPressed())
                return false;
            
            if (!info.Spectating && info.InLiquid)
                return true;
            
            return false;
        }

        public bool ShouldExit(PlayerActions inputData, PlayerStatus info)
        {
            if (inputData.Gameplay.Movement.IsPressed() ||
                    inputData.Gameplay.Ascend.IsPressed() ||
                    inputData.Gameplay.Descend.IsPressed())
                return true;
            
            if (info.Spectating || !info.InLiquid)
                return true;
            
            return false;
        }

        public void OnEnter(PlayerStatus info, Rigidbody rigidbody, PlayerController player)
        {
            info.GravityScale = SwimState.SURFING_GRAVITY_SCALE;
        }

        public void OnExit(PlayerStatus info, Rigidbody rigidbody, PlayerController player)
        {
            // Restore gravity scale
            info.GravityScale = 1F;
        }

        public override string ToString() => "Tread";

    }
}