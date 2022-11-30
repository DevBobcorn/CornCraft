#nullable enable
using UnityEngine;

namespace MinecraftClient.Control
{
    public class MoveState : IPlayerState
    {
        public const float THRESHOULD_CLIMB_2M = -2.05F;
        public const float THRESHOULD_CLIMB_1M = -1.85F;
        public const float THRESHOULD_CLIMB_UP = -0.85F;
        public const float THRESHOULD_STEP_UP  = -0.01F;


        public void UpdatePlayer(float interval, PlayerUserInputData inputData, PlayerStatus info, PlayerAbility ability, Rigidbody rigidbody, PlayerController player)
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

                if (inputData.horInputNormalized != Vector2.zero && Mathf.Abs(info.YawOffset) < 40F) // Trying to moving forward
                {
                    if (info.FrontDownDist <= THRESHOULD_CLIMB_1M && info.FrontDownDist > THRESHOULD_CLIMB_2M && info.BarrierAngle < 30F) // Climb up platform
                    {
                        var moveHorDir = Quaternion.AngleAxis(info.TargetVisualYaw, Vector3.up) * Vector3.forward;
                        var horOffset = info.BarrierDist - 1.0F;

                        var org  = rigidbody.transform.position;
                        var dest = org + (-info.FrontDownDist - 1.98F) * Vector3.up + moveHorDir * horOffset;

                        player.StartForceMoveOperation("Climb over wall",
                                new ForceMoveOperation[] {
                                        new(org,  dest, 0.1F),
                                        new(dest, ability.Climb2mCurves, player.visualTransform!.rotation, 0F, 2.25F,
                                            playbackSpeed: 1.8F,
                                            init: (info, ability, rigidbody, player) =>
                                                player.CrossFadeState(PlayerAbility.CLIMB_2M),
                                            update: (interval, inputData, info, ability, rigidbody, player) =>
                                                info.Moving = inputData.horInputNormalized != Vector2.zero
                                        )
                                } );
                    }
                    else if (info.FrontDownDist <= THRESHOULD_CLIMB_UP && info.FrontDownDist > THRESHOULD_CLIMB_1M && info.BarrierAngle < 30F) // Climb up platform
                    {
                        var moveHorDir = Quaternion.AngleAxis(info.TargetVisualYaw, Vector3.up) * Vector3.forward;
                        var horOffset = info.BarrierDist - 1.0F;

                        var org  = rigidbody.transform.position;
                        var dest = org + (-info.FrontDownDist - 0.98F) * Vector3.up + moveHorDir * horOffset;

                        player.StartForceMoveOperation("Climb over barrier",
                                new ForceMoveOperation[] {
                                        new(org,  dest, 0.1F),
                                        new(dest, ability.Climb1mCurves, player.visualTransform!.rotation, 0F, 0.95F,
                                            init: (info, ability, rigidbody, player) =>
                                                player.CrossFadeState(PlayerAbility.CLIMB_1M),
                                            update: (interval, inputData, info, ability, rigidbody, player) =>
                                                info.Moving = inputData.horInputNormalized != Vector2.zero
                                        )
                                } );
                    }
                    else if (info.FrontDownDist <= THRESHOULD_STEP_UP && info.FrontDownDist > THRESHOULD_CLIMB_UP) // Walk up stairs
                    {
                        if (info.GroundSlope > 80F) // Stairs or slabs, just step up
                        {
                            var moveHorDir = Quaternion.AngleAxis(info.TargetVisualYaw, Vector3.up) * Vector3.forward;

                            // Start force move operation
                            var org  = rigidbody.transform.position;
                            var dest = rigidbody.transform.position + (-info.FrontDownDist + 0.01F) * Vector3.up + moveHorDir * 0.7F;
                            var time = (dest - org).magnitude / moveSpeed;

                            player.StartForceMoveOperation("Walk up stairs", new ForceMoveOperation[] {
                                    new(org, dest, time,
                                        update: (interval, inputData, info, ability, rigidbody, player) =>
                                        {
                                            // Force grounded while doing the move
                                            info.Grounded = true;
                                            // Update player yaw
                                            info.CurrentVisualYaw = Mathf.LerpAngle(info.CurrentVisualYaw, info.TargetVisualYaw, ability!.SteerSpeed * interval);
                                        })
                                    } );
                        }
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
            if (!info.Spectating && info.Grounded && !info.OnWall && !info.InLiquid && info.Moving)
                return true;
            return false;
        }

        public bool ShouldExit(PlayerStatus info)
        {
            if (info.Spectating)
                return true;

            if (!info.Grounded || info.OnWall || info.InLiquid || !info.Moving)
                return true;
            return false;
        }

        public override string ToString() => "Move";

    }
}