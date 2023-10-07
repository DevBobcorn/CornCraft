#nullable enable
using UnityEngine;

namespace CraftSharp.Control
{
    public class GlideState : IPlayerState
    {
        public const float THRESHOLD_CLIMB_2M = -2.05F;
        public const float THRESHOLD_CLIMB_1M = -1.55F;
        public const float THRESHOLD_CLIMB_UP = -1.35F;

        public const float THRESHOLD_ANGLE_FORWARD = 40F;

        public void UpdatePlayer(float interval, PlayerActions inputData, PlayerStatus info, Rigidbody rigidbody, PlayerController player)
        {
            var ability = player.Ability;

            info.Sprinting = false;

            if (inputData.Gameplay.Jump.IsPressed()) // Check stop gliding
            {
                info.Gliding = false;
            }

            Vector3 moveVelocity;
            
            if (inputData.Gameplay.Movement.IsPressed()) // Trying to move
            {
                // Smooth rotation for player model
                info.CurrentVisualYaw = Mathf.LerpAngle(info.CurrentVisualYaw, info.TargetVisualYaw, ability.SteerSpeed * interval * 0.08F);
                // Horizontal speed
                moveVelocity = Quaternion.AngleAxis(info.TargetVisualYaw, Vector3.up) * Vector3.forward * ability.GlideSpeed;

                if (info.YawOffset <= THRESHOLD_ANGLE_FORWARD) // Trying to move forward
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
                                        new(dest, ability.Climb2mCurves, player.GetRotation(), 0F, 2.2F,
                                            playbackSpeed: 1.5F,
                                            init: (info, rigidbody, player) =>
                                            {
                                                player.RandomizeMirroredFlag();
                                                player.CrossFadeState(PlayerAbility.CLIMB_2M);
                                            },
                                            update: (interval, inputData, info, rigidbody, player) =>
                                                info.Moving = inputData.Gameplay.Movement.IsPressed()
                                        )
                                } );
                    }
                    else if (info.FrontDownDist <= THRESHOLD_CLIMB_UP && info.FrontDownDist > THRESHOLD_CLIMB_1M && info.BarrierAngle < 30F) // Climb up platform
                    {
                        var moveHorDir = Quaternion.AngleAxis(info.TargetVisualYaw, Vector3.up) * Vector3.forward;
                        var horOffset = info.BarrierDist - 1.0F;

                        var org  = rigidbody.transform.position;
                        var dest = org + (-info.FrontDownDist - 0.95F) * Vector3.up + moveHorDir * horOffset;

                        player.StartForceMoveOperation("Climb over barrier",
                                new ForceMoveOperation[] {
                                        new(org,  dest, 0.1F),
                                        new(dest, ability.Climb1mCurves, player.GetRotation(), 0F, 0.9F,
                                            init: (info, rigidbody, player) => {
                                                player.RandomizeMirroredFlag();
                                                player.CrossFadeState(PlayerAbility.CLIMB_1M);
                                            },
                                            update: (interval, inputData, info, rigidbody, player) =>
                                                info.Moving = inputData.Gameplay.Movement.IsPressed()
                                        )
                                } );
                    }
                }
            }
            else // No horizontal movement
            {
                moveVelocity = Vector3.zero;
            }

            // Vertical speed
            moveVelocity.y = Mathf.Max(rigidbody.velocity.y, ability.MaxGlideFallSpeed);

            // Apply new velocity to rigidbody
            info.MoveVelocity = moveVelocity;
            
            if (info.Grounded) // Restore stamina
                info.StaminaLeft = Mathf.MoveTowards(info.StaminaLeft, ability.MaxStamina, interval * ability.StaminaRestore);
            else // Cost stamina
                info.StaminaLeft = Mathf.MoveTowards(info.StaminaLeft, 0F, interval * ability.GlideStaminaCost);

            if (info.StaminaLeft == 0) // Out of stamina, we're gonna FALL!
            {
                info.Gliding = false;
            }
        }

        public bool ShouldEnter(PlayerActions inputData, PlayerStatus info)
        {
            if (!info.Spectating && !info.Grounded && info.Gliding && !info.OnWall && !info.InLiquid)
                return true;
            
            return false;
        }

        public bool ShouldExit(PlayerActions inputData, PlayerStatus info)
        {
            if (info.Spectating || info.Grounded || !info.Gliding || info.OnWall || info.InLiquid)
                return true;
            
            return false;
        }

        public override string ToString() => "Fall";
    }
}