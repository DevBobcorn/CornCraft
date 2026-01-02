using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

using CraftSharp.Event;
using CraftSharp.Protocol.Message;
using CraftSharp.Resource;

namespace CraftSharp.Rendering
{
    public class EntityRender : MonoBehaviour
    {
        protected const float MOVE_THRESHOLD = 5F * 5F; // Treat as teleport if move more than 5 meters at once
        protected static readonly ResourceLocation FIRE_0_ID = new("block/fire_0");
        protected static readonly ResourceLocation FIRE_1_ID = new("block/fire_1");

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
        /// Entity metadata
        /// </summary>
        public Dictionary<int, object?>? Metadata { get; private set; }
        
        /// <summary>
        /// Entity shared flags
        /// <br/>
        /// See https://minecraft.wiki/w/Java_Edition_protocol/Entity_metadata#Entity
        /// </summary>
        public readonly TrackedValue<byte> SharedFlags = new(0);

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
        public readonly TrackedValue<EntityPose> Pose = new(EntityPose.Standing);

        protected bool isClientEntity { get; private set; } = false;

        /// <summary>
        /// Update entity metadata, validate control variables,
        /// then check for visual/material update
        /// </summary>
        public virtual void UpdateMetadata(Dictionary<int, object?> updatedMeta)
        {
            // Create if not present
            Metadata ??= new Dictionary<int, object?>();

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

            // Update entity pose (Only update for non-client entities)
            if (!isClientEntity && Type.MetaSlotByName.TryGetValue("data_pose", out var metaSlot4)
                && Type.MetaEntries[metaSlot4].DataType == EntityMetadataType.Pose)
            {
                if (Metadata.TryGetValue(metaSlot4, out var value) && value is int pose)
                {
                    Pose.Value = (EntityPose) pose;
                }
            }
            
            // Update entity shared flags
            if (Type.MetaSlotByName.TryGetValue("data_shared_flags_id", out var metaSlot5)
                && Type.MetaEntries[metaSlot5].DataType == EntityMetadataType.Byte)
            {
                if (Metadata.TryGetValue(metaSlot5, out var value) && value is byte sharedFlags)
                {
                    SharedFlags.Value = sharedFlags;
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

        private Transform _fireBillboardTransform;
        private static Mesh FIRE_BILLBOARD_MESH;

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

        public void SetClientEntityFlag()
        {
            isClientEntity = true;
        }

        /// <summary>
        /// Initialize this entity render
        /// </summary>
        public virtual void Initialize(EntitySpawnData source, Vector3Int originOffset)
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
            Health.Value = 1F;
            MaxHealth.Value = 1F;

            // Initialize other fields with default values
            // These fields will be later assigned by metadata packets
            CustomName = null;
            CustomNameJson = null;
            IsCustomNameVisible = true;
            Item.Value = null;
            Pose.Value = EntityPose.Standing;
            Metadata = null;
            Equipment = null;

            UnityEngine.Random.InitState(NumeralId);
            _pseudoRandomOffset = UnityEngine.Random.Range(0F, 1F);

            _visualTransform.eulerAngles = new Vector3(0F, lastYaw, 0F);

            // Initialize materials (This requires metadata to be present)
            if (TryGetComponent(out EntityMaterialAssigner materialControl))
            {
                materialControl.InitializeMaterials(source.Type, GetControlVariables(), HandleMaterialUpdate);
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

        public void UpdateLocalSneakingStatus(bool sneaking)
        {
            // Update local entity pose. Note that metadata stays
            // unchanged, and is consistent with server-side data
            Pose.Value = sneaking ? EntityPose.Sneaking : EntityPose.Standing;
        }

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

        protected virtual void UpdateTransform(float tickMilSec, Transform cameraTransform)
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

        public virtual void ManagedUpdate(float tickMilSec, Transform cameraTransform)
        {
            if (!_deathAnimationStarted && Health.Value <= 0F)
            {
                _deathAnimationStarted = true;
            }
            
            UpdateTransform(tickMilSec, cameraTransform);
            
            // Check on fire flag and update billboard
            var onFire = (SharedFlags.Value & 0x01) != 0;
            UpdateFireBillboard(onFire, cameraTransform);

            UpdateAnimation(tickMilSec);
        }
        
        public void UpdateFireBillboard(bool enable, Transform cameraTransform)
        {
            if (enable)
            {
                if (!_fireBillboardTransform)
                {
                    CreateFireBillboard();
                }

                UpdateFireBillboardFacing(cameraTransform);
            }
            else
            {
                if (_fireBillboardTransform)
                {
                    Destroy(_fireBillboardTransform.gameObject);
                    _fireBillboardTransform = null;
                }
            }
        }

        private void CreateFireBillboard()
        {
            var client = CornApp.CurrentClient;
            if (!client) return;

            var chunkMaterial = client.ChunkMaterialManager.GetAtlasMaterial(RenderType.UNLIT);
            var billboardObj = new GameObject("Fire Billboard");

            billboardObj.transform.SetParent(transform, false);
            billboardObj.transform.localPosition = Vector3.zero;

            var meshFilter = billboardObj.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = GetFireBillboardMesh();

            var meshRenderer = billboardObj.AddComponent<MeshRenderer>();
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            meshRenderer.sharedMaterial = chunkMaterial;

            // Scale roughly to entity dimensions
            billboardObj.transform.localScale = new Vector3(
                Mathf.Max(0.6F, Type.Width), Mathf.Max(0.6F, Type.Height), 1F);

            _fireBillboardTransform = billboardObj.transform;
        }

        private static Mesh GetFireBillboardMesh()
        {
            if (FIRE_BILLBOARD_MESH)
            {
                return FIRE_BILLBOARD_MESH;
            }

            var mesh = new Mesh
            {
                name = "Fire Billboard Quad Stack"
            };

            var (uvs, anim) = ResourcePackManager.Instance.GetUVs(FIRE_0_ID, new Vector4(0, 0, 1, 1), 0);
            var baseUv0 = uvs.Select(x => (Vector3) x).ToArray();
            var baseUv1 = Enumerable.Repeat((Vector4) anim, 4).ToArray();

            // Multiple quads with decreasing height; lower quads sit slightly closer (negative Z)
            var heights = new[] { 1.5F, 1.0F, 0.5F };

            var vertices = new List<Vector3>(heights.Length * 4);
            var uv0 = new List<Vector3>(heights.Length * 4);
            var uv1 = new List<Vector4>(heights.Length * 4);
            var triangles = new List<int>(heights.Length * 6);

            for (int i = 0; i < heights.Length; i++)
            {
                var h = heights[i];
                var zOffset = -0.0625F * i;
                int vStart = vertices.Count;

                vertices.Add(new Vector3(-0.5F, h, zOffset));
                vertices.Add(new Vector3( 0.5F, h, zOffset));
                vertices.Add(new Vector3(-0.5F, 0F, zOffset));
                vertices.Add(new Vector3( 0.5F, 0F, zOffset));

                uv0.AddRange(baseUv0);
                uv1.AddRange(baseUv1);

                triangles.AddRange(new[] { vStart + 0, vStart + 1, vStart + 2, vStart + 2, vStart + 1, vStart + 3 });
            }

            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv0);
            mesh.SetUVs(1, uv1);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();

            FIRE_BILLBOARD_MESH = mesh;
            return mesh;
        }

        private void UpdateFireBillboardFacing(Transform cameraTransform)
        {
            if (!_fireBillboardTransform)
            {
                return;
            }

            var directionToTarget = _fireBillboardTransform.position - cameraTransform.position;
            // Ignore vertical distance
            directionToTarget.y = 0;
            
            if (directionToTarget != Vector3.zero)
            {
                _fireBillboardTransform.localRotation = Quaternion.LookRotation(directionToTarget);
            }
        }

        public virtual Vector2 GetDimensions()
        {
            return new Vector2(Type.Width, Type.Height);
        }
        
        private void OnDrawGizmos()
        {
            // Draw player AABB
            var entityPos = transform.position;
            
            var dimensions = GetDimensions();
            var width = dimensions.x;
            var height = dimensions.y;
            
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