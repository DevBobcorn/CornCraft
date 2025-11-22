#nullable enable
using UnityEngine;

namespace CraftSharp.Control
{
    public interface IPlayerState
    {
        /// <summary>
        /// Update velocity for Kinematic Character Controller motor, as well as perform other state logic
        /// </summary>
        public void UpdateMain(ref Vector3 currentVelocity, float interval,
                PlayerActions inputData, PlayerStatus info, PlayerController player);

        public bool ShouldEnter(PlayerActions inputData, PlayerStatus info);

        public bool ShouldExit(PlayerActions inputData, PlayerStatus info);

        public void OnEnter(IPlayerState prevState, PlayerStatus info, PlayerController player) { }

        public void OnExit(IPlayerState nextState, PlayerStatus info, PlayerController player) { }
    }
}