#nullable enable
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CraftSharp.Control
{
    public class MeleeState : IPlayerState
    {
        public void UpdatePlayer(float interval, PlayerActions inputData, PlayerStatus info, Rigidbody rigidbody, PlayerController player)
        {
            info.Sprinting = false;
            info.Moving = inputData.Gameplay.Movement.IsPressed();

            info.Grounded = true; // Force grounded
            info.MoveVelocity = Vector3.zero; // Cancel move

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
                if (inputData.Gameplay.Movement.IsPressed()) // Start moving, exit attack state
                {
                    info.Attacking = false;
                    attackStatus.AttackStage = -1;
                }
            }
        }

        private void StartMeleeStage(PlayerStagedSkill meleeAttack, AttackStatus attackStatus, int stage, PlayerController player)
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
            player.CrossFadeState(PlayerAbility.SKILL, 0F);
            //player.TurnToAttackTarget();

            if (stageData.VisualFXPrefab != null)
            {
                var fxObj = GameObject.Instantiate(stageData.VisualFXPrefab);
                // Disable loop for all particle components
                foreach (var c in fxObj.GetComponentsInChildren<ParticleSystem>())
                {
                    // Main module? c.loop = false;
                }
                player.AttachVisualFX(fxObj!);
            }
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
            
            if (info.Spectating || info.InLiquid || !info.Grounded)
                return true;
            
            return false;
        }

        private Action<InputAction.CallbackContext>? chargedAttackCallback;
        private Action<InputAction.CallbackContext>? normalAttackCallback;

        public void OnEnter(PlayerStatus info, Rigidbody rigidbody, PlayerController player)
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

            rigidbody.velocity = Vector3.zero;
            info.MoveVelocity = Vector3.zero;

            // Register input action events
            player.Actions.Attack.ChargedAttack.performed += chargedAttackCallback = (context) =>
            {
                player.TryStartChargedAttack();
            };

            player.Actions.Attack.NormalAttack.performed += normalAttackCallback = (context) =>
            {
                if (attackStatus.AttackCooldown <= 0F && meleeAttack.StageCount > 0) // Attack available
                {
                    info.Attacking = true;
                    var nextStage = (attackStatus.AttackStage + 1) % meleeAttack.StageCount;

                    StartMeleeStage(meleeAttack, attackStatus, nextStage, player);
                }
            };
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

            player.ChangeItemState(PlayerController.CurrentItemState.Mount);
            player.UseRootMotion = false;

            // Unregister input action events
            player.Actions.Attack.ChargedAttack.performed -= chargedAttackCallback;
            player.Actions.Attack.NormalAttack.performed -= normalAttackCallback;
        }

        public override string ToString() => "Melee";
    }
}