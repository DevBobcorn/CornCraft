#nullable enable
using UnityEngine;

namespace MinecraftClient.Control
{
    public class MoveState : IPlayerState
    {
        public void UpdatePlayer(float interval, PlayerUserInputData inputData, PlayerStatus info, PlayerAbility ability, Rigidbody rigidbody)
        {
            if (inputData.horInputNormalized != Vector2.zero)
            {
                info.Moving = true;

                // Smooth rotation for player model
                info.CurrentVisualYaw = Mathf.LerpAngle(info.CurrentVisualYaw, info.TargetVisualYaw, ability.SteerSpeed * interval);

                var moveSpeed = info.WalkMode ? ability.WalkSpeed : ability.RunSpeed;

                // Use the target visual yaw as actual movement direction
                var moveVelocity = Quaternion.AngleAxis(info.TargetVisualYaw, Vector3.up) * Vector3.forward * moveSpeed;
                moveVelocity.y = rigidbody.velocity.y;

                // Apply new velocity to rigidbody
                rigidbody.velocity = moveVelocity;
                
                if (inputData.ascend) // Jump up, keep horizontal speed
                    rigidbody.velocity = new(rigidbody.velocity.x, ability.JumpSpeed, rigidbody.velocity.z);
                
            }
            else // Stop moving
                info.Moving = false;

        }

        public bool ShouldEnter(PlayerStatus info)
        {
            if (!info.Spectating && info.Grounded && info.Moving)
                return true;
            return false;
        }

        public bool ShouldExit(PlayerStatus info)
        {
            if (info.Spectating)
                return true;

            if (!info.Grounded || !info.Moving)
                return true;
            return false;
        }

        public override string ToString() => "Move";

    }
}