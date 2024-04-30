#nullable enable
using UnityEngine;

namespace CraftSharp.Control
{
    public class SpectateState : IPlayerState
    {
        public void UpdatePlayer(float interval, PlayerActions inputData, PlayerStatus info, Rigidbody rigidbody, PlayerController player)
        {
            var ability = player.Ability;

            info.Sprinting = false;

            Vector3 moveVelocity = Vector3.zero;

            if (inputData.Gameplay.Movement.IsPressed())
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
            if (inputData.Gameplay.Ascend.IsPressed())
                moveVelocity.y =  ability.WalkSpeed * 3F;
            else if (inputData.Gameplay.Descend.IsPressed())
                moveVelocity.y = -ability.WalkSpeed * 3F;
            else
                moveVelocity.y = 0F;

            // Apply new velocity to rigidbody
            info.MoveVelocity = moveVelocity;
        }

        public bool ShouldEnter(PlayerActions inputData, PlayerStatus info) => info.Spectating;

        public bool ShouldExit(PlayerActions inputData, PlayerStatus info)  => !info.Spectating;

        public override string ToString() => "Spectate";

    }
}