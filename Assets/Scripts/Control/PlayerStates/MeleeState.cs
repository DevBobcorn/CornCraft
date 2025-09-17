#nullable enable
using KinematicCharacterController;
using UnityEngine;

namespace CraftSharp.Control
{
    public class MeleeState : IPlayerState
    {
        private bool _nextAttackFlag = false;

        public void UpdateMain(ref Vector3 currentVelocity, float interval, PlayerActions inputData, PlayerStatus info, KinematicCharacterMotor motor, PlayerController player)
        {
            info.Sprinting = false;
            info.Moving = inputData.Locomotion.Movement.IsPressed();

            info.Grounded = true; // Force grounded
            currentVelocity = Vector3.zero; // Cancel move

            var attackStatus = info.AttackStatus;
            attackStatus.AttackCooldown -= interval;

            if (attackStatus.AttackCooldown < -0.5F)
            {
                // Attack timed out, exit
                info.Attacking = false;
            }
            else if (attackStatus.AttackCooldown <= 0F) // Attack available
            {
                if (inputData.Locomotion.Movement.IsPressed()) // Start moving, exit attack state
                {
                    info.Attacking = false;
                }

                if (_nextAttackFlag)
                {
                    _nextAttackFlag = false;
                    // Reset attack timers
                    attackStatus.AttackCooldown = 0.8F;
                }
            }
        }

        public void SetNextAttackFlag()
        {
            _nextAttackFlag = true;
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

        public void OnEnter(IPlayerState prevState, PlayerStatus info, KinematicCharacterMotor motor, PlayerController player)
        {
            info.Attacking = true;
            _nextAttackFlag = false;
        }

        public void OnExit(IPlayerState nextState, PlayerStatus info, KinematicCharacterMotor motor, PlayerController player)
        {
            info.Attacking = false;
            _nextAttackFlag = false;

            var attackStatus = info.AttackStatus;
            attackStatus.AttackCooldown = 0F;

            player.ChangeItemState(PlayerController.CurrentItemState.Mount);
        }

        public override string ToString() => "Melee";
    }
}