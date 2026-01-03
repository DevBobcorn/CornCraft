#nullable enable
using UnityEngine;

using CraftSharp.Rendering;

namespace CraftSharp.Control
{
    public class PlayerStatusUpdater : MonoBehaviour
    {
        public const float ABOVE_LIQUID_HEIGHT_WHEN_FLOATING = 0.75F;
        private const float GROUND_CHECK_TOLERANCE = 1E-2F; // Allow slight penetration/float
        private const float MOVEMENT_EPSILON = 1E-4F; // Threshold to consider movement negligible
        private const float MAX_STEP_HEIGHT = 0.125F;
        private const float HIGH_STEP_MAX_HEIGHT = 1.125F;
        private const float HIGH_STEP_MAX_HEIGHT_SNEAKING = 0.5F;
        private const float HIGH_STEP_LIFT_AMOUNT = 0.125F;
        private const float FORWARD_STEP_DOT_THRESHOLD = 0.5F;
        private const float STEP_FORWARD_CHECK_OFFSET = 0.125F;
        private const float SNEAK_FALL_PREVENTION_THRESHOLD = 0.6F;

        private float gizmoPlayerWidth = 1F;
        private float gizmoPlayerHeight = 1F;

        private readonly struct PlayerDimensions
        {
            public readonly float Width;
            public readonly float Height;
            public readonly float Radius;
            public readonly float CenterY;
            public readonly float GroundRaycastDist;
            public readonly float LiquidRaycastDist;

            public PlayerDimensions(float width, float height)
            {
                Width = width + 0.0001F; // Make it slightly wider to account for floating point error
                Height = height;
                Radius = Width * 0.5F;
                CenterY = Height * 0.5F;
                GroundRaycastDist = Height;
                LiquidRaycastDist = Height;
            }
        }

        public readonly PlayerStatus Status = new();
        
        // Store last movement for gizmo visualization
        private Vector3 lastMovementOffset = Vector3.zero;
        
        // Store blocking AABB for gizmo visualization
        private bool hasBlockingAABB = false;
        private Vector3 blockingAABBMin = Vector3.zero;
        private Vector3 blockingAABBMax = Vector3.zero;

        private void CachePlayerDimensions(PlayerDimensions dimensions)
        {
            gizmoPlayerWidth = dimensions.Width;
            gizmoPlayerHeight = dimensions.Height;
        }

        public void UpdatePlayerStatus(UnityAABB[] terrainAABBs, UnityAABB[] liquidAABBs,
            float playerWidth, float playerHeight)
        {
            var dimensions = new PlayerDimensions(playerWidth, playerHeight);
            CachePlayerDimensions(dimensions);

            // Grounded state update
            CheckGrounded(terrainAABBs, dimensions);
            
            // Ceiling distance update
            CheckCeiling(terrainAABBs, dimensions);
            
            // Liquid status update
            CheckInLiquid(liquidAABBs, dimensions);
        }

        public void UpdatePlayerPosition(ref Vector3 velocity, float deltaTime, bool isSneaking, UnityAABB[] terrainAABBs,
            float playerWidth, float playerHeight)
        {
            var dimensions = new PlayerDimensions(playerWidth, playerHeight);
            CachePlayerDimensions(dimensions);

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
            
            var steppingLimit = HIGH_STEP_MAX_HEIGHT - Status.GroundDistFromFeet;

            // Special handling: If player is currently in/above liquid, don't
            // use GroundDistFromFeet (because the liquid can support them)
            if (Status.LiquidDistFromHead < dimensions.LiquidRaycastDist)
            {
                steppingLimit = HIGH_STEP_MAX_HEIGHT;
            }

            // Special handling: If player is sneaking, limit the stepping height
            if (Status.Sneaking)
            {
                steppingLimit = HIGH_STEP_MAX_HEIGHT_SNEAKING - Status.GroundDistFromFeet;
            }

            var newPosition = CalculateNewPlayerPosition(transform.position, offset, movementForward,
                steppingLimit, isSneaking, terrainAABBs, dimensions,
                out var wasBlockedVertically, out var wasBlockedHorizontally,
                out var duringStepping, out var finishedStepping,
                out var blockingMin, out var blockingMax);

            if (wasBlockedHorizontally || wasBlockedVertically)
            {
                hasBlockingAABB = true;
                blockingAABBMin = blockingMin;
                blockingAABBMax = blockingMax;
            }

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
        private static Vector3 CalculateNewPlayerPosition(Vector3 currentPosition, Vector3 offset, Vector3 forward, float steppingLimit, bool isSneaking,
            UnityAABB[] terrainAABBs, PlayerDimensions dimensions, out bool wasBlockedVertically, out bool wasBlockedHorizontally, out bool duringStepping, out bool finishedStepping, out Vector3 blockingMin, out Vector3 blockingMax)
        {
            var newPos = currentPosition;
            var blockMin = Vector3.zero;
            var blockMax = Vector3.zero;

            var playerRadius = dimensions.Radius;
            var playerHeight = dimensions.Height;
            
            // innerRadius is used for falling prevention during sneaking
            // Make the radius smaller to prevent falling after sneaking state is stopped
            var innerRadius = dimensions.Radius - 0.01F;

            var effectiveOffset = offset;
            var adjustedToSurface = false;
            UnityAABB supportingSurface = default;
            var hasMovement = effectiveOffset.sqrMagnitude > MOVEMENT_EPSILON * MOVEMENT_EPSILON;
            
            // Whether a stepping operation was performed during this tick and has not yet finished
            duringStepping = false;
            // Whether a stepping operation was finished during this tick
            finishedStepping = false;

            if (terrainAABBs.Length > 0 &&
                !hasMovement &&
                TryGetSupportingSurface(currentPosition, out supportingSurface))
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
            if (effectiveOffset.sqrMagnitude <= MOVEMENT_EPSILON * MOVEMENT_EPSILON || terrainAABBs.Length == 0)
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

                var startMinX = currentFeet.x - playerRadius;
                var startMaxX = currentFeet.x + playerRadius;
                var startMinZ = currentFeet.z - playerRadius;
                var startMaxZ = currentFeet.z + playerRadius;

                var endMinX = targetFeet.x - playerRadius;
                var endMaxX = targetFeet.x + playerRadius;
                var endMinZ = targetFeet.z - playerRadius;
                var endMaxZ = targetFeet.z + playerRadius;

                var sweepMinX = Mathf.Min(startMinX, endMinX);
                var sweepMaxX = Mathf.Max(startMaxX, endMaxX);
                var sweepMinZ = Mathf.Min(startMinZ, endMinZ);
                var sweepMaxZ = Mathf.Max(startMaxZ, endMaxZ);

                var found = false;
                var bestSurface = float.MaxValue;

                foreach (var aabb in terrainAABBs)
                {
                    if (aabb.IsTrigger) continue;

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
                var testMaxY = testMinY + playerHeight;

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
                var playerMinX = shiftedX - playerRadius - 0.125F;
                var playerMaxX = shiftedX + playerRadius + 0.125F;
                var playerMinZ = shiftedZ - playerRadius - 0.125F;
                var playerMaxZ = shiftedZ + playerRadius + 0.125F;

                foreach (var aabb in terrainAABBs)
                {
                    if (aabb.IsTrigger) continue;

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

            // Gets the fall distance at a given horizontal position (X, Z) from the current feet Y position.
            // Returns the distance from feetY to the ground below, or a large value if no ground is found.
            float GetFallDistanceAtPosition(float testX, float testZ, float feetY)
            {
                var playerMinX = testX - innerRadius;
                var playerMaxX = testX + innerRadius;
                var playerMinZ = testZ - innerRadius;
                var playerMaxZ = testZ + innerRadius;

                var closestGroundY = float.MinValue;
                var foundGround = false;

                foreach (var aabb in terrainAABBs)
                {
                    if (aabb.IsTrigger) continue;

                    // Check if player's feet area overlaps with this AABB in XZ plane
                    var xOverlap = playerMaxX > aabb.Min.x && playerMinX < aabb.Max.x;
                    var zOverlap = playerMaxZ > aabb.Min.z && playerMinZ < aabb.Max.z;

                    if (!xOverlap || !zOverlap)
                        continue;

                    var aabbTopY = aabb.Max.y;

                    // Only consider ground that is at or below the current feet position
                    if (aabbTopY <= feetY && aabbTopY > closestGroundY)
                    {
                        closestGroundY = aabbTopY;
                        foundGround = true;
                    }
                }

                if (foundGround)
                {
                    return feetY - closestGroundY;
                }

                // If no ground found, return a large value to indicate unsafe movement
                return float.MaxValue;
            }

            // Finds the maximum safe movement distance along X axis that won't cause a fall > threshold.
            // Uses binary search to find the safe distance.
            float FindMaxSafeMovementX(float startX, float startZ, float feetY, float movement, float maxFallDistance)
            {
                var direction = Mathf.Sign(movement);
                var absMovement = Mathf.Abs(movement);
                var minMovement = 0F;
                var maxMovement = absMovement;

                // Binary search for maximum safe distance
                const int maxIterations = 10;
                for (int i = 0; i < maxIterations; i++)
                {
                    var testMovement = (minMovement + maxMovement) * 0.5F;
                    var testX = startX + testMovement * direction;
                    var testZ = startZ;

                    var fallDistance = GetFallDistanceAtPosition(testX, testZ, feetY);

                    if (fallDistance <= maxFallDistance)
                    {
                        // This distance is safe, try to go further
                        minMovement = testMovement;
                    }
                    else
                    {
                        // This distance is unsafe, reduce movement
                        maxMovement = testMovement;
                    }

                    // Stop if we've converged
                    if (maxMovement - minMovement < MOVEMENT_EPSILON)
                        break;
                }

                // Return the safe movement distance with original sign
                return minMovement * direction;
            }

            // Finds the maximum safe movement distance along Z axis that won't cause a fall > threshold.
            // Uses binary search to find the safe distance.
            float FindMaxSafeMovementZ(float startX, float startZ, float feetY, float movement, float maxFallDistance)
            {
                var direction = Mathf.Sign(movement);
                var absMovement = Mathf.Abs(movement);
                var minMovement = 0F;
                var maxMovement = absMovement;

                // Binary search for maximum safe distance
                const int maxIterations = 10;
                for (int i = 0; i < maxIterations; i++)
                {
                    var testMovement = (minMovement + maxMovement) * 0.5F;
                    var testX = startX;
                    var testZ = startZ + testMovement * direction;

                    var fallDistance = GetFallDistanceAtPosition(testX, testZ, feetY);

                    if (fallDistance <= maxFallDistance)
                    {
                        // This distance is safe, try to go further
                        minMovement = testMovement;
                    }
                    else
                    {
                        // This distance is unsafe, reduce movement
                        maxMovement = testMovement;
                    }

                    // Stop if we've converged
                    if (maxMovement - minMovement < MOVEMENT_EPSILON)
                        break;
                }

                // Return the safe movement distance with original sign
                return minMovement * direction;
            }

            bool TryGetSupportingSurface(Vector3 feetPosition, out UnityAABB surface)
            {
                var playerMinX = feetPosition.x - playerRadius;
                var playerMaxX = feetPosition.x + playerRadius;
                var playerMinZ = feetPosition.z - playerRadius;
                var playerMaxZ = feetPosition.z + playerRadius;

                foreach (var aabb in terrainAABBs)
                {
                    if (aabb.IsTrigger) continue;

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
                var playerMaxY = newPos.y + playerHeight;
                var playerMinZ = newPos.z - playerRadius;
                var playerMaxZ = newPos.z + playerRadius;
                var playerMinX = newPos.x - playerRadius;
                var playerMaxX = newPos.x + playerRadius;

                var clamped = movement;

                foreach (var aabb in terrainAABBs)
                {
                    if (aabb.IsTrigger) continue;

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

                // Sneaking fall prevention: check if movement would cause a fall > 0.6 units
                if (isSneaking && Mathf.Abs(clamped) > MOVEMENT_EPSILON)
                {
                    var testX = newPos.x + clamped;
                    var fallDistance = GetFallDistanceAtPosition(testX, newPos.z, newPos.y);
                    
                    if (fallDistance > SNEAK_FALL_PREVENTION_THRESHOLD)
                    {
                        // Binary search to find the maximum safe movement distance
                        var safeMovement = FindMaxSafeMovementX(newPos.x, newPos.z, newPos.y, clamped, SNEAK_FALL_PREVENTION_THRESHOLD);
                        clamped = safeMovement;
                        if (Mathf.Abs(clamped) < Mathf.Abs(movement))
                        {
                            blocked = true;
                        }
                    }
                }

                newPos.x += clamped;
            }

            void ResolveHorizontalZ(float movement, ref bool blocked)
            {
                if (Mathf.Abs(movement) <= 0F) return;

                var playerMinY = newPos.y;
                var playerMaxY = newPos.y + playerHeight;
                var playerMinX = newPos.x - playerRadius;
                var playerMaxX = newPos.x + playerRadius;
                var playerMinZ = newPos.z - playerRadius;
                var playerMaxZ = newPos.z + playerRadius;

                var clamped = movement;

                foreach (var aabb in terrainAABBs)
                {
                    if (aabb.IsTrigger) continue;

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

                // Sneaking fall prevention: check if movement would cause a fall > 0.6 units
                if (isSneaking && Mathf.Abs(clamped) > MOVEMENT_EPSILON)
                {
                    var testZ = newPos.z + clamped;
                    var fallDistance = GetFallDistanceAtPosition(newPos.x, testZ, newPos.y);
                    
                    if (fallDistance > SNEAK_FALL_PREVENTION_THRESHOLD)
                    {
                        // Binary search to find the maximum safe movement distance
                        var safeMovement = FindMaxSafeMovementZ(newPos.x, newPos.z, newPos.y, clamped, SNEAK_FALL_PREVENTION_THRESHOLD);
                        clamped = safeMovement;
                        if (Mathf.Abs(clamped) < Mathf.Abs(movement))
                        {
                            blocked = true;
                        }
                    }
                }

                newPos.z += clamped;
            }

            void ResolveVertical(float movement, ref bool blocked)
            {
                if (Mathf.Abs(movement) <= 0F) return;

                var playerMinX = newPos.x - playerRadius;
                var playerMaxX = newPos.x + playerRadius;
                var playerMinZ = newPos.z - playerRadius;
                var playerMaxZ = newPos.z + playerRadius;
                var playerMinY = newPos.y;
                var playerMaxY = newPos.y + playerHeight;

                var clamped = movement;

                foreach (var aabb in terrainAABBs)
                {
                    if (aabb.IsTrigger) continue;

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

        private void CheckGrounded(UnityAABB[] terrainAABBs, PlayerDimensions dimensions)
        {
            var playerRadius = dimensions.Radius;
            var groundRaycastDist = dimensions.GroundRaycastDist;

            // Player feet position
            var playerPos = transform.position;
            var playerFeetY = playerPos.y;
            
            // Check player's feet area (XZ plane) against terrain AABBs
            var playerMinX = playerPos.x - playerRadius;
            var playerMaxX = playerPos.x + playerRadius;
            var playerMinZ = playerPos.z - playerRadius;
            var playerMaxZ = playerPos.z + playerRadius;

            var isGrounded = false;
            var closestGroundDist = groundRaycastDist;
            var raycastMinY = playerFeetY - groundRaycastDist;

            // Check terrain AABBs (not triggers)
            foreach (var aabb in terrainAABBs)
            {
                // Skip trigger AABBs (NoCollision blocks)
                if (aabb.IsTrigger) continue;
                
                // Check if player's feet area overlaps with this AABB in XZ plane
                var xOverlap = playerMaxX > aabb.Min.x && playerMinX < aabb.Max.x;
                var zOverlap = playerMaxZ > aabb.Min.z && playerMinZ < aabb.Max.z;
                
                if (!xOverlap || !zOverlap)
                    continue;

                var aabbTopY = aabb.Max.y;
                var aabbBottomY = aabb.Min.y;

                if (playerFeetY >= aabbBottomY && playerFeetY <= aabbTopY)
                {
                    closestGroundDist = 0F;
                }
                else if (aabbTopY <= playerFeetY && aabbTopY >= raycastMinY)
                {
                    var distance = playerFeetY - aabbTopY;
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

            Status.GroundDistFromFeet = Mathf.Clamp(closestGroundDist, 0F, groundRaycastDist);
            Status.Grounded = isGrounded;
        }

        private void CheckCeiling(UnityAABB[] terrainAABBs, PlayerDimensions dimensions)
        {
            var playerRadius = dimensions.Radius;
            var ceilingRaycastDist = dimensions.Height; // Use same distance as ground check

            // Player head position
            var playerPos = transform.position;
            var playerHeadY = playerPos.y + dimensions.Height;
            
            // Check player's head area (XZ plane) against terrain AABBs
            var playerMinX = playerPos.x - playerRadius;
            var playerMaxX = playerPos.x + playerRadius;
            var playerMinZ = playerPos.z - playerRadius;
            var playerMaxZ = playerPos.z + playerRadius;

            var closestCeilingDist = ceilingRaycastDist;
            var raycastMaxY = playerHeadY + ceilingRaycastDist;

            // Check terrain AABBs (not triggers)
            foreach (var aabb in terrainAABBs)
            {
                // Skip trigger AABBs (NoCollision blocks)
                if (aabb.IsTrigger) continue;
                
                // Check if player's head area overlaps with this AABB in XZ plane
                var xOverlap = playerMaxX > aabb.Min.x && playerMinX < aabb.Max.x;
                var zOverlap = playerMaxZ > aabb.Min.z && playerMinZ < aabb.Max.z;
                
                if (!xOverlap || !zOverlap)
                    continue;

                var aabbTopY = aabb.Max.y;
                var aabbBottomY = aabb.Min.y;

                // If player's head is inside the AABB
                if (playerHeadY >= aabbBottomY && playerHeadY <= aabbTopY)
                {
                    closestCeilingDist = 0F;
                }
                // If AABB bottom is above player's head and within raycast range
                else if (aabbBottomY >= playerHeadY && aabbBottomY <= raycastMaxY)
                {
                    var distance = aabbBottomY - playerHeadY;
                    if (distance < closestCeilingDist)
                    {
                        closestCeilingDist = distance;
                    }
                }

                if (closestCeilingDist <= 0F)
                    break;
            }

            Status.CeilingDistFromHead = Mathf.Clamp(closestCeilingDist, 0F, ceilingRaycastDist);
        }

        private void CheckInLiquid(UnityAABB[] liquidAABBs, PlayerDimensions dimensions)
        {
            var playerPos = transform.position;

            var playerMinX = playerPos.x - dimensions.Radius;
            var playerMaxX = playerPos.x + dimensions.Radius;
            var playerMinY = playerPos.y;
            var playerMaxY = playerPos.y + dimensions.Height;
            var playerMinZ = playerPos.z - dimensions.Radius;
            var playerMaxZ = playerPos.z + dimensions.Radius;

            var inLiquid = false;
            var highestLiquidTop = float.MinValue;

            foreach (var aabb in liquidAABBs)
            {
                var xOverlap = playerMaxX > aabb.Min.x && playerMinX < aabb.Max.x;
                var yOverlap = playerMaxY > aabb.Min.y && playerMinY < aabb.Max.y;
                var zOverlap = playerMaxZ > aabb.Min.z && playerMinZ < aabb.Max.z;

                if (!xOverlap || !yOverlap || !zOverlap)
                    continue;

                inLiquid = true;
                highestLiquidTop = Mathf.Max(highestLiquidTop, aabb.Max.y);
            }

            Status.InLiquid = inLiquid;
            if (inLiquid)
            {
                // Distance from player head to the top of the overlapped liquid (negative when submerged)
                Status.LiquidDistFromHead = playerMaxY - highestLiquidTop;
                
                // LiquidDist <= 0 means player is submerged
                Status.Floating = ABOVE_LIQUID_HEIGHT_WHEN_FLOATING > Status.LiquidDistFromHead;
            }
            else
            {
                Status.LiquidDistFromHead = dimensions.LiquidRaycastDist;
                Status.Floating = false;
            }
        }

        private void OnDrawGizmos()
        {
            // Draw player AABB
            var playerPos = transform.position;
            var dimensions = new PlayerDimensions(gizmoPlayerWidth, gizmoPlayerHeight);
            
            // Player AABB center (position is at feet, so center is at half height)
            var center = new Vector3(
                playerPos.x,
                playerPos.y + dimensions.CenterY,
                playerPos.z
            );
            
            // Player AABB size
            var size = new Vector3(
                dimensions.Width,
                dimensions.Height,
                dimensions.Width
            );
            
            // Set gizmo color (cyan / magenta)
            Gizmos.color = Status.Grounded ? new Color(0F, 1F, 1F, 0.25F) : new Color(1F, 0F, 1F, 0.25F);
            
            // Draw cube for the AABB
            Gizmos.DrawCube(center, size);
            
            // Draw movement line if there's any movement
            if (lastMovementOffset.sqrMagnitude > 1E-6F)
            {
                // Draw line from current position to intended destination
                var endPos = playerPos + lastMovementOffset;
                
                // Use yellow for movement line
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(playerPos, endPos);
                
                // Draw a small sphere at the destination
                Gizmos.DrawSphere(endPos, 0.1F);
            }
            
            // Draw blocking AABB if movement was blocked
            if (hasBlockingAABB)
            {
                // Calculate center and size of blocking AABB
                var blockingCenter = (blockingAABBMin + blockingAABBMax) * 0.5F;
                var blockingSize = blockingAABBMax - blockingAABBMin;
                
                // Use red for blocking AABB
                Gizmos.color = new Color(1F, 0F, 0F, 0.5F); // Red with transparency
                Gizmos.DrawWireCube(blockingCenter, blockingSize);
                
                // Also draw filled cube with lower opacity
                Gizmos.color = new Color(1F, 0F, 0F, 0.25F);
                Gizmos.DrawCube(blockingCenter, blockingSize);
            }
        }
    }
}