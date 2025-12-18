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
        private const float GROUND_RAYCAST_DIST    = 5.0F;

        // Liquid status and distance check
        private const float LIQUID_RAYCAST_DIST    =  5.0F;
        public const float FLOATING_DIST_THRESHOLD = -0.5F;

        // Barrier/wall distance check
        private const float BARRIER_RAYCAST_LENGTH =  2.0F;

        // Player dimensions
        const float PLAYER_WIDTH = 0.60001F; // Make it slightly wider than 0.6 to account for floating point error
        const float PLAYER_HEIGHT = 1.8F;
        const float PLAYER_RADIUS = PLAYER_WIDTH * 0.5F; // 0.3
        const float PLAYER_CENTER_Y = PLAYER_HEIGHT * 0.5F; // 0.9
        const float GROUND_CHECK_TOLERANCE = 1E-2F; // Allow slight penetration/float
        const float MOVEMENT_EPSILON = 1E-4F; // Threshold to consider movement negligible
        const float MAX_STEP_HEIGHT = 0.125F;
        const float HIGH_STEP_MAX_HEIGHT = 1.125F;
        const float HIGH_STEP_LIFT_AMOUNT = 0.125F;
        const float FORWARD_STEP_DOT_THRESHOLD = 0.5F;
        const float STEP_FORWARD_CHECK_OFFSET = 0.05F;

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

        public void UpdatePlayerPosition(ref Vector3 velocity, float deltaTime, UnityAABB[] aabbs)
        {
            // Update player position
            var offset = velocity * deltaTime;
            lastMovementOffset = offset; // Store for gizmo visualization
            
            // Reset blocking AABB
            hasBlockingAABB = false;
            
            var planarVelocity = new Vector3(offset.x, 0F, offset.z);
            var movementForward = transform.forward;

            if (planarVelocity.sqrMagnitude > MOVEMENT_EPSILON * MOVEMENT_EPSILON)
            {
                movementForward = planarVelocity.normalized;
            }
            
            var shouldUseStepping = Status.GroundDist < HIGH_STEP_MAX_HEIGHT;

            var newPosition = CalculateNewPlayerPosition(transform.position, offset, movementForward, shouldUseStepping,
                aabbs, out bool blocked, out Vector3 blockingMin, out Vector3 blockingMax);
            
            if (blocked)
            {
                hasBlockingAABB = true;
                blockingAABBMin = blockingMin;
                blockingAABBMax = blockingMax;
                
                if (!shouldUseStepping)
                {
                    velocity.x = 0F;
                    velocity.z = 0F;
                }
            }
            
            transform.position = newPosition;
        }

        /// <summary>
        /// Calculates new player position given current position, movement offset, and a list of AABBs.
        /// If any AABB is in the way, the player moves to the point touching the AABB.
        /// </summary>
        private static Vector3 CalculateNewPlayerPosition(Vector3 currentPosition, Vector3 offset, Vector3 forward, bool shouldUseStepping,
            UnityAABB[] aabbs, out bool wasBlocked, out Vector3 blockingMin, out Vector3 blockingMax)
        {
            var newPos = currentPosition;
            var blocked = false;
            var blockMin = Vector3.zero;
            var blockMax = Vector3.zero;

            var effectiveOffset = offset;
            var adjustedToSurface = false;
            UnityAABB supportingSurface = default;
            var hasMovement = effectiveOffset.sqrMagnitude > MOVEMENT_EPSILON * MOVEMENT_EPSILON;

            if (aabbs.Length > 0 &&
                !hasMovement &&
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

            bool steppingApplied = false;

            if (shouldUseStepping)
            {
                // Check for forward step-ups before resolving axes
                steppingApplied = TryHandleForwardStep();
            }

            // Apply movement axis by axis (Y first for gravity/ground resolution)
            ResolveVertical(effectiveOffset.y);
            // Don't use horizontal movement if lifting is applied
            if (!steppingApplied)
            {
                ResolveHorizontalX(effectiveOffset.x);
                ResolveHorizontalZ(effectiveOffset.z);
            }
            
            wasBlocked = blocked;
            blockingMin = blockMin;
            blockingMax = blockMax;
            
            return newPos;

            bool TryHandleForwardStep()
            {
                Vector3 horizontalOffset = new Vector3(effectiveOffset.x, 0F, effectiveOffset.z);
                if (horizontalOffset.sqrMagnitude <= MOVEMENT_EPSILON * MOVEMENT_EPSILON)
                    return false;

                Vector3 planarForward = new Vector3(forward.x, 0F, forward.z);
                if (planarForward.sqrMagnitude <= MOVEMENT_EPSILON * MOVEMENT_EPSILON)
                    return false;

                planarForward.Normalize();

                Vector3 moveDir = horizontalOffset.normalized;
                if (Vector3.Dot(moveDir, planarForward) < FORWARD_STEP_DOT_THRESHOLD)
                    return false;

                if (!TryFindStepSurface(horizontalOffset, moveDir, out float targetSurfaceY, out float desiredLift))
                    return false;
                Debug.Log($"Desired lift: {desiredLift} (Target height: {targetSurfaceY})");

                if (desiredLift <= GROUND_CHECK_TOLERANCE)
                {
                    return false;
                }

                // Short steps are climbed in one frame, taller steps inch upward until reached
                if (desiredLift <= MAX_STEP_HEIGHT + GROUND_CHECK_TOLERANCE)
                {
                    effectiveOffset.y = desiredLift; // snap directly onto the surface
                    return false;
                }

                if (desiredLift > HIGH_STEP_MAX_HEIGHT + GROUND_CHECK_TOLERANCE)
                    return false;

                float incrementalLift = Mathf.Min(HIGH_STEP_LIFT_AMOUNT, desiredLift);
                if (incrementalLift <= GROUND_CHECK_TOLERANCE)
                    return false;

                effectiveOffset.y = incrementalLift; // climb part-way this frame
                Debug.Log($"Incremental lift: {incrementalLift}");
                
                return true;
            }

            bool TryFindStepSurface(Vector3 horizontalOffset, Vector3 moveDir, out float stepTargetY, out float stepLift)
            {
                Vector3 currentFeet = newPos;
                Vector3 targetFeet = currentFeet + horizontalOffset;
                stepLift = 0F;

                float startMinX = currentFeet.x - PLAYER_RADIUS;
                float startMaxX = currentFeet.x + PLAYER_RADIUS;
                float startMinZ = currentFeet.z - PLAYER_RADIUS;
                float startMaxZ = currentFeet.z + PLAYER_RADIUS;

                float endMinX = targetFeet.x - PLAYER_RADIUS;
                float endMaxX = targetFeet.x + PLAYER_RADIUS;
                float endMinZ = targetFeet.z - PLAYER_RADIUS;
                float endMaxZ = targetFeet.z + PLAYER_RADIUS;

                float sweepMinX = Mathf.Min(startMinX, endMinX);
                float sweepMaxX = Mathf.Max(startMaxX, endMaxX);
                float sweepMinZ = Mathf.Min(startMinZ, endMinZ);
                float sweepMaxZ = Mathf.Max(startMaxZ, endMaxZ);

                bool found = false;
                float bestSurface = float.MaxValue;

                foreach (var aabb in aabbs)
                {
                    if (aabb.IsTrigger || aabb.IsLiquid) continue;

                    float surfaceY = aabb.Max.y;
                    if (surfaceY <= currentFeet.y + GROUND_CHECK_TOLERANCE ||
                        surfaceY > currentFeet.y + HIGH_STEP_MAX_HEIGHT + GROUND_CHECK_TOLERANCE)
                        continue;
                    float liftAmount = surfaceY - currentFeet.y;

                    if (!AxisOverlap(sweepMinX, sweepMaxX, aabb.Min.x, aabb.Max.x, false) ||
                        !AxisOverlap(sweepMinZ, sweepMaxZ, aabb.Min.z, aabb.Max.z, false))
                        continue;

                    Vector3 aabbCenter = (aabb.Min + aabb.Max) * 0.5F;
                    Vector3 toAabb = new Vector3(aabbCenter.x - currentFeet.x, 0F, aabbCenter.z - currentFeet.z);

                    if (toAabb.sqrMagnitude <= MOVEMENT_EPSILON * MOVEMENT_EPSILON)
                        continue;

                    toAabb.Normalize();

                    if (Vector3.Dot(toAabb, moveDir) <= 0F)
                        continue;

                    if (WouldOverlapAfterLift(liftAmount, moveDir))
                        continue;

                    if (!found || surfaceY < bestSurface)
                    {
                        bestSurface = surfaceY;
                        stepLift = liftAmount;
                        found = true;
                    }
                }

                stepTargetY = bestSurface;
                return found;
            }

            bool WouldOverlapAfterLift(float lift, Vector3 checkDirection)
            {
                float testMinY = newPos.y + lift;
                float testMaxY = testMinY + PLAYER_HEIGHT;

                Vector3 forwardShift = Vector3.zero;
                Vector3 planarDirection = new Vector3(checkDirection.x, 0F, checkDirection.z);
                if (planarDirection.sqrMagnitude <= MOVEMENT_EPSILON * MOVEMENT_EPSILON)
                {
                    planarDirection = new Vector3(forward.x, 0F, forward.z);
                }

                if (planarDirection.sqrMagnitude > MOVEMENT_EPSILON * MOVEMENT_EPSILON)
                {
                    forwardShift = planarDirection.normalized * STEP_FORWARD_CHECK_OFFSET;
                }

                float shiftedX = newPos.x + forwardShift.x;
                float shiftedZ = newPos.z + forwardShift.z;

                float playerMinX = shiftedX - PLAYER_RADIUS;
                float playerMaxX = shiftedX + PLAYER_RADIUS;
                float playerMinZ = shiftedZ - PLAYER_RADIUS;
                float playerMaxZ = shiftedZ + PLAYER_RADIUS;

                foreach (var aabb in aabbs)
                {
                    if (aabb.IsTrigger || aabb.IsLiquid) continue;

                    if (!AxisOverlap(playerMinX, playerMaxX, aabb.Min.x, aabb.Max.x, false) ||
                        !AxisOverlap(playerMinZ, playerMaxZ, aabb.Min.z, aabb.Max.z, false))
                        continue;

                    if (!AxisOverlap(testMinY, testMaxY, aabb.Min.y, aabb.Max.y, false))
                        continue;

                    return true;
                }

                return false;
            }

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
            float playerFeetY = playerPos.y;
            
            // Check player's feet area (XZ plane) against terrain AABBs
            var playerMinX = playerPos.x - PLAYER_RADIUS;
            var playerMaxX = playerPos.x + PLAYER_RADIUS;
            var playerMinZ = playerPos.z - PLAYER_RADIUS;
            var playerMaxZ = playerPos.z + PLAYER_RADIUS;

            bool isGrounded = false;
            float closestGroundDist = GROUND_RAYCAST_DIST;
            float raycastMinY = playerFeetY - GROUND_RAYCAST_DIST;

            // Check terrain AABBs (not liquid, not triggers)
            foreach (var aabb in aabbs)
            {
                // Skip trigger AABBs (NoCollision blocks)
                if (aabb.IsTrigger) continue;
                
                // Check if player's feet area overlaps with this AABB in XZ plane
                bool xOverlap = playerMaxX > aabb.Min.x && playerMinX < aabb.Max.x;
                bool zOverlap = playerMaxZ > aabb.Min.z && playerMinZ < aabb.Max.z;
                
                if (!xOverlap || !zOverlap)
                    continue;

                float aabbTopY = aabb.Max.y;
                float aabbBottomY = aabb.Min.y;

                if (playerFeetY >= aabbBottomY && playerFeetY <= aabbTopY)
                {
                    closestGroundDist = 0F;
                }
                else if (aabbTopY <= playerFeetY && aabbTopY >= raycastMinY)
                {
                    float distance = playerFeetY - aabbTopY;
                    if (distance < closestGroundDist)
                    {
                        closestGroundDist = distance;
                    }
                }

                if (!isGrounded &&
                    playerFeetY >= aabbTopY - GROUND_CHECK_TOLERANCE &&
                    playerFeetY <= aabbTopY + GROUND_CHECK_TOLERANCE)
                {
                    isGrounded = true;
                }

                if (isGrounded && closestGroundDist <= 0F)
                    break;
            }

            Status.GroundDist = Mathf.Clamp(closestGroundDist, 0F, GROUND_RAYCAST_DIST);
            Status.Grounded = isGrounded;
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