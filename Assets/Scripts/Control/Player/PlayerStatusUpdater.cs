#nullable enable
using System;
using CraftSharp.Event;
using KinematicCharacterController;
using UnityEngine;

namespace CraftSharp.Control
{
    public class PlayerStatusUpdater : MonoBehaviour
    {
        private const string TERRAIN_BOX_COLLIDER_LAYER_NAME = "TerrainBoxCollider";
        private const string LIQUID_BOX_COLLIDER_LAYER_NAME = "LiquidBoxCollider";
        private const string LIQUID_MESH_COLLIDER_LAYER_NAME = "LiquidMeshCollider";
        
        // Ground distance check
        private const float GROUND_RAYCAST_START   = 2.5F;
        private const float GROUND_RAYCAST_DIST    = 5.0F;

        // Liquid status and distance check
        private const float LIQUID_RAYCAST_START   =  2.0F; // Liquid raycast goes downward from top of player
        private const float LIQUID_RAYCAST_DIST    =  5.0F;
        public const float FLOATING_DIST_THRESHOLD = -0.5F;

        // Barrier/wall distance check
        private const float BARRIER_RAYCAST_LENGTH =  2.0F;

        public readonly PlayerStatus Status = new();

        public void UpdatePlayerStatus(KinematicCharacterMotor motor, Quaternion targetOrientation)
        {
            var frontDirNormalized = targetOrientation * Vector3.forward;
            var terrainBoxColliderLayer = 1 << LayerMask.NameToLayer(TERRAIN_BOX_COLLIDER_LAYER_NAME);
            var liquidBoxColliderLayer = 1 << LayerMask.NameToLayer(LIQUID_BOX_COLLIDER_LAYER_NAME);
            var liquidMeshColliderLayer = 1 << LayerMask.NameToLayer(LIQUID_MESH_COLLIDER_LAYER_NAME);

            // Grounded state update
            var groundCheck = motor.GroundingStatus.FoundAnyGround;
            Status.GroundCheck = groundCheck;
            var rayCenter = transform.position + GROUND_RAYCAST_START * motor.CharacterUp;

            // > Cast a ray downward from above the player
            if (Physics.Raycast(rayCenter, -motor.CharacterUp, out RaycastHit centerDownHit,
                GROUND_RAYCAST_DIST, terrainBoxColliderLayer, QueryTriggerInteraction.Ignore))
                Status.CenterDownDist = centerDownHit.distance - GROUND_RAYCAST_START;
            else
                Status.CenterDownDist = GROUND_RAYCAST_DIST - GROUND_RAYCAST_START;

            Debug.DrawRay(rayCenter, motor.CharacterUp * -GROUND_RAYCAST_DIST, Color.cyan);

            // Perform barrier check and wall check
            const float barrierLowerCheckRayHeight = 0.1F;
            const float barrierUpperCheckRayHeight = 0.6F;
            var barrierLowerCheckRayOrigin = transform.position + barrierLowerCheckRayHeight * motor.CharacterUp;
            var barrierLowerResult = Physics.Raycast(barrierLowerCheckRayOrigin, frontDirNormalized,
                out RaycastHit barrierLowerForwardHit,
                BARRIER_RAYCAST_LENGTH, terrainBoxColliderLayer, QueryTriggerInteraction.Ignore);
            var barrierUpperCheckRayOrigin = transform.position + barrierUpperCheckRayHeight * motor.CharacterUp;
            var barrierUpperResult = Physics.Raycast(barrierUpperCheckRayOrigin, frontDirNormalized,
                out RaycastHit barrierUpperForwardHit,
                BARRIER_RAYCAST_LENGTH, terrainBoxColliderLayer, QueryTriggerInteraction.Ignore);
            
            // > Cast a ray forward from feet height
            if (barrierLowerResult || barrierUpperResult)
            {
                RaycastHit barrierForwardHit;

                if (barrierLowerResult && barrierUpperResult) // Both rays hit something, pick the nearer one
                {
                    barrierForwardHit = barrierLowerForwardHit.distance <= barrierUpperForwardHit.distance ?
                            barrierLowerForwardHit : barrierUpperForwardHit;
                }
                else // Only one of them hit something, check which
                {
                    barrierForwardHit = barrierLowerResult ? barrierLowerForwardHit : barrierUpperForwardHit;
                }
                
                Status.BarrierYawAngle = Vector3.Angle(frontDirNormalized, -barrierForwardHit.normal);
                Status.BarrierDistance = barrierForwardHit.distance;

                // > Cast another ray downward in front of the player
                var rayFront = rayCenter + frontDirNormalized * (barrierForwardHit.distance + 0.1F);

                if (Physics.Raycast(rayFront, -motor.CharacterUp, out RaycastHit barrierDownwardHit, 
                    GROUND_RAYCAST_DIST, terrainBoxColliderLayer, QueryTriggerInteraction.Ignore))
                    Status.BarrierHeight = GROUND_RAYCAST_START - barrierDownwardHit.distance;
                else
                    Status.BarrierHeight = GROUND_RAYCAST_START - GROUND_RAYCAST_DIST;
                
                Debug.DrawRay(rayFront,  motor.CharacterUp * -GROUND_RAYCAST_DIST, Color.green);

                // Then perform wall check to help determine whether this barrier can be climbed over
                var wallCheckRayHeight = motor.Capsule.height + 0.4F;
                var wallCheckRayOrigin = transform.position + wallCheckRayHeight * motor.CharacterUp;

                // > Cast another ray forward from head height
                Status.WallDistance = Physics.Raycast(wallCheckRayOrigin, frontDirNormalized,out RaycastHit wallForwardHit, 
                    BARRIER_RAYCAST_LENGTH, terrainBoxColliderLayer, QueryTriggerInteraction.Ignore) ?
                    wallForwardHit.distance :
                    // Should be enough space for player to climb over, use an arbitrary value that's big enough
                    BARRIER_RAYCAST_LENGTH;
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

            // In liquid state update
            Status.InLiquid = Physics.CheckBox(transform.position + motor.CharacterUp * 0.25F,
                new Vector3(0.25F, 0.25F, 0.25F), motor.transform.rotation, liquidBoxColliderLayer,
                QueryTriggerInteraction.Collide);

            if (Status.Grounded) // Grounded in last update
            {
                // Workaround: Extra check to make sure the player is just walking on some bumped surface and happen to leave the ground
                if (!groundCheck)
                {
                    Status.Grounded = Status.CenterDownDist <= 1.25F;
                }
                else
                {
                    Status.Grounded = true;
                }
            }
            else // Not grounded in last update
            {
                Status.Grounded = groundCheck;
            }
            
            // Cast a ray downwards again, but check liquid layer this time
            if (Status.InLiquid)
            {
                // Perform floating check
                Status.Floating = Status is { LiquidDist: <= FLOATING_DIST_THRESHOLD, Grounded: false };
                
                if (Physics.Raycast(rayCenter, -transform.up, out centerDownHit,
                    LIQUID_RAYCAST_DIST, liquidBoxColliderLayer | liquidMeshColliderLayer,
                    QueryTriggerInteraction.Collide))
                {
                    Status.LiquidDist = centerDownHit.distance - LIQUID_RAYCAST_START;
                }
                else // Dived completely into water
                {
                    Status.LiquidDist = -LIQUID_RAYCAST_DIST;
                }
            }
            else // Not in liquid
            {
                // Reset floating flag and liquid distance
                Status.Floating = false;
                Status.LiquidDist = 0F;
            }
        }
    }
}