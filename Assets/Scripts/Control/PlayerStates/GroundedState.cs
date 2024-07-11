#nullable enable
using System;
using KinematicCharacterController;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CraftSharp.Control
{
    public class GroundedState : IPlayerState
    {
        private const float THRESHOLD_CLIMB_1M =  1.35F;
        private const float THRESHOLD_CLIMB_UP = 0.626F;

        private bool _jumpRequested = false;
        private bool _jumpConfirmed = false;
        private bool _walkToggleRequested = false;

        public void UpdateBeforeMotor(float interval, PlayerActions inputData, PlayerStatus info, KinematicCharacterMotor motor, PlayerController player)
        {
            // Check climb over barrier
            if (info.Moving && info.BarrierHeight > THRESHOLD_CLIMB_UP &&
                    info.BarrierHeight < THRESHOLD_CLIMB_1M && info.YawDeltaAbs < 10F && info.BarrierYawAngle < 30F) // Climb up platform
            {
                if (info.YawDeltaAbs <= 10F && info.BarrierYawAngle < 30F) // Trying to moving forward
                {
                    var forwardDir = player.GetTargetOrientation() * Vector3.forward;

                    var curPos  = motor.transform.position;
                    var dstPos = curPos + (info.BarrierHeight - 0.95F) * motor.CharacterUp + forwardDir * (0.7F - info.BarrierDistance);

                    player.StartForceMoveOperation("Climb over barrier",
                        new ForceMoveOperation[] {
                                new(curPos, dstPos, 0.2F),
                                new(dstPos, 0.9F,
                                    init: (info, motor, player) => {
                                        player.RandomizeMirroredFlag();
                                        player.StartCrossFadeState(PlayerAbility.CLIMB_1M);
                                        motor.SetPosition(dstPos);
                                    },
                                    exit: (info, motor, player) => {
                                        info.Grounded = true;
                                    },
                                    update: (interval, inputData, info, motor, player) =>
                                        info.Moving = inputData.Gameplay.Movement.IsPressed()
                                )
                        } );

                    // Prevent jump preparation
                    _jumpRequested = false;
                }
            }
            
            if (_jumpRequested) // Jump
            {
                
                // Check if jumping is available
                if (info.Grounded)
                {
                    // Set jump confirmation flag
                    _jumpConfirmed = true;

                    // Makes the character skip ground probing/snapping on its next update
                    motor.ForceUnground(0.1F);
                    // Randomize mirror flag before jumping
                    player.RandomizeMirroredFlag();
                    // Set up jump flag for animator
                    player.StartJump();
                    // Also reset grounded flag
                    info.Grounded = false;
                }

                // Reset jump flag
                _jumpRequested = false;
            }
        }

        public void UpdateMain(ref Vector3 currentVelocity, float interval, PlayerActions inputData, PlayerStatus info, KinematicCharacterMotor motor, PlayerController player)
        {
            var ability = player.Ability;

            // Reset gliding state
            info.Gliding = false;

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

                // Reset jump confirmation flag
                _jumpConfirmed = false;
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

                if (info.Moving) // Moving
                {
                    float moveSpeed;

                    if ((inputData.Gameplay.Sprint.IsPressed() || info.Sprinting) && info.StaminaLeft > 0.1F)
                        info.Sprinting = true;
                    else
                        info.Sprinting = false;
                    
                    if (info.Sprinting)
                        moveSpeed = ability.SprintSpeed;
                    else
                        moveSpeed = info.WalkMode ? ability.WalkSpeed : ability.RunSpeed;

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

                    // Smooth deceleration
                    moveVelocity = Vector3.MoveTowards(currentVelocity, Vector3.zero, ability.DecSpeed * interval);
                }

                // Workaround: Used when fake grounded status is used (to avoid airborne state when moving off a block)
                if (!motor.GroundingStatus.FoundAnyGround)
                {
                    // Apply fake gravity
                    moveVelocity -= motor.CharacterUp * 6;
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
        private Action<InputAction.CallbackContext>? jumpRequestCallback;
        private Action<InputAction.CallbackContext>? walkToggleRequestCallback;

        public void OnEnter(PlayerStatus info, KinematicCharacterMotor motor, PlayerController player)
        {
            info.Sprinting = false;

            // Reset request flags
            _jumpRequested = false;
            _jumpConfirmed = false;
            _walkToggleRequested = false;

            // Register input action events
            player.Actions.Attack.ChargedAttack.performed += chargedAttackCallback = (context) =>
            {
                player.TryStartChargedAttack();
            };

            player.Actions.Attack.NormalAttack.performed += normalAttackCallback = (context) =>
            {
                player.TryStartNormalAttack();
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
        }

        public void OnExit(PlayerStatus info, KinematicCharacterMotor motor, PlayerController player)
        {
            info.Sprinting = false;

            // Unregister input action events
            player.Actions.Attack.ChargedAttack.performed -= chargedAttackCallback;
            player.Actions.Attack.NormalAttack.performed -= normalAttackCallback;
            player.Actions.Gameplay.Jump.performed -= jumpRequestCallback;
            player.Actions.Gameplay.WalkToggle.performed -= walkToggleRequestCallback;
        }

        public override string ToString() => "Grounded";
    }
}