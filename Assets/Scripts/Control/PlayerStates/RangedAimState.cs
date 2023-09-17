#nullable enable
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
            info.Moving = false;

            if (inputData.AttackPressTime > 0F)
            {

            }
            else
            {
                info.Attacking = false;
            }

            info.MoveVelocity = Vector3.zero;

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

            if (rangedAttack == null) // Melee attack data is not assigned, stop it
            {
                info.Attacking = false;
                attackStatus.AttackStage = -1;
                return;
            }

            player.ChangeItemState(PlayerController.CurrentItemState.Hold);
            player.UseRootMotion = true;

            rigidbody.velocity = Vector3.zero;
            info.MoveVelocity = Vector3.zero;
        }

        public void OnExit(PlayerStatus info, Rigidbody rigidbody, PlayerController player)
        {
            info.Attacking = false;

            var attackStatus = info.AttackStatus;

            player.ChangeItemState(PlayerController.CurrentItemState.Mount);
            player.UseRootMotion = false;
        }
        public override string ToString() => "RangedAim";
    }
}