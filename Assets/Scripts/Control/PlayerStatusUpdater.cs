#nullable enable
using UnityEngine;
using MinecraftClient.Mapping;

namespace MinecraftClient.Control
{
    public class PlayerStatusUpdater : MonoBehaviour
    {
        // Ground distance check
        private const float GROUND_RAYCAST_START   = 2.5F;
        private const float GROUND_RAYCAST_DIST    = 4.0F;
        private const float GROUND_RAYCAST_OFFSET  = 0.8F; // The distance to move forward for front raycast
        private static readonly Vector3 GROUND_BOXCAST_START_POINT = new(0F, GROUND_RAYCAST_START, 0F);

        private const float LIQUID_RAYCAST_START   = 2.0F; // Liquid raycast goes downward from top of player
        private const float LIQUID_RAYCAST_DIST    = 5.0F;
        private static readonly Vector3 LIQUID_BOXCAST_START_POINT = new(0F, LIQUID_RAYCAST_START, 0F);

        private static readonly Vector3 ANGLE_CHECK_RAYCAST_START_POINT = new(0F, 0.02F, 0F);

        private static readonly Vector3 IN_WATER_CHECK_POINT_LOWER = new(0F, 0.3F, 0F);
        private static readonly Vector3 IN_WATER_CHECK_POINT_UPPER = new(0F, 0.8F, 0F);

        public const float SURFING_LIQUID_DIST_THERSHOLD = -0.6F;

        [SerializeField] public LayerMask GroundLayer;
        [SerializeField] public LayerMask LiquidLayer;

        [HideInInspector] public bool UseBoxCastForGroundedCheck = false;

        // Grounded check - using boxcast
        [HideInInspector] public Vector3 GroundBoxcastCenter   = new(0F,    0.05F,    0F);
        [HideInInspector] public Vector3 GroundBoxcastHalfSize = new(0.35F, 0.01F, 0.35F);
        [HideInInspector] public float   GroundBoxcastDist     = 0.1F;

        // Grounded check - using spherecast
        [HideInInspector] public Vector3 GroundSpherecastCenter = new(0F,   0.4F,   0F);
        [HideInInspector] public float   GroundSpherecastRadius = 0.35F;
        [HideInInspector] public float   GroundSpherecastDist   = 0.1F;

        public PlayerStatus Status = new();

        public void UpdatePlayerStatus(World world, Vector3 frontDirNormalized)
        {
            // Update player state - in water or not?
            // ENABLED START
            Status.InLiquid = world.IsWaterAt(CoordConvert.Unity2MC(transform.position + IN_WATER_CHECK_POINT_LOWER));
            // ENABLED END */

            // Update player state - on ground or not?
            if (Status.InLiquid)
                Status.Grounded = false;
            else // Perform grounded check
            {
                if (UseBoxCastForGroundedCheck) // Cast a box down to check if player is grounded
                    Status.Grounded = Physics.BoxCast(transform.position + GroundBoxcastCenter, GroundBoxcastHalfSize, -transform.up, Quaternion.identity, GroundBoxcastDist, GroundLayer);
                else
                {
                    RaycastHit hit;
                    Status.Grounded = Physics.SphereCast(transform.position + GroundSpherecastCenter, GroundSpherecastRadius, -transform.up, out hit, GroundSpherecastDist, GroundLayer);
                }
            }

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
            
            Debug.DrawRay(rayFront,  transform.up * -GROUND_RAYCAST_DIST, Color.green);
            
            // Cast a ray downwards again, but check liquid layer this time
            if (Status.InLiquid)
            {
                var rayLiquid = transform.position + LIQUID_BOXCAST_START_POINT;

                if (Physics.Raycast(rayCenter, -transform.up, out centerDownHit, LIQUID_RAYCAST_DIST, LiquidLayer))
                {
                    Status.LiquidDist = centerDownHit.distance - LIQUID_RAYCAST_START;
                }
                else // Dived completely into water
                    Status.LiquidDist = -LIQUID_RAYCAST_DIST;
            }
            else // Not in water
                Status.LiquidDist = 0F;
            
            RaycastHit angleCheckHit;

            var angleCheckRayOrigin = transform.position + ANGLE_CHECK_RAYCAST_START_POINT;

            // Cast a ray forward from feet to figure out whether the slope angle before the player
            if (Physics.Raycast(angleCheckRayOrigin, frontDirNormalized, out angleCheckHit, 1F, GroundLayer))
            {
                Debug.DrawRay(angleCheckHit.point, angleCheckHit.normal, Color.magenta);

                Status.GroundSlope = Vector3.Angle(transform.up, angleCheckHit.normal);
            }
            else
                Status.GroundSlope = 0F;
            
            Debug.DrawRay(angleCheckRayOrigin, frontDirNormalized, Color.red);

            var barrierCheckRayOrigin = transform.position + new Vector3(0F, Status.FrontDownDist < -0.2F ? -Status.FrontDownDist - 0.1F : 0.1F, 0F);

            // Cast another ray forward from the height of barrier to figure out whether the slope angle before the player
            if (Physics.Raycast(barrierCheckRayOrigin, frontDirNormalized, out angleCheckHit, 1F, GroundLayer))
            {
                Status.BarrierAngle = Vector3.Angle(frontDirNormalized, -angleCheckHit.normal);
                Status.BarrierDist = angleCheckHit.distance;
            }
            else // No barrier
            {
                Status.BarrierAngle = 0F;
                Status.BarrierDist = 0F;
            }

            Debug.DrawRay(barrierCheckRayOrigin, frontDirNormalized, Color.blue);
            
        }

        /* DISABLED START
        void OnTriggerEnter(Collider trigger)
        {
            if ((LiquidLayer.value & (1 << trigger.gameObject.layer)) != 0)
            {
                Debug.Log($"Into liquid");
                Status.InLiquid = true;
            }
        }

        void OnTriggerExit(Collider trigger)
        {
            if ((LiquidLayer.value & (1 << trigger.gameObject.layer)) != 0)
            {
                Debug.Log($"Out of liquid");
                Status.InLiquid = false;
            }
        }
        // DISABLED END */

        void OnDrawGizmos()
        {
            if (UseBoxCastForGroundedCheck)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireCube(transform.position + GroundBoxcastCenter, GroundBoxcastHalfSize * 2F);
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireCube(transform.position + GroundBoxcastCenter + Vector3.down * GroundBoxcastDist, GroundBoxcastHalfSize * 2F);
            }
            else
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(transform.position + GroundSpherecastCenter, GroundSpherecastRadius);
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(transform.position + GroundSpherecastCenter + Vector3.down * GroundSpherecastDist, GroundSpherecastRadius);
            }
        }

    }
}