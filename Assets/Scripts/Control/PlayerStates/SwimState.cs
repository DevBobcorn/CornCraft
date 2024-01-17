#nullable enable
using UnityEngine;

namespace CraftSharp.Control
{
    public class SwimState : IPlayerState
    {
        public const float THRESHOLD_LAND_PLATFORM  = -1.4F;
        public const float AFLOAT_LIQUID_DIST_THERSHOLD = -0.55F;
        public const float SOAKED_LIQUID_DIST_THERSHOLD = -0.65F;
        public const float SURFING_GRAVITY_SCALE = 0.5F;

        public void UpdatePlayer(float interval, PlayerActions inputData, PlayerStatus info, Rigidbody rigidbody, PlayerController player)
        {
            var ability = player.Ability;
            
            info.Sprinting = false;
            info.Gliding = false;
            info.Moving = true;

            var moveSpeed = ability.SwimSpeed;

            Vector3 moveVelocity;

            bool costStamina = true;

            if (inputData.Gameplay.Movement.IsPressed()) // Move horizontally
            {
                // Smooth rotation for player model
                info.CurrentVisualYaw = Mathf.LerpAngle(info.CurrentVisualYaw, info.TargetVisualYaw, ability.SteerSpeed * interval);
                // Use the target visual yaw as actual movement direction
                moveVelocity = Quaternion.AngleAxis(info.TargetVisualYaw, Vector3.up) * Vector3.forward * moveSpeed;

                if (info.FrontDownDist < -0.05F) // A platform ahead of the player
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
                                            new(dest, ability.Climb1mCurves, player.GetRotation(), 0F, 1.1F,
                                                init: (info, rigidbody, player) => {
                                                    player.RandomizeMirroredFlag();
                                                    player.CrossFadeState(PlayerAbility.CLIMB_1M);
                                                },
                                                update: (interval, inputData, info, rigidbody, player) =>
                                                    info.Moving = inputData.Gameplay.Movement.IsPressed()
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
                                            update: (interval, inputData, info, rigidbody, player) =>
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
                                            update: (interval, inputData, info, rigidbody, player) =>
                                                info.Moving = true
                                        )
                                } );
                    }
                }
            }
            else
                moveVelocity = Vector3.zero;
            
            // Whether gravity should be reduced by liquid, and whether the player can move around by swimming
            bool soaked = info.LiquidDist <= SOAKED_LIQUID_DIST_THERSHOLD;
            float distToAfloat = AFLOAT_LIQUID_DIST_THERSHOLD - info.LiquidDist;

            if (soaked) // Use no gravity
            {
                info.GravityScale = 0F;
            }
            else // Still in air, use reduced gravity
            {
                info.GravityScale = SURFING_GRAVITY_SCALE;
            }

            moveVelocity.y = rigidbody.velocity.y * ability.LiquidMoveMultiplier;

            // Check vertical movement...
            if (inputData.Gameplay.Ascend.IsPressed())
            {
                if (distToAfloat > 0F && distToAfloat < 1F) // Move up no further than top of the surface
                {
                    moveVelocity.y = distToAfloat;
                    costStamina = false;
                }
                else
                {
                    moveVelocity.y =  moveSpeed; // Move up
                }
            }
            else if (inputData.Gameplay.Descend.IsPressed())
            {
                if (!info.Grounded)
                {
                    moveVelocity.y = -moveSpeed;
                }
            }

            // Apply new velocity to rigidbody
            info.MoveVelocity = moveVelocity;
            
            if (info.Grounded) // Restore stamina
                info.StaminaLeft = Mathf.MoveTowards(info.StaminaLeft, ability.MaxStamina, interval * ability.StaminaRestore);
            else if (costStamina) // Update stamina
                info.StaminaLeft = Mathf.MoveTowards(info.StaminaLeft, 0F, interval * ability.SwimStaminaCost);

        }

        public bool ShouldEnter(PlayerActions inputData, PlayerStatus info)
        {
            if (inputData.Gameplay.Movement.ReadValue<Vector2>() == Vector2.zero
                    && !inputData.Gameplay.Ascend.IsPressed()
                    && !inputData.Gameplay.Descend.IsPressed())
                return false;

            if (!info.Spectating && info.InLiquid)
                return true;
            
            return false;
        }

        public bool ShouldExit(PlayerActions inputData, PlayerStatus info)
        {
            if (inputData.Gameplay.Movement.ReadValue<Vector2>() == Vector2.zero
                    && !inputData.Gameplay.Ascend.IsPressed()
                    && !inputData.Gameplay.Descend.IsPressed())
                return true;
            
            if (info.Spectating || !info.InLiquid)
                return true;
            
            return false;
        }

        public void OnEnter(PlayerStatus info, Rigidbody rigidbody, PlayerController player)
        {
            info.GravityScale = SURFING_GRAVITY_SCALE;
        }

        public void OnExit(PlayerStatus info, Rigidbody rigidbody, PlayerController player)
        {
            // Restore gravity scale
            info.GravityScale = 1F;
        }

        public override string ToString() => "Swim";

    }
}