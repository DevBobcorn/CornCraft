#nullable enable
using System;
using KinematicCharacterController;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CraftSharp.Control
{
    public class AirborneState : IPlayerState
    {
        public const float FLIGHT_START_AIRTIME_MAX = 0.3F;
        public const float FLIGHT_STOP_TIMEOUT_MAX = 0.3F;

        public const float THRESHOLD_ANGLE_FORWARD = 40F;

        //private bool _glideToggleRequested = false;
        private bool _flightRequested = false;

        private float _stopFlightTimer = 0F;

        public void UpdateMain(ref Vector3 currentVelocity, float interval, PlayerActions inputData, PlayerStatus info, KinematicCharacterMotor motor, PlayerController player)
        {
            var ability = player.AbilityConfig;

            /* DISABLE GLIDING FEATURE FOR NOW
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
            */
            
            if (info.GameMode == GameMode.Creative)
            {
                if (_flightRequested)
                    info.Flying = true;
            }
            else
            {
                info.Flying = false;
            }
            _flightRequested = false;
            
            info.AirTime += interval;

            // Check stamina for gliding
            if (info.StaminaLeft == 0)
            {
                // Out of stamina, we're gonna FALL!
                info.Gliding = false;
            }

            // Update moving status before exit, for smooth transition into other states
            info.Moving = inputData.Locomotion.Movement.IsPressed();

            // Movement velocity update
            Vector3 moveVelocity;

            if (info.Gliding || info.Flying) // Gliding or flying
            {
                if (info.Moving) // Trying to move
                {
                    // Smooth rotation for player model
                    info.CurrentVisualYaw = Mathf.MoveTowardsAngle(info.CurrentVisualYaw, info.TargetVisualYaw, ability.TurnSpeed * interval * 0.5F);
                    // Horizontal speed
                    moveVelocity = player.GetMovementOrientation() * Vector3.forward * ability.GlideSpeed;
                }
                else // No horizontal movement
                {
                    moveVelocity = Vector3.zero;
                }

                if (info.Flying)
                {
                    // Accumulate stop flight timer value
                    _stopFlightTimer += interval;
                    
                    // Check vertical movement...
                    if (inputData.Locomotion.Ascend.IsPressed())
                        moveVelocity += ability.WalkSpeed * 3F * motor.CharacterUp;
                    else if (inputData.Locomotion.Descend.IsPressed())
                        moveVelocity -= ability.WalkSpeed * 3F * motor.CharacterUp;
                }
                else
                {
                    // Apply gravity when gliding and not flying (Not additive when gliding)
                    moveVelocity += info.GravityScale * 1.4f * interval * Physics.gravity;
                }
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
            return !info.Spectating && !info.Grounded && !info.Clinging && !info.Floating;
        }

        public bool ShouldExit(PlayerActions inputData, PlayerStatus info)
        {
            return info.Spectating || info.Grounded || info.Clinging || info.Floating;
        }

        private Action<InputAction.CallbackContext>? glideToggleRequestCallback;

        public void OnEnter(IPlayerState prevState, PlayerStatus info, KinematicCharacterMotor motor, PlayerController player)
        {
            info.Sprinting = false;
            info.Gliding = false;
            info.AirTime = 0F;

            // Reset request flags
            //_glideToggleRequested = false;
            _flightRequested = false;

            // Register input action events
            player.Actions.Locomotion.Jump.performed += glideToggleRequestCallback = _ =>
            {
                if (info.GameMode == GameMode.Creative)
                {
                    if (!info.Flying)
                    {
                        if (info.AirTime <= FLIGHT_START_AIRTIME_MAX)
                        {
                            _flightRequested = true;
                        }
                        info.AirTime = 0F; // Reset air time for next flight request check
                    }
                    else
                    {
                        if (_stopFlightTimer <= FLIGHT_STOP_TIMEOUT_MAX)
                        {
                            info.Flying = false; // Stop flight
                        }
                        _stopFlightTimer = 0F;
                    }
                }
                
                //_glideToggleRequested = true;
            };
        }

        public void OnExit(IPlayerState nextState, PlayerStatus info, KinematicCharacterMotor motor, PlayerController player)
        {
            // Unregister input action events
            player.Actions.Locomotion.Jump.performed -= glideToggleRequestCallback;
        }

        public override string ToString() => "Airborne";
    }
}