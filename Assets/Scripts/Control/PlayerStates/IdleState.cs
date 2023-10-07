#nullable enable
using System;
using UnityEngine;
using UnityEngine.InputSystem;

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

        private Action<InputAction.CallbackContext>? chargedAttackCallback;
        private Action<InputAction.CallbackContext>? normalAttackCallback;

        public void OnEnter(PlayerStatus info, Rigidbody rigidbody, PlayerController player)
        {
            info.Sprinting = false;

            // Register input action events
            player.Actions.Attack.ChargedAttack.performed += chargedAttackCallback = (context) =>
            {
                player.TryStartChargedAttack();
            };

            player.Actions.Attack.NormalAttack.performed += normalAttackCallback = (context) =>
            {
                player.TryStartNormalAttack();
            };
        }

        public void OnExit(PlayerStatus info, Rigidbody rigidbody, PlayerController player)
        {
            info.Sprinting = false;

            // Unregister input action events
            player.Actions.Attack.ChargedAttack.performed -= chargedAttackCallback;
            player.Actions.Attack.NormalAttack.performed -= normalAttackCallback;
        }

        public override string ToString() => "Idle";
    }
}