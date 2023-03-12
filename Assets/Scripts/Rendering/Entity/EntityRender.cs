#nullable enable
using UnityEngine;

using MinecraftClient.Mapping;

namespace MinecraftClient.Rendering
{
    public class EntityRender : MonoBehaviour
    {
        protected const float MOVE_THRESHOLD = 5F * 5F; // Treat as teleport if move more than 5 meters at once
        protected Vector3? lastPosition = null, targetPosition = null;
        protected float lastYaw = 0F, targetYaw = 0F;
        protected float lastHeadYaw = 0F, targetHeadYaw = 0F;
        protected float lastPitch = 0F, targetPitch = 0F;
        protected Vector3 visualMovementVelocity = Vector3.zero;

        [SerializeField] protected Transform? infoAnchor, visual;
        public Transform InfoAnchor => infoAnchor is null ? transform : infoAnchor;

        // A number made from the entity's numeral id, used in animations to prevent
        // several mobs of a same type moving synchronisedly, which looks unnatural
        protected float pseudoRandomOffset = 0F;

        protected Entity? entity;

        public Entity Entity
        {
            get => entity!;

            set {
                entity = value;

                Random.InitState(entity.ID);
                pseudoRandomOffset = Random.Range(0F, 1F);
                
                lastPosition = targetPosition = transform.position = CoordConvert.MC2Unity(entity.Location);
            }
        }

        public void Unload() => Destroy(this.gameObject);

        public void MoveTo(Vector3 position) => targetPosition = position;

        public void RotateTo(float yaw, float pitch)
        {
            targetYaw = yaw;
            targetPitch = pitch;
        }

        public void RotateHeadTo(float headYaw)
        {
            targetHeadYaw = headYaw;
        }

        public virtual void Initialize(EntityType entityType, Entity entity)
        {
            if (visual is null)
            {
                Debug.LogWarning("Visual transform for entity render not assigned!");
                visual = transform;
            }
        }

        public virtual void UpdateTransform(float tickMilSec)
        {
            // Update position
            if (lastPosition is not null && targetPosition is not null)
            {
                if ((targetPosition.Value - transform.position).sqrMagnitude > MOVE_THRESHOLD) // Treat as teleport
                    transform.position = targetPosition.Value;
                else // Smoothly move to current position
                    transform.position = Vector3.SmoothDamp(transform.position, targetPosition.Value, ref visualMovementVelocity, tickMilSec);

            }

            // Update rotation
            var headYawDelta = Mathf.Abs(Mathf.DeltaAngle(lastHeadYaw, targetHeadYaw));
            var bodyYawDelta = Mathf.Abs(Mathf.DeltaAngle(lastYaw, targetYaw));

            if (bodyYawDelta > 0.0025F)
            {   // TODO Transition
                lastYaw = Mathf.MoveTowardsAngle(lastYaw, targetYaw, Time.deltaTime * 300F);

                transform.eulerAngles = new(0F, lastYaw, 0F);
            }

            if (headYawDelta > 0.0025F)
                lastHeadYaw = Mathf.MoveTowardsAngle(lastHeadYaw, targetHeadYaw, Time.deltaTime * 150F);
            else
            {
                if (visualMovementVelocity.magnitude < 0.1F)
                    targetYaw = targetHeadYaw;
            }
            
            if (Mathf.Abs(Mathf.DeltaAngle(targetYaw, targetHeadYaw)) > 75F)
                targetYaw = targetHeadYaw;
            
            if (lastPitch != targetPitch)
                lastPitch = Mathf.MoveTowardsAngle(lastPitch, targetPitch, Time.deltaTime * 300F);

        }

        public virtual void SetVisualMovementVelocity(Vector3 velocity)
        {
            velocity.y = 0; // Ignore y velocity by default
            visualMovementVelocity = velocity;
        }

        public virtual void UpdateAnimation(float tickMilSec) { }

        public virtual void ManagedUpdate(float tickMilSec)
        {
            UpdateTransform(tickMilSec);
            UpdateAnimation(tickMilSec);

        }

    }
}