#nullable enable
using UnityEngine;

namespace CraftSharp.Control
{
    public class SpectateState : IPlayerState
    {
        public void UpdatePlayer(float interval, PlayerUserInputData inputData, PlayerStatus info, Rigidbody rigidbody, PlayerController player)
        {
            var ability = player.Ability;

            info.Sprinting = false;

            Vector3 moveVelocity = Vector3.zero;

            if (inputData.HorInputNormalized != Vector2.zero)
            {
                info.Moving = true;

                // Smooth rotation for player model
                info.CurrentVisualYaw = info.TargetVisualYaw;

                var moveSpeed = info.WalkMode ? ability.WalkSpeed : ability.RunSpeed;

                // Use the target visual yaw as actual movement direction, y speed is set to 0 by this point
                moveVelocity = Quaternion.AngleAxis(info.TargetVisualYaw, Vector3.up) * Vector3.forward * moveSpeed;
            }
            else
                info.Moving = false;

            // Check vertical movement...
            if (inputData.Ascend)
                moveVelocity.y =  ability.WalkSpeed * 3F;
            else if (inputData.Descend)
                moveVelocity.y = -ability.WalkSpeed * 3F;

            // Apply new velocity to rigidbody
            info.MoveVelocity = moveVelocity;
        }

        public bool ShouldEnter(PlayerUserInputData inputData, PlayerStatus info) => info.Spectating;

        public bool ShouldExit(PlayerUserInputData inputData, PlayerStatus info)  => !info.Spectating;

        public override string ToString() => "Spectate";

    }
}