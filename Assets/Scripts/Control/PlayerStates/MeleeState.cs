#nullable enable
using KinematicCharacterController;
using UnityEngine;

namespace CraftSharp.Control
{
    public class MeleeState : IPlayerState
    {
        public void UpdateMain(ref Vector3 currentVelocity, float interval, PlayerActions inputData, PlayerStatus info, KinematicCharacterMotor motor, PlayerController player)
        {
            info.Sprinting = false;
            info.Moving = inputData.Locomotion.Movement.IsPressed();

            info.Grounded = true; // Force grounded
            currentVelocity = Vector3.zero; // Cancel move

            var attackStatus = info.AttackStatus;
            var meleeAttack = attackStatus.CurrentStagedAttack;

            if (meleeAttack == null) // Melee attack data is not assigned, stop it
            {
                info.Attacking = false;
                attackStatus.AttackStage = -1;
                return;
            }

            attackStatus.AttackCooldown -= interval;
            attackStatus.StageTime += interval;

            if (attackStatus.CausingDamage)
            {
                if (attackStatus.StageTime >= attackStatus.StageDamageEnd)
                {
                    attackStatus.CausingDamage = false;
                    player.MeleeDamageEnd();
                }
            }
            else
            {
                if (attackStatus.StageTime >= attackStatus.StageDamageStart
                        && attackStatus.StageTime < attackStatus.StageDamageEnd)
                {
                    attackStatus.CausingDamage = true;
                    player.MeleeDamageStart();
                }
            }

            if (attackStatus.AttackCooldown < meleeAttack.IdleTimeout)
            {
                // Attack timed out, exit
                info.Attacking = false;
                attackStatus.AttackStage = -1;
            }
            else if (attackStatus.AttackCooldown <= 0F && meleeAttack.StageCount > 0) // Attack available
            {
                if (inputData.Locomotion.Movement.IsPressed()) // Start moving, exit attack state
                {
                    info.Attacking = false;
                    attackStatus.AttackStage = -1;
                }
            }
        }

        public void StartMeleeStage(PlayerStagedSkill meleeAttack, AttackStatus attackStatus, int stage, PlayerController player)
        {
            attackStatus.AttackStage = stage;

            var stageData = meleeAttack.Stages[stage];
            // Reset attack timers
            attackStatus.AttackCooldown = stageData.Duration;
            attackStatus.StageTime = 0F;
            // Update stage damage time
            attackStatus.StageDamageStart = stageData.DamageStart;
            attackStatus.StageDamageEnd = stageData.DamageEnd;

            player.OverrideStateAnimation(meleeAttack.DummyAnimationClip!, stageData.AnimationClip!);
            player.StartCrossFadeState(PlayerAbilityConfig.SKILL, 0F);
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
            var meleeAttack = attackStatus.CurrentStagedAttack;

            if (meleeAttack == null) // Melee attack data is not assigned, stop it
            {
                info.Attacking = false;
                attackStatus.AttackStage = -1;
                return;
            }

            StartMeleeStage(meleeAttack, attackStatus, 0, player);

            player.ChangeItemState(PlayerController.CurrentItemState.HoldInMainHand);
            player.UseRootMotion = true;
            player.IgnoreAnimatorScale = false;
        }

        public void OnExit(IPlayerState nextState, PlayerStatus info, KinematicCharacterMotor motor, PlayerController player)
        {
            info.Attacking = false;

            var attackStatus = info.AttackStatus;
            attackStatus.AttackCooldown = 0F;

            if (attackStatus.CausingDamage) // Interrupted while dealing damage
            {
                attackStatus.CausingDamage = false;
                player.MeleeDamageEnd();
            }

            player.ChangeItemState(PlayerController.CurrentItemState.Mount);
            player.UseRootMotion = false;
        }

        public override string ToString() => "Melee";
    }
}