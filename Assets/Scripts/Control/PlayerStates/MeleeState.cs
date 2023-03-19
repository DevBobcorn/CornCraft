#nullable enable
using UnityEngine;

namespace MinecraftClient.Control
{
    public class MeleeState : IPlayerState
    {
        public void UpdatePlayer(float interval, PlayerUserInputData inputData, PlayerStatus info, Rigidbody rigidbody, PlayerController player)
        {
            var meleeAttack = player.MeleeAttack;

            info.Sprinting = false;
            info.Moving = inputData.horInputNormalized != Vector2.zero;

            info.Grounded = true; // Force grounded
            info.MoveVelocity = Vector3.zero; // Cancel move

            var attackStatus = info.AttackStatus;

            attackStatus.AttackCooldown -= interval;

            if (attackStatus.AttackCooldown < meleeAttack.IdleTimeout)
            {
                // Attack timed out, exit
                info.Attacking = false;
                attackStatus.AttackStage = -1;
            }
            else if (attackStatus.AttackCooldown <= 0F) // Attack available
            {
                if (inputData.horInputNormalized != Vector2.zero) // Start moving, exit attack state
                {
                    info.Attacking = false;
                    attackStatus.AttackStage = -1;
                }
                else if (inputData.attack) // Enter next attack stage
                {
                    info.Attacking = true;
                    var nextStage = (attackStatus.AttackStage + 1) % meleeAttack.StageCount;

                    StartMeleeStage(meleeAttack, attackStatus, false, nextStage, player);
                    
                }
                
            }
            else // Cooldown not ready
            {
                
            }
            
        }

        private void StartMeleeStage(PlayerMeleeAttack meleeAttack, AttackStatus attackStatus, bool init, int stage, PlayerController player)
        {
            attackStatus.AttackStage = stage;
            attackStatus.AttackCooldown = meleeAttack.MaxStageDuration;

            player.CrossFadeState($"Melee{stage}", init ? 0F : 0.2F);
            player.TurnToAttackTarget();

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
            var meleeAttack = player.MeleeAttack;

            StartMeleeStage(meleeAttack, attackStatus, true, 0, player);

            //Debug.Log("Attack starts!");
            player.AccessoryWidget.HoldWeapon();
            player.UseRootMotion = true;

            rigidbody.velocity = Vector3.zero;
            info.MoveVelocity = Vector3.zero;
            
        }

        public void OnExit(PlayerStatus info, Rigidbody rigidbody, PlayerController player)
        {
            info.Attacking = false;

            var attackStatus = info.AttackStatus;

            attackStatus.AttackCooldown = 0F;

            //Debug.Log("Attack ends!");
            player.AccessoryWidget.MountWeapon();
            player.UseRootMotion = false;

        }

        public override string ToString() => "Melee";

    }
}