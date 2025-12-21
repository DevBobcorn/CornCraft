#nullable enable
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
        private const float PLAYER_WIDTH = 0.60001F; // Make it slightly wider than 0.6 to account for floating point error
        private const float PLAYER_HEIGHT = 1.8F;
        private const float PLAYER_RADIUS = PLAYER_WIDTH * 0.5F; // 0.3
        private const float PLAYER_CENTER_Y = PLAYER_HEIGHT * 0.5F; // 0.9
        private const float GROUND_CHECK_TOLERANCE = 1E-2F; // Allow slight penetration/float
        private const float MOVEMENT_EPSILON = 1E-4F; // Threshold to consider movement negligible
        private const float MAX_STEP_HEIGHT = 0.125F;
        private const float HIGH_STEP_MAX_HEIGHT = 1.125F;
        private const float HIGH_STEP_LIFT_AMOUNT = 0.125F;
        private const float FORWARD_STEP_DOT_THRESHOLD = 0.5F;
        private const float STEP_FORWARD_CHECK_OFFSET = 0.125F;

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
            
            var steppingLimit = HIGH_STEP_MAX_HEIGHT - Status.GroundDist;

            var newPosition = CalculateNewPlayerPosition(transform.position, offset, movementForward, steppingLimit,
                aabbs, out var wasBlockedVertically, out var wasBlockedHorizontally,
                out var duringStepping, out var finishedStepping,
                out var blockingMin, out var blockingMax);

            if (wasBlockedHorizontally || wasBlockedVertically)
            {
                hasBlockingAABB = true;
                blockingAABBMin = blockingMin;
                blockingAABBMax = blockingMax;
            }
            
            /*
            if (wasBlockedHorizontally)
            {
                // Kill horizontal movement if no stepping was performed or stepping is not finished
                if (!finishedStepping)
                {
                    velocity.x = 0F;
                    velocity.z = 0F;
                }
            }
            */

            if (wasBlockedVertically)
            {
                // Kill vertical movement if moving upwards
                if (velocity.y > 0F)
                {
                    velocity.y = 0F;
                }
            }
            
            transform.position = newPosition;
        }

        /// <summary>
        /// Calculates new player position given current position, movement offset, and a list of AABBs.
        /// If any AABB is in the way, the player moves to the point touching the AABB.
        /// </summary>
        private static Vector3 CalculateNewPlayerPosition(Vector3 currentPosition, Vector3 offset, Vector3 forward, float steppingLimit,
            UnityAABB[] aabbs, out bool wasBlockedVertically, out bool wasBlockedHorizontally, out bool duringStepping, out bool finishedStepping, out Vector3 blockingMin, out Vector3 blockingMax)
        {
            var newPos = currentPosition;
            var blockMin = Vector3.zero;
            var blockMax = Vector3.zero;

            var effectiveOffset = offset;
            var adjustedToSurface = false;
            UnityAABB supportingSurface = default;
            var hasMovement = effectiveOffset.sqrMagnitude > MOVEMENT_EPSILON * MOVEMENT_EPSILON;
            
            // Whether a stepping operation was performed during this tick and has not yet finished
            duringStepping = false;
            // Whether a stepping operation was finished during this tick
            finishedStepping = false;

            if (aabbs.Length > 0 &&
                !hasMovement &&
                TryGetSupportingSurface(currentPosition, aabbs, out supportingSurface))
            {
                if (effectiveOffset.y < 0F)
                {
                    var targetY = supportingSurface.Max.y;

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
                wasBlockedVertically = adjustedToSurface;
                wasBlockedHorizontally = false;
                blockingMin = adjustedToSurface ? supportingSurface.Min : Vector3.zero;
                blockingMax = adjustedToSurface ? supportingSurface.Max : Vector3.zero;
                return finalPos;
            }

            if (steppingLimit > 0F)
            {
                // Check for forward step-ups before resolving axes
                TryHandleForwardStep(out duringStepping, out finishedStepping);
            }

            var xBlocked = false;
            var zBlocked = false;
            var yBlocked = false;

            // Apply movement axis by axis (Y first for gravity/ground resolution)
            ResolveVertical(effectiveOffset.y, ref yBlocked);
            // Don't use horizontal movement if lifting is applied
            if (!duringStepping)
            {
                ResolveHorizontalX(effectiveOffset.x, ref xBlocked);
                ResolveHorizontalZ(effectiveOffset.z, ref zBlocked);
            }
            
            wasBlockedHorizontally = xBlocked || zBlocked;
            wasBlockedVertically = yBlocked;
            blockingMin = blockMin;
            blockingMax = blockMax;
            
            return newPos;

            void TryHandleForwardStep(out bool duringStepping, out bool finishedStepping)
            {
                duringStepping = false;
                finishedStepping = false;

                var horizontalOffset = new Vector3(effectiveOffset.x, 0F, effectiveOffset.z);
                if (horizontalOffset.sqrMagnitude <= MOVEMENT_EPSILON * MOVEMENT_EPSILON)
                    return;

                var planarForward = new Vector3(forward.x, 0F, forward.z);
                if (planarForward.sqrMagnitude <= MOVEMENT_EPSILON * MOVEMENT_EPSILON)
                    return;

                planarForward.Normalize();

                var moveDir = horizontalOffset.normalized;
                if (Vector3.Dot(moveDir, planarForward) < FORWARD_STEP_DOT_THRESHOLD)
                    return;

                if (!TryFindStepSurface(horizontalOffset, moveDir, out var targetSurfaceY, out var desiredLift))
                    return;
                
                //Debug.Log($"Desired lift: {desiredLift} (Target height: {targetSurfaceY})");

                if (desiredLift > steppingLimit)
                {
                    //Debug.Log("Cancelled stepping due to height limit");
                    return;
                }

                switch (desiredLift)
                {
                    case <= GROUND_CHECK_TOLERANCE:
                        return;
                    // Short steps are climbed in one frame, taller steps inch upward until reached
                    case <= MAX_STEP_HEIGHT + GROUND_CHECK_TOLERANCE:
                        effectiveOffset.y = desiredLift; // snap directly onto the surface
                        finishedStepping = true;
                        return;
                    case > HIGH_STEP_MAX_HEIGHT + GROUND_CHECK_TOLERANCE:
                        return;
                }

                var incrementalLift = Mathf.Min(HIGH_STEP_LIFT_AMOUNT, desiredLift);
                if (incrementalLift <= GROUND_CHECK_TOLERANCE)
                    return;
                
                // If the incremental lift is greater than or equal to the desired lift, the stepping is finished
                if (incrementalLift >= desiredLift)
                    finishedStepping = true;
                else
                    duringStepping = true;

                effectiveOffset.y = incrementalLift; // climb part-way this frame
                //Debug.Log($"Incremental lift: {incrementalLift}");
            }

            bool TryFindStepSurface(Vector3 horizontalOffset, Vector3 moveDir, out float stepTargetY, out float stepLift)
            {
                var currentFeet = newPos;
                var targetFeet = currentFeet + horizontalOffset;
                stepLift = 0F;

                var startMinX = currentFeet.x - PLAYER_RADIUS;
                var startMaxX = currentFeet.x + PLAYER_RADIUS;
                var startMinZ = currentFeet.z - PLAYER_RADIUS;
                var startMaxZ = currentFeet.z + PLAYER_RADIUS;

                var endMinX = targetFeet.x - PLAYER_RADIUS;
                var endMaxX = targetFeet.x + PLAYER_RADIUS;
                var endMinZ = targetFeet.z - PLAYER_RADIUS;
                var endMaxZ = targetFeet.z + PLAYER_RADIUS;

                var sweepMinX = Mathf.Min(startMinX, endMinX);
                var sweepMaxX = Mathf.Max(startMaxX, endMaxX);
                var sweepMinZ = Mathf.Min(startMinZ, endMinZ);
                var sweepMaxZ = Mathf.Max(startMaxZ, endMaxZ);

                var found = false;
                var bestSurface = float.MaxValue;

                foreach (var aabb in aabbs)
                {
                    if (aabb.IsTrigger || aabb.IsLiquid) continue;

                    var surfaceY = aabb.Max.y;
                    if (surfaceY <= currentFeet.y + GROUND_CHECK_TOLERANCE ||
                        surfaceY > currentFeet.y + HIGH_STEP_MAX_HEIGHT + GROUND_CHECK_TOLERANCE)
                        continue;
                    var liftAmount = surfaceY - currentFeet.y;

                    if (!AxisOverlap(sweepMinX, sweepMaxX, aabb.Min.x, aabb.Max.x, false) ||
                        !AxisOverlap(sweepMinZ, sweepMaxZ, aabb.Min.z, aabb.Max.z, false))
                        continue;

                    var aabbCenter = (aabb.Min + aabb.Max) * 0.5F;
                    var toAabb = new Vector3(aabbCenter.x - currentFeet.x, 0F, aabbCenter.z - currentFeet.z);

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
                var testMinY = newPos.y + lift;
                var testMaxY = testMinY + PLAYER_HEIGHT;

                var forwardShift = Vector3.zero;
                var planarDirection = new Vector3(checkDirection.x, 0F, checkDirection.z);
                if (planarDirection.sqrMagnitude <= MOVEMENT_EPSILON * MOVEMENT_EPSILON)
                {
                    planarDirection = new Vector3(forward.x, 0F, forward.z);
                }

                if (planarDirection.sqrMagnitude > MOVEMENT_EPSILON * MOVEMENT_EPSILON)
                {
                    forwardShift = planarDirection.normalized * STEP_FORWARD_CHECK_OFFSET;
                }

                var shiftedX = newPos.x + forwardShift.x;
                var shiftedZ = newPos.z + forwardShift.z;

                // Add extra player radius to prevent false-negative
                var playerMinX = shiftedX - PLAYER_RADIUS - 0.125F;
                var playerMaxX = shiftedX + PLAYER_RADIUS + 0.125F;
                var playerMinZ = shiftedZ - PLAYER_RADIUS - 0.125F;
                var playerMaxZ = shiftedZ + PLAYER_RADIUS + 0.125F;

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
                var playerMinX = feetPosition.x - PLAYER_RADIUS;
                var playerMaxX = feetPosition.x + PLAYER_RADIUS;
                var playerMinZ = feetPosition.z - PLAYER_RADIUS;
                var playerMaxZ = feetPosition.z + PLAYER_RADIUS;

                foreach (var aabb in environment)
                {
                    if (aabb.IsTrigger || aabb.IsLiquid) continue;

                    var xOverlap = playerMaxX > aabb.Min.x && playerMinX < aabb.Max.x;
                    var zOverlap = playerMaxZ > aabb.Min.z && playerMinZ < aabb.Max.z;

                    if (!xOverlap || !zOverlap)
                        continue;

                    var surfaceY = aabb.Max.y;
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

            void ResolveHorizontalX(float movement, ref bool blocked)
            {
                if (Mathf.Abs(movement) <= 0F) return;

                var playerMinY = newPos.y;
                var playerMaxY = newPos.y + PLAYER_HEIGHT;
                var playerMinZ = newPos.z - PLAYER_RADIUS;
                var playerMaxZ = newPos.z + PLAYER_RADIUS;
                var playerMinX = newPos.x - PLAYER_RADIUS;
                var playerMaxX = newPos.x + PLAYER_RADIUS;

                var clamped = movement;

                foreach (var aabb in aabbs)
                {
                    if (aabb.IsTrigger || aabb.IsLiquid) continue;

                    if (!AxisOverlap(playerMinY, playerMaxY, aabb.Min.y, aabb.Max.y, false) ||
                        !AxisOverlap(playerMinZ, playerMaxZ, aabb.Min.z, aabb.Max.z, false))
                        continue;

                    var prev = clamped;

                    if (clamped > 0F)
                    {
                        if (playerMaxX <= aabb.Min.x &&
                            playerMaxX + clamped > aabb.Min.x)
                        {
                            var allowed = aabb.Min.x - playerMaxX;
                            clamped = Mathf.Min(clamped, Mathf.Max(0F, allowed));
                        }
                    }
                    else
                    {
                        if (playerMinX >= aabb.Max.x &&
                            playerMinX + clamped < aabb.Max.x)
                        {
                            var allowed = aabb.Max.x - playerMinX;
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

            void ResolveHorizontalZ(float movement, ref bool blocked)
            {
                if (Mathf.Abs(movement) <= 0F) return;

                var playerMinY = newPos.y;
                var playerMaxY = newPos.y + PLAYER_HEIGHT;
                var playerMinX = newPos.x - PLAYER_RADIUS;
                var playerMaxX = newPos.x + PLAYER_RADIUS;
                var playerMinZ = newPos.z - PLAYER_RADIUS;
                var playerMaxZ = newPos.z + PLAYER_RADIUS;

                var clamped = movement;

                foreach (var aabb in aabbs)
                {
                    if (aabb.IsTrigger || aabb.IsLiquid) continue;

                    if (!AxisOverlap(playerMinY, playerMaxY, aabb.Min.y, aabb.Max.y, false) ||
                        !AxisOverlap(playerMinX, playerMaxX, aabb.Min.x, aabb.Max.x, false))
                        continue;

                    var prev = clamped;

                    if (clamped > 0F)
                    {
                        if (playerMaxZ <= aabb.Min.z &&
                            playerMaxZ + clamped > aabb.Min.z)
                        {
                            var allowed = aabb.Min.z - playerMaxZ;
                            clamped = Mathf.Min(clamped, Mathf.Max(0F, allowed));
                        }
                    }
                    else
                    {
                        if (playerMinZ >= aabb.Max.z &&
                            playerMinZ + clamped < aabb.Max.z)
                        {
                            var allowed = aabb.Max.z - playerMinZ;
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

            void ResolveVertical(float movement, ref bool blocked)
            {
                if (Mathf.Abs(movement) <= 0F) return;

                var playerMinX = newPos.x - PLAYER_RADIUS;
                var playerMaxX = newPos.x + PLAYER_RADIUS;
                var playerMinZ = newPos.z - PLAYER_RADIUS;
                var playerMaxZ = newPos.z + PLAYER_RADIUS;
                var playerMinY = newPos.y;
                var playerMaxY = newPos.y + PLAYER_HEIGHT;

                var clamped = movement;

                foreach (var aabb in aabbs)
                {
                    if (aabb.IsTrigger || aabb.IsLiquid) continue;

                    if (!AxisOverlap(playerMinX, playerMaxX, aabb.Min.x, aabb.Max.x, false) ||
                        !AxisOverlap(playerMinZ, playerMaxZ, aabb.Min.z, aabb.Max.z, false))
                        continue;

                    var prev = clamped;

                    if (clamped > 0F)
                    {
                        if (playerMaxY <= aabb.Min.y &&
                            playerMaxY + clamped > aabb.Min.y)
                        {
                            var allowed = aabb.Min.y - playerMaxY;
                            clamped = Mathf.Min(clamped, Mathf.Max(0F, allowed));
                        }
                    }
                    else
                    {
                        if (playerMinY >= aabb.Max.y &&
                            playerMinY + clamped < aabb.Max.y)
                        {
                            var allowed = aabb.Max.y - playerMinY;
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