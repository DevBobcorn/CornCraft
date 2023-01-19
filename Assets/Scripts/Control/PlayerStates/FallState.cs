#nullable enable
using UnityEngine;

namespace MinecraftClient.Control
{
    public class FallState : IPlayerState
    {
        public const float THRESHOLD_CLIMB_2M = -2.05F;
        public const float THRESHOLD_CLIMB_1M = -1.55F;
        public const float THRESHOLD_CLIMB_UP = -1.35F;

        public const float THRESHOLD_ANGLE_FORWARD = 40F;

        public void UpdatePlayer(float interval, PlayerUserInputData inputData, PlayerStatus info, PlayerAbility ability, Rigidbody rigidbody, PlayerController player)
        {
            info.Sprinting = false;

            if (inputData.horInputNormalized != Vector2.zero)
            {
                // Smooth rotation for player model
                info.CurrentVisualYaw = Mathf.LerpAngle(info.CurrentVisualYaw, info.TargetVisualYaw, ability.SteerSpeed * interval * 0.05F);

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
                                        new(dest, ability.Climb2mCurves, player.visualTransform!.rotation, 0F, 2.2F,
                                            playbackSpeed: 1.5F,
                                            init: (info, ability, rigidbody, player) =>
                                                player.CrossFadeState(PlayerAbility.CLIMB_2M),
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
                                        new(dest, player.visualTransform!.rotation, 0F, 0.9F,
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
                }
            }
            
            var moveSpeed = rigidbody.velocity;

            // Check and constrain fall speed
            if (moveSpeed.y < ability.MaxFallSpeed)
                moveSpeed = new(moveSpeed.x, ability.MaxFallSpeed, moveSpeed.z);
            // Otherwise free fall, leave velocity unchanged

            rigidbody.velocity = moveSpeed;

            // Leave stamina value unchanged
        }

        public bool ShouldEnter(PlayerStatus info)
        {
            if (!info.Spectating && !info.Grounded && !info.OnWall && !info.InLiquid)
                return true;
            return false;
        }

        public bool ShouldExit(PlayerStatus info)
        {
            if (info.Spectating)
                return true;
            
            if (info.Grounded || info.OnWall || info.InLiquid)
                return true;
            return false;
        }

        public override string ToString() => "Fall";

    }
}