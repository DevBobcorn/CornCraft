#nullable enable
using UnityEngine;

namespace MinecraftClient.Control
{
    public class SwimState : IPlayerState
    {
        public void UpdatePlayer(float interval, PlayerUserInputData inputData, PlayerStatus info, PlayerAbility ability, Rigidbody rigidbody)
        {
            if (inputData.horInputNormalized != Vector2.zero || inputData.ascend || inputData.descend)
            {
                var moveSpeed = (info.WalkMode ? ability.WalkSpeed : ability.RunSpeed) * ability.WaterMoveMultiplier;

                info.Moving = true;

                // Use the target visual yaw as actual movement direction
                Vector3 moveVelocity;

                if (inputData.horInputNormalized != Vector2.zero) // Move horizontally
                {
                    // Smooth rotation for player model
                    info.CurrentVisualYaw = Mathf.LerpAngle(info.CurrentVisualYaw, info.TargetVisualYaw, ability.SteerSpeed * interval);
                    moveVelocity = Quaternion.AngleAxis(info.TargetVisualYaw, Vector3.up) * Vector3.forward * moveSpeed;
                }
                else
                    moveVelocity = Vector3.zero;

                // Check vertical movement...
                if (info.FrontDownDist < -0.05F) // Swim up to land
                {
                    var moveHorDir = Quaternion.AngleAxis(info.TargetVisualYaw, Vector3.up) * Vector3.forward;

                    // Start force move operation
                    info.ForceMoveOrigin = rigidbody.transform.position;
                    info.ForceMoveDist = rigidbody.transform.position + (-info.FrontDownDist + 0.1F) * Vector3.up + moveHorDir * 0.5F;
                    info.ForceMoveTimeTotal = info.ForceMoveTimeCurrent = 0.3F; // 1 second to finish
                }
                else if (inputData.ascend)
                {
                    if (!info.OnWaterSurface) // Move up
                        moveVelocity.y =  moveSpeed;
                    else // Cancel gravity, but don't move up further
                        moveVelocity.y = -Time.fixedDeltaTime * Physics.gravity.y;
                }
                else if (inputData.descend)
                    moveVelocity.y = -moveSpeed;
                else if (inputData.horInputNormalized != Vector2.zero) // Moving, cancel gravity
                    moveVelocity.y = -Time.fixedDeltaTime * Physics.gravity.y;
                else // Preserve vertical speed
                    moveVelocity.y = Mathf.Max(ability.MaxWaterFallSpeed, rigidbody.velocity.y);

                // Clamp velocity magnitude
                if (moveVelocity.magnitude > ability.MaxWaterMoveSpeed)
                    moveVelocity *= (ability.MaxWaterMoveSpeed / moveVelocity.magnitude);

                // Apply new velocity to rigidbody
                rigidbody.velocity = moveVelocity;
            }
            else // Stop moving
                info.Moving = false;

        }

        public bool ShouldEnter(PlayerStatus info)
        {
            if (!info.Spectating && info.InWater && info.Moving)
                return true;
            return false;
        }

        public bool ShouldExit(PlayerStatus info)
        {
            if (info.Spectating)
                return true;

            if (!info.InWater || !info.Moving)
                return true;
            return false;
        }

        public override string ToString() => "Swim";

    }
}