#nullable enable
using UnityEngine;

namespace MinecraftClient.Control
{
    public class SpectateState : IPlayerState
    {
        public void UpdatePlayer(float interval, PlayerUserInputData inputData, PlayerStatus info, PlayerAbility ability, Rigidbody rigidbody)
        {
            info.Sprinting = false;

            Vector3 moveVelocity = Vector3.zero;

            if (inputData.horInputNormalized != Vector2.zero)
            {
                info.Moving = true;

                // Smooth rotation for player model
                info.CurrentVisualYaw = Mathf.LerpAngle(info.CurrentVisualYaw, info.TargetVisualYaw, ability.SteerSpeed * interval);

                var moveSpeed = info.WalkMode ? ability.WalkSpeed : ability.RunSpeed;

                // Use the target visual yaw as actual movement direction, y speed is set to 0 by this point
                moveVelocity = Quaternion.AngleAxis(info.TargetVisualYaw, Vector3.up) * Vector3.forward * moveSpeed;
            }
            else
                info.Moving = false;

            // Check vertical movement...
            if (inputData.ascend)
                moveVelocity.y =  ability.WalkSpeed * 3F;
            else if (inputData.descend)
                moveVelocity.y = -ability.WalkSpeed * 3F;

            // Apply movement...
            rigidbody.velocity = moveVelocity;
        }

        public bool ShouldEnter(PlayerStatus info) => info.Spectating;

        public bool ShouldExit(PlayerStatus info)  => !info.Spectating;

        public override string ToString() => "Spectate";

    }
}