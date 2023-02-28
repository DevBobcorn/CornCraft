#nullable enable
using UnityEngine;

namespace MinecraftClient.Control
{
    public class IdleState : IPlayerState
    {
        public void UpdatePlayer(float interval, PlayerUserInputData inputData, PlayerStatus info, PlayerAbility ability, Rigidbody rigidbody, PlayerController player)
        {
            info.Sprinting = false;
            info.Moving = false;

            //rigidbody.AddForce(-rigidbody.velocity * 0.2F, ForceMode.VelocityChange);

            if (inputData.attack) // Try start attacking
                player.AttackManager.TryStart();
            else if (inputData.ascend) // Jump in place
                rigidbody.velocity = new(0F, ability.JumpSpeed, 0F);

            // Restore stamina
            info.StaminaLeft = Mathf.MoveTowards(info.StaminaLeft, ability.MaxStamina, interval * ability.StaminaRestore);
        }

        public bool ShouldEnter(PlayerUserInputData inputData, PlayerStatus info)
        {
            if (inputData.horInputNormalized != Vector2.zero)
                return false;
            
            if (!info.Spectating && info.Grounded && !info.OnWall && !info.InLiquid)
                return true;
            
            return false;
        }

        public bool ShouldExit(PlayerUserInputData inputData, PlayerStatus info)
        {
            if (inputData.horInputNormalized != Vector2.zero)
                return true;
            
            if (info.Spectating || !info.Grounded || info.OnWall || info.InLiquid)
                return true;
            
            return false;
        }

        public override string ToString() => "Idle";

    }
}