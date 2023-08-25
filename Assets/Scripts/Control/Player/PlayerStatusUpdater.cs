#nullable enable
using System;
using CraftSharp.Event;
using UnityEngine;

namespace CraftSharp.Control
{
    public class PlayerStatusUpdater : MonoBehaviour
    {
        // Ground distance check
        private const float GROUND_RAYCAST_START   = 2.5F;
        private const float GROUND_RAYCAST_DIST    = 5.0F;
        private const float GROUND_RAYCAST_OFFSET  = 0.8F; // The distance to move forward for front raycast
        private static readonly Vector3 GROUND_BOXCAST_START_POINT = new(0F, GROUND_RAYCAST_START, 0F);

        private const float LIQUID_RAYCAST_START   = 2.0F; // Liquid raycast goes downward from top of player
        private const float LIQUID_RAYCAST_DIST    = 5.0F;
        private static readonly Vector3 ANGLE_CHECK_RAYCAST_START_POINT = new(0F, 0.02F, 0F);

        [SerializeField] public LayerMask GroundLayer;
        [SerializeField] public LayerMask LiquidSurfaceLayer;
        [SerializeField] public LayerMask LiquidVolumeLayer;

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

        public void UpdatePlayerStatus(Vector3 frontDirNormalized)
        {
            // Update player state - on ground or not?
            bool groundCheck;
            if (UseBoxCastForGroundedCheck) // Cast a box down to check if player is grounded
            {
                groundCheck = Physics.BoxCast(transform.position + GroundBoxcastCenter, GroundBoxcastHalfSize, -transform.up, Quaternion.identity, GroundBoxcastDist, GroundLayer);
            }
            else
            {
                groundCheck = Physics.SphereCast(transform.position + GroundSpherecastCenter, GroundSpherecastRadius, -transform.up, out _, GroundSpherecastDist, GroundLayer);
            }

            var rayCenter = transform.position + GROUND_BOXCAST_START_POINT;
            // TODO var rayFront  = rayCenter + AngleConvert.GetAxisAlignedDirection(frontDirNormalized) * GROUND_RAYCAST_OFFSET;
            var rayFront  = rayCenter + frontDirNormalized * GROUND_RAYCAST_OFFSET;

            // Cast a ray downwards
            if (Physics.Raycast(rayCenter, -transform.up, out RaycastHit centerDownHit, GROUND_RAYCAST_DIST, GroundLayer))
                Status.CenterDownDist = centerDownHit.distance - GROUND_RAYCAST_START;
            else
                Status.CenterDownDist = GROUND_RAYCAST_DIST - GROUND_RAYCAST_START;

            Debug.DrawRay(rayCenter, transform.up * -GROUND_RAYCAST_DIST, Color.cyan);

            // Cast another ray downwards in front of the player
            if (Physics.Raycast(rayFront, -transform.up, out RaycastHit frontDownHit, GROUND_RAYCAST_DIST, GroundLayer))
                Status.FrontDownDist = frontDownHit.distance - GROUND_RAYCAST_START;
            else
                Status.FrontDownDist = GROUND_RAYCAST_DIST - GROUND_RAYCAST_START;
            
            Debug.DrawRay(rayFront,  transform.up * -GROUND_RAYCAST_DIST, Color.green);

            if (Status.InLiquid && Status.CenterDownDist < 1F)
            {
                if (Status.LiquidDist > -0.4F)
                    Status.Grounded = true;
            }
            else if (Status.Grounded)
            {
                // Extra check to make sure the player is just walking on some bumped surface and happen to leave the ground
                if (!groundCheck && Status.CenterDownDist > 1.75F)
                    Status.Grounded = false;
            }
            else
                Status.Grounded = groundCheck;
            
            // Cast a ray downwards again, but check liquid layer this time
            if (Status.InLiquid)
            {
                if (Physics.Raycast(rayCenter, -transform.up, out centerDownHit, LIQUID_RAYCAST_DIST, LiquidSurfaceLayer | LiquidVolumeLayer))
                {
                    Status.LiquidDist = centerDownHit.distance - LIQUID_RAYCAST_START;
                }
                else // Dived completely into water
                    Status.LiquidDist = -LIQUID_RAYCAST_DIST;
            }
            else // Not in water
                Status.LiquidDist = 0F;

            var angleCheckRayOrigin = transform.position + ANGLE_CHECK_RAYCAST_START_POINT;

            // Cast a ray forward from feet to figure out whether the slope angle before the player
            if (Physics.Raycast(angleCheckRayOrigin, frontDirNormalized, out RaycastHit angleCheckHit, 1F, GroundLayer))
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

        // Called by trigger collider
        void OnTriggerEnter(Collider trigger)
        {
            if ((LiquidVolumeLayer.value & (1 << trigger.gameObject.layer)) != 0)
            {
                EventManager.Instance.Broadcast(new PlayerLiquidEvent(true));
            }
        }

        // Called by trigger collider
        void OnTriggerExit(Collider trigger)
        {
            if ((LiquidVolumeLayer.value & (1 << trigger.gameObject.layer)) != 0)
            {
                EventManager.Instance.Broadcast(new PlayerLiquidEvent(false));
            }
        }

        public void IntoLiquid()
        {
            Status.InLiquid = true;
        }

        public void OutOfLiquid()
        {
            Status.InLiquid = false;
        }

        private Action<PlayerLiquidEvent>? playerLiquidCallback;

        void Start()
        {
            playerLiquidCallback = (e) => {
                if (e.Enter)
                {
                    IntoLiquid();
                }
                else
                {
                    OutOfLiquid();
                }
            };

            EventManager.Instance.Register(playerLiquidCallback);
        }

        void OnDestroy()
        {
            if (playerLiquidCallback is not null)
            {
                EventManager.Instance.Unregister(playerLiquidCallback);
            }
        }

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