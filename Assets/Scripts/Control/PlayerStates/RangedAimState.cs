#nullable enable
using KinematicCharacterController;
using UnityEngine;

namespace CraftSharp.Control
{
    public class RangedAimState : IPlayerState
    {
        public void UpdateMain(ref Vector3 currentVelocity, float interval, PlayerActions inputData, PlayerStatus info, KinematicCharacterMotor motor, PlayerController player)
        {
            var ability = player.Ability;

            info.Sprinting = false;
            info.Gliding = false;

            var attackStatus = info.AttackStatus;
            var rangedAttack = attackStatus.CurrentChargedAttack;

            if (rangedAttack == null) // Ranged attack data is not assigned, stop it
            {
                info.Attacking = false;
                attackStatus.AttackStage = -1;
                return;
            }

            // Exit if attack button is released AND we have finished setup state
            if (attackStatus.StageTime < rangedAttack.SetupTime)
            {
                info.Moving = false;
                currentVelocity = Vector3.zero;

                //info.TargetVisualYaw += rangedAttack.SetupYaw / rangedAttack.SetupTime * Time.deltaTime;

                attackStatus.StageTime += interval;
                // Reset cooldown
                attackStatus.AttackCooldown = 0F;
            }
            else if (inputData.Attack.ChargedAttack.IsPressed())
            {
                attackStatus.StageTime += interval;
                // Reset cooldown
                attackStatus.AttackCooldown = 0F;
            }
            else // Charging state ends
            {
                // Idle timeout
                attackStatus.AttackCooldown -= interval;

                if (attackStatus.AttackCooldown < rangedAttack.IdleTimeout) // Timed out, exit state
                {
                    // Attack timed out, exit
                    info.Attacking = false;
                    attackStatus.AttackStage = -1;
                }
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
            
            if (info.Spectating || info.Floating || !info.Grounded)
                return true;
            
            return false;
        }

        public void OnEnter(IPlayerState prevState, PlayerStatus info, KinematicCharacterMotor motor, PlayerController player)
        {
            info.Attacking = true;

            var attackStatus = info.AttackStatus;
            var rangedAttack = attackStatus.CurrentChargedAttack;

            if (rangedAttack == null) // Ranged attack data is not assigned, stop it
            {
                info.Attacking = false;
                attackStatus.AttackStage = -1;
                return;
            }

            attackStatus.AttackStage = 0;
            attackStatus.StageTime = 0F;

            player.OverrideStateAnimation(rangedAttack.DummyAnimationClip!, rangedAttack.DrawWeapon!);
            player.StartCrossFadeState(PlayerAbility.SKILL, 0.01F);

            player.ChangeItemState(PlayerController.CurrentItemState.HoldInOffhand);

            player.StartAiming();
        }

        public void OnExit(IPlayerState nextState, PlayerStatus info, KinematicCharacterMotor motor, PlayerController player)
        {
            info.Attacking = false;

            var attackStatus = info.AttackStatus;
            attackStatus.AttackCooldown = 0F;

            player.ChangeItemState(PlayerController.CurrentItemState.Mount);

            player.StopAiming();
        }

        public override string ToString() => "RangedAim";
    }
}