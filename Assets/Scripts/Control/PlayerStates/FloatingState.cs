#nullable enable
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CraftSharp.Control
{
    public class FloatingState : IPlayerState
    {
        private bool _flightRequested = false;

        public void UpdateMain(ref Vector3 currentVelocity, float interval, PlayerActions inputData, PlayerStatus info, PlayerController player)
        {
            var ability = player.AbilityConfig;
            
            info.Sprinting = false;
            info.Gliding = false;

            var swimSpeed = ability.SwimSpeed;
            var flightSpeed = ability.GlideSpeed;

            if (_flightRequested)
            {
                info.Flying = info.GameMode == GameMode.Creative;
                _flightRequested = false;
            }

            info.JumpTime += interval;

            Vector3 moveVelocity = Vector3.zero;

            // Update moving status
            var prevMoving = info.Moving;
            info.Moving = inputData.Locomotion.Movement.IsPressed();

            // Animation mirror randomization
            if (info.Moving != prevMoving)
            {
                player.RandomizeMirroredFlag();
            }

            // Check vertical movement...
            var distToAfloat = PlayerStatusUpdater.ABOVE_LIQUID_HEIGHT_WHEN_FLOATING - info.LiquidDistFromHead;
            var targetVerticalVelocity = 0F;

            if (inputData.Locomotion.Ascend.IsPressed())
            {
                if (distToAfloat > 0F) // Can go up
                {
                    targetVerticalVelocity = (info.Flying ? flightSpeed : swimSpeed);
                }
            }
            else if (inputData.Locomotion.Descend.IsPressed())
            {
                if (!info.Grounded)
                {
                    targetVerticalVelocity = -(info.Flying ? flightSpeed : swimSpeed);
                }
            }

            // Check horizontal movement...
            if (info.Moving)
            {
                // Smooth rotation for player model
                info.CurrentVisualYaw = Mathf.MoveTowardsAngle(info.CurrentVisualYaw, info.TargetVisualYaw, ability.TurnSpeed * interval * 0.5F);
                
                // Smoothly reach target horizontal direction/speed
                var targetMoveVelocity = player.GetMovementOrientation() * Vector3.forward * (info.Flying ? flightSpeed : swimSpeed);
                var currentHorizontalVelocity = currentVelocity;
                currentHorizontalVelocity.y = 0F;
                moveVelocity += Vector3.Lerp(currentHorizontalVelocity, targetMoveVelocity, 0.2F);
            }
            else // No horizontal input, smoothly brake
            {
                var currentHorizontalVelocity = currentVelocity;
                currentHorizontalVelocity.y = 0F;
                moveVelocity += Vector3.Lerp(currentHorizontalVelocity, Vector3.zero, 0.2F);
            }

            // Smooth vertical movement while floating/flying
            moveVelocity.y = Mathf.Lerp(currentVelocity.y, targetVerticalVelocity, 0.2F);

            // Apply gravity (nonexistent)

            currentVelocity = moveVelocity;
            
            // Consume stamina (nonexistent)
        }

        public bool ShouldEnter(PlayerActions inputData, PlayerStatus info)
        {
            return !info.Spectating && info.Floating;
        }

        public bool ShouldExit(PlayerActions inputData, PlayerStatus info)
        {
            return info.Spectating || !info.Floating;
        }

        private Action<InputAction.CallbackContext>? flightToggleRequestCallback;

        public void OnEnter(IPlayerState prevState, PlayerStatus info, PlayerController player)
        {
            info.Sprinting = false;
            
            // Register input action events
            player.Actions.Locomotion.Jump.performed += flightToggleRequestCallback = _ =>
            {
                if (info.GameMode == GameMode.Creative)
                {
                    if (!info.Flying)
                    {
                        if (info.JumpTime <= AirborneState.FLIGHT_START_TIMEOUT_MAX)
                        {
                            _flightRequested = true;
                        }
                    }
                    else
                    {
                        if (info.JumpTime <= AirborneState.FLIGHT_STOP_TIMEOUT_MAX)
                        {
                            info.Flying = false; // Stop flight
                        }
                    }
                    info.JumpTime = 0F;
                }
            };
        }

        public void OnExit(IPlayerState nextState, PlayerStatus info, PlayerController player)
        {
            if (nextState is not AirborneState and not FloatingState)
            {
                info.Flying = false;

                // Set jump time to infinity
                info.JumpTime = float.PositiveInfinity;

                // And reset air time
                info.AirTime = 0F;
            }

            // Unregister input action events
            player.Actions.Locomotion.Jump.performed -= flightToggleRequestCallback;
        }

        public override string ToString() => "Floating";
    }
}