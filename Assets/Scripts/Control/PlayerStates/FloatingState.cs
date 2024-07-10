#nullable enable
using KinematicCharacterController;
using UnityEngine;

namespace CraftSharp.Control
{
    public class FloatingState : IPlayerState
    {
        private bool _forceUngroundRequested = false;
        private bool _forceUngroundConfirmed = false;

        public void UpdateBeforeMotor(float interval, PlayerActions inputData, PlayerStatus info, KinematicCharacterMotor motor, PlayerController player)
        {
            if (_forceUngroundRequested) // Force unground
            {
                if (info.Grounded)
                {
                    _forceUngroundConfirmed = true;

                    // Makes the character skip ground probing/snapping on its next update
                    motor.ForceUnground(0.1F);
                    // Also reset grounded flag
                    info.Grounded = false;
                }

                _forceUngroundRequested = false;
            }
        }

        public void UpdateMain(ref Vector3 currentVelocity, float interval, PlayerActions inputData, PlayerStatus info, KinematicCharacterMotor motor, PlayerController player)
        {
            var ability = player.Ability;
            
            info.Sprinting = false;
            info.Gliding = false;

            var swimSpeed = ability.SwimSpeed;

            Vector3 moveVelocity = Vector3.zero;

            if (_forceUngroundConfirmed)
            {
                // Apply vertical velocity to reduced horizontal velocity
                moveVelocity = currentVelocity * 0.5F + motor.CharacterUp * ability.JumpSpeedCurve.Evaluate(currentVelocity.magnitude);

                _forceUngroundConfirmed = false;
            }
            else
            {
                // Update moving status
                bool prevMoving = info.Moving;
                info.Moving = inputData.Gameplay.Movement.IsPressed();

                // Animation mirror randomation
                if (info.Moving != prevMoving)
                {
                    player.RandomizeMirroredFlag();
                }

                // Check vertical movement...
                float distToAfloat = PlayerStatusUpdater.FLOATING_DIST_THERSHOLD - 0.2F - info.LiquidDist;

                if (inputData.Gameplay.Ascend.IsPressed())
                {
                    if (distToAfloat > 0F) // Underwater
                    {
                        if(distToAfloat <= 1F) // Move up no further than top of the surface
                        {
                            moveVelocity = distToAfloat * 2f * motor.CharacterUp;
                        }
                        else // Just move up
                        {
                            moveVelocity = swimSpeed * motor.CharacterUp;
                        }

                        // Workaround: Special handling for force unground
                        if (info.Grounded && !_forceUngroundRequested)
                        {
                            _forceUngroundRequested = true;
                        }

                        
                    }
                }
                else if (inputData.Gameplay.Descend.IsPressed())
                {
                    if (!info.Grounded)
                    {
                        moveVelocity = -swimSpeed * motor.CharacterUp;
                    }
                }

                // Check horizontal movement...
                if (info.Moving)
                {
                    // Smooth rotation for player model
                    info.CurrentVisualYaw = Mathf.MoveTowardsAngle(info.CurrentVisualYaw, info.TargetVisualYaw, ability.TurnSpeed * interval * 0.5F);
                    
                    // Use *current* orientation to calculate actual movement direction, unlike grounded state
                    // This means our player will not turn around in-place, but rather need to make a U-turn
                    moveVelocity += player.GetCurrentOrientation() * Vector3.forward * swimSpeed;
                }

                // Smooth movement (if accelerating)
                if (moveVelocity.sqrMagnitude > currentVelocity.sqrMagnitude)
                {
                    moveVelocity = Vector3.MoveTowards(currentVelocity, moveVelocity, ability.AccSpeed * interval);
                }

                // Apply gravity (nonexistent)
            }

            currentVelocity = moveVelocity;
            
            // Consume stamina (nonexistent)
        }

        public bool ShouldEnter(PlayerActions inputData, PlayerStatus info)
        {
            if (!info.Spectating && info.Floating)
                return true;
            
            return false;
        }

        public bool ShouldExit(PlayerActions inputData, PlayerStatus info)
        {
            if (info.Spectating || !info.Floating)
                return true;
            
            return false;
        }

        public void OnEnter(PlayerStatus info, KinematicCharacterMotor motor, PlayerController player)
        {
            info.Sprinting = false;

            // Reset request flags
            _forceUngroundRequested = false;
            _forceUngroundConfirmed = false;
        }

        public void OnExit(PlayerStatus info, KinematicCharacterMotor motor, PlayerController player)
        {
            
        }

        public override string ToString() => "Floating";
    }
}