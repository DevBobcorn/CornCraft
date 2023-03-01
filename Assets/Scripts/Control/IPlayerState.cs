#nullable enable
using UnityEngine;

namespace MinecraftClient.Control
{
    public interface IPlayerState
    {
        public void UpdatePlayer(float interval, PlayerUserInputData inputData, PlayerStatus info, Rigidbody rigidbody, PlayerController player);

        public abstract bool ShouldEnter(PlayerUserInputData inputData, PlayerStatus info);

        public abstract bool ShouldExit(PlayerUserInputData inputData, PlayerStatus info);

        public virtual void OnEnter(PlayerStatus info, Rigidbody rigidbody, PlayerController player) { }

        public virtual void OnExit(PlayerStatus info, Rigidbody rigidbody, PlayerController player) { }
    }
}