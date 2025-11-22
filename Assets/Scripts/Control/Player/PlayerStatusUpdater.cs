#nullable enable
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

        public void UpdatePlayerStatus(Quaternion targetOrientation)
        {
            var frontDirNormalized = targetOrientation * Vector3.forward;

            // Grounded state update
            

            // Perform barrier check and wall check
            
        }
    }
}