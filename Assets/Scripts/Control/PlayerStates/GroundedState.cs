#nullable enable
using System;
using KinematicCharacterController;
using UnityEngine;
using UnityEngine.InputSystem;

using CraftSharp.Rendering;

namespace CraftSharp.Control
{
    public class GroundedState : IPlayerState
    {
        private const float THRESHOLD_CLIMB_1M = 1.1F;
        private const float THRESHOLD_WALK_UP  = 0.6F;
        private const float THRESHOLD_CLIMB_UP = 0.4F;
        
        private const float THRESHOLD_LIQUID_SINK = -0.6F;
        private const float SPRINT_BRAKE_TIME = 0.4F;
        private const float SPRINT_STAMINA_START_MIN = 5F;
        private const float SPRINT_STAMINA_STOP = 1F;

        private bool _jumpRequested = false;
        private bool _jumpConfirmed = false;
        private bool _walkToggleRequested = false;
        private bool _sprintRequested = false;

        private float _timeSinceGrounded = -1F;

        public static string GetEntryAnimatorStateName(PlayerStatus info)
        {
            if (info.Moving)
            {
                if (info.Sprinting)
                {
                    return AnimatorEntityRender.SPRINT_NAME;
                }
                return info.WalkMode ? AnimatorEntityRender.WALK_NAME : AnimatorEntityRender.RUN_NAME;
            }
            return AnimatorEntityRender.IDLE_NAME;
        }
        
        public static float DistanceToSquareSide(Vector2 direction, float halfSideLength)
        {
            // Handle edge cases where direction aligns exactly with axes
            if (direction.x == 0) return halfSideLength / Math.Abs(direction.y);
            if (direction.y == 0) return halfSideLength / Math.Abs(direction.x);

            // Calculate the angle in the first quadrant (0 to π/2)
            var absX = Mathf.Abs(direction.x);
            var absY = Mathf.Abs(direction.y);
        
            // The distance is halfSideLength divided by the maximum of the absolute components
            // or equivalently, halfSideLength divided by the appropriate component
            return halfSideLength / Mathf.Max(absX, absY);
        }

        private void CheckClimbOver(PlayerStatus info, PlayerController player)
        {
            var yawRadian = info.TargetVisualYaw * Mathf.Deg2Rad;
            var dirVector = new Vector2(Mathf.Sin(yawRadian), Mathf.Cos(yawRadian));
            var maxDist = DistanceToSquareSide(dirVector, player.AbilityConfig.ClimbOverMaxDist);
            
            if (info is { Moving: true, BarrierHeight: > THRESHOLD_CLIMB_UP and < THRESHOLD_CLIMB_1M } &&
                info.BarrierDistance < maxDist && info.WallDistance - info.BarrierDistance > 0.7F) // Climb up platform
            {
                if (info.YawDeltaAbs <= 10F) // Trying to moving forward
                {
                    // Workaround: Use a cooldown value to disable climbing in a short period after landing
                    if (_timeSinceGrounded > 0.3F)
                    {
                        player.ClimbOverBarrier(info.BarrierDistance, info.BarrierHeight, info.BarrierHeight < THRESHOLD_WALK_UP, false);
                    }

                    // Prevent jump preparation if climbing is successfully initiated, or only timer is not ready
                    _jumpRequested = false;
                }
            }
        }

        public void UpdateBeforeMotor(float interval, PlayerActions inputData, PlayerStatus info, KinematicCharacterMotor motor, PlayerController player)
        {
            if (info.Grounded)
            {
                CheckClimbOver(info, player);

                if (_jumpRequested) // Jump
                {
                    // Set jump confirmation flag
                    _jumpConfirmed = true;

                    // Makes the character skip ground probing/snapping on its next update
                    motor.ForceUnground();
                    // Randomize mirror flag before jumping
                    player.RandomizeMirroredFlag();

                    string stateName = "JumpFor" + GetEntryAnimatorStateName(info);

                    // Go to jump state
                    player.StartCrossFadeState(stateName, 0.1F);

                    // Also reset grounded flag
                    info.Grounded = false;
                }

                // Reset jump flag
                _jumpRequested = false;
            }
        }

        public void UpdateMain(ref Vector3 currentVelocity, float interval, PlayerActions inputData, PlayerStatus info, KinematicCharacterMotor motor, PlayerController player)
        {
            var ability = player.AbilityConfig;

            // Reset gliding state
            info.Gliding = false;

            // Update grounded timer
            _timeSinceGrounded += interval;

            // Check walk toggle request
            if (_walkToggleRequested)
            {
                player.ToggleWalkMode();

                _walkToggleRequested = false;
            }

            // Movement velocity update
            Vector3 moveVelocity;

            if (_jumpConfirmed) // Jump
            {
                // Update current yaw to target yaw, immediately
                info.CurrentVisualYaw = info.TargetVisualYaw;

                // Apply vertical velocity to reduced horizontal velocity
                moveVelocity = currentVelocity * 0.7F + motor.CharacterUp * ability.JumpSpeedCurve.Evaluate(currentVelocity.magnitude);
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
                    if (info.Moving && _sprintRequested && info.StaminaLeft > SPRINT_STAMINA_START_MIN)
                    {
                        info.Sprinting = true;
                        info.SprintBrakeTime = SPRINT_BRAKE_TIME;
                    }

                    if (info.Sprinting)
                    {
                        if ((!inputData.Locomotion.Sprint.IsPressed() || info.StaminaLeft <= SPRINT_STAMINA_STOP))
                        {
                            info.Sprinting = false;
                        }
                        info.SprintBrakeTime = SPRINT_BRAKE_TIME;
                    }

                    var moveSpeed = Mathf.Lerp(info.WalkMode ? ability.WalkSpeed : ability.RunSpeed,
                        ability.SprintSpeed, Mathf.Max(info.SprintBrakeTime, 0F) / SPRINT_BRAKE_TIME);
                    
                    info.SprintBrakeTime -= interval;
                    _sprintRequested = false;
                    
                    // Workaround: Slow down when walking downstairs
                    if (!motor.GroundingStatus.FoundAnyGround)
                    {
                        moveSpeed *= info.Moving ? (info.Sprinting ? 0.8F : 0.35F) : 1F;
                    }
                    else if (_timeSinceGrounded is >= 0F and < 0.5F)
                    {
                        moveSpeed *= _timeSinceGrounded / 0.5F;
                    }

                    // Smooth rotation for player model
                    info.CurrentVisualYaw = Mathf.MoveTowardsAngle(Mathf.LerpAngle(info.CurrentVisualYaw,
                            info.TargetVisualYaw, 0.2F), info.TargetVisualYaw, ability.TurnSpeed * interval);
                    
                    // Use target orientation to calculate actual movement direction, taking ground shape into consideration
                    moveVelocity = motor.GetDirectionTangentToSurface(player.GetMovementOrientation() * Vector3.forward, motor.GroundingStatus.GroundNormal) * moveSpeed;

                    // Smooth movement (if accelerating)
                    if (moveVelocity.sqrMagnitude > currentVelocity.sqrMagnitude)
                    {
                        float accFactor;
                        if (currentVelocity == Vector3.zero)
                        {
                            // Accelerate slowly
                            accFactor = ability.AccSpeed * interval;
                        }
                        else
                        {
                            var angleDelta = Vector3.Angle(moveVelocity, currentVelocity);
                            // Accelerate immediately if making a sharp turn, accelerate slowly if not
                            accFactor = Mathf.Lerp(ability.AccSpeed * interval, (moveVelocity - currentVelocity).magnitude, angleDelta / 180F);
                        }
                        
                        moveVelocity = Vector3.MoveTowards(currentVelocity, moveVelocity, accFactor);
                    }
                }
                else // Idle or braking
                {
                    // Reset sprinting state
                    if (info.Sprinting)
                    {
                        info.Sprinting = false;
                        info.SprintBrakeTime = SPRINT_BRAKE_TIME;
                        moveVelocity = currentVelocity;
                    }

                    if (info.SprintBrakeTime > 0F)
                    {
                        var moveSpeed = Mathf.Lerp(info.WalkMode ? ability.WalkSpeed : ability.RunSpeed,
                            ability.SprintSpeed, Mathf.Max(info.SprintBrakeTime, 0F) / SPRINT_BRAKE_TIME);
                    
                        info.SprintBrakeTime -= interval;

                        // Use target orientation to calculate actual movement direction, taking ground shape into consideration
                        moveVelocity = motor.GetDirectionTangentToSurface(player.GetMovementOrientation() * Vector3.forward, motor.GroundingStatus.GroundNormal) * moveSpeed;
                    }
                    else
                    {
                        // Smooth deceleration
                        moveVelocity = Vector3.MoveTowards(currentVelocity, Vector3.zero, ability.DecSpeed * interval);
                    }
                    _sprintRequested = false;
                }

                // Workaround: Used when fake grounded status is active (to avoid airborne state when moving off a block)
                if (!motor.GroundingStatus.FoundAnyGround && (!info.InLiquid || info.LiquidDist > THRESHOLD_LIQUID_SINK))
                {
                    // Apply fake gravity
                    moveVelocity -= motor.CharacterUp * 5F;
                }
            }

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

        public void OnEnter(IPlayerState prevState, PlayerStatus info, KinematicCharacterMotor motor, PlayerController player)
        {
            info.Sprinting = false;

            // Reset request flags
            _jumpRequested = false;
            _jumpConfirmed = false;
            _walkToggleRequested = false;
            _sprintRequested = player.Actions.Locomotion.Sprint.IsPressed();

            player.Actions.Locomotion.Jump.performed += jumpRequestCallback = _ =>
            {
                // Set jump flag
                _jumpRequested = true;
            };

            player.Actions.Locomotion.WalkToggle.performed += walkToggleRequestCallback = _ =>
            {
                // Set walk toggle flag
                _walkToggleRequested = true;
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

            if (prevState is not ForceMoveState)
            {
                var stateName = prevState == PlayerStates.AIRBORNE ?
                    AnimatorEntityRender.LANDING_NAME :
                    GetEntryAnimatorStateName(info);

                player.StartCrossFadeState(stateName, 0.1F);

                _timeSinceGrounded = 0F;
            }
            else
            {
                _timeSinceGrounded = Mathf.Max(_timeSinceGrounded, 0F);
            }
        }

        public void OnExit(IPlayerState nextState, PlayerStatus info, KinematicCharacterMotor motor, PlayerController player)
        {
            info.Sprinting = false;

            // Unregister input action events
            player.Actions.Locomotion.Jump.performed -= jumpRequestCallback;
            player.Actions.Locomotion.WalkToggle.performed -= walkToggleRequestCallback;
            player.Actions.Locomotion.Sprint.performed -= sprintRequestCallback;

            // Ungrounded, go to falling state
            if (nextState == PlayerStates.AIRBORNE && !info.Grounded && !_jumpConfirmed)
            {
                // Make sure it isn't jumping
                player.StartCrossFadeState(AnimatorEntityRender.FALLING_NAME);
            }

            // Reset jump confirmation flag
            _jumpConfirmed = false;
        }

        public override string ToString() => "Grounded";
    }
}