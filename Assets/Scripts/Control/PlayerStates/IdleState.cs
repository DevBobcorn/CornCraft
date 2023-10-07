#nullable enable
using UnityEngine;

namespace CraftSharp.Control
{
    public class IdleState : IPlayerState
    {
        public void UpdatePlayer(float interval, PlayerActions inputData, PlayerStatus info, Rigidbody rigidbody, PlayerController player)
        {
            var ability = player.Ability;

            info.Sprinting = false;
            info.Gliding = false;
            info.Moving = false;

            /*
            if (inputData.AttackReleased) // Attack initiated
            {
                if (inputData.AttackPressTime < PlayerUserInput.LONG_ATTACK_THRESHOLD)
                {
                    player.TryStartNormalAttack();
                    //Debug.Log($"Normal attack: press time {inputData.AttackPressTime}");
                }
            }
            else if (inputData.AttackPressTime >= PlayerUserInput.LONG_ATTACK_THRESHOLD)
            {
                player.TryStartChargedAttack();
                //Debug.Log($"Charged attack: press time {inputData.AttackPressTime}");
            }
            else 
            */
            if (inputData.Gameplay.Jump.WasPressedThisFrame()) // Jump in place
            {
                // Preserve horizontal speed and change vertical speed
                var newVelocity = rigidbody.velocity;
                newVelocity.y = ability.JumpSpeedCurve.Evaluate(0F);
                info.Grounded = false;
                // Clear jump flag after jumping once to prevent playing
                // from bouncing on ground when holding jump key
                //inputData.JumpFlag = false;

                rigidbody.velocity = newVelocity;
                //Debug.Log($"Jump [IDLE] velocity {rigidbody.velocity} ({Mathf.Sqrt(newVelocity.x * newVelocity.x + newVelocity.z * newVelocity.z)})");
            }
            
            info.MoveVelocity = Vector3.zero;

            // Restore stamina
            info.StaminaLeft = Mathf.MoveTowards(info.StaminaLeft, ability.MaxStamina, interval * ability.StaminaRestore);
        }

        public bool ShouldEnter(PlayerActions inputData, PlayerStatus info)
        {
            if (inputData.Gameplay.Movement.IsPressed())
                return false;
            
            if (!info.Spectating && info.Grounded && !info.OnWall && !info.InLiquid)
                return true;
            
            return false;
        }

        public bool ShouldExit(PlayerActions inputData, PlayerStatus info)
        {
            if (inputData.Gameplay.Movement.IsPressed())
                return true;
            
            if (info.Spectating || !info.Grounded || info.OnWall || info.InLiquid)
                return true;
            
            return false;
        }

        public override string ToString() => "Idle";
    }
}