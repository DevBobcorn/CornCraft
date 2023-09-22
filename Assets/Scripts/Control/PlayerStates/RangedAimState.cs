#nullable enable
using CraftSharp.Event;
using UnityEngine;

namespace CraftSharp.Control
{
    public class RangedAimState : IPlayerState
    {
        public void UpdatePlayer(float interval, PlayerUserInputData inputData, PlayerStatus info, Rigidbody rigidbody, PlayerController player)
        {
            var ability = player.Ability;

            info.Sprinting = false;
            info.Gliding = false;

            var attackStatus = info.AttackStatus;
            var rangedAttack = attackStatus.CurrentRangedAttack;

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
                info.MoveVelocity = Vector3.zero;

                //info.TargetVisualYaw += rangedAttack.SetupYaw / rangedAttack.SetupTime * Time.deltaTime;

                attackStatus.StageTime += interval;
                // Reset cooldown
                attackStatus.AttackCooldown = 0F;
            }
            else if (inputData.AttackPressTime > 0F)
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

        public bool ShouldEnter(PlayerUserInputData inputData, PlayerStatus info)
        {
            // State only available via direct transition
            return false;
        }

        public bool ShouldExit(PlayerUserInputData inputData, PlayerStatus info)
        {
            if (!info.Attacking)
                return true;
            
            if (info.Spectating || info.InLiquid || !info.Grounded)
                return true;
            
            return false;
        }

        public void OnEnter(PlayerStatus info, Rigidbody rigidbody, PlayerController player)
        {
            info.Attacking = true;

            var attackStatus = info.AttackStatus;
            var rangedAttack = attackStatus.CurrentRangedAttack;

            if (rangedAttack == null) // Ranged attack data is not assigned, stop it
            {
                info.Attacking = false;
                attackStatus.AttackStage = -1;
                return;
            }

            attackStatus.AttackStage = 0;
            attackStatus.StageTime = 0F;

            player.OverrideState(rangedAttack.DummyAnimationClip!, rangedAttack.DrawWeapon!);
            player.CrossFadeState(PlayerAbility.ATTACK, 0.01F);

            player.ChangeItemState(PlayerController.CurrentItemState.HoldInOffhand);
            player.UseRootMotion = true;

            rigidbody.velocity = Vector3.zero;
            info.MoveVelocity = Vector3.zero;

            player.StartAiming();
        }

        public void OnExit(PlayerStatus info, Rigidbody rigidbody, PlayerController player)
        {
            info.Attacking = false;

            var attackStatus = info.AttackStatus;
            attackStatus.AttackCooldown = 0F;

            player.ChangeItemState(PlayerController.CurrentItemState.Mount);
            player.UseRootMotion = false;

            player.StopAiming();
        }

        public override string ToString() => "RangedAim";
    }
}