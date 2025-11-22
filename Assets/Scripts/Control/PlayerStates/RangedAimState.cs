#nullable enable
using UnityEngine;

namespace CraftSharp.Control
{
    public class RangedAimState : IPlayerState
    {
        public void UpdateMain(ref Vector3 currentVelocity, float interval, PlayerActions inputData, PlayerStatus info, PlayerController player)
        {
            var ability = player.AbilityConfig;

            info.Sprinting = false;
            info.Gliding = false;

            var attackStatus = info.AttackStatus;

            // Stay in this state if attack button is still pressed or the initiation phase is not yet complete
            if (attackStatus.AttackCooldown > 0F || inputData.Interaction.ChargedAttack.IsPressed())
            {
                // Update moving status
                bool prevMoving = info.Moving;
                info.Moving = inputData.Locomotion.Movement.IsPressed();

                // Animation mirror randomization
                if (info.Moving != prevMoving)
                {
                    player.RandomizeMirroredFlag();
                }

                // Player can only move slowly in this state
                var moveSpeed = ability.SneakSpeed;
                Vector3 moveVelocity;

                // Use target orientation to calculate actual movement direction, taking ground shape into consideration
                if (info.Moving)
                {
                    moveVelocity = player.GetMovementOrientation() * Vector3.forward * moveSpeed;
                }
                else // Idle
                {
                    moveVelocity = Vector3.zero;
                }

                // Apply updated velocity
                currentVelocity = moveVelocity;

                // Reset cooldown
                attackStatus.AttackCooldown = 0F;
            }
            else // Charging state ends
            {
                info.Attacking = false;
            }

            // Restore stamina
            info.StaminaLeft = Mathf.MoveTowards(info.StaminaLeft, ability.MaxStamina, interval * ability.StaminaRestore);
        }

        public bool ShouldEnter(PlayerActions inputData, PlayerStatus info)
        {
            // State only available via direct transition
            return false;
        }

        public bool ShouldExit(PlayerActions inputData, PlayerStatus info)
        {
            if (!info.Attacking)
                return true;
            
            return info.Spectating || info.Floating || !info.Grounded;
        }

        public void OnEnter(IPlayerState prevState, PlayerStatus info, PlayerController player)
        {
            info.Attacking = true;

            var attackStatus = info.AttackStatus;
            attackStatus.AttackCooldown = 0F;

            player.ChangeItemState(PlayerController.CurrentItemState.HoldInOffhand);

            player.UseAimingCamera(true);
        }

        public void OnExit(IPlayerState nextState, PlayerStatus info, PlayerController player)
        {
            info.Attacking = false;

            var attackStatus = info.AttackStatus;
            attackStatus.AttackCooldown = 0F;

            player.ChangeItemState(PlayerController.CurrentItemState.Mount);

            player.UseAimingCamera(false);
        }

        public override string ToString() => "RangedAim";
    }
}