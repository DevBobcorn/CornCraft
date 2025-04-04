#nullable enable
using KinematicCharacterController;
using UnityEngine;

namespace CraftSharp.Control
{
    public interface IPlayerState
    {
        /// <summary>
        /// Update before motor calculation. e.g. Force unground for jumping
        /// </summary>
        public void UpdateBeforeMotor(float interval, PlayerActions inputData,
                PlayerStatus info, KinematicCharacterMotor motor, PlayerController player) { }

        /// <summary>
        /// Update velocity for Kinematic Character Controller motor, as well as perform other state logic
        /// </summary>
        public void UpdateMain(ref Vector3 currentVelocity, float interval,
                PlayerActions inputData, PlayerStatus info, KinematicCharacterMotor motor, PlayerController player);
        
        public bool IgnoreCollision()
        {
            return false;
        }

        public bool ShouldEnter(PlayerActions inputData, PlayerStatus info);

        public bool ShouldExit(PlayerActions inputData, PlayerStatus info);

        public void OnEnter(IPlayerState prevState, PlayerStatus info, KinematicCharacterMotor motor, PlayerController player) { }

        public void OnExit(IPlayerState nextState, PlayerStatus info, KinematicCharacterMotor motor, PlayerController player) { }
    }
}