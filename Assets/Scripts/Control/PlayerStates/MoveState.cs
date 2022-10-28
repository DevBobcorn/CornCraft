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

                // Auto walk up aid
                if (inputData.horInputNormalized != Vector2.zero && Mathf.Abs(info.YawOffset) < 60F) // Trying to moving forward
                {
                    if (info.FrontDownDist < -0.03F && info.FrontDownDist > -0.6F)
                        moveVelocity.y = ability.AidSpeedCurve.Evaluate(info.FrontDownDist);
                }

                if (inputData.ascend) // Jump up, keep horizontal speed
                    moveVelocity.y = ability.JumpSpeed;

                // Apply new velocity to rigidbody
                rigidbody.velocity = moveVelocity;
            }
            else // Stop moving
                info.Moving = false;

        }

        public bool ShouldEnter(PlayerStatus info)
        {
            if (!info.Spectating && info.Grounded && !info.OnWall && !info.InWater && info.Moving)
                return true;
            return false;
        }

        public bool ShouldExit(PlayerStatus info)
        {
            if (info.Spectating)
                return true;

            if (!info.Grounded || info.OnWall || info.InWater || !info.Moving)
                return true;
            return false;
        }

        public override string ToString() => "Move";

    }
}