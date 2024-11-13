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

        private const float SPRINT_START_TIME = 0.9F;

        private bool _jumpRequested = false;
        private bool _jumpConfirmed = false;
        private bool _walkToggleRequested = false;
        private bool _sprintRequested = false;
        private bool _sprintContinues = false;

        private float _timeSinceGrounded = -1F;
        private float _timeSinceSprintStart = -1F;

        public static string GetEntryAnimatorStateName(PlayerStatus info)
        {
            if (info.Moving)
            {
                if (info.Sprinting)
                {
                    return AnimatorEntityRender.SPRINT_NAME;
                }
                else
                {
                    return info.WalkMode ? AnimatorEntityRender.WALK_NAME : AnimatorEntityRender.RUN_NAME;
                }
            }
            else
            {
                return AnimatorEntityRender.IDLE_NAME;
            }
        }

        private void CheckClimbOver(PlayerStatus info, PlayerController player)
        {
            if (info.Moving && info.BarrierHeight > THRESHOLD_CLIMB_UP && info.BarrierHeight < THRESHOLD_CLIMB_1M &&
                    info.BarrierDistance < player.AbilityConfig.ClimbOverMaxDist && info.WallDistance - info.BarrierDistance > 0.7F) // Climb up platform
            {
                bool walkUp = info.BarrierHeight < THRESHOLD_WALK_UP;

                if (walkUp || info.BarrierYawAngle < 30F) // Check if available, for high barriers check cooldown and angle
                {
                    if (info.YawDeltaAbs <= 10F) // Trying to moving forward
                    {
                        // Workround: Use a cooldown value to disable climbing in a short period after landing
                        if (_timeSinceGrounded > 0.3F)
                        {
                            player.ClimbOverBarrier(info.BarrierDistance, info.BarrierHeight, walkUp, false);
                        }

                        // Prevent jump preparation if climbing is successfully initiated, or only timer is not ready
                        _jumpRequested = false;
                    }
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
                    motor.ForceUnground(0.1F);
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
                info.Moving = inputData.Gameplay.Movement.IsPressed();

                // Animation mirror randomation
                if (info.Moving != prevMoving)
                {
                    player.RandomizeMirroredFlag();
                }

                var sprintStarting = info.Sprinting && _timeSinceSprintStart < SPRINT_START_TIME;

                if (info.Moving || sprintStarting) // Moving (or starting to sprint)
                {
                    float moveSpeed;

                    // Initiate sprinting check
                    if (_sprintRequested && info.StaminaLeft > 0F)
                    {
                        info.Sprinting = true;
                        _timeSinceSprintStart = 0F;
                        sprintStarting = true;
                    }

                    // Continue sprinting check
                    if (info.Sprinting && _timeSinceSprintStart >= SPRINT_START_TIME && !_sprintContinues)
                    {
                        if (inputData.Gameplay.Sprint.IsPressed())
                        {
                            _sprintContinues = true;
                        }
                        else
                        {
                            _sprintContinues = false;
                        }
                    }

                    if (sprintStarting || _sprintContinues)
                    {
                        info.Sprinting = true;
                        _timeSinceSprintStart += interval;

                        if (info.Moving)
                        {
                            moveSpeed = ability.SprintSpeed;
                        }
                        else
                        {
                            moveSpeed = Mathf.Lerp(ability.SprintSpeed, 0.4F * ability.SprintSpeed, _timeSinceSprintStart / SPRINT_START_TIME);
                        }
                    }
                    else
                    {
                        info.Sprinting = false;
                        _sprintContinues = false;
                        _timeSinceSprintStart = -1F;

                        moveSpeed = info.WalkMode ? ability.WalkSpeed : ability.RunSpeed;
                    }
                    
                    _sprintRequested = false;
                    
                    // Workaround: Slow down when walking downstairs
                    if (!motor.GroundingStatus.FoundAnyGround)
                    {
                        moveSpeed *= info.Moving ? (info.Sprinting ? 0.8F : 0.35F) : 1F;
                    }
                    else if (_timeSinceGrounded >= 0F && _timeSinceGrounded < 0.5F)
                    {
                        moveSpeed *= _timeSinceGrounded / 0.5F;
                    }

                    // Smooth rotation for player model
                    info.CurrentVisualYaw = Mathf.MoveTowardsAngle(Mathf.LerpAngle(info.CurrentVisualYaw,
                            info.TargetVisualYaw, 0.2F), info.TargetVisualYaw, ability.TurnSpeed * interval);
                    
                    // Use target orientation to calculate actual movement direction, taking ground shape into consideration
                    moveVelocity = motor.GetDirectionTangentToSurface(player.GetTargetOrientation() * Vector3.forward, motor.GroundingStatus.GroundNormal) * moveSpeed;

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
                    info.Sprinting = false;
                    _timeSinceSprintStart = -1F;
                    _sprintRequested = false;
                    _sprintContinues = false;

                    // Smooth deceleration
                    moveVelocity = Vector3.MoveTowards(currentVelocity, Vector3.zero, ability.DecSpeed * interval);
                }

                // Workaround: Used when fake grounded status is active (to avoid airborne state when moving off a block)
                if (!motor.GroundingStatus.FoundAnyGround)
                {
                    // Apply fake gravity
                    if (info.Moving)
                    {
                        moveVelocity -= motor.CharacterUp * 5F;
                    }
                    else
                    {
                        moveVelocity -= motor.CharacterUp;
                    }
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
            if (!info.Spectating && info.Grounded && !info.Clinging && !info.Floating)
                return true;
            
            return false;
        }

        public bool ShouldExit(PlayerActions inputData, PlayerStatus info)
        {
            if (info.Spectating || !info.Grounded || info.Clinging || info.Floating)
                return true;
            
            return false;
        }

        private Action<InputAction.CallbackContext>? chargedAttackCallback;
        private Action<InputAction.CallbackContext>? normalAttackCallback;
        private Action<InputAction.CallbackContext>? toggleAimingLockCallback;
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
            _sprintRequested = false;

            // Register input action events
            player.Actions.Attack.ChargedAttack.performed += chargedAttackCallback = (context) =>
            {
                player.TryStartChargedAttackOrDigging();
            };

            player.Actions.Attack.NormalAttack.performed += normalAttackCallback = (context) =>
            {
                player.TryStartNormalAttack();
            };

            player.Actions.Attack.ToggleAimingLock.performed += toggleAimingLockCallback = (context) =>
            {
                player.ToggleAimingLock();
            };

            player.Actions.Gameplay.Jump.performed += jumpRequestCallback = (context) =>
            {
                // Set jump flag
                _jumpRequested = true;
            };

            player.Actions.Gameplay.WalkToggle.performed += walkToggleRequestCallback = (context) =>
            {
                // Set walk toggle flag
                _walkToggleRequested = true;
            };

            player.Actions.Gameplay.Sprint.performed += sprintRequestCallback = (context) =>
            {
                // Set sprint flag
                _sprintRequested = true;
            };

            if (prevState is not ForceMoveState)
            {
                string stateName;

                if (prevState == PlayerStates.AIRBORNE)
                {
                    stateName = AnimatorEntityRender.LANDING_NAME;
                }
                else
                {
                    stateName = GetEntryAnimatorStateName(info);
                }

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
            player.Actions.Attack.ChargedAttack.performed -= chargedAttackCallback;
            player.Actions.Attack.NormalAttack.performed -= normalAttackCallback;
            player.Actions.Attack.ToggleAimingLock.performed -= toggleAimingLockCallback;
            player.Actions.Gameplay.Jump.performed -= jumpRequestCallback;
            player.Actions.Gameplay.WalkToggle.performed -= walkToggleRequestCallback;
            player.Actions.Gameplay.Sprint.performed -= sprintRequestCallback;

            // Ungrounded, go to falling state
            if (nextState == PlayerStates.AIRBORNE && !info.Grounded && !_jumpConfirmed)
            {
                // Make sure it isn't jumping
                player.StartCrossFadeState(AnimatorEntityRender.FALLING_NAME, 0.2F);
            }

            // Reset jump confirmation flag
            _jumpConfirmed = false;

            // Disable aiming lock if next state is not applicable
            if (nextState is not DiggingAimState || nextState is not RangedAimState)
            {
                player.UseAimingLock(false);
            }
        }

        public override string ToString() => "Grounded";
    }
}