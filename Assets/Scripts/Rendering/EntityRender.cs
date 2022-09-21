using UnityEngine;
using TMPro;

using MinecraftClient.Mapping;

namespace MinecraftClient.Rendering
{
    public class EntityRender : MonoBehaviour
    {
        private const float MOVE_THERESHOLD = 3F * 3F; // Treat as teleport if move more than 3 meters at once

        public Vector3 currentVelocity = Vector3.zero;
        //public float smoothTime = 0.1F;
        public float lerpFactor = 0.5F;
        public float showInfoDist = 20F;
        public float hideInfoDist = 25F;

        private TMP_Text nameText;
        private bool nameTextShown = false, initialized = false;
        private CornClient game;
        private Entity entity;

        public Entity Entity
        {
            get {
                return entity;
            }

            set {
                entity = value;
                UpdateDisplayName();
            }
        }

        public void Unload()
        {
            Destroy(this.gameObject);
        }

        public void TeleportTo(Vector3 position)
        {
            transform.position = position;
        }

        private void EnsureInitialized()
        {
            if (initialized)
                return;
            
            // Initialze info plate
            nameText = FindHelper.FindChildRecursively(transform, "Name Text").GetComponent<TMP_Text>();
            nameTextShown    = false;
            nameText.enabled = false;

            initialized = true;

        }

        private void UpdateDisplayName()
        {
            EnsureInitialized();

            if (entity.CustomName is not null)
                nameText.text = $"#{entity.ID} {entity.CustomName}";
            else if (entity.Name is not null)
                nameText.text = $"#{entity.ID} {entity.Name}";
            else
                nameText.text = $"#{entity.ID} {entity.Type}";
            
        }

        void Start()
        {
            // Get game instance
            game = CornClient.Instance;

            EnsureInitialized();

        }

        void Update()
        {
            // Update position
            Vector3 targetPos = CoordConvert.MC2Unity(entity.Location);

            if ((targetPos - transform.position).sqrMagnitude > MOVE_THERESHOLD) // Treat as teleport
                transform.position = targetPos;
            else
            {
                // Smoothly move to current position
                //transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref currentVelocity, smoothTime);
                transform.position = Vector3.Lerp(transform.position, targetPos, Mathf.Min(0.2F, lerpFactor * Time.deltaTime));
            }

            // Update info plate
            var cameraPos = game.GetCameraPosition();
            float dist = (cameraPos - transform.position).magnitude;

            if (nameTextShown)
            {
                if (dist > hideInfoDist) // Hide info plate
                {
                    nameText.enabled = false;
                    nameTextShown = false;
                }
                else // Update info plate rotation
                {
                    nameText.transform.parent.LookAt(cameraPos);
                }
            }
            else
            {
                if (dist < showInfoDist) // Show info plate
                {
                    nameText.enabled = true;
                    nameTextShown = true;
                }
            }

        }
        
    }
}