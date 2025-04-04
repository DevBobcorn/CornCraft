#nullable enable
using KinematicCharacterController;
using UnityEngine;

namespace CraftSharp.Control
{
    public class DiggingAimState : IPlayerState
    {
        public void UpdateMain(ref Vector3 currentVelocity, float interval, PlayerActions inputData, PlayerStatus info, KinematicCharacterMotor motor, PlayerController player)
        {
            var ability = player.AbilityConfig;

            info.Sprinting = false;
            info.Gliding = false;

            var attackStatus = info.AttackStatus;

            // Stay in this state if attack button is still pressed or the initiation phase is not yet complete
            if (inputData.Interaction.ChargedAttack.IsPressed())
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
                var moveSpeed = ability.WalkSpeed;
                Vector3 moveVelocity;

                // Use target orientation to calculate actual movement direction, taking ground shape into consideration
                if (info.Moving)
                {
                    moveVelocity = motor.GetDirectionTangentToSurface(player.GetMovementOrientation() * Vector3.forward, motor.GroundingStatus.GroundNormal) * moveSpeed;
                }
                else // Idle
                {
                    moveVelocity = Vector3.zero;
                }

                // Apply updated velocity
                currentVelocity = moveVelocity;

                attackStatus.StageTime += interval;
                // Reset cooldown
                attackStatus.AttackCooldown = 0F;
            }
            else // Digging state ends
            {
                // Idle timeout
                attackStatus.AttackCooldown -= interval;

                info.Attacking = false;
                attackStatus.AttackStage = -1;
            }

            if (!motor.GroundingStatus.FoundAnyGround)
            {
                // Apply fake gravity
                currentVelocity = - motor.CharacterUp * 5F;
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
            return !info.Attacking || info.Spectating;
        }

        public void OnEnter(IPlayerState prevState, PlayerStatus info, KinematicCharacterMotor motor, PlayerController player)
        {
            // Digging also use this attacking flag
            info.Attacking = true;

            var attackStatus = info.AttackStatus;

            attackStatus.AttackStage = 0;
            attackStatus.StageTime = 0F;

            player.ChangeItemState(PlayerController.CurrentItemState.HoldInOffhand);

            player.UseAimingCamera(true);
        }

        public void OnExit(IPlayerState nextState, PlayerStatus info, KinematicCharacterMotor motor, PlayerController player)
        {
            // Digging also use this attacking flag
            info.Attacking = false;

            var attackStatus = info.AttackStatus;
            attackStatus.AttackCooldown = 0F;

            player.ChangeItemState(PlayerController.CurrentItemState.Mount);

            player.UseAimingCamera(false);
        }

        public override string ToString() => "DiggingAim";
    }
}