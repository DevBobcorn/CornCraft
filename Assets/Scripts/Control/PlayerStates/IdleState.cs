#nullable enable
using UnityEngine;

namespace MinecraftClient.Control
{
    public class IdleState : IPlayerState
    {
        public void UpdatePlayer(float interval, PlayerUserInputData inputData, PlayerStatus info, PlayerAbility ability, Rigidbody rigidbody)
        {
            rigidbody.velocity = Vector3.zero;

            if (inputData.horInputNormalized != Vector2.zero) // Start moving
                info.Moving = true;
            else
                info.Moving = false;
            
            if (inputData.ascend) // Jump in place
                rigidbody.velocity = new(0F, ability.JumpSpeed, 0F);

        }

        public bool ShouldEnter(PlayerStatus info)
        {
            if (!info.Spectating && info.Grounded && !info.Moving)
                return true;
            return false;
        }

        public bool ShouldExit(PlayerStatus info)
        {
            if (info.Spectating)
                return true;
            
            if (!info.Grounded || info.Moving)
                return true;
            return false;
        }

        public override string ToString() => "Idle";

    }
}