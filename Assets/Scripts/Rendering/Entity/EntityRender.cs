#nullable enable
using UnityEngine;
using TMPro;

using MinecraftClient.Mapping;

namespace MinecraftClient.Rendering
{
    public class EntityRender : MonoBehaviour
    {
        private const float MOVE_THERESHOLD = 5F * 5F; // Treat as teleport if move more than 5 meters at once
        protected Vector3? lastPosition = null, targetPosition = null;
        protected float lastYaw = 0F, targetYaw = 0F;
        protected Vector3 currentVelocity = Vector3.zero;

        public float showInfoDist = 10F;
        public float hideInfoDist = 12F;

        // A number made from the entity's numeral id, used in animations to prevent
        // several mobs of a same type moving synchronisedly, which looks unnatural
        protected float pseudoRandomOffset = 0F;

        protected TMP_Text? nameText;
        private bool nameTextShown = false, initialized = false;

        protected Entity? entity;

        public Entity Entity
        {
            get {
                return entity!;
            }

            set {
                entity = value;

                UpdateDisplayName();

                Random.InitState(entity.ID);
                pseudoRandomOffset = Random.Range(0F, 1F);
                
                lastPosition = targetPosition = transform.position = CoordConvert.MC2Unity(entity.Location);
            }
        }

        public void Unload() => Destroy(this.gameObject);

        public void MoveTo(Vector3 position) => targetPosition = position;

        public void RotateTo(float yaw, float pitch) => targetYaw = yaw;

        private void EnsureInitialized()
        {
            if (initialized)
                return;
            
            Initialize();
            initialized = true;
        }

        protected virtual void Initialize()
        {
            // Initialze info plate
            nameText = FindHelper.FindChildRecursively(transform, "Name Text").GetComponent<TMP_Text>();
            nameTextShown    = false;
            nameText.enabled = false;
        }

        private void UpdateDisplayName()
        {
            EnsureInitialized();

            if (entity!.CustomName is not null)
                nameText!.text = $"#{entity.ID} {entity.CustomName}";
            else if (entity.Name is not null)
                nameText!.text = $"#{entity.ID} {entity.Name}";
            else
                nameText!.text = $"#{entity.ID} {entity.Type}";
            
        }

        void Start() => EnsureInitialized();

        protected void UpdateInfoPlate(Vector3 cameraPos, float dist2Cam)
        {
            if (nameTextShown)
            {
                if (dist2Cam > hideInfoDist) // Hide info plate
                {
                    nameText!.enabled = false;
                    nameTextShown = false;
                }
                else // Update info plate rotation
                {
                    //nameText!.text = $"T: {partialTick}"; // TODO Remove
                    nameText!.transform.parent.LookAt(cameraPos);
                }
            }
            else
            {
                if (dist2Cam < showInfoDist) // Show info plate
                {
                    nameText!.enabled = true;
                    nameTextShown = true;
                }
            }

        }

        protected void UpdateTransform(float dist2Cam, float tickMilSec)
        {
            // Update position
            if (lastPosition is not null && targetPosition is not null)
            {
                if (dist2Cam > 100F || (targetPosition.Value - transform.position).sqrMagnitude > MOVE_THERESHOLD) // Treat as teleport
                    transform.position = targetPosition.Value;
                else // Smoothly move to current position
                    transform.position = Vector3.SmoothDamp(transform.position, targetPosition.Value, ref currentVelocity, tickMilSec);

            }

            // Update rotation
            if (lastYaw != targetYaw)
            {
                // TODO Transition
                lastYaw = Mathf.MoveTowardsAngle(lastYaw, targetYaw, Time.deltaTime * 400F);

                transform.eulerAngles = new(0F, lastYaw, 0F);
            }

        }

        public void SetCurrentVelocity(Vector3 velocity) => currentVelocity = velocity;

        public virtual void UpdateAnimation(float tickMilSec) { }

        public virtual void ManagedUpdate(Vector3 cameraPos, float tickMilSec)
        {
            var dist2Cam  = (cameraPos - transform.position).magnitude;

            UpdateInfoPlate(cameraPos, dist2Cam);
            UpdateTransform(dist2Cam, tickMilSec);
            
            UpdateAnimation(tickMilSec);

        }

    }
}