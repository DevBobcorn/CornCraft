#nullable enable
using UnityEngine;
using MinecraftClient.Mapping;

namespace MinecraftClient.Control
{
    public class PlayerStatusUpdater : MonoBehaviour
    {
        private static readonly Vector3 GROUND_BOXCAST_CENTER    = new(0F,   0.05F,   0F);
        private static readonly Vector3 GROUND_BOXCAST_HALF_SIZE = new(0.4F, 0.01F, 0.4F);
        private const float GROUND_BOXCAST_DIST  = 0.1F;

        private static readonly Vector3 GROUND_RAYCAST_START = new(0F,     0.5F,  0F);
        private const float GROUND_RAYCAST_DIST  = 2.0F;

        public LayerMask BlockSelectionLayer;
        public LayerMask GroundLayer;

        public PlayerStatus Status = new();

        public void UpdateBlockSelection(Camera playerCamera)
        {
            // Update block selection
            var viewRay = playerCamera.ViewportPointToRay(new(0.5F, 0.5F, 0F));

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
            Status.InWater = world.IsWaterAt(CoordConvert.Unity2MC(transform.position));

            // Update player state - on ground or not?
            if (Status.InWater)
                Status.Grounded = false;
            else // Cast a box down by 0.1 meter
                Status.Grounded = Physics.BoxCast(transform.position + GROUND_BOXCAST_CENTER, GROUND_BOXCAST_HALF_SIZE, Vector3.down, Quaternion.identity, GROUND_BOXCAST_DIST, GroundLayer);

            var rayCenter = transform.position + GROUND_RAYCAST_START;
            var rayFront  = rayCenter + AngleConvert.GetAxisAlignedOrientation(frontDirNormalized) * 0.5F;

            // Cast a ray downwards
            RaycastHit centerDownHit, frontDownHit;
            if (Physics.Raycast(rayCenter, -transform.up, out centerDownHit, GROUND_RAYCAST_DIST, GroundLayer))
                Status.CenterDownDist = centerDownHit.distance;
            else Status.CenterDownDist = GROUND_RAYCAST_DIST;

            Debug.DrawRay(rayCenter, transform.up * -GROUND_RAYCAST_DIST, Color.cyan);

            // Cast another ray downwards in front of the player
            if (Physics.Raycast(rayFront, -transform.up, out frontDownHit, GROUND_RAYCAST_DIST, GroundLayer))
                Status.FrontDownDist = frontDownHit.distance;
            else Status.FrontDownDist = GROUND_RAYCAST_DIST;

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