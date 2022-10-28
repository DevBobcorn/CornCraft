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
                if (inputData.ascend)
                {
                    if (!info.OnWaterSurface) // Move up
                        moveVelocity.y =  moveSpeed;
                    else // Cancel gravity, but don't move up further
                        moveVelocity.y = -Time.fixedDeltaTime * Physics.gravity.y;
                }
                else if (inputData.descend)
                    moveVelocity.y = -moveSpeed;
                else // Preserve vertical speed
                    moveVelocity.y = Mathf.Max(ability.MaxWaterFallSpeed, rigidbody.velocity.y);

                // Clamp velocity magnitude
                if (moveVelocity.magnitude > ability.MaxWaterMoveSpeed)
                    moveVelocity *= (ability.MaxWaterMoveSpeed / moveVelocity.magnitude);
                
                // Auto walk up aid (in water), or aid when player is trying to get onto land from water
                // Note that this is applied after velocity magnitude clamping
                if (inputData.horInputNormalized != Vector2.zero && Mathf.Abs(info.YawOffset) < 60F) // Trying to moving forward
                {
                    if (info.FrontDownDist < -0.03F && info.FrontDownDist > -0.9F)
                        moveVelocity.y = ability.WaterAidSpeedCurve.Evaluate(info.FrontDownDist);
                }

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