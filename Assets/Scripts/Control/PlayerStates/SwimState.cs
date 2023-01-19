#nullable enable
using UnityEngine;

namespace MinecraftClient.Control
{
    public class SwimState : IPlayerState
    {
        public const float THRESHOLD_LAND_PLATFORM  = -1.4F;

        public void UpdatePlayer(float interval, PlayerUserInputData inputData, PlayerStatus info, PlayerAbility ability, Rigidbody rigidbody, PlayerController player)
        {
            info.Sprinting = false;
            
            if (inputData.horInputNormalized != Vector2.zero || inputData.ascend || inputData.descend)
            {
                var moveSpeed = (info.WalkMode ? ability.WalkSpeed : ability.RunSpeed) * ability.WaterMoveMultiplier;

                var distAboveLiquidSurface = info.LiquidDist - PlayerStatusUpdater.SURFING_LIQUID_DIST_THERSHOLD;

                info.Moving = true;

                if ((inputData.horInputNormalized == Vector2.zero && !inputData.ascend) && info.Grounded)
                    info.Moving = false; // Awful exceptions

                bool costStamina = true;

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
                if (info.FrontDownDist < -0.05F)
                {
                    if (info.LiquidDist > info.FrontDownDist) // On water suface now, and the platform is above water
                    {
                        if (info.LiquidDist > THRESHOLD_LAND_PLATFORM)
                        {
                            var moveHorDir = Quaternion.AngleAxis(info.TargetVisualYaw, Vector3.up) * Vector3.forward;
                            var horOffset = info.BarrierDist - 1.0F;

                            // Start force move operation
                            var org  = rigidbody.transform.position;
                            var dest = org + (-info.FrontDownDist - 0.95F) * Vector3.up + moveHorDir * horOffset;

                            player.StartForceMoveOperation("Climb to land",
                                        new ForceMoveOperation[] {
                                                new(org,  dest, 0.01F + Mathf.Max(info.LiquidDist - info.FrontDownDist - 0.2F, 0F) * 0.5F),
                                                new(dest, player.visualTransform!.rotation, 0F, 1F,
                                                    init: (info, ability, rigidbody, player) => {
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
                        // Otherwise the platform/land is too high to reach
                    }
                    else if (info.LiquidDist > THRESHOLD_LAND_PLATFORM) // Approaching Water suface, and the platform is under water 
                    {
                        var moveHorDir = Quaternion.AngleAxis(info.TargetVisualYaw, Vector3.up) * Vector3.forward;

                        // Start force move operation
                        var org  = rigidbody.transform.position;
                        var dest = org + (-info.FrontDownDist) * Vector3.up + moveHorDir * 0.55F;

                        player.StartForceMoveOperation("Swim over barrier underwater",
                                    new ForceMoveOperation[] {
                                            new(org,  dest, 0.8F,
                                                update: (interval, inputData, info, ability, rigidbody, player) =>
                                                    info.Moving = true
                                            )
                                    } );
                    }
                    else // Below water surface now, swim up a bit
                    {
                        var moveHorDir = Quaternion.AngleAxis(info.TargetVisualYaw, Vector3.up) * Vector3.forward;

                        // Start force move operation
                        var org  = rigidbody.transform.position;
                        var dest = org + (-info.FrontDownDist + 0.2F) * Vector3.up + moveHorDir * 0.35F;

                        player.StartForceMoveOperation("Swim over barrier underwater",
                                    new ForceMoveOperation[] {
                                            new(org,  dest, 0.5F,
                                                update: (interval, inputData, info, ability, rigidbody, player) =>
                                                    info.Moving = true
                                            )
                                    } );
                    }
                    
                }
                else if (inputData.ascend)
                {
                    if (distAboveLiquidSurface > 0.1F) // Free fall in air
                    {
                        moveVelocity.y =  rigidbody.velocity.y;
                        costStamina = false;
                    }
                    else if (distAboveLiquidSurface > 0F) // Cancel gravity, but don't move up further
                    {
                        moveVelocity.y = -Time.fixedDeltaTime * Physics.gravity.y;
                        costStamina = false;
                    }
                    else
                        moveVelocity.y =  moveSpeed; // Move up
                }
                else if (!info.Grounded && inputData.descend)
                    moveVelocity.y = -moveSpeed;
                else // Sink
                {
                    if (distAboveLiquidSurface > 0F) // Free fall in air
                        moveVelocity.y =  rigidbody.velocity.y;
                    else if (inputData.horInputNormalized != Vector2.zero) // Moving, cancel gravity
                        moveVelocity.y = -Time.fixedDeltaTime * Physics.gravity.y;
                    else // Preserve vertical speed, free fall
                        moveVelocity.y = Mathf.Max(ability.MaxWaterFallSpeed, rigidbody.velocity.y);
                }

                // Clamp velocity magnitude
                if (moveVelocity.magnitude > ability.MaxWaterMoveSpeed)
                    moveVelocity *= (ability.MaxWaterMoveSpeed / moveVelocity.magnitude);

                // Apply new velocity to rigidbody
                rigidbody.velocity = moveVelocity;
                
                if (info.Grounded) // Restore stamina
                    info.StaminaLeft = Mathf.MoveTowards(info.StaminaLeft, ability.MaxStamina, interval * ability.StaminaRestore);
                else if (costStamina) // Update stamina
                    info.StaminaLeft = Mathf.MoveTowards(info.StaminaLeft, 0F, interval * ability.SwimStaminaCost);
            }
            else // Stop moving
                info.Moving = false;

        }

        public bool ShouldEnter(PlayerStatus info)
        {
            if (!info.Spectating && info.InLiquid && info.Moving)
                return true;
            return false;
        }

        public bool ShouldExit(PlayerStatus info)
        {
            if (info.Spectating)
                return true;

            if (!info.InLiquid || !info.Moving)
                return true;
            return false;
        }

        public override string ToString() => "Swim";

    }
}