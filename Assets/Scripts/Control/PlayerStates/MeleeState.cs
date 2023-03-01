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
            info.Moving = false;

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
                    attackStatus.AttackStage = (attackStatus.AttackStage + 1) % meleeAttack.StageCount;

                    attackStatus.AttackCooldown =
                            meleeAttack.StageDurations[attackStatus.AttackStage];
                    
                }
                
            }
            else // Cooldown not ready
            {
                
            }
            
        }

        public bool ShouldEnter(PlayerUserInputData inputData, PlayerStatus info)
        {
            if (!info.Attacking)
                return false;
            
            if (info.Spectating || !info.Grounded || info.OnWall || info.InLiquid)
                return false;
            
            return false;
        }

        public bool ShouldExit(PlayerUserInputData inputData, PlayerStatus info)
        {
            if (!info.Attacking)
                return true;
            
            if (info.Spectating || !info.Grounded || info.OnWall || info.InLiquid)
                return true;
            
            return false;
        }

        public void OnEnter(PlayerStatus info, Rigidbody rigidbody, PlayerController player)
        {
            info.Attacking = true;

            var attackStatus = info.AttackStatus;
            var meleeAttack = player.MeleeAttack;

            attackStatus.AttackStage = 0;

            attackStatus.AttackCooldown =
                    meleeAttack.StageDurations[attackStatus.AttackStage];

            //Debug.Log("Attack starts!");
            
        }

        public void OnExit(PlayerStatus info, Rigidbody rigidbody, PlayerController player)
        {
            info.Attacking = false;

            //Debug.Log("Attack ends!");

        }

        public override string ToString() => "Melee";

    }
}