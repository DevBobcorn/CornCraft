#nullable enable
using UnityEngine;

namespace CraftSharp.Control
{
    public class MeleeState : IPlayerState
    {
        public void UpdatePlayer(float interval, PlayerUserInputData inputData, PlayerStatus info, Rigidbody rigidbody, PlayerController player)
        {
            info.Sprinting = false;
            info.Moving = inputData.HorInputNormalized != Vector2.zero;

            info.Grounded = true; // Force grounded
            info.MoveVelocity = Vector3.zero; // Cancel move

            var attackStatus = info.AttackStatus;
            var meleeAttack = attackStatus.CurrentAttack;

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
                if (inputData.HorInputNormalized != Vector2.zero) // Start moving, exit attack state
                {
                    info.Attacking = false;
                    attackStatus.AttackStage = -1;
                }
                else if (inputData.Attack) // Enter next attack stage
                {
                    info.Attacking = true;
                    var nextStage = (attackStatus.AttackStage + 1) % meleeAttack.StageCount;

                    StartMeleeStage(meleeAttack, attackStatus, nextStage, player);
                }
            }
        }

        private void StartMeleeStage(PlayerMeleeAttack meleeAttack, AttackStatus attackStatus, int stage, PlayerController player)
        {
            attackStatus.AttackStage = stage;

            var stageData = meleeAttack.Stages[stage];
            // Reset attack timers
            attackStatus.AttackCooldown = stageData.Duration;
            attackStatus.StageTime = 0F;
            // Update stage damage time
            attackStatus.StageDamageStart = stageData.DamageStart;
            attackStatus.StageDamageEnd = stageData.DamageEnd;

            player.OverrideState(meleeAttack.DummyAnimationClip!, stageData.AnimationClip!);
            player.CrossFadeState("Melee", 0F);
            //player.TurnToAttackTarget();
        }

        public bool ShouldEnter(PlayerUserInputData inputData, PlayerStatus info)
        {
            if (!info.Attacking)
                return false;
            
            if (info.Spectating || info.InLiquid)
                return false;
            
            return false;
        }

        public bool ShouldExit(PlayerUserInputData inputData, PlayerStatus info)
        {
            if (!info.Attacking)
                return true;
            
            if (info.Spectating || info.InLiquid)
                return true;
            
            return false;
        }

        public void OnEnter(PlayerStatus info, Rigidbody rigidbody, PlayerController player)
        {
            info.Attacking = true;

            var attackStatus = info.AttackStatus;
            var meleeAttack = attackStatus.CurrentAttack;

            if (meleeAttack == null) // Melee attack data is not assigned, stop it
            {
                info.Attacking = false;
                attackStatus.AttackStage = -1;
                return;
            }

            StartMeleeStage(meleeAttack, attackStatus, 0, player);

            player.ChangeItemState(PlayerController.CurrentItemState.Hold);
            player.UseRootMotion = true;

            rigidbody.velocity = Vector3.zero;
            info.MoveVelocity = Vector3.zero;
        }

        public void OnExit(PlayerStatus info, Rigidbody rigidbody, PlayerController player)
        {
            info.Attacking = false;

            var attackStatus = info.AttackStatus;

            attackStatus.AttackCooldown = 0F;

            if (attackStatus.CausingDamage) // Interrupted while dealing damage
            {
                attackStatus.CausingDamage = false;
                player.MeleeDamageEnd();
            }

            //Debug.Log("Attack ends!");
            player.ChangeItemState(PlayerController.CurrentItemState.Mount);
            player.UseRootMotion = false;
        }

        public override string ToString() => "Melee";
    }
}