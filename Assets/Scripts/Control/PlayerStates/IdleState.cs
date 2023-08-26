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
            info.Gliding = false;
            info.Moving = false;

            if (inputData.Attack) // Attack available
            {
                player.TryStartAttack();
                
            }
            else if (inputData.JumpFlag) // Jump in place
            {
                // Preserve horizontal speed and change vertical speed
                var newVelocity = rigidbody.velocity;
                newVelocity.y = ability.JumpSpeedCurve.Evaluate(0F);
                info.Grounded = false;
                // Clear jump flag after jumping once to prevent playing
                // from bouncing on ground when holding jump key
                inputData.JumpFlag = false;

                rigidbody.velocity = newVelocity;
                //Debug.Log($"Jump [IDLE] velocity {rigidbody.velocity} ({Mathf.Sqrt(newVelocity.x * newVelocity.x + newVelocity.z * newVelocity.z)})");
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