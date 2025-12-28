using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

namespace CraftSharp.Control
{
    /// <summary>
    /// Utility class for grid and AABB-based raycasting.
    /// </summary>
    public static class Raycaster
    {
        public struct AABBRaycastHit
        {
            public bool hit;
            public Vector3 point;
            public Direction direction;
        }
        
        public static void RaycastGridCells(Vector3 rayOrigin, Vector3 rayDirection, float maxDistance, List<Vector3Int> traversedCells)
        {
            traversedCells.Clear();
            
            // Normalize direction and handle zero-length directions
            if (rayDirection.sqrMagnitude == 0) return;
            rayDirection.Normalize();

            // Initialize current cell coordinates
            Vector3Int currentCell = new Vector3Int(
                Mathf.FloorToInt(rayOrigin.x),
                Mathf.FloorToInt(rayOrigin.y),
                Mathf.FloorToInt(rayOrigin.z));

            traversedCells.Add(currentCell);

            // Calculate step directions
            Vector3Int step = new Vector3Int(
                rayDirection.x > 0 ? 1 : -1,
                rayDirection.y > 0 ? 1 : -1,
                rayDirection.z > 0 ? 1 : -1);

            // Calculate tMax values (distance to next cell boundary)
            Vector3 nextBoundary = new Vector3(
                currentCell.x + (step.x > 0 ? 1 : 0),
                currentCell.y + (step.y > 0 ? 1 : 0),
                currentCell.z + (step.z > 0 ? 1 : 0));

            Vector3 tMax = new Vector3(
                (nextBoundary.x - rayOrigin.x) / rayDirection.x,
                (nextBoundary.y - rayOrigin.y) / rayDirection.y,
                (nextBoundary.z - rayOrigin.z) / rayDirection.z);

            // Calculate tDelta (distance between cell boundaries along ray)
            Vector3 tDelta = new Vector3(
                step.x / rayDirection.x,
                step.y / rayDirection.y,
                step.z / rayDirection.z);

            // Replace Infinity values with large numbers to avoid NaN issues
            for (int i = 0; i < 3; i++)
            {
                if (float.IsInfinity(tMax[i])) tMax[i] = float.MaxValue;
                if (float.IsInfinity(tDelta[i])) tDelta[i] = float.MaxValue;
            }

            float traveledDistance = 0f;

            while (traveledDistance < maxDistance)
            {
                // Find which axis has the smallest tMax
                if (tMax.x < tMax.y && tMax.x < tMax.z)
                {
                    traveledDistance = tMax.x;
                    tMax.x += tDelta.x;
                    currentCell.x += step.x;
                }
                else if (tMax.y < tMax.z)
                {
                    traveledDistance = tMax.y;
                    tMax.y += tDelta.y;
                    currentCell.y += step.y;
                }
                else
                {
                    traveledDistance = tMax.z;
                    tMax.z += tDelta.z;
                    currentCell.z += step.z;
                }

                if (traveledDistance > maxDistance) break;

                traversedCells.Add(currentCell);
            }
        }

        public static AABBRaycastHit RaycastBlockShape(Ray cellSpaceRay, BlockShape blockShape, float3? blockOffset)
        {
            AABBRaycastHit nearestHit = new AABBRaycastHit
            {
                hit = false,
                point = Vector3.zero,
                direction = Direction.Up
            };
            
            var minDistance = float.PositiveInfinity;

            foreach (var res in blockShape.AABBs.Select(aabb => RaycastAABB(cellSpaceRay, blockOffset.HasValue ?
                         aabb.WithOffset(blockOffset.Value.z, blockOffset.Value.y, blockOffset.Value.x) : aabb)))
            {
                float curDistance;
                if (res.hit && (curDistance = (res.point - cellSpaceRay.origin).magnitude) < minDistance)
                {
                    nearestHit = res;
                    minDistance = curDistance;
                }
            }
            
            return nearestHit;
        }
        
        public static AABBRaycastHit RaycastAABB(Ray ray, ShapeAABB aabb)
        {
            AABBRaycastHit result = new AABBRaycastHit
            {
                hit = false,
                point = Vector3.zero,
                direction = Direction.Up
            };

            // Calculate inverse direction for division optimization
            Vector3 invDir = new Vector3(
                1f / ray.direction.x,
                1f / ray.direction.y,
                1f / ray.direction.z);

            // Calculate intersections with the AABB's planes (Swap X and Z of AABBs)
            Vector3 t0 = new Vector3(
                (aabb.MinZ - ray.origin.x) * invDir.x,
                (aabb.MinY - ray.origin.y) * invDir.y,
                (aabb.MinX - ray.origin.z) * invDir.z);
        
            Vector3 t1 = new Vector3(
                (aabb.MaxZ - ray.origin.x) * invDir.x,
                (aabb.MaxY - ray.origin.y) * invDir.y,
                (aabb.MaxX - ray.origin.z) * invDir.z);

            // Find min and max values for each axis
            Vector3 tMin = Vector3.Min(t0, t1);
            Vector3 tMax = Vector3.Max(t0, t1);

            // Find the largest min value and smallest max value
            float largestMin = Mathf.Max(Mathf.Max(tMin.x, tMin.y), tMin.z);
            float smallestMax = Mathf.Min(Mathf.Min(tMax.x, tMax.y), tMax.z);

            // Check if the ray misses the AABB entirely
            if (largestMin > smallestMax || smallestMax < 0)
            {
                return result;
            }

            // Calculate hit distance and point
            float hitDistance = largestMin > 0 ? largestMin : smallestMax;

            result.hit = true;
            result.point = ray.origin + ray.direction * hitDistance;

            // Determine which face was hit
            if (Mathf.Approximately(hitDistance, tMin.x))
            {
                result.direction = invDir.x > 0 ? Direction.North : Direction.South;
            }
            else if (Mathf.Approximately(hitDistance, tMin.y))
            {
                result.direction = invDir.y > 0 ? Direction.Down : Direction.Up;
            }
            else if (Mathf.Approximately(hitDistance, tMin.z))
            {
                result.direction = invDir.z > 0 ? Direction.West : Direction.East;
            }
            else if (Mathf.Approximately(hitDistance, tMax.x))
            {
                result.direction = invDir.x > 0 ? Direction.South : Direction.North;
            }
            else if (Mathf.Approximately(hitDistance, tMax.y))
            {
                result.direction = invDir.y > 0 ? Direction.Up : Direction.Down;
            }
            else if (Mathf.Approximately(hitDistance, tMax.z))
            {
                result.direction = invDir.z > 0 ? Direction.East : Direction.West;
            }

            return result;
        }
    }
}