#nullable enable
using UnityEngine;

namespace CraftSharp.Control
{
    public class IdleState : IPlayerState
    {
        public void UpdatePlayer(float interval, PlayerUserInputData inputData, PlayerStatus info, Rigidbody rigidbody, PlayerController player)
        {
            var ability = player.Ability;

            info.Sprinting = false;
            info.Moving = false;

            if (inputData.Attack) // Attack available
            {
                player.TryStartAttack();
                
            }
            else if (inputData.Ascend) // Jump in place
            {
                rigidbody.velocity = new(0F, ability.JumpSpeed, 0F);
                info.Grounded = false;
            }
            
            info.MoveVelocity = Vector3.zero;

            // Restore stamina
            info.StaminaLeft = Mathf.MoveTowards(info.StaminaLeft, ability.MaxStamina, interval * ability.StaminaRestore);
        }

        public bool ShouldEnter(PlayerUserInputData inputData, PlayerStatus info)
        {
            if (inputData.HorInputNormalized != Vector2.zero)
                return false;
            
            if (!info.Spectating && info.Grounded && !info.OnWall && !info.InLiquid)
                return true;
            
            return false;
        }

        public bool ShouldExit(PlayerUserInputData inputData, PlayerStatus info)
        {
            if (inputData.HorInputNormalized != Vector2.zero)
                return true;
            
            if (info.Spectating || !info.Grounded || info.OnWall || info.InLiquid)
                return true;
            
            return false;
        }

        public override string ToString() => "Idle";

    }
}