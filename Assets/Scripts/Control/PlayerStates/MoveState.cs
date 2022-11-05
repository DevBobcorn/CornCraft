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

                bool costStamina = false;

                // Smooth rotation for player model
                info.CurrentVisualYaw = Mathf.LerpAngle(info.CurrentVisualYaw, info.TargetVisualYaw, ability.SteerSpeed * interval);

                float moveSpeed;

                if ((inputData.sprint || info.Sprinting) && info.StaminaLeft > ability.SprintMinStamina)
                {
                    info.Sprinting = true;
                    costStamina = true;
                }
                else
                    info.Sprinting = false;
                
                if (info.Sprinting)
                    moveSpeed = ability.SprintSpeed;
                else
                    moveSpeed = info.WalkMode ? ability.WalkSpeed : ability.RunSpeed;

                // Use the target visual yaw as actual movement direction
                var moveVelocity = Quaternion.AngleAxis(info.TargetVisualYaw, Vector3.up) * Vector3.forward * moveSpeed;
                moveVelocity.y = rigidbody.velocity.y;

                if (inputData.horInputNormalized != Vector2.zero && Mathf.Abs(info.YawOffset) < 60F) // Trying to moving forward
                {
                    if (info.FrontDownDist < -0.05F && info.FrontDownDist > -1.05F) // Walk up aid
                    {
                        if (info.GroundSlope > 80F) // Stairs or slabs, just step up
                        {
                            var moveHorDir = Quaternion.AngleAxis(info.TargetVisualYaw, Vector3.up) * Vector3.forward;

                            // Start force move operation
                            info.ForceMoveOrigin = rigidbody.transform.position;
                            info.ForceMoveDestination = rigidbody.transform.position + (-info.FrontDownDist + 0.01F) * Vector3.up + moveHorDir * 0.8F;
                            var moveDist = (info.ForceMoveDestination - info.ForceMoveOrigin).Value.magnitude;
                            info.ForceMoveTimeTotal = info.ForceMoveTimeCurrent = moveDist / moveSpeed;
                        }
                    }
                    else if (info.FrontDownDist > 0.55F && info.FrontDownDist < 0.95F) // Walk down aid
                    {
                        var moveHorDir = Quaternion.AngleAxis(info.TargetVisualYaw, Vector3.up) * Vector3.forward;

                        // Start force move operation
                        info.ForceMoveOrigin = rigidbody.transform.position;
                        info.ForceMoveDestination = rigidbody.transform.position + (-info.FrontDownDist + 0.01F) * Vector3.up + moveHorDir * 0.8F;
                        var moveDist = (info.ForceMoveDestination - info.ForceMoveOrigin).Value.magnitude;
                        info.ForceMoveTimeTotal = info.ForceMoveTimeCurrent = moveDist / moveSpeed;
                    }

                }

                if (inputData.ascend) // Jump up, keep horizontal speed
                    moveVelocity.y = ability.JumpSpeed;

                // Apply new velocity to rigidbody
                rigidbody.velocity = moveVelocity;

                if (costStamina) // Cost stamina
                    info.StaminaLeft = Mathf.MoveTowards(info.StaminaLeft, 0F, interval * ability.SwimStaminaCost);
                else // Restore stamina
                    info.StaminaLeft = Mathf.MoveTowards(info.StaminaLeft, ability.MaxStamina, interval * ability.SwimStaminaCost);
            }
            else // Stop moving
            {
                info.Moving = false;
                info.Sprinting = false;
            }

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