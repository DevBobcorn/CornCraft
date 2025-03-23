#nullable enable
using System;
using CraftSharp.Event;
using KinematicCharacterController;
using UnityEngine;

namespace CraftSharp.Control
{
    public class PlayerStatusUpdater : MonoBehaviour
    {
        // Ground distance check
        private const float GROUND_RAYCAST_START   = 2.5F;
        private const float GROUND_RAYCAST_DIST    = 5.0F;

        // Liquid status and distance check
        private const float LIQUID_RAYCAST_START   = 2.0F; // Liquid raycast goes downward from top of player
        private const float LIQUID_RAYCAST_DIST    = 5.0F;
        
        private bool _useServerLiquidCheck = false;
        public const float FLOATING_DIST_THERSHOLD = -0.5F;

        // Barrier/wall distance check
        private const float BARRIER_RAYCAST_LENGTH =  2.0F;

        public LayerMask SolidLayer;
        public LayerMask LiquidSurfaceLayer;
        public LayerMask LiquidVolumeLayer;

        public PlayerStatus Status = new();

        public void UpdatePlayerStatus(KinematicCharacterMotor motor, Quaternion targetOrientation)
        {
            var frontDirNormalized = targetOrientation * Vector3.forward;

            // Grounded state update
            bool groundCheck = motor.GroundingStatus.FoundAnyGround;
            //Status.GroundedCheck = groundCheck;

            var rayCenter = transform.position + GROUND_RAYCAST_START * motor.CharacterUp;

            // > Cast a ray downward from above the player
            if (Physics.Raycast(rayCenter, -motor.CharacterUp, out RaycastHit centerDownHit, GROUND_RAYCAST_DIST, SolidLayer, QueryTriggerInteraction.Ignore))
                Status.CenterDownDist = centerDownHit.distance - GROUND_RAYCAST_START;
            else
                Status.CenterDownDist = GROUND_RAYCAST_DIST - GROUND_RAYCAST_START;

            Debug.DrawRay(rayCenter, motor.CharacterUp * -GROUND_RAYCAST_DIST, Color.cyan);

            // Perform barrier check and wall check
            var barrierCheckRayHeight = 0.1F;
            var barrierCheckRayOrigin = transform.position + barrierCheckRayHeight * motor.CharacterUp;

            // > Cast a ray forward from feet height
            if (Physics.Raycast(barrierCheckRayOrigin, frontDirNormalized, out RaycastHit barrierForwardHit, BARRIER_RAYCAST_LENGTH, SolidLayer, QueryTriggerInteraction.Ignore))
            {
                Status.BarrierYawAngle = Vector3.Angle(frontDirNormalized, -barrierForwardHit.normal);
                Status.BarrierDistance = barrierForwardHit.distance;

                // > Cast another ray downward in front of the player
                var rayFront = rayCenter + frontDirNormalized * (barrierForwardHit.distance + 0.1F);

                if (Physics.Raycast(rayFront, -motor.CharacterUp, out RaycastHit barrierDownwardHit, GROUND_RAYCAST_DIST, SolidLayer, QueryTriggerInteraction.Ignore))
                    Status.BarrierHeight = GROUND_RAYCAST_START - barrierDownwardHit.distance;
                else
                    Status.BarrierHeight = GROUND_RAYCAST_START - GROUND_RAYCAST_DIST;
                
                Debug.DrawRay(rayFront,  motor.CharacterUp * -GROUND_RAYCAST_DIST, Color.green);

                // Then perform wall check to help determine whether this barrier can be climbed over
                var wallCheckRayHeight = motor.Capsule.height + 0.4F;
                var wallCheckRayOrigin = transform.position + wallCheckRayHeight * motor.CharacterUp;

                // > Cast another ray forward from head height
                if (Physics.Raycast(wallCheckRayOrigin, frontDirNormalized, out RaycastHit wallForwardHit, BARRIER_RAYCAST_LENGTH, SolidLayer, QueryTriggerInteraction.Ignore))
                {
                    Status.WallDistance = wallForwardHit.distance;
                }
                else
                {
                    // Should be enough space for player to climb over, use an arbitrary value that's big enough
                    Status.WallDistance = BARRIER_RAYCAST_LENGTH;
                }
            }
            else // No barrier
            {
                Status.BarrierHeight = 0F;

                // These values should not be used when barrier height is less than or equal to 0
                Status.BarrierYawAngle = 0F;
                Status.BarrierDistance = 0F;
                // Skip wall check
                Status.WallDistance = 0F;
            }

            Debug.DrawRay(barrierCheckRayOrigin, frontDirNormalized, Color.blue);

            // Perform water volume check, if not using water check from server
            if (!_useServerLiquidCheck)
            {
                // In liquid state update
                var capsule = GetComponent<CapsuleCollider>();
                if (Physics.CheckBox(transform.position + capsule.center, new Vector3(capsule.radius, capsule.height / 2f, capsule.radius), motor.transform.rotation, LiquidVolumeLayer, QueryTriggerInteraction.Collide))
                {
                    Status.InLiquid = true;
                }
                else
                {
                    Status.InLiquid = false;
                }
            }

            if (Status.InLiquid) // In liquid
            {
                // Perform floating check
                Status.Floating = Status.LiquidDist <= FLOATING_DIST_THERSHOLD;
            }
            else // In air or on ground
            {
                // Reset floating flag, we are not even in liquid
                Status.Floating = false;
            }

            if (Status.Grounded) // Grounded in last update
            {
                // Workaround: Extra check to make sure the player is just walking on some bumped surface and happen to leave the ground
                if (!groundCheck && Status.CenterDownDist > 1.25F)
                {
                    Status.Grounded = false;
                }
            }
            else // Not grounded in last update
            {
                Status.Grounded = groundCheck;
            }
            
            // Cast a ray downwards again, but check liquid layer this time
            if (Status.InLiquid)
            {
                if (Physics.Raycast(rayCenter, -transform.up, out centerDownHit, LIQUID_RAYCAST_DIST, LiquidSurfaceLayer | LiquidVolumeLayer, QueryTriggerInteraction.Collide))
                {
                    Status.LiquidDist = centerDownHit.distance - LIQUID_RAYCAST_START;
                }
                else // Dived completely into water
                {
                    Status.LiquidDist = -LIQUID_RAYCAST_DIST;
                }
            }
            else // Not in water
            {
                Status.LiquidDist = 0F;
            }
        }

        private Action<PlayerLiquidEvent>? serverLiquidCallback;

        void Start()
        {
            serverLiquidCallback = (e) => {
                Status.InLiquid = e.Enter;
                _useServerLiquidCheck = true;
            };

            EventManager.Instance.Register(serverLiquidCallback);
        }

        void OnDestroy()
        {
            if (serverLiquidCallback is not null)
            {
                EventManager.Instance.Unregister(serverLiquidCallback);
            }
        }
    }
}