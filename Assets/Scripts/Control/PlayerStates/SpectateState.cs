#nullable enable
using KinematicCharacterController;
using UnityEngine;

using CraftSharp.Rendering;

namespace CraftSharp.Control
{
    public class SpectateState : IPlayerState
    {
        public void UpdateMain(ref Vector3 currentVelocity, float interval, PlayerActions inputData, PlayerStatus info, KinematicCharacterMotor motor, PlayerController player)
        {
            var ability = player.AbilityConfig;

            // Sprinting and moving downwards uses the same key, so we don't use it
            info.Sprinting = false;

            Vector3 moveVelocity = Vector3.zero;

            if (inputData.Locomotion.Movement.IsPressed())
            {
                info.Moving = true;

                // Update orientation for player model
                info.CurrentVisualYaw = info.TargetVisualYaw;

                var moveSpeed = info.WalkMode ? ability.WalkSpeed : ability.RunSpeed;

                // Use target orientation to calculate actual movement direction, y speed is set to 0 by this point
                moveVelocity = player.GetMovementOrientation() * Vector3.forward * moveSpeed;
            }
            else
            {
                info.Moving = false;
            }

            // Check vertical movement...
            if (inputData.Locomotion.Ascend.IsPressed())
                moveVelocity += ability.WalkSpeed * 3F * motor.CharacterUp;
            else if (inputData.Locomotion.Descend.IsPressed())
                moveVelocity -= ability.WalkSpeed * 3F * motor.CharacterUp;

            currentVelocity = moveVelocity;

            // Stamina should be full, Restore if not
            info.StaminaLeft = Mathf.MoveTowards(info.StaminaLeft, ability.MaxStamina, interval * ability.StaminaRestore);
        }

        public bool ShouldEnter(PlayerActions inputData, PlayerStatus info) => info.Spectating;

        public bool ShouldExit(PlayerActions inputData, PlayerStatus info)  => !info.Spectating;

        public void OnExit(IPlayerState nextState, PlayerStatus info, KinematicCharacterMotor motor, PlayerController player)
        {
            info.Sprinting = false;
        }

        public override string ToString() => "Spectate";
    }
}