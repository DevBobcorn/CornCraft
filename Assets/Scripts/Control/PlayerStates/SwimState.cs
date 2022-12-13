#nullable enable
using UnityEngine;

namespace MinecraftClient.Control
{
    public class SwimState : IPlayerState
    {
        public void UpdatePlayer(float interval, PlayerUserInputData inputData, PlayerStatus info, PlayerAbility ability, Rigidbody rigidbody, PlayerController player)
        {
            info.Sprinting = false;
            
            if (inputData.horInputNormalized != Vector2.zero || inputData.ascend || inputData.descend)
            {
                var moveSpeed = (info.WalkMode ? ability.WalkSpeed : ability.RunSpeed) * ability.WaterMoveMultiplier;

                var distAboveLiquidSurface = info.LiquidDist - PlayerStatusUpdater.SURFING_LIQUID_DIST_THERSHOLD;

                info.Moving = true;

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
                    if (info.LiquidDist > -0.5F) // On water suface now, swim up to land
                    {
                        var moveHorDir = Quaternion.AngleAxis(info.TargetVisualYaw, Vector3.up) * Vector3.forward;
                        var horOffset = info.BarrierDist - 1.0F;

                        // Start force move operation
                        var org  = rigidbody.transform.position;
                        var dest = org + (-info.FrontDownDist - 0.99F) * Vector3.up + moveHorDir * horOffset;

                        player.StartForceMoveOperation("Climb to land",
                                    new ForceMoveOperation[] {
                                            new(org,  dest, 0.15F),
                                            new(dest, ability.Climb1mCurves, player.visualTransform!.rotation, 0F, 0.95F,
                                                init: (info, ability, rigidbody, player) =>
                                                    player.CrossFadeState(PlayerAbility.CLIMB_1M),
                                                update: (interval, inputData, info, ability, rigidbody, player) =>
                                                    info.Moving = inputData.horInputNormalized != Vector2.zero
                                            )
                                    } );

                        Debug.Log("Climb to land " + info.LiquidDist);
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
                        
                        Debug.Log("Swim over barrier underwater " + info.LiquidDist);
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
                else if (inputData.descend)
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
                
                if (costStamina) // Update stamina
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