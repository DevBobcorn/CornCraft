#nullable enable
using UnityEngine;

namespace CraftSharp.Control
{
    public class TreadState : IPlayerState
    {
        public void UpdatePlayer(float interval, PlayerUserInputData inputData, PlayerStatus info, Rigidbody rigidbody, PlayerController player)
        {
            var ability = player.Ability;
            
            info.Sprinting = false;
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

        public bool ShouldEnter(PlayerUserInputData inputData, PlayerStatus info)
        {
            if (inputData.horInputNormalized != Vector2.zero || inputData.ascend || inputData.descend)
                return false; 
            
            if (!info.Spectating && info.InLiquid)
                return true;
            
            return false;
        }

        public bool ShouldExit(PlayerUserInputData inputData, PlayerStatus info)
        {
            if (inputData.horInputNormalized != Vector2.zero || inputData.ascend || inputData.descend)
                return true; 
            
            if (info.Spectating || !info.InLiquid)
                return true;
            
            return false;
        }

        public override string ToString() => "Tread";

    }
}