#nullable enable
using System;
using KinematicCharacterController;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CraftSharp.Control
{
    public class AirborneState : IPlayerState
    {
        public const float THRESHOLD_CLIMB_2M = -2.05F;
        public const float THRESHOLD_CLIMB_1M = -1.55F;
        public const float THRESHOLD_CLIMB_UP = -1.35F;

        public const float THRESHOLD_ANGLE_FORWARD = 40F;

        private bool _glideToggleRequested = false;

        public void UpdateMain(ref Vector3 currentVelocity, float interval, PlayerActions inputData, PlayerStatus info, KinematicCharacterMotor motor, PlayerController player)
        {
            var ability = player.Ability;

            // Check toggle gliding
            if (_glideToggleRequested)
            {
                if (!info.Gliding)
                {
                    // Not so low above the ground, and we have stamina left
                    if (info.CenterDownDist > 2F && info.StaminaLeft > 0.01F)
                    {
                        player.RandomizeMirroredFlag();
                        info.Gliding = true;
                    }
                }
                else
                {
                    info.Gliding = false;
                }

                _glideToggleRequested = false;
            }

            // Check stamina for gliding
            if (info.StaminaLeft == 0)
            {
                // Out of stamina, we're gonna FALL!
                info.Gliding = false;
            }

            // Update moving status before exit, for smooth transition into other states
            info.Moving = inputData.Gameplay.Movement.IsPressed();

            // Movement velocity update
            Vector3 moveVelocity;

            if (info.Gliding) // Gliding
            {
                if (info.Moving) // Trying to move
                {
                    // Smooth rotation for player model
                    info.CurrentVisualYaw = Mathf.MoveTowardsAngle(info.CurrentVisualYaw, info.TargetVisualYaw, ability.TurnSpeed * interval * 0.5F);
                    // Horizontal speed
                    moveVelocity = Quaternion.AngleAxis(info.TargetVisualYaw, motor.CharacterUp) * Vector3.forward * ability.GlideSpeed;
                }
                else // No horizontal movement
                {
                    moveVelocity = Vector3.zero;
                }

                // Apply gravity (Not additive when gliding)
                moveVelocity += info.GravityScale * 1.4f * interval * Physics.gravity;
            }
            else // Falling
            {
                // Apply gravity
                moveVelocity = currentVelocity - info.GravityScale * 12f * interval * motor.CharacterUp;
                
                // Speed limit check
                if (moveVelocity.magnitude > ability.MaxFallSpeed)
                {
                    moveVelocity = moveVelocity.normalized * ability.MaxFallSpeed;
                }

                // Landing check, positive when ground is near and character is going downwards
                if (info.CenterDownDist < 0.2F && Vector3.Dot(moveVelocity, motor.CharacterUp) <= 0)
                {
                    info.Grounded = true;
                    info.TimeSinceGrounded = 0F;
                }
            }

            currentVelocity = moveVelocity;

            // Stamina update
            if (info.Gliding)
            {
                // Consume stamina
                info.StaminaLeft = Mathf.MoveTowards(info.StaminaLeft, 0F, interval * ability.GlideStaminaCost);
            }
            // Otherwise leave stamina value unchanged
        }

        public bool ShouldEnter(PlayerActions inputData, PlayerStatus info)
        {
            if (!info.Spectating && !info.Grounded && !info.Clinging && !info.Floating)
                return true;
            
            return false;
        }

        public bool ShouldExit(PlayerActions inputData, PlayerStatus info)
        {
            if (info.Spectating || info.Grounded || info.Clinging || info.Floating)
                return true;
            
            return false;
        }

        private Action<InputAction.CallbackContext>? glideToggleRequestCallback;

        public void OnEnter(PlayerStatus info, KinematicCharacterMotor motor, PlayerController player)
        {
            info.Sprinting = false;

            // Reset request flags
            _glideToggleRequested = false;

            // Register input action events
            player.Actions.Gameplay.Jump.performed += glideToggleRequestCallback = (context) =>
            {
                _glideToggleRequested = true;
            };
        }

        public void OnExit(PlayerStatus info, KinematicCharacterMotor motor, PlayerController player)
        {
            // Unregister input action events
            player.Actions.Gameplay.Jump.performed -= glideToggleRequestCallback;
        }

        public override string ToString() => "Airborne";
    }
}