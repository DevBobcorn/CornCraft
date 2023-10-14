#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

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
        public EntityType? Type;

        /// <summary>
        /// Entity position
        /// </summary>
        public Vector3 Position;
        protected Vector3 lastPosition;

        /// <summary>
        /// Entity yaw
        /// </summary>
        public float Yaw = 0;
        protected float lastYaw = 0F;

        /// <summary>
        /// Entity head pitch
        /// </summary>
        public float Pitch = 0;
        protected float lastPitch = 0F;

        /// <summary>
        /// Entity head yaw
        /// </summary>
        public float HeadYaw = 0;
        protected float lastHeadYaw = 0F;

        /// <summary>
        /// Used in Item Frame, Falling Block and Fishing Float.
        /// See https://wiki.vg/Object_Data for details.
        /// </summary>
        /// <remarks>Untested</remarks>
        public int ObjectData = -1;

        /// <summary>
        /// Health of the entity
        /// </summary>
        public float Health;

        /// <summary>
        /// Max health of the entity
        /// </summary>
        public float MaxHealth;
        
        /// <summary>
        /// Item of the entity if ItemFrame or Item
        /// </summary>
        public ItemStack? Item;
        
        /// <summary>
        /// Entity pose in the Minecraft world
        /// </summary>
        public EntityPose Pose;
        
        /// <summary>
        /// Entity metadata
        /// </summary>
        public Dictionary<int, object?>? Metadata;

        /// <summary>
        /// Entity equipment
        /// </summary>
        public Dictionary<int, ItemStack>? Equipment;

        protected Vector3 visualMovementVelocity = Vector3.zero;
        protected bool turnedIntoRagdoll = false;

        [SerializeField] protected Transform? infoAnchor, visual;
        public Transform InfoAnchor
        {
            get => infoAnchor ?? transform;
            protected set => infoAnchor = value;
        }
        public Transform VisualTransform
        {
            get => visual!;
            protected set => visual = value;
        }

        [SerializeField] protected GameObject? ragdollPrefab;
        [SerializeField] public GameObject? FloatingInfoPrefab;

        /// <summary>
        /// A number made from the entity's numeral id, used in animations to prevent
        /// several mobs of a same type moving synchronisedly, which looks unnatural
        /// </summary>
        protected float pseudoRandomOffset = 0F;

        public void Unload()
        {
            if (!turnedIntoRagdoll && Health <= 0F)
            {
                TurnIntoRagdoll();
            }
            
            Destroy(this.gameObject);
        }

        public virtual void Initialize(EntityType entityType, Entity source)
        {
            if (visual == null)
            {
                Debug.LogWarning("Visual transform for entity render not assigned!");
                visual = transform;
            }

            NumeralId = source.ID;
            UUID = source.UUID;
            Name = source.Name;
            CustomNameJson = source.CustomNameJson;
            IsCustomNameVisible = source.IsCustomNameVisible;
            CustomName = source.CustomName;
            Type = entityType;

            Position = CoordConvert.MC2Unity(source.Location);
            lastPosition = Position;
            lastYaw = Yaw = source.Yaw;
            lastHeadYaw = HeadYaw = source.HeadYaw;
            lastPitch = Pitch = source.Pitch;

            ObjectData = source.ObjectData;
            Health = source.Health;
            MaxHealth = source.MaxHealth;
            Item = source.Item;
            Pose = source.Pose;
            Metadata = source.Metadata;
            Equipment = source.Equipment;

            UnityEngine.Random.InitState(NumeralId);
            pseudoRandomOffset = UnityEngine.Random.Range(0F, 1F);

            visual.eulerAngles = new(0F, lastYaw, 0F);
        }

        public virtual void UpdateTransform(float tickMilSec)
        {
            // Update position
            if ((Position - transform.position).sqrMagnitude > MOVE_THRESHOLD) // Treat as teleport
                transform.position = Position;
            else // Smoothly move to current position
                transform.position = Vector3.SmoothDamp(transform.position, Position,
                        ref visualMovementVelocity, tickMilSec);

            // Update rotation
            var headYawDelta = Mathf.Abs(Mathf.DeltaAngle(lastHeadYaw, HeadYaw));
            var bodyYawDelta = Mathf.Abs(Mathf.DeltaAngle(lastYaw, Yaw));

            if (bodyYawDelta > 0.0025F)
            {
                lastYaw = Mathf.MoveTowardsAngle(lastYaw, Yaw, Time.deltaTime * 300F);
                visual!.eulerAngles = new(0F, lastYaw, 0F);
            }

            if (headYawDelta > 0.0025F)
                lastHeadYaw = Mathf.MoveTowardsAngle(lastHeadYaw, HeadYaw, Time.deltaTime * 150F);
            else
            {
                if (visualMovementVelocity.magnitude < 0.1F)
                    Yaw = HeadYaw;
            }
            
            if (Mathf.Abs(Mathf.DeltaAngle(Yaw, HeadYaw)) > 75F)
                Yaw = HeadYaw;
            
            if (lastPitch != Pitch)
                lastPitch = Mathf.MoveTowardsAngle(lastPitch, Pitch, Time.deltaTime * 300F);

        }

        public virtual Transform SetupCameraRef(Vector3 pos)
        {
            var cameraRefObj = new GameObject("Camera Ref");
            var cameraRef = cameraRefObj.transform;

            cameraRef.SetParent(visual, false);
            cameraRef.localPosition = pos;

            return cameraRef;
        }

        public virtual void SetVisualMovementVelocity(Vector3 velocity)
        {
            velocity.y = 0; // Ignore y velocity by default
            visualMovementVelocity = velocity;
        }

        public virtual void UpdateAnimation(float tickMilSec) { }

        protected virtual void TurnIntoRagdoll()
        {
            // 'ragdollPrefab is not null' is not properly supported in Unity yet, so just use '!='
            if (ragdollPrefab != null) // Create ragdoll in place
            {
                var ragdollObj = GameObject.Instantiate(ragdollPrefab)!;

                ragdollObj.transform.position = transform.position;

                var ragdoll = ragdollObj.GetComponentInChildren<EntityRagdoll>();

                if (ragdoll != null)
                {
                    ragdoll.Visual.rotation = visual!.rotation;
                    ragdoll.Visual.localScale = visual.localScale;

                    // Make it fly!
                    if (visualMovementVelocity.sqrMagnitude > 0F)
                        ragdoll.mainRigidbody?.AddForce(visualMovementVelocity.normalized * 15F, ForceMode.VelocityChange);
                    else
                        ragdoll.mainRigidbody?.AddForce(Vector3.up * 10F, ForceMode.VelocityChange);
                }
            }

            visual?.gameObject.SetActive(false);

            turnedIntoRagdoll = true;
        }

        public virtual void ManagedUpdate(float tickMilSec)
        {
            if (!turnedIntoRagdoll && Health <= 0F)
            {
                TurnIntoRagdoll();
            }

            UpdateTransform(tickMilSec);
            UpdateAnimation(tickMilSec);
        }
    }
}