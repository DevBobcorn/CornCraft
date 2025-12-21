#nullable enable
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CraftSharp.Control
{
    public class GroundedState : IPlayerState
    {
        private const float THRESHOLD_CLIMB_1M = 1.1F;
        private const float THRESHOLD_WALK_UP  = 0.6F;
        private const float THRESHOLD_CLIMB_UP = 0.4F;
        
        private const float THRESHOLD_LIQUID_SINK = -0.6F;
        private const float SPRINT_STAMINA_START_MIN = 5F;
        private const float SPRINT_STAMINA_STOP = 1F;

        private bool _jumpRequested = false;
        private bool _sneakToggleRequested = false;
        private bool _sprintRequested = false;

        private float _timeSinceGrounded = -1F;

        public void UpdateMain(ref Vector3 currentVelocity, float interval, PlayerActions inputData, PlayerStatus info, PlayerController player)
        {
            var ability = player.AbilityConfig;

            // Reset gliding state
            info.Gliding = false;

            // Update grounded timer
            _timeSinceGrounded += interval;

            // Check sneak toggle request
            if (_sneakToggleRequested)
            {
                player.ToggleSneaking();

                _sneakToggleRequested = false;
            }

            // Movement velocity update
            Vector3 moveVelocity;

            if (_jumpRequested) // Jump
            {
                // Update current yaw to target yaw, immediately
                info.CurrentVisualYaw = info.TargetVisualYaw;

                // Apply vertical velocity to horizontal velocity
                moveVelocity = currentVelocity + player.transform.up * 9F /* ability.JumpSpeedCurve.Evaluate(currentVelocity.magnitude) */;

                _jumpRequested = false;
            }
            else // Stay on ground
            {
                // Update moving status
                bool prevMoving = info.Moving;
                info.Moving = inputData.Locomotion.Movement.IsPressed();

                // Animation mirror randomization
                if (info.Moving != prevMoving)
                {
                    player.RandomizeMirroredFlag();
                }

                if (info.Moving) // Moving
                {
                    // Initiate sprinting check
                    if (info.Moving && !info.Sneaking && _sprintRequested && info.StaminaLeft > SPRINT_STAMINA_START_MIN)
                    {
                        info.Sprinting = true;
                    }

                    if (info.Sprinting)
                    {
                        if (!inputData.Locomotion.Sprint.IsPressed() || info.StaminaLeft <= SPRINT_STAMINA_STOP)
                        {
                            info.Sprinting = false;
                        }
                    }

                    var moveSpeed = info.Sprinting ? ability.SprintSpeed : info.Sneaking ? ability.SneakSpeed : ability.WalkSpeed;
                    
                    _sprintRequested = false;

                    // Smooth rotation for player model
                    info.CurrentVisualYaw = Mathf.MoveTowardsAngle(Mathf.LerpAngle(info.CurrentVisualYaw,
                            info.TargetVisualYaw, 0.2F), info.TargetVisualYaw, ability.TurnSpeed * interval);
                    
                    // Use target orientation to calculate actual movement direction, taking ground shape into consideration
                    moveVelocity = player.GetMovementOrientation() * Vector3.forward * moveSpeed;
                }
                else // Idle or braking
                {
                    // Reset sprinting state
                    info.Sprinting = false;
                    moveVelocity = Vector3.zero;
                    
                    _sprintRequested = false;
                }
            }
            
            // Apply gravity
            moveVelocity += Physics.gravity * (info.GravityScale * 3F * interval);

            currentVelocity = moveVelocity;

            // Stamina update
            if (info.Sprinting)
            {
                // Consume stamina
                info.StaminaLeft = Mathf.MoveTowards(info.StaminaLeft, 0F, interval * ability.SprintStaminaCost);
            }
            else
            {
                // Restore stamina
                info.StaminaLeft = Mathf.MoveTowards(info.StaminaLeft, ability.MaxStamina, interval * ability.StaminaRestore);
            }
        }

        public bool ShouldEnter(PlayerActions inputData, PlayerStatus info)
        {
            return !info.Spectating && info.Grounded && !info.Clinging && !info.Floating;
        }

        public bool ShouldExit(PlayerActions inputData, PlayerStatus info)
        {
            return info.Spectating || !info.Grounded || info.Clinging || info.Floating;
        }

        private Action<InputAction.CallbackContext>? jumpRequestCallback;
        private Action<InputAction.CallbackContext>? walkToggleRequestCallback;
        private Action<InputAction.CallbackContext>? sprintRequestCallback;

        public void OnEnter(IPlayerState prevState, PlayerStatus info, PlayerController player)
        {
            info.Sprinting = false;

            // Reset request flags
            _jumpRequested = false;
            _sneakToggleRequested = false;
            _sprintRequested = player.Actions.Locomotion.Sprint.IsPressed();

            player.Actions.Locomotion.Jump.performed += jumpRequestCallback = _ =>
            {
                // Set jump flag
                _jumpRequested = true;
            };

            player.Actions.Locomotion.WalkToggle.performed += walkToggleRequestCallback = _ =>
            {
                // Set walk toggle flag
                _sneakToggleRequested = true;
            };

            player.Actions.Locomotion.Sprint.performed += sprintRequestCallback = _ =>
            {
                if (player.IsUsingAimingCamera() && Mathf.Abs(
                    Mathf.DeltaAngle(info.CurrentVisualYaw, info.MovementInputYaw)) > 30F)
                {
                    return; // Aiming and not moving forward
                }

                // Set sprint flag
                _sprintRequested = true;
            };

            _timeSinceGrounded = Mathf.Max(_timeSinceGrounded, 0F);
        }

        public void OnExit(IPlayerState nextState, PlayerStatus info, PlayerController player)
        {
            info.Sprinting = false;

            // Unregister input action events
            player.Actions.Locomotion.Jump.performed -= jumpRequestCallback;
            player.Actions.Locomotion.WalkToggle.performed -= walkToggleRequestCallback;
            player.Actions.Locomotion.Sprint.performed -= sprintRequestCallback;
        }

        public override string ToString() => "Grounded";
    }
}