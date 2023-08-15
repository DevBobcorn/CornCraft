#nullable enable
using UnityEngine;

namespace CraftSharp.Control
{
    public class FallState : IPlayerState
    {
        public const float THRESHOLD_CLIMB_2M = -2.05F;
        public const float THRESHOLD_CLIMB_1M = -1.55F;
        public const float THRESHOLD_CLIMB_UP = -1.35F;

        public const float THRESHOLD_ANGLE_FORWARD = 40F;

        public void UpdatePlayer(float interval, PlayerUserInputData inputData, PlayerStatus info, Rigidbody rigidbody, PlayerController player)
        {
            var ability = player.Ability;

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
                                        new(org,  dest, 0.01F),
                                        new(dest, ability.Climb2mCurves, player.visualTransform!.rotation, 0F, 2.2F,
                                            playbackSpeed: 1.5F,
                                            init: (info, rigidbody, player) =>
                                            {
                                                player.RandomizeMirroredFlag();
                                                player.CrossFadeState(PlayerAbility.CLIMB_2M);
                                            },
                                            update: (interval, inputData, info, rigidbody, player) =>
                                                info.Moving = inputData.horInputNormalized != Vector2.zero
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
                                        new(org,  dest, 0.01F),
                                        new(dest, ability.Climb1mCurves, player.visualTransform!.rotation, 0F, 0.9F,
                                            init: (info, rigidbody, player) => {
                                                player.RandomizeMirroredFlag();
                                                player.CrossFadeState(PlayerAbility.CLIMB_1M);
                                            },
                                            update: (interval, inputData, info, rigidbody, player) =>
                                                info.Moving = inputData.horInputNormalized != Vector2.zero
                                        )
                                } );
                    }
                }
            }

            Vector3 moveVelocity = rigidbody.velocity;
            moveVelocity.y = Mathf.Max(rigidbody.velocity.y, ability.MaxFallSpeed);

            // Apply new velocity to rigidbody
            info.MoveVelocity = moveVelocity;
            
            if (info.Grounded) // Restore stamina
                info.StaminaLeft = Mathf.MoveTowards(info.StaminaLeft, ability.MaxStamina, interval * ability.StaminaRestore);

            // Leave stamina value unchanged
        }

        public bool ShouldEnter(PlayerUserInputData inputData, PlayerStatus info)
        {
            if (!info.Spectating && !info.Grounded && !info.OnWall && !info.InLiquid)
                return true;
            
            return false;
        }

        public bool ShouldExit(PlayerUserInputData inputData, PlayerStatus info)
        {
            if (info.Spectating || info.Grounded || info.OnWall || info.InLiquid)
                return true;
            
            return false;
        }

        public override string ToString() => "Fall";

    }
}