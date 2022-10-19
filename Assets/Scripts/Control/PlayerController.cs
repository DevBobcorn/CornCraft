#nullable enable
using System;
using UnityEngine;
using MinecraftClient.Event;
using MinecraftClient.Mapping;
using MinecraftClient.Rendering;

namespace MinecraftClient.Control
{
    [RequireComponent(typeof (Rigidbody), typeof (EntityRender))]
    public class PlayerController : MonoBehaviour, IPlayerController
    {
        private CornClient? game;
        private Rigidbody? rigidBody;
        private BoxCollider? boxCollider;

        public float walkSpeed = 1F;
        public float runSpeed  = 2F;
        public float jumpSpeed = 4F;

        public float steerSpeed = 20F;

        private LayerMask interactionLayer, movementLayer, entityLayer;

        private bool entityDisabled = false;
        public bool EntityDisabled { get { return entityDisabled; } }

        private CameraController? camControl;
        private Transform? visualTransform, blockSelectionTransform;
        private EntityRender? playerRender;
        private Entity fakeEntity = new(0, EntityType.Player, new());

        private MeshRenderer? blockSelection;

        public void DisableEntity()
        {
            entityDisabled = true;
            // Update components state...
            boxCollider!.enabled = false;
            rigidBody!.velocity = Vector3.zero;
            rigidBody!.useGravity = false;

        }

        public void EnableEntity()
        {
            // Update and control...
            entityDisabled = false;
            // Update components state...
            boxCollider!.enabled = true;
            rigidBody!.useGravity = true;
        }

        public void SetEntityId(int entityId)
        {
            fakeEntity.ID = entityId;
            // Reassign this entity to refresh
            playerRender!.Entity = fakeEntity;
        }

        public void SetPosition(Location pos)
        {
            transform.position = CoordConvert.MC2Unity(pos);
            Debug.Log($"Position set to {transform.position}");

            if (game!.GetPlayer().GameMode == GameMode.Spectator) // Spectating
                DisableEntity();
            else
                EnableEntity();
        }

        public Location GetLocation() => CoordConvert.Unity2MC(transform.position);

        public void ManagedUpdate(float interval, float horInput, float verInput, bool walkMode, bool attack, bool up, bool down)
        {
            if (updateTarget) // Update block selection
            {
                var viewRay = camControl!.ActiveCamera.ViewportPointToRay(new(0.5F, 0.5F, 0F));

                RaycastHit viewHit;
                if (Physics.Raycast(viewRay.origin, viewRay.direction, out viewHit, 10F, interactionLayer))
                {
                    targetPos = viewHit.point;
                    targetDir = viewHit.normal;
                }
                else
                    targetPos = targetDir = null;

                if (targetPos is not null && targetDir is not null)
                {
                    Vector3 offseted  = onCubeSurface(targetPos.Value) ? targetPos.Value - targetDir.Value * 0.5F : targetPos.Value;
                    Vector3 selection = new(Mathf.Floor(offseted.x), Mathf.Floor(offseted.y), Mathf.Floor(offseted.z));
                    blockSelectionTransform!.position = selection;

                    targetBlockPos = CoordConvert.Unity2MC(selection);
                    blockSelection!.enabled = true;
                }
                else
                {
                    targetBlockPos = null;
                    blockSelection!.enabled = false;
                }

            }

            if (entityDisabled)
                UpdateAsSpectator(interval, horInput, verInput, walkMode, attack, up, down);
            else
                UpdateAsEntity(interval, horInput, verInput, walkMode, attack, up, down);
            
            // Update render
            playerRender!.UpdateInfoPlate(game!.GetCameraPosition(), 0F); // Use 0 as distance to camera since it doesn't matter
            playerRender!.UpdateAnimation(game!.GetTickMilSec());
            
        }

        private void UpdateAsSpectator(float interval, float horInput, float verInput, bool walkMode, bool attack, bool up, bool down)
        {
            // Update player velocity
            var newVelocity = Vector3.zero;

            // Check horizontal movement...
            if (Mathf.Abs(verInput) > 0F || Mathf.Abs(horInput) > 0F)
            {
                // Calculate current velocity...
                newVelocity = UpdateMoveVelocity(interval, horInput, verInput, walkMode ? walkSpeed : runSpeed) * 3F;
            }

            // Check vertical movement...
            if (up)
                newVelocity.y =  walkSpeed * 3F;
            else if (down)
                newVelocity.y = -walkSpeed * 3F;

            // Apply movement...
            transform.position += newVelocity * interval;

            // No need to check position validity in spectator mode,
            // just tell server the new position...
            CornClient.Instance.SyncLocation(CoordConvert.Unity2MC(transform.position), visualTransform!.eulerAngles.y - 90F, 0F);

            playerRender!.SetCurrentVelocity(newVelocity);

        }

        private static readonly Vector3 groundBoxCenter   = new(0F, 0.2F, 0F);
        private static readonly Vector3 groundBoxHalfSize = new(0.375F, 0.05F, 0.375F);
        private const float castDist = 0.22F;

        private bool isOnGround = false;
        public bool IsOnGround { get { return isOnGround; } }
        private bool isInWater = false;
        public bool IsInWater { get { return isInWater; } }
        private float centerDownDist, frontDownDist;
        private bool updateTarget = true;
        private Vector3? targetPos = null, targetDir = null;
        private Location? targetBlockPos = null;

        private void UpdateAsEntity(float interval, float horInput, float verInput, bool walkMode, bool attack, bool up, bool down)
        {
            // Update player state - in water or not?
            isInWater = game!.GetWorld().IsWaterAt(CoordConvert.Unity2MC(transform.position));

            // Update player state - on ground or not?
            if (isInWater)
                isOnGround = false;
            else // Cast a box down by 0.1 meter
                isOnGround = Physics.BoxCast(transform.position + groundBoxCenter, groundBoxHalfSize, Vector3.down, Quaternion.identity, castDist, movementLayer);

            var rayCenter = transform.position + transform.up * 0.2F;
            var rayFront  = rayCenter + GetAxisAlignedOrientation(visualTransform!.forward) * 0.5F;

            RaycastHit centerDownHit, frontDownHit;
            if (Physics.Raycast(rayCenter, -transform.up, out centerDownHit, 1F, movementLayer))
                centerDownDist = centerDownHit.distance;
            else centerDownDist = 1F;

            Debug.DrawRay(rayCenter, transform.up * -1F, Color.cyan);

            if (Physics.Raycast(rayFront,  -transform.up, out frontDownHit, 1F, movementLayer))
                frontDownDist = frontDownHit.distance;
            else frontDownDist = 1F;

            Debug.DrawRay(rayFront,  transform.up * -1F, Color.green);

            // Update player velocity
            Vector3 newVelocity;

            if (IsOnGround) // Player is on the ground
            {
                bool horizontalInput = Mathf.Abs(verInput) > 0F || Mathf.Abs(horInput) > 0F;

                // Check horizontal movement...
                if (horizontalInput) // Move
                {
                    newVelocity = UpdateMoveVelocity(interval, horInput, verInput, walkMode ? walkSpeed : runSpeed);
                    newVelocity.y = rigidBody!.velocity.y;
                }
                else // Idle
                {
                    newVelocity = Vector3.zero;
                    newVelocity.y = rigidBody!.velocity.y;
                }
                
                // Check vertical movement...
                if (up) // Jump up
                {
                    newVelocity.y = jumpSpeed;
                }
                else if (horizontalInput) // Check auto-jump
                {
                    if (centerDownDist < 0.4F) // Center is on ground, that is, not standing on the edge of a block
                    {
                        // Clamp delta to [0, 0.8], and amplify to [0, 1]
                        float slope = Mathf.Clamp01((centerDownDist - frontDownDist) * 1.25F);
                        if (IsOnGround && slope > 0F) // Amplify and apply auto-jump speed
                        {
                            //transform.position = transform.position + transform.up * (frontDownDist - centerDownDist);
                            newVelocity.y = jumpSpeed * Mathf.Sqrt(slope);
                        }
                    }
                }
            }
            else if (isInWater) // Player is in water
            {
                bool horizontalInput = Mathf.Abs(verInput) > 0F || Mathf.Abs(horInput) > 0F;

                // Check horizontal movement...
                if (horizontalInput) // Move
                {
                    newVelocity = UpdateMoveVelocity(interval, horInput, verInput, (walkMode ? walkSpeed : runSpeed) * 0.6F);
                    newVelocity.y = rigidBody!.velocity.y;
                }
                else // Tread
                {
                    newVelocity = Vector3.zero;
                    newVelocity.y = rigidBody!.velocity.y;
                }
                
                // Check vertical movement...
                if (up) // Swim up
                {
                    newVelocity.y = jumpSpeed * 0.5F;
                }
                else if (down) // Swim down
                {
                    newVelocity.y = -jumpSpeed * 0.5F;
                }
            }
            else // Player is in the air
            {
                // Leave velocity to Unity physics
                newVelocity = rigidBody!.velocity;
            }

            if (isInWater)
            {
                if (newVelocity.magnitude > walkSpeed)
                    newVelocity = newVelocity.normalized * walkSpeed;
            }

            // Apply movement...
            rigidBody!.velocity = newVelocity;

            // Tell server our current position
            var rawLocation = CoordConvert.Unity2MC(transform.position);

            // Preprocess the location before sending it (to avoid position correction from server)
            if ((isOnGround || centerDownDist < 0.5F) && rawLocation.Y - (int)rawLocation.Y > 0.9D)
                rawLocation.Y = (int)rawLocation.Y + 1;

            CornClient.Instance.SyncLocation(rawLocation, visualTransform!.eulerAngles.y - 90F, 0F);

            playerRender!.SetCurrentVelocity(newVelocity);

        }

        Vector3 GetAxisAlignedOrientation(Vector3 original)
        {
            if (Mathf.Abs(original.x) > Mathf.Abs(original.z))
                return original.x > 0F ? Vector3.right   : Vector3.left;
            else
                return original.z > 0F ? Vector3.forward : Vector3.back;
        }

        private float targetYaw = 0F;

        Vector3 UpdateMoveVelocity(float interval, float horInput, float verInput, float speed)
        {
            // hor: x, ver: y
            var inputDirection = new Vector2(horInput, verInput).normalized;
            
            if (inputDirection.y > 0F)
                targetYaw = Mathf.Atan(inputDirection.x / inputDirection.y) * Mathf.Rad2Deg;
            else if (inputDirection.y < 0F)
                targetYaw = Mathf.Atan(inputDirection.x / inputDirection.y) * Mathf.Rad2Deg + 180F;
            else
                targetYaw = inputDirection.x > 0 ? 90F : 270F;

            float targetVisualYaw = camControl!.transform.rotation.eulerAngles.y + targetYaw;

            // Smooth rotation for player model
            float newVisualYaw = Mathf.LerpAngle(visualTransform!.eulerAngles.y, targetVisualYaw, steerSpeed * interval);
            visualTransform!.eulerAngles = new Vector3(0F, newVisualYaw, 0F);
            
            // Use the target visual yaw as actual movement direction
            var dir = Quaternion.AngleAxis(targetVisualYaw, Vector3.up) * Vector3.forward;

            return dir * speed;
        }

        private Action<PerspectiveUpdateEvent>? perspectiveCallback;
        private Action<GameModeUpdateEvent>? gameModeCallback;

        void Start()
        {
            interactionLayer  = LayerMask.GetMask("Interaction"); // ~LayerMask.GetMask("Player", "Ignore Raycast");
            movementLayer     = LayerMask.GetMask("Movement");

            entityLayer   = LayerMask.GetMask("Entity");
            camControl    = GameObject.FindObjectOfType<CameraController>();
              
            var blockSelectionPrefab = Resources.Load<GameObject>("Prefabs/Block Selection");
            var blockSelectionObj = GameObject.Instantiate(blockSelectionPrefab);
            blockSelection = blockSelectionObj.GetComponentInChildren<MeshRenderer>();
            blockSelection.enabled = false;

            game = CornClient.Instance;
            
            // Initialize player visuals
            visualTransform = transform.Find("Visual");
            playerRender    = GetComponent<EntityRender>();

            fakeEntity.Name = game!.GetUsername();
            fakeEntity.ID   = 0;
            playerRender.Entity = fakeEntity;
            
            blockSelectionTransform = blockSelectionObj.transform;

            boxCollider = transform.Find("Collider").GetComponent<BoxCollider>();
            rigidBody   = GetComponent<Rigidbody>();

            perspectiveCallback = (e) => {
                switch (e.newPerspective)
                {
                    case Perspective.FirstPerson: // Enable block selection
                        updateTarget = true;
                        break;
                    case Perspective.ThirdPerson: // Disable block selection
                        updateTarget = false;
                        targetBlockPos = null;
                        blockSelection!.enabled = false;
                        break;
                }
            };

            gameModeCallback = (e) => {
                if (e.newGameMode != GameMode.Spectator && game!.LocationReceived)
                    EnableEntity();
                else
                    DisableEntity();
            };

            EventManager.Instance.Register(perspectiveCallback);
            EventManager.Instance.Register(gameModeCallback);

        }

        void OnDestroy()
        {
            if (perspectiveCallback is not null)
                EventManager.Instance.Unregister(perspectiveCallback);

            if (gameModeCallback is not null)
                EventManager.Instance.Unregister(gameModeCallback);
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube(transform.position + groundBoxCenter, groundBoxHalfSize * 2F);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position + groundBoxCenter + Vector3.down * castDist, groundBoxHalfSize * 2F);
        }

        public string GetDebugInfo()
        {
            string targetBlockInfo = string.Empty;
            var pos = CoordConvert.Unity2MC(transform.position);
            var world = game!.GetWorld();

            if (updateTarget && targetBlockPos is not null)
            {
                var targetBlockState = world?.GetBlock(targetBlockPos.Value).State;
                if (targetBlockState is not null)
                    targetBlockInfo = targetBlockState.ToString();
            }

            if (entityDisabled)
                return $"Position:\t{pos}\nTarget Block:\t{targetBlockPos}\n{targetBlockInfo}\nBiome:\n{world?.GetBiome(pos)}";
            else
                return $"Position:\t{pos}\nGrounded:\t{isOnGround}\t{centerDownDist:0.00} {frontDownDist:0.00}\nIn water:\t{isInWater}\nTarget Block:\t{targetBlockPos}\n{targetBlockInfo}\nBiome:\n{world?.GetBiome(pos)}";

        }

        private static bool onGridEdge(float value)
        {
            var delta = value - Mathf.Floor(value);
            return delta < 0.01F || delta > 0.99F;
        }

        private static bool onCubeSurface(Vector3 point)
        {
            return onGridEdge(point.x) || onGridEdge(point.y) || onGridEdge(point.z);
        }

    }
}
