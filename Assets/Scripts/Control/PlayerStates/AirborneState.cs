#nullable enable
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CraftSharp.Control
{
    public class AirborneState : IPlayerState
    {
        public const float FLIGHT_START_TIMEOUT_MAX = 0.3F;
        public const float FLIGHT_STOP_TIMEOUT_MAX  = 0.3F;

        public const float STOP_FLYING_MAXIMUM_DIST = 0.1F;

        //private bool _glideToggleRequested = false;
        private bool _flightRequested = false;

        public void UpdateMain(ref Vector3 currentVelocity, float interval, PlayerActions inputData, PlayerStatus info, PlayerController player)
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

            if (_flightRequested)
            {
                info.Flying = info.GameMode == GameMode.Creative;
                _flightRequested = false;
            }

            info.AirTime += interval;
            info.JumpTime += interval;

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
                    
                    // Smoothly reach target horizontal direction/speed
                    var targetMoveVelocity = player.GetMovementOrientation() * Vector3.forward * ability.GlideSpeed;
                    var currentHorizontalVelocity = currentVelocity;
                    currentHorizontalVelocity.y = 0F;
                    moveVelocity = Vector3.Lerp(currentHorizontalVelocity, targetMoveVelocity, 0.25F);
                }
                else // No horizontal movement, smoothly brake
                {
                    var currentHorizontalVelocity = currentVelocity;
                    currentHorizontalVelocity.y = 0F;
                    moveVelocity = Vector3.Lerp(currentHorizontalVelocity, Vector3.zero, 0.15F);
                }

                if (info.Flying)
                {
                    // Smooth vertical movement while flying
                    var targetVerticalVelocity = 0F;
                    if (inputData.Locomotion.Ascend.IsPressed())
                        targetVerticalVelocity = ability.SneakSpeed * 3F;
                    else if (inputData.Locomotion.Descend.IsPressed())
                        targetVerticalVelocity = -ability.SneakSpeed * 3F;

                    moveVelocity.y = Mathf.Lerp(currentVelocity.y, targetVerticalVelocity, 0.25F);

                    // Flying doesn't have any gravity, which can prevent proper ground-check
                    // So here we stop flying when getting close enough to the ground
                    if (info.GroundDistFromFeet < STOP_FLYING_MAXIMUM_DIST)
                    {
                        info.Flying = false;
                    }
                }
                else
                {
                    // Apply gravity when gliding and not flying (Not additive when gliding)
                    moveVelocity += info.GravityScale * 3F * interval * Physics.gravity;
                }
            }
            else // Falling
            {
                // Apply gravity
                moveVelocity = currentVelocity + Physics.gravity * (info.GravityScale * 3F * interval);
                
                // Speed limit check
                if (moveVelocity.magnitude > ability.MaxFallSpeed)
                {
                    moveVelocity = moveVelocity.normalized * ability.MaxFallSpeed;
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

        private Action<InputAction.CallbackContext>? flightToggleRequestCallback;

        public void OnEnter(IPlayerState prevState, PlayerStatus info, PlayerController player)
        {
            info.Sprinting = false;
            info.Gliding = false;
            
            // Reset air time but not jump time
            info.AirTime = 0F;

            // Reset request flags
            //_glideToggleRequested = false;
            _flightRequested = false;

            // Register input action events
            player.Actions.Locomotion.Jump.performed += flightToggleRequestCallback = _ =>
            {
                if (info.GameMode == GameMode.Creative)
                {
                    if (!info.Flying)
                    {
                        if (info.JumpTime <= FLIGHT_START_TIMEOUT_MAX)
                        {
                            _flightRequested = true;
                        }
                    }
                    else
                    {
                        if (info.JumpTime <= FLIGHT_STOP_TIMEOUT_MAX)
                        {
                            info.Flying = false; // Stop flight
                        }
                    }
                    info.JumpTime = 0F;
                }
                
                //_glideToggleRequested = true;
            };
        }

        public void OnExit(IPlayerState nextState, PlayerStatus info, PlayerController player)
        {
            if (nextState is not AirborneState) // Preserve flying & gliding status when switching player renders
            {
                info.Gliding = false;

                // Preserve flying status when switching to floating state
                if (nextState is not FloatingState)
                {
                    info.Flying = false;

                    // Set jump time to infinity
                    info.JumpTime = float.PositiveInfinity;

                    // And reset air time
                    info.AirTime = 0F;
                }
            }

            // Unregister input action events
            player.Actions.Locomotion.Jump.performed -= flightToggleRequestCallback;
        }

        public override string ToString() => "Airborne";
    }
}