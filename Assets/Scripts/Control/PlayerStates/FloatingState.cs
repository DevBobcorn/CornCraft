#nullable enable
using UnityEngine;

namespace CraftSharp.Control
{
    public class FloatingState : IPlayerState
    {
        public void UpdateMain(ref Vector3 currentVelocity, float interval, PlayerActions inputData, PlayerStatus info, PlayerController player)
        {
            var ability = player.AbilityConfig;
            
            info.Sprinting = false;
            info.Gliding = false;
            info.Flying = false;

            var swimSpeed = ability.SwimSpeed;

            Vector3 moveVelocity = Vector3.zero;

            // Update moving status
            bool prevMoving = info.Moving;
            info.Moving = inputData.Locomotion.Movement.IsPressed();

            // Animation mirror randomization
            if (info.Moving != prevMoving)
            {
                player.RandomizeMirroredFlag();
            }

            // Check vertical movement...
            float distToAfloat = PlayerStatusUpdater.FLOATING_DIST_THRESHOLD - 0.2F - info.LiquidDist;

            if (inputData.Locomotion.Ascend.IsPressed())
            {
                if (distToAfloat > 0F) // Underwater
                {
                    if(distToAfloat <= 1F) // Move up no further than top of the surface
                    {
                        moveVelocity = distToAfloat * 2f * player.transform.up;
                    }
                    else // Just move up
                    {
                        moveVelocity = swimSpeed * player.transform.up;
                    }
                }
            }
            else if (inputData.Locomotion.Descend.IsPressed())
            {
                if (!info.Grounded)
                {
                    moveVelocity = -swimSpeed * player.transform.up;
                }
            }

            // Check horizontal movement...
            if (info.Moving)
            {
                // Smooth rotation for player model
                info.CurrentVisualYaw = Mathf.MoveTowardsAngle(info.CurrentVisualYaw, info.TargetVisualYaw, ability.TurnSpeed * interval * 0.5F);
                
                // Use target orientation to calculate actual movement direction
                moveVelocity += player.GetMovementOrientation() * Vector3.forward * swimSpeed;
            }

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

        public void OnEnter(IPlayerState prevState, PlayerStatus info, PlayerController player)
        {
            info.Sprinting = false;
        }

        public void OnExit(IPlayerState nextState, PlayerStatus info, PlayerController player)
        {
            
        }

        public override string ToString() => "Floating";
    }
}