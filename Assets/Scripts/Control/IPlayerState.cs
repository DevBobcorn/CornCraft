#nullable enable
using UnityEngine;

namespace MinecraftClient.Control
{
    public interface IPlayerState
    {
        public void UpdatePlayer(float interval, PlayerUserInputData inputData, PlayerStatus info, PlayerAbility ability, Rigidbody rigidbody);

        public abstract bool ShouldEnter(PlayerStatus info);

        public abstract bool ShouldExit(PlayerStatus info);

    }
}