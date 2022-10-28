#nullable enable
using UnityEngine;
using MinecraftClient.Mapping;

namespace MinecraftClient.Control
{
    public class PlayerStatusUpdater : MonoBehaviour
    {
        private static readonly Vector3 GROUND_BOXCAST_CENTER    = new(0F,   0.05F,   0F);
        private static readonly Vector3 GROUND_BOXCAST_HALF_SIZE = new(0.375F, 0.01F, 0.375F);
        private const float GROUND_BOXCAST_DIST  = 0.1F;

        private const float GROUND_RAYCAST_START   = 1.1F;
        private const float GROUND_RAYCAST_DIST    = 4.0F;
        private const float GROUND_RAYCAST_OFFSET  = 0.8F;
        private static readonly Vector3 GROUND_BOXCAST_START_POINT = new(0F, GROUND_RAYCAST_START, 0F);

        private static readonly Vector3 IN_WATER_CHECK_POINT_LOWER = new(0F, 0.3F, 0F);
        private static readonly Vector3 IN_WATER_CHECK_POINT_UPPER = new(0F, 0.8F, 0F);

        public LayerMask BlockSelectionLayer;
        public LayerMask GroundLayer;

        public PlayerStatus Status = new();

        public void UpdateBlockSelection(Ray viewRay)
        {
            RaycastHit viewHit;

            Vector3? castResultPos  = null;
            Vector3? castSurfaceDir = null;

            if (Physics.Raycast(viewRay.origin, viewRay.direction, out viewHit, 10F, BlockSelectionLayer))
            {
                castResultPos  = viewHit.point;
                castSurfaceDir = viewHit.normal;
            }
            else
                castResultPos = castSurfaceDir = null;

            if (castResultPos is not null && castSurfaceDir is not null)
            {
                Vector3 offseted  = PointOnCubeSurface(castResultPos.Value) ?
                        castResultPos.Value - castSurfaceDir.Value * 0.5F : castResultPos.Value;
                
                Vector3 selection = new(Mathf.Floor(offseted.x), Mathf.Floor(offseted.y), Mathf.Floor(offseted.z));

                Status.TargetBlockPos = CoordConvert.Unity2MC(selection);
            }
            else
                Status.TargetBlockPos = null;

        }

        public void UpdatePlayerStatus(World world, Vector3 frontDirNormalized)
        {
            // Update player state - in water or not?
            Status.InWater = world.IsWaterAt(CoordConvert.Unity2MC(transform.position + IN_WATER_CHECK_POINT_LOWER));

            if (Status.InWater)
                Status.OnWaterSurface = !world.IsWaterAt(CoordConvert.Unity2MC(transform.position + IN_WATER_CHECK_POINT_UPPER));
            else
                Status.OnWaterSurface = false;

            // Update player state - on ground or not?
            if (Status.InWater)
                Status.Grounded = false;
            else // Cast a box down by 0.1 meter
                Status.Grounded = Physics.BoxCast(transform.position + GROUND_BOXCAST_CENTER, GROUND_BOXCAST_HALF_SIZE, Vector3.down, Quaternion.identity, GROUND_BOXCAST_DIST, GroundLayer);

            var rayCenter = transform.position + GROUND_BOXCAST_START_POINT;
            // TODO var rayFront  = rayCenter + AngleConvert.GetAxisAlignedDirection(frontDirNormalized) * GROUND_RAYCAST_OFFSET;
            var rayFront  = rayCenter + frontDirNormalized * GROUND_RAYCAST_OFFSET;

            // Cast a ray downwards
            RaycastHit centerDownHit, frontDownHit;
            if (Physics.Raycast(rayCenter, -transform.up, out centerDownHit, GROUND_RAYCAST_DIST, GroundLayer))
            {
                //Status.CenterDownDist = Mathf.Max(0F, centerDownHit.distance - GROUND_RAYCAST_START);
                Status.CenterDownDist = centerDownHit.distance - GROUND_RAYCAST_START;
            }
            else
                Status.CenterDownDist = GROUND_RAYCAST_DIST - GROUND_RAYCAST_START;

            Debug.DrawRay(rayCenter, transform.up * -GROUND_RAYCAST_DIST, Color.cyan);

            // Cast another ray downwards in front of the player
            if (Physics.Raycast(rayFront, -transform.up, out frontDownHit, GROUND_RAYCAST_DIST, GroundLayer))
            {
                //Status.FrontDownDist = Mathf.Max(0F, frontDownHit.distance - GROUND_RAYCAST_START);
                Status.FrontDownDist = frontDownHit.distance - GROUND_RAYCAST_START;
            }
            else
                Status.FrontDownDist = GROUND_RAYCAST_DIST - GROUND_RAYCAST_START;

            Debug.DrawRay(rayFront,  transform.up * -GROUND_RAYCAST_DIST, Color.green);
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube(transform.position + GROUND_BOXCAST_CENTER, GROUND_BOXCAST_HALF_SIZE * 2F);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position + GROUND_BOXCAST_CENTER + Vector3.down * GROUND_BOXCAST_DIST, GROUND_BOXCAST_HALF_SIZE * 2F);
        }

        private static bool PointOnGridEdge(float value)
        {
            var delta = value - Mathf.Floor(value);
            return delta < 0.01F || delta > 0.99F;
        }

        private static bool PointOnCubeSurface(Vector3 point)
        {
            return PointOnGridEdge(point.x) || PointOnGridEdge(point.y) || PointOnGridEdge(point.z);
        }


    }
}