using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using CraftSharp.Event;
using CraftSharp.Protocol.Message;

namespace CraftSharp.Rendering
{
    public class EntityRender : MonoBehaviour
    {
        protected const float MOVE_THRESHOLD = 5F * 5F; // Treat as teleport if move more than 5 meters at once

        /// <summary>
        /// ID of the entity on the Minecraft server
        /// </summary>
        public int NumeralId;

        /// <summary>
        /// UUID of the entity if it is a player.
        /// </summary>
        public Guid UUID;
        
        #nullable enable

        /// <summary>
        /// Nickname of the entity if it is a player.
        /// </summary>
        public string? Name;
        
        /// <summary>
        /// CustomName of the entity.
        /// </summary>
        public string? CustomNameJson;
        
        /// <summary>
        /// IsCustomNameVisible of the entity.
        /// </summary>
        public bool IsCustomNameVisible;

        /// <summary>
        /// CustomName of the entity.
        /// </summary>
        public string? CustomName;

        /// <summary>
        /// Entity type
        /// </summary>
        public EntityType Type { get; private set; } = EntityType.DUMMY_ENTITY_TYPE;

        /// <summary>
        /// Entity position
        /// </summary>
        public TrackedValue<Vector3> Position { get; private set; } = new(Vector3.zero);

        /// <summary>
        /// Interval between server location updates (ticks)
        /// </summary>
        protected double movementUpdateInterval = int.MaxValue;
        
        private Vector3 lastPosition;
        private double currentElapsedMovementUpdateMilSec = 0;
        private double currentElapsedYawUpdateMilSec = 0;

        /// <summary>
        /// Entity velocity received from server
        /// </summary>
        private Vector3? receivedVelocity;
        
        public string GetDebugText()
        {
            return $"{(int) currentElapsedMovementUpdateMilSec} / {(int) movementUpdateInterval} ({(int) (currentElapsedMovementUpdateMilSec / movementUpdateInterval * 100.0)}%)";
        }

        /// <summary>
        /// Entity yaw
        /// </summary>
        public readonly TrackedValue<float> Yaw = new(0F);
        protected float lastYaw = 0F;
        
        /// <summary>
        /// Entity (head) pitch
        /// </summary>
        public readonly TrackedValue<float> Pitch = new(0F);
        protected float lastPitch = 0F;
        
        /// <summary>
        /// Used in Item Frame, Falling Block and Fishing Float.
        /// See https://wiki.vg/Object_Data for details.
        /// </summary>
        /// <remarks>Untested</remarks>
        public int ObjectData = -1;

        /// <summary>
        /// Health of the entity
        /// </summary>
        public readonly TrackedValue<float> Health = new(0F);

        /// <summary>
        /// Max health of the entity
        /// </summary>
        public readonly TrackedValue<float> MaxHealth = new(0F);
        
        /// <summary>
        /// Item of the entity if ItemFrame or Item
        /// </summary>
        public readonly TrackedValue<ItemStack?> Item = new(null);
        
        /// <summary>
        /// Entity pose in the Minecraft world
        /// </summary>
        public EntityPose Pose;
        
        /// <summary>
        /// Entity metadata
        /// </summary>
        protected Dictionary<int, object?>? Metadata { get; private set; }

        /// <summary>
        /// Update entity metadata, validate control variables,
        /// then check for visual/material update
        /// </summary>
        public virtual void UpdateMetadata(Dictionary<int, object?> updatedMeta)
        {
            // Create if not present
            Metadata ??= new();

            foreach (var entry in updatedMeta)
            {
                Metadata[entry.Key] = entry.Value;
            }

            // Update entity health
            if (Type.MetaSlotByName.TryGetValue("data_health_id", out var metaSlot1)
                && Type.MetaEntries[metaSlot1].DataType == EntityMetadataType.Float)
            {
                if (Metadata.TryGetValue(metaSlot1, out var value) && value is float health)
                {
                    Health.Value = health;
                    MaxHealth.Value = Mathf.Max(MaxHealth.Value, health);
                }
            }

            // Update entity custom name
            if (Type.MetaSlotByName.TryGetValue("data_custom_name", out var metaSlot2)
                && Type.MetaEntries[metaSlot2].DataType == EntityMetadataType.OptionalChat)
            {
                if (Metadata.TryGetValue(metaSlot2, out var value) && value is string customName)
                {
                    CustomNameJson = customName;
                    CustomName = ChatParser.ParseText(customName);
                }
            }

            // Update entity custom name
            if (Type.MetaSlotByName.TryGetValue("data_custom_name_visible", out var metaSlot3)
                && Type.MetaEntries[metaSlot3].DataType == EntityMetadataType.Boolean)
            {
                if (Metadata.TryGetValue(metaSlot3, out var value) && value is bool customNameVisible)
                {
                    IsCustomNameVisible = customNameVisible;
                }
            }

            // Update entity pose
            if (Type.MetaSlotByName.TryGetValue("data_pose", out var metaSlot4)
                && Type.MetaEntries[metaSlot4].DataType == EntityMetadataType.Pose)
            {
                if (Metadata.TryGetValue(metaSlot4, out var value) && value is int pose)
                {
                    Pose = (EntityPose) pose;
                }
            }

            // TODO: Update control variables

            // Update own materials
            if (TryGetComponent(out EntityMaterialAssigner materialControl))
            {
                materialControl.UpdateMaterials(Type, null, updatedMeta.Keys.ToHashSet(), GetControlVariables(), Metadata);
            }
        }

        /// <summary>
        /// Entity equipment
        /// </summary>
        public Dictionary<int, ItemStack>? Equipment;

        /// <summary>
        /// Used to control animation transition (for animators) or limb swing (for vanilla entity renders)
        /// </summary>
        protected Vector3 _visualMovementVelocity = Vector3.zero;

        #nullable disable

        /// <summary>
        /// Whether the current entity has started death animation
        /// </summary>
        private bool _deathAnimationStarted = false;

        [SerializeField] protected Transform _infoAnchor;
        [SerializeField] protected Transform _visualTransform;

        /// <summary>
        /// Anchor transform for locating floating labels, health bars, etc.
        /// </summary>
        public Transform InfoAnchor
        {
            get => _infoAnchor ? _infoAnchor : transform;
            protected set => _infoAnchor = value;
        }

        /// <summary>
        /// Visual transform of the entity render
        /// </summary>
        public Transform VisualTransform
        {
            get => _visualTransform ? _visualTransform : transform;
            protected set => _visualTransform = value;
        }

        public GameObject FloatingInfoPrefab;

        /// <summary>
        /// A number made from the entity's numeral id, used in animations to prevent
        /// several mobs of a same type moving synchronisedly, which looks unnatural
        /// </summary>
        protected float _pseudoRandomOffset = 0F;

        /// <summary>
        /// Initialize this entity render
        /// </summary>
        public virtual void Initialize(EntityData source, Vector3Int originOffset)
        {
            if (!_visualTransform)
            {
                Debug.LogWarning("Visual transform for entity render not assigned!");
                _visualTransform = transform;
            }

            NumeralId = source.Id;
            UUID = source.UUID;
            Name = source.Name;
            Type = source.Type;

            Position.Value = CoordConvert.MC2Unity(originOffset, source.Location);
            lastPosition = Position.Value;
            lastYaw = Yaw.Value = source.Yaw;
            lastPitch = Pitch.Value = source.Pitch;

            // TODO: Check this
            var intervalTicks = Type.UpdateInterval;
            if (intervalTicks < 0) intervalTicks = 0;
            movementUpdateInterval = intervalTicks == int.MaxValue ? double.MaxValue : (intervalTicks + 1) * 50D;

            Position.OnValueUpdate += (_, _) =>
            {
                // Update old position and reset update timer
                lastPosition = transform.position;
                currentElapsedMovementUpdateMilSec = 0.0;
            };
            
            Yaw.OnValueUpdate += (_, _) =>
            {
                // Update old yaw and reset update timer
                lastYaw = VisualTransform!.eulerAngles.y;
                currentElapsedYawUpdateMilSec = 0.0;
            };

            ObjectData = source.ObjectData;
            Health.Value = source.Health;
            MaxHealth.Value = source.MaxHealth;

            // Initialize other fields with default values
            // These fields will be later assigned by metadata packets
            CustomName = null;
            CustomNameJson = null;
            IsCustomNameVisible = true;
            Item.Value = null;
            Pose = EntityPose.Standing;
            Metadata = null;
            Equipment = null;

            UnityEngine.Random.InitState(NumeralId);
            _pseudoRandomOffset = UnityEngine.Random.Range(0F, 1F);

            _visualTransform.eulerAngles = new Vector3(0F, lastYaw, 0F);

            // Initialize materials (This requires metadata to be present)
            if (TryGetComponent(out EntityMaterialAssigner materialControl))
            {
                materialControl.InitializeMaterials(source.Type, GetControlVariables(), source.Metadata, HandleMaterialUpdate);
            }
        }
        
        public string GetDisplayName()
        {
            if (IsCustomNameVisible)
            {
                if (CustomNameJson is not null)
                {
                    return ChatParser.ParseText(CustomNameJson);
                }
                
                if (CustomName is not null)
                {
                    return CustomName;
                }
            }

            return Name ?? ChatParser.TranslateString(Type.TypeId.GetTranslationKey("entity"));
        }
        
        #nullable enable

        /// <summary>
        /// Get variable table for render control (pose, texture, etc.)
        /// </summary>
        protected virtual Dictionary<string, string>? GetControlVariables()
        {
            return null;
        }
        
        #nullable disable

        /// <summary>
        /// Finalize this entity render
        /// </summary>
        public virtual void Unload()
        {
            if (this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Used when updating world origin offset to seamlessly teleport the EntityRender
        /// and maintain its relative position to everything else
        /// </summary>
        public virtual void TeleportByDelta(Vector3 posDelta)
        {
            Position.Value += posDelta;
            transform.position += posDelta;
        }

        /// <summary>
        /// Set entity velocity from server
        /// </summary>
        public virtual void SetReceivedVelocity(Vector3 velocity)
        {
            receivedVelocity = velocity;
        }

        protected virtual void UpdateTransform(float tickMilSec)
        {
            // Update elapsed time in current tick
            currentElapsedMovementUpdateMilSec += Time.unscaledDeltaTime * 1000;
            currentElapsedYawUpdateMilSec += Time.unscaledDeltaTime * 1000;

            // Update position
            transform.position = Vector3.Lerp(lastPosition, Position.Value, (float) (currentElapsedMovementUpdateMilSec / movementUpdateInterval));

            // Update visual velocity (for leg animations, etc.)
            var distanceToTarget = Vector3.Distance(transform.position, Position.Value);
            if (distanceToTarget <= 0.01f)
            {
                _visualMovementVelocity = Vector3.zero;
            }
            else
            {
                _visualMovementVelocity = (Position.Value - lastPosition) / (float) movementUpdateInterval * 1000;
            }

            var _visualYaw = Mathf.LerpAngle(_visualTransform!.eulerAngles.y, Yaw.Value, (float)(currentElapsedYawUpdateMilSec / movementUpdateInterval));
            _visualTransform!.eulerAngles = new Vector3(0F, _visualYaw, 0F);
        }

        public virtual Transform SetupCameraRef()
        {
            var aimingLocalHeight = transform.InverseTransformPoint(GetAimingRef().position).y;
            var pos = new Vector3(0F, aimingLocalHeight, 0F);

            return SetupCameraRef(pos);
        }
        
        public virtual Transform GetAimingRef()
        {
            return transform;
        }

        public ShapeAABB GetAABB()
        {
            // AABBs should use Minecraft coordinate system
            var minX = transform.position.z - Type.Width / 2F;
            var maxX = transform.position.z + Type.Width / 2F;
            var minZ = transform.position.x - Type.Width / 2F;
            var maxZ = transform.position.x + Type.Width / 2F;
            var minY = transform.position.y;
            var maxY = transform.position.y + Type.Height;

            return new ShapeAABB(minX, minY, minZ, maxX, maxY, maxZ);
        }
        
        public virtual void HandleAimingModeChange(CameraAimingEvent e)
        {
            
        }

        private Transform SetupCameraRef(Vector3 pos)
        {
            var cameraRefObj = new GameObject("Camera Ref Obj");
            var cameraRef = cameraRefObj.transform;

            cameraRef.SetParent(_visualTransform, false);
            cameraRef.localPosition = pos;

            return cameraRef;
        }

        public void SetVisualMovementVelocity(Vector3 velocity)
        {
            _visualMovementVelocity = velocity;
        }

        public virtual void UpdateAnimation(float tickMilSec) { }

        protected virtual void HandleMaterialUpdate(EntityMaterialManager matManager, ResourceLocation textureId, Material updatedMaterial)
        {

        }

        public virtual void ManagedUpdate(float tickMilSec)
        {
            if (!_deathAnimationStarted && Health.Value <= 0F)
            {
                _deathAnimationStarted = true;
            }

            UpdateTransform(tickMilSec);
            UpdateAnimation(tickMilSec);
        }
        
        private void OnDrawGizmos()
        {
            // Draw player AABB
            var entityPos = transform.position;
            
            var width = Type.Width;
            var height = Type.Height;
            
            // Player AABB center (position is at feet, so center is at half height)
            var center = new Vector3(entityPos.x, entityPos.y + height / 2F, entityPos.z);
            
            // Player AABB size
            var size = new Vector3(width, height, width);
            
            // Set gizmo color (orange)
            Gizmos.color = Color.orange;
            
            // Draw wireframe cube for the AABB
            Gizmos.DrawWireCube(center, size);
        }
    }
}