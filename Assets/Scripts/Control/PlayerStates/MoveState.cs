#nullable enable
using UnityEngine;

namespace MinecraftClient.Control
{
    public class MoveState : IPlayerState
    {
        public const float THRESHOLD_CLIMB_2M = -2.05F;
        public const float THRESHOLD_CLIMB_1M = -1.85F;
        public const float THRESHOLD_CLIMB_UP = -0.85F;
        public const float THRESHOLD_STEP_UP  = -0.01F;

        public const float RUN_BRAKE_TIME    = 0.2F;
        public const float SPRINT_BRAKE_TIME = 0.3F;

        public const float THRESHOLD_SPRINT_STOP   = 50F;
        public const float THRESHOLD_ANGLE_FORWARD = 70F;

        public void UpdatePlayer(float interval, PlayerUserInputData inputData, PlayerStatus info, PlayerAbility ability, Rigidbody rigidbody, PlayerController player)
        {
            if (inputData.horInputNormalized != Vector2.zero)
            {
                info.Moving = true;
                info.BrakeTime = info.Sprinting ? SPRINT_BRAKE_TIME : RUN_BRAKE_TIME;

                // Stop sprinting if steer angle too sharp
                if (info.YawOffset > THRESHOLD_SPRINT_STOP)
                    info.Sprinting = false;
            }
            else
                info.Moving = false;

            if (info.BrakeTime > 0F)
            {
                if (!info.Moving)
                    info.BrakeTime = Mathf.Max(0F, info.BrakeTime - interval);
                
                bool costStamina = false;

                // Smooth rotation for player model
                if (info.Sprinting)
                {
                    info.CurrentVisualYaw =
                            Mathf.LerpAngle(info.CurrentVisualYaw, info.TargetVisualYaw, ability.SteerSpeed * 0.3F * interval);
                }
                else
                {
                    info.CurrentVisualYaw =
                            Mathf.LerpAngle(info.CurrentVisualYaw, info.TargetVisualYaw, ability.SteerSpeed * interval);
                }

                float moveSpeed;

                if ((inputData.sprint || info.Sprinting) && info.StaminaLeft > 0F)
                {
                    info.Sprinting = true;
                    costStamina = true;
                }
                else
                    info.Sprinting = false;
                
                if (info.Sprinting)
                    moveSpeed = ability.SprintSpeed * Mathf.Sqrt(info.BrakeTime / SPRINT_BRAKE_TIME);
                else
                    moveSpeed = (info.WalkMode ? ability.WalkSpeed : ability.RunSpeed) *  Mathf.Sqrt(info.BrakeTime / RUN_BRAKE_TIME);

                // Use the target visual yaw as actual movement direction
                var moveVelocity = Quaternion.AngleAxis(info.TargetVisualYaw, Vector3.up) * Vector3.forward * moveSpeed;

                // Simulate player inertia
                moveVelocity = Vector3.Lerp(rigidbody.velocity, moveVelocity, interval * 20F);

                moveVelocity.y = rigidbody.velocity.y;

                if (inputData.horInputNormalized != Vector2.zero && info.YawOffset <= THRESHOLD_ANGLE_FORWARD) // Trying to moving forward
                {
                    if (info.FrontDownDist <= THRESHOLD_CLIMB_1M && info.FrontDownDist > THRESHOLD_CLIMB_2M && info.BarrierAngle < 30F) // Climb up platform
                    {
                        var moveHorDir = Quaternion.AngleAxis(info.TargetVisualYaw, Vector3.up) * Vector3.forward;
                        var horOffset = info.BarrierDist - 1.0F;

                        var org  = rigidbody.transform.position;
                        var dest = org + (-info.FrontDownDist - 1.99F) * Vector3.up + moveHorDir * horOffset;

                        player.StartForceMoveOperation("Climb over wall",
                                new ForceMoveOperation[] {
                                        new(org,  dest, 0.1F),
                                        new(dest, ability.Climb2mCurves, player.visualTransform!.rotation, 0F, 2.25F,
                                            playbackSpeed: 1.5F,
                                            init: (info, ability, rigidbody, player) =>
                                            {
                                                player.RandomizeMirroredFlag();
                                                player.CrossFadeState(PlayerAbility.CLIMB_2M);
                                            },
                                            update: (interval, inputData, info, ability, rigidbody, player) =>
                                                info.Moving = inputData.horInputNormalized != Vector2.zero
                                        )
                                } );
                    }
                    else if (info.FrontDownDist <= THRESHOLD_CLIMB_UP && info.FrontDownDist > THRESHOLD_CLIMB_1M && info.BarrierAngle < 30F) // Climb up platform
                    {
                        var moveHorDir = Quaternion.AngleAxis(info.TargetVisualYaw, Vector3.up) * Vector3.forward;
                        var horOffset = info.BarrierDist - 1.0F;

                        var org  = rigidbody.transform.position;
                        var dest = org + (-info.FrontDownDist - 0.99F) * Vector3.up + moveHorDir * horOffset;

                        player.StartForceMoveOperation("Climb over barrier",
                                new ForceMoveOperation[] {
                                        new(org,  dest, 0.1F),
                                        new(dest, player.visualTransform!.rotation, 0F, 1F,
                                            init: (info, ability, rigidbody, player) => {
                                                player.RandomizeMirroredFlag();
                                                player.CrossFadeState(PlayerAbility.CLIMB_1M);
                                                player.UseRootMotion = true;
                                            },
                                            update: (interval, inputData, info, ability, rigidbody, player) =>
                                                info.Moving = inputData.horInputNormalized != Vector2.zero,
                                            exit: (info, ability, rigidbody, player) => {
                                                player.UseRootMotion = false;
                                            }
                                        )
                                } );
                    }
                    else if (info.FrontDownDist <= THRESHOLD_STEP_UP && info.FrontDownDist > THRESHOLD_CLIMB_UP) // Walk up stairs
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
                {
                    moveVelocity.y = ability.JumpSpeed + Mathf.Min(1F, moveSpeed);
                }

                // Apply new velocity to rigidbody
                rigidbody.velocity = moveVelocity;

                if (costStamina) // Cost stamina
                    info.StaminaLeft = Mathf.MoveTowards(info.StaminaLeft, 0F, interval * ability.SwimStaminaCost);
                else // Restore stamina
                    info.StaminaLeft = Mathf.MoveTowards(info.StaminaLeft, ability.MaxStamina, interval * ability.SwimStaminaCost);
            }
            else // Not moving
            {
                // Stop moving and clear sprinting flag anyway
                info.Moving = false;
                info.Sprinting = false;
            }

        }

        public bool ShouldEnter(PlayerStatus info)
        {
            if (!info.Spectating && info.Grounded && !info.OnWall && !info.InLiquid && (info.Moving || info.BrakeTime > 0F))
                return true;
            return false;
        }

        public bool ShouldExit(PlayerStatus info)
        {
            if (info.Spectating)
                return true;

            if (!info.Grounded || info.OnWall || info.InLiquid || (!info.Moving && info.BrakeTime <= 0F))
                return true;
            return false;
        }

        public void OnExit(PlayerStatus info, PlayerAbility ability, Rigidbody rigidbody, PlayerController player)
        {
            info.Sprinting = false;
        }

        public override string ToString() => "Move";

    }
}