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

        private const float GROUND_RAYCAST_START   = 1.5F;
        private const float GROUND_RAYCAST_DIST    = 4.0F;
        private const float GROUND_RAYCAST_OFFSET  = 0.8F; // The distance to move forward for front raycast
        private static readonly Vector3 GROUND_BOXCAST_START_POINT = new(0F, GROUND_RAYCAST_START, 0F);

        private const float LIQUID_RAYCAST_START   = 2.0F; // Liquid raycast goes downward from top of player
        private const float LIQUID_RAYCAST_DIST    = 5.0F;
        private static readonly Vector3 LIQUID_BOXCAST_START_POINT = new(0F, LIQUID_RAYCAST_START, 0F);

        private static readonly Vector3 IN_WATER_CHECK_POINT_LOWER = new(0F, 0.3F, 0F);
        private static readonly Vector3 IN_WATER_CHECK_POINT_UPPER = new(0F, 0.8F, 0F);

        public const float SURFING_LIQUID_DIST_THERSHOLD = -1.3F;

        [SerializeField] public LayerMask BlockSelectionLayer;
        [SerializeField] public LayerMask GroundLayer;
        [SerializeField] public LayerMask LiquidLayer;

        public PlayerStatus Status = new();

        public void UpdateBlockSelection(Ray? viewRay)
        {
            if (viewRay is null)
                return;

            RaycastHit viewHit;

            Vector3? castResultPos  = null;
            Vector3? castSurfaceDir = null;

            if (Physics.Raycast(viewRay.Value.origin, viewRay.Value.direction, out viewHit, 10F, BlockSelectionLayer))
            {
                castResultPos  = viewHit.point;
                castSurfaceDir = viewHit.normal;
            }
            else
                castResultPos = castSurfaceDir = null;

            if (castResultPos is not null && castSurfaceDir is not null)
            {
                Vector3 offseted = PointOnCubeSurface(castResultPos.Value) ?
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
            // ENABLED START
            Status.InWater = world.IsWaterAt(CoordConvert.Unity2MC(transform.position + IN_WATER_CHECK_POINT_LOWER));
            // ENABLED END

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
                Status.CenterDownDist = centerDownHit.distance - GROUND_RAYCAST_START;
            else
                Status.CenterDownDist = GROUND_RAYCAST_DIST - GROUND_RAYCAST_START;

            Debug.DrawRay(rayCenter, transform.up * -GROUND_RAYCAST_DIST, Color.cyan);

            // Cast another ray downwards in front of the player
            if (Physics.Raycast(rayFront, -transform.up, out frontDownHit, GROUND_RAYCAST_DIST, GroundLayer))
                Status.FrontDownDist = frontDownHit.distance - GROUND_RAYCAST_START;
            else
                Status.FrontDownDist = GROUND_RAYCAST_DIST - GROUND_RAYCAST_START;
            
            // Cast a ray downwards again, but check liquid layer this time
            if (Status.InWater)
            {
                if (Physics.Raycast(rayCenter, -transform.up, out centerDownHit, LIQUID_RAYCAST_DIST, LiquidLayer))
                {
                    Status.LiquidDist = centerDownHit.distance - LIQUID_RAYCAST_START;
                }
                else // Dived completely into water
                    Status.LiquidDist = -LIQUID_RAYCAST_DIST;
            }
            else
            {
                Status.LiquidDist = 0F;
            }

            Debug.DrawRay(rayCenter, transform.up * -GROUND_RAYCAST_DIST, Color.cyan);

            Debug.DrawRay(rayFront,  transform.up * -GROUND_RAYCAST_DIST, Color.green);
        }

        /* DISABLED START
        void OnTriggerEnter(Collider trigger)
        {
            if ((LiquidLayer.value & (1 << trigger.gameObject.layer)) != 0)
            {
                Debug.Log($"Into liquid");
                Status.InWater = true;
            }
        }

        void OnTriggerExit(Collider trigger)
        {
            if ((LiquidLayer.value & (1 << trigger.gameObject.layer)) != 0)
            {
                Debug.Log($"Out of liquid");
                Status.InWater = false;
            }
        }
        // DISABLED END */

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