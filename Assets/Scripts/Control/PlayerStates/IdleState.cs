#nullable enable
using UnityEngine;

namespace MinecraftClient.Control
{
    public class IdleState : IPlayerState
    {
        public void UpdatePlayer(float interval, PlayerUserInputData inputData, PlayerStatus info, PlayerAbility ability, Rigidbody rigidbody, PlayerController player)
        {
            info.Sprinting = false;

            rigidbody.velocity = Vector3.zero;

            if (inputData.horInputNormalized != Vector2.zero) // Start moving
                info.Moving = true;
            else
                info.Moving = false;
            
            if (inputData.ascend) // Jump in place
                rigidbody.velocity = new(0F, ability.JumpSpeed, 0F);
            
            if (inputData.descend)
            {
                var moveHorDir = Quaternion.AngleAxis(info.TargetVisualYaw, Vector3.up) * Vector3.forward;
                        var horOffset = info.BarrierDist - 1.0F;

                        var org  = rigidbody.transform.position;
                        var dest = org + Vector3.up * 2;

                        player.StartForceMoveOperation("Climb over barrier",
                                new ForceMoveOperation[] {
                                        new(org,  dest, 0.1F),
                                        new(dest, player.visualTransform!.rotation, 0F, 1F,
                                            init: (info, ability, rigidbody, player) => {
                                                rigidbody.isKinematic = true;
                                                rigidbody.detectCollisions = false;

                                                player.CrossFadeState(PlayerAbility.CLIMB_1M);
                                                player.UseRootMotion = true;
                                            },
                                            update: (interval, inputData, info, ability, rigidbody, player) =>
                                                info.Moving = inputData.horInputNormalized != Vector2.zero,
                                            exit: (info, ability, rigidbody, player) => {
                                                rigidbody.isKinematic = false;
                                                rigidbody.detectCollisions = true;

                                                player.UseRootMotion = false;
                                            }
                                        )
                                } );
            }

            // Restore stamina
            info.StaminaLeft = Mathf.MoveTowards(info.StaminaLeft, ability.MaxStamina, interval * ability.StaminaRestore);
        }

        public bool ShouldEnter(PlayerStatus info)
        {
            if (!info.Spectating && info.Grounded && !info.OnWall && !info.InLiquid && !info.Moving)
                return true;
            return false;
        }

        public bool ShouldExit(PlayerStatus info)
        {
            if (info.Spectating)
                return true;
            
            if (!info.Grounded || info.OnWall || info.InLiquid || info.Moving)
                return true;
            return false;
        }

        public override string ToString() => "Idle";

    }
}