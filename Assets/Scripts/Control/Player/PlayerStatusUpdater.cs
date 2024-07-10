#nullable enable
using System;
using CraftSharp.Event;
using KinematicCharacterController;
using UnityEngine;

namespace CraftSharp.Control
{
    public class PlayerStatusUpdater : MonoBehaviour
    {
        // Ground distance check
        private const float GROUND_RAYCAST_START   = 2.5F;
        private const float GROUND_RAYCAST_DIST    = 5.0F;
        private const float GROUND_RAYCAST_OFFSET  = 0.8F; // The distance to move forward for front raycast

        private const float LIQUID_RAYCAST_START   = 2.0F; // Liquid raycast goes downward from top of player
        private const float LIQUID_RAYCAST_DIST    = 5.0F;

        // Liquid status and distance check
        private bool _useServerLiquidCheck = false;
        public const float FLOATING_DIST_THERSHOLD = -0.5F;

        [SerializeField] public LayerMask SolidLayer;
        [SerializeField] public LayerMask LiquidSurfaceLayer;
        [SerializeField] public LayerMask LiquidVolumeLayer;

        public PlayerStatus Status = new();

        public void UpdatePlayerStatus(KinematicCharacterMotor motor, Quaternion targetOrientation)
        {
            var frontDirNormalized = targetOrientation * Vector3.forward;

            // Grounded state update
            bool groundCheck = motor.GroundingStatus.FoundAnyGround;

            var rayCenter = transform.position + GROUND_RAYCAST_START * motor.CharacterUp;
            var rayFront  = rayCenter + frontDirNormalized * GROUND_RAYCAST_OFFSET;

            // Cast a ray downwards
            if (Physics.Raycast(rayCenter, -motor.CharacterUp, out RaycastHit centerDownHit, GROUND_RAYCAST_DIST, SolidLayer))
                Status.CenterDownDist = centerDownHit.distance - GROUND_RAYCAST_START;
            else
                Status.CenterDownDist = GROUND_RAYCAST_DIST - GROUND_RAYCAST_START;

            Debug.DrawRay(rayCenter, motor.CharacterUp * -GROUND_RAYCAST_DIST, Color.cyan);

            // Cast another ray downwards in front of the player
            if (Physics.Raycast(rayFront, -motor.CharacterUp, out RaycastHit frontDownHit, GROUND_RAYCAST_DIST, SolidLayer))
                Status.FrontDownDist = frontDownHit.distance - GROUND_RAYCAST_START;
            else
                Status.FrontDownDist = GROUND_RAYCAST_DIST - GROUND_RAYCAST_START;
            
            Debug.DrawRay(rayFront,  motor.CharacterUp * -GROUND_RAYCAST_DIST, Color.green);

            // Perform water volume check, if not using water check from server
            if (!_useServerLiquidCheck)
            {
                // In liquid state update
                var capsule = GetComponent<CapsuleCollider>();
                if (Physics.CheckBox(transform.position + capsule.center, new Vector3(capsule.radius, capsule.height / 2f, capsule.radius), motor.transform.rotation, LiquidVolumeLayer, QueryTriggerInteraction.Collide))
                {
                    Status.InLiquid = true;
                }
                else
                {
                    Status.InLiquid = false;
                }
            }

            if (Status.InLiquid) // In liquid
            {
                // Perform floating check
                Status.Floating = Status.LiquidDist <= FLOATING_DIST_THERSHOLD;
            }
            else // In air or on ground
            {
                // Reset floating flag, we are not even in liquid
                Status.Floating = false;
            }

            if (Status.Grounded) // Grounded in last update
            {
                // Workaround: Extra check to make sure the player is just walking on some bumped surface and happen to leave the ground
                if (!groundCheck && Status.CenterDownDist > 1.25F)
                {
                    Status.Grounded = false;
                }
            }
            else // Not grounded in last update
            {
                Status.Grounded = groundCheck;
            }
            
            // Cast a ray downwards again, but check liquid layer this time
            if (Status.InLiquid)
            {
                if (Physics.Raycast(rayCenter, -transform.up, out centerDownHit, LIQUID_RAYCAST_DIST, LiquidSurfaceLayer | LiquidVolumeLayer))
                {
                    Status.LiquidDist = centerDownHit.distance - LIQUID_RAYCAST_START;
                }
                else // Dived completely into water
                {
                    Status.LiquidDist = -LIQUID_RAYCAST_DIST;
                }
            }
            else // Not in water
            {
                Status.LiquidDist = 0F;
            }

            var barrierCheckRayHeight = Status.FrontDownDist < -0.2F ? -Status.FrontDownDist - 0.1F : 0.1F;
            var barrierCheckRayOrigin = transform.position + barrierCheckRayHeight * motor.CharacterUp;

            // Cast another ray forward from the height of barrier to figure out the slope angle before the player
            if (Physics.Raycast(barrierCheckRayOrigin, frontDirNormalized, out RaycastHit raycastHit, 1F, SolidLayer))
            {
                Status.BarrierYawAngle = Vector3.Angle(frontDirNormalized, -raycastHit.normal);
                Status.BarrierDistance = raycastHit.distance;
            }
            else // No barrier
            {
                Status.BarrierYawAngle = 0F;
                Status.BarrierDistance = 0F;
            }

            Debug.DrawRay(barrierCheckRayOrigin, frontDirNormalized, Color.blue);
        }

        private Action<PlayerLiquidEvent>? serverLiquidCallback;

        void Start()
        {
            serverLiquidCallback = (e) => {
                Status.InLiquid = e.Enter;
                _useServerLiquidCheck = true;
            };

            EventManager.Instance.Register(serverLiquidCallback);
        }

        void OnDestroy()
        {
            if (serverLiquidCallback is not null)
            {
                EventManager.Instance.Unregister(serverLiquidCallback);
            }
        }
    }
}