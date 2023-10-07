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

            // Whether movement should be slowed down by liquid, and whether the player can move around by swimming
            bool movementAffected = info.LiquidDist <= -0.4F;

            Vector3 moveVelocity = rigidbody.velocity;

            if (movementAffected) // In liquid
                moveVelocity.y = Mathf.Max(rigidbody.velocity.y, ability.MaxInLiquidFallSpeed);
            else // Still in air, free fall
                moveVelocity.y = rigidbody.velocity.y;

            // Apply new velocity to rigidbody
            info.MoveVelocity = moveVelocity;
            
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

        public override string ToString() => "Tread";

    }
}