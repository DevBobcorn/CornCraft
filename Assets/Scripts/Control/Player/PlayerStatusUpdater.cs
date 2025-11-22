#nullable enable
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using CraftSharp.Rendering;

namespace CraftSharp.Control
{
    public class PlayerStatusUpdater : MonoBehaviour
    {
        // Ground distance check
        private const float GROUND_RAYCAST_START   = 2.5F;
        private const float GROUND_RAYCAST_DIST    = 5.0F;

        // Liquid status and distance check
        private const float LIQUID_RAYCAST_START   =  2.0F; // Liquid raycast goes downward from top of player
        private const float LIQUID_RAYCAST_DIST    =  5.0F;
        public const float FLOATING_DIST_THRESHOLD = -0.5F;

        // Barrier/wall distance check
        private const float BARRIER_RAYCAST_LENGTH =  2.0F;

        // Player dimensions
        const float PLAYER_WIDTH = 0.6F;
        const float PLAYER_HEIGHT = 1.8F;
        const float PLAYER_RADIUS = PLAYER_WIDTH * 0.5F; // 0.3
        const float PLAYER_CENTER_Y = PLAYER_HEIGHT * 0.5F; // 0.9
        const float GROUND_CHECK_TOLERANCE = 1E-2F; // Allow slight penetration/float
        const float MOVEMENT_EPSILON = 1E-4F; // Threshold to consider movement negligible

        public readonly PlayerStatus Status = new();
        
        // Store last movement for gizmo visualization
        private Vector3 lastMovementOffset = Vector3.zero;
        
        // Store blocking AABB for gizmo visualization
        private bool hasBlockingAABB = false;
        private Vector3 blockingAABBMin = Vector3.zero;
        private Vector3 blockingAABBMax = Vector3.zero;

        public void UpdatePlayerStatus(Quaternion targetOrientation, UnityAABB[] aabbs)
        {
            // Grounded state update
            CheckGrounded(aabbs);

            // Perform barrier check and wall check
            
        }

        public void UpdatePlayerPosition(Vector3 velocity, float deltaTime, UnityAABB[] aabbs)
        {
            // Update player position
            var offset = velocity * deltaTime;
            lastMovementOffset = offset; // Store for gizmo visualization
            
            // Reset blocking AABB
            hasBlockingAABB = false;
            
            var newPosition = CalculateNewPlayerPosition(transform.position, offset, aabbs, 
                out bool blocked, out Vector3 blockingMin, out Vector3 blockingMax);
            
            if (blocked)
            {
                hasBlockingAABB = true;
                blockingAABBMin = blockingMin;
                blockingAABBMax = blockingMax;
            }
            
            transform.position = newPosition;
        }

        /// <summary>
        /// Calculates new player position given current position, movement offset, and a list of AABBs.
        /// If any AABB is in the way, the player moves to the point touching the AABB.
        /// </summary>
        private static Vector3 CalculateNewPlayerPosition(Vector3 currentPosition, Vector3 offset, UnityAABB[] aabbs,
            out bool wasBlocked, out Vector3 blockingMin, out Vector3 blockingMax)
        {
            var newPos = currentPosition;
            bool blocked = false;
            Vector3 blockMin = Vector3.zero;
            Vector3 blockMax = Vector3.zero;

            var effectiveOffset = offset;
            bool adjustedToSurface = false;
            UnityAABB supportingSurface = default;

            if (aabbs.Length > 0 &&
                effectiveOffset.sqrMagnitude <= MOVEMENT_EPSILON * MOVEMENT_EPSILON &&
                TryGetSupportingSurface(currentPosition, aabbs, out supportingSurface))
            {
                if (effectiveOffset.y < 0F)
                {
                    float targetY = supportingSurface.Max.y;

                    if (newPos.y + effectiveOffset.y < targetY)
                    {
                        // Keep player exactly on top of the surface
                        effectiveOffset.y = targetY - newPos.y;
                        adjustedToSurface = true;
                    }
                }
            }

            // If there is no meaningful movement or nothing to collide with, apply the offset directly
            if (effectiveOffset.sqrMagnitude <= MOVEMENT_EPSILON * MOVEMENT_EPSILON || aabbs.Length == 0)
            {
                var finalPos = newPos + effectiveOffset;
                wasBlocked = adjustedToSurface;
                blockingMin = adjustedToSurface ? supportingSurface.Min : Vector3.zero;
                blockingMax = adjustedToSurface ? supportingSurface.Max : Vector3.zero;
                return finalPos;
            }

            // Apply movement axis by axis (Y first for gravity/ground resolution)
            ResolveVertical(effectiveOffset.y);
            ResolveHorizontalX(effectiveOffset.x);
            ResolveHorizontalZ(effectiveOffset.z);

            wasBlocked = blocked;
            blockingMin = blockMin;
            blockingMax = blockMax;
            return newPos;

            bool AxisOverlap(float minA, float maxA, float minB, float maxB, bool inclusive)
            {
                return inclusive
                    ? maxA >= minB && minA <= maxB
                    : maxA > minB && minA < maxB;
            }

            bool TryGetSupportingSurface(Vector3 feetPosition, UnityAABB[] environment, out UnityAABB surface)
            {
                float playerMinX = feetPosition.x - PLAYER_RADIUS;
                float playerMaxX = feetPosition.x + PLAYER_RADIUS;
                float playerMinZ = feetPosition.z - PLAYER_RADIUS;
                float playerMaxZ = feetPosition.z + PLAYER_RADIUS;

                foreach (var aabb in environment)
                {
                    if (aabb.IsTrigger || aabb.IsLiquid) continue;

                    bool xOverlap = playerMaxX > aabb.Min.x && playerMinX < aabb.Max.x;
                    bool zOverlap = playerMaxZ > aabb.Min.z && playerMinZ < aabb.Max.z;

                    if (!xOverlap || !zOverlap)
                        continue;

                    float surfaceY = aabb.Max.y;
                    if (feetPosition.y >= surfaceY - GROUND_CHECK_TOLERANCE &&
                        feetPosition.y <= surfaceY + GROUND_CHECK_TOLERANCE)
                    {
                        surface = aabb;
                        return true;
                    }
                }

                surface = default;
                return false;
            }

            void ResolveHorizontalX(float movement)
            {
                if (Mathf.Abs(movement) <= 0F) return;

                float playerMinY = newPos.y;
                float playerMaxY = newPos.y + PLAYER_HEIGHT;
                float playerMinZ = newPos.z - PLAYER_RADIUS;
                float playerMaxZ = newPos.z + PLAYER_RADIUS;
                float playerMinX = newPos.x - PLAYER_RADIUS;
                float playerMaxX = newPos.x + PLAYER_RADIUS;

                float clamped = movement;

                foreach (var aabb in aabbs)
                {
                    if (aabb.IsTrigger || aabb.IsLiquid) continue;

                    if (!AxisOverlap(playerMinY, playerMaxY, aabb.Min.y, aabb.Max.y, false) ||
                        !AxisOverlap(playerMinZ, playerMaxZ, aabb.Min.z, aabb.Max.z, false))
                        continue;

                    float prev = clamped;

                    if (clamped > 0F)
                    {
                        if (playerMaxX <= aabb.Min.x &&
                            playerMaxX + clamped > aabb.Min.x)
                        {
                            float allowed = aabb.Min.x - playerMaxX;
                            clamped = Mathf.Min(clamped, Mathf.Max(0F, allowed));
                        }
                    }
                    else
                    {
                        if (playerMinX >= aabb.Max.x &&
                            playerMinX + clamped < aabb.Max.x)
                        {
                            float allowed = aabb.Max.x - playerMinX;
                            clamped = Mathf.Max(clamped, Mathf.Min(0F, allowed));
                        }
                    }

                    if (Mathf.Abs(prev - clamped) > 0F)
                    {
                        blocked = true;
                        blockMin = aabb.Min;
                        blockMax = aabb.Max;
                    }

                    if (Mathf.Abs(clamped) <= 0F)
                        break;
                }

                newPos.x += clamped;
            }

            void ResolveHorizontalZ(float movement)
            {
                if (Mathf.Abs(movement) <= 0F) return;

                float playerMinY = newPos.y;
                float playerMaxY = newPos.y + PLAYER_HEIGHT;
                float playerMinX = newPos.x - PLAYER_RADIUS;
                float playerMaxX = newPos.x + PLAYER_RADIUS;
                float playerMinZ = newPos.z - PLAYER_RADIUS;
                float playerMaxZ = newPos.z + PLAYER_RADIUS;

                float clamped = movement;

                foreach (var aabb in aabbs)
                {
                    if (aabb.IsTrigger || aabb.IsLiquid) continue;

                    if (!AxisOverlap(playerMinY, playerMaxY, aabb.Min.y, aabb.Max.y, false) ||
                        !AxisOverlap(playerMinX, playerMaxX, aabb.Min.x, aabb.Max.x, false))
                        continue;

                    float prev = clamped;

                    if (clamped > 0F)
                    {
                        if (playerMaxZ <= aabb.Min.z &&
                            playerMaxZ + clamped > aabb.Min.z)
                        {
                            float allowed = aabb.Min.z - playerMaxZ;
                            clamped = Mathf.Min(clamped, Mathf.Max(0F, allowed));
                        }
                    }
                    else
                    {
                        if (playerMinZ >= aabb.Max.z &&
                            playerMinZ + clamped < aabb.Max.z)
                        {
                            float allowed = aabb.Max.z - playerMinZ;
                            clamped = Mathf.Max(clamped, Mathf.Min(0F, allowed));
                        }
                    }

                    if (Mathf.Abs(prev - clamped) > 0F)
                    {
                        blocked = true;
                        blockMin = aabb.Min;
                        blockMax = aabb.Max;
                    }

                    if (Mathf.Abs(clamped) <= 0F)
                        break;
                }

                newPos.z += clamped;
            }

            void ResolveVertical(float movement)
            {
                if (Mathf.Abs(movement) <= 0F) return;

                float playerMinX = newPos.x - PLAYER_RADIUS;
                float playerMaxX = newPos.x + PLAYER_RADIUS;
                float playerMinZ = newPos.z - PLAYER_RADIUS;
                float playerMaxZ = newPos.z + PLAYER_RADIUS;
                float playerMinY = newPos.y;
                float playerMaxY = newPos.y + PLAYER_HEIGHT;

                float clamped = movement;

                foreach (var aabb in aabbs)
                {
                    if (aabb.IsTrigger || aabb.IsLiquid) continue;

                    if (!AxisOverlap(playerMinX, playerMaxX, aabb.Min.x, aabb.Max.x, false) ||
                        !AxisOverlap(playerMinZ, playerMaxZ, aabb.Min.z, aabb.Max.z, false))
                        continue;

                    float prev = clamped;

                    if (clamped > 0F)
                    {
                        if (playerMaxY <= aabb.Min.y &&
                            playerMaxY + clamped > aabb.Min.y)
                        {
                            float allowed = aabb.Min.y - playerMaxY;
                            clamped = Mathf.Min(clamped, Mathf.Max(0F, allowed));
                        }
                    }
                    else
                    {
                        if (playerMinY >= aabb.Max.y &&
                            playerMinY + clamped < aabb.Max.y)
                        {
                            float allowed = aabb.Max.y - playerMinY;
                            clamped = Mathf.Max(clamped, Mathf.Min(0F, allowed));
                        }
                    }

                    if (Mathf.Abs(prev - clamped) > 0F)
                    {
                        blocked = true;
                        blockMin = aabb.Min;
                        blockMax = aabb.Max;
                    }

                    if (Mathf.Abs(clamped) <= 0F)
                        break;
                }

                newPos.y += clamped;
            }
        }

        private void CheckGrounded(UnityAABB[] aabbs)
        {
            // Player feet position
            var playerPos = transform.position;
            
            // Check player's feet area (XZ plane) against terrain AABBs
            var playerMinX = playerPos.x - PLAYER_RADIUS;
            var playerMaxX = playerPos.x + PLAYER_RADIUS;
            var playerMinZ = playerPos.z - PLAYER_RADIUS;
            var playerMaxZ = playerPos.z + PLAYER_RADIUS;

            bool isGrounded = false;

            // Check terrain AABBs (not liquid, not triggers)
            foreach (var aabb in aabbs)
            {
                // Skip trigger AABBs (NoCollision blocks)
                if (aabb.IsTrigger) continue;
                
                // Check if player's feet area overlaps with this AABB in XZ plane
                bool xOverlap = playerMaxX > aabb.Min.x && playerMinX < aabb.Max.x;
                bool zOverlap = playerMaxZ > aabb.Min.z && playerMinZ < aabb.Max.z;
                
                if (xOverlap && zOverlap)
                {
                    // Check if player's bottom is standing on top of this AABB
                    // Player bottom should be within tolerance above the AABB's top surface
                    var aabbTopY = aabb.Max.y;
                    if (playerPos.y >= aabbTopY - GROUND_CHECK_TOLERANCE && 
                        playerPos.y <= aabbTopY + GROUND_CHECK_TOLERANCE)
                    {
                        isGrounded = true;
                        break;
                    }
                }
            }

            Status.Grounded = isGrounded;
            Debug.Log($"Grounded: {isGrounded}");
        }

        private void OnDrawGizmos()
        {
            // Draw player AABB
            var playerPos = transform.position;
            
            // Player AABB center (position is at feet, so center is at half height)
            Vector3 center = new Vector3(
                playerPos.x,
                playerPos.y + PLAYER_CENTER_Y,
                playerPos.z
            );
            
            // Player AABB size
            Vector3 size = new Vector3(
                PLAYER_WIDTH,
                PLAYER_HEIGHT,
                PLAYER_WIDTH
            );
            
            // Set gizmo color (cyan / magenta)
            Gizmos.color = Status.Grounded ? Color.cyan : Color.magenta;
            
            // Draw wireframe cube for the AABB
            Gizmos.DrawWireCube(center, size);
            
            // Draw movement line if there's any movement
            if (lastMovementOffset.sqrMagnitude > 1E-6F)
            {
                // Draw line from current position to intended destination
                Vector3 startPos = playerPos;
                Vector3 endPos = playerPos + lastMovementOffset;
                
                // Use yellow for movement line
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(startPos, endPos);
                
                // Draw a small sphere at the destination
                Gizmos.DrawSphere(endPos, 0.1F);
            }
            
            // Draw blocking AABB if movement was blocked
            if (hasBlockingAABB)
            {
                // Calculate center and size of blocking AABB
                Vector3 blockingCenter = (blockingAABBMin + blockingAABBMax) * 0.5F;
                Vector3 blockingSize = blockingAABBMax - blockingAABBMin;
                
                // Use red for blocking AABB
                Gizmos.color = new Color(1F, 0F, 0F, 0.5F); // Red with transparency
                Gizmos.DrawWireCube(blockingCenter, blockingSize);
                
                // Also draw filled cube with lower opacity
                Gizmos.color = new Color(1F, 0F, 0F, 0.2F);
                Gizmos.DrawCube(blockingCenter, blockingSize);
            }
        }
    }
}