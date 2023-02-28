#nullable enable
using UnityEngine;

namespace MinecraftClient.Control
{
    public class MeleeState : IPlayerState
    {
        public void UpdatePlayer(float interval, PlayerUserInputData inputData, PlayerStatus info, PlayerAbility ability, Rigidbody rigidbody, PlayerController player)
        {
            info.Sprinting = false;

            
        }

        public bool ShouldEnter(PlayerUserInputData inputData, PlayerStatus info)
        {
            return false;
        }

        public bool ShouldExit(PlayerUserInputData inputData, PlayerStatus info)
        {
            return false;
        }

        public override string ToString() => "Melee";

    }
}