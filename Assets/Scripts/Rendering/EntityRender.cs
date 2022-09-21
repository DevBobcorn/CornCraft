#nullable enable
using UnityEngine;
using TMPro;

using MinecraftClient.Mapping;

namespace MinecraftClient.Rendering
{
    public class EntityRender : MonoBehaviour
    {
        private const float MOVE_THERESHOLD = 5F * 5F; // Treat as teleport if move more than 5 meters at once
        private Vector3? targetPosition = null;
        private Vector3 currentVelocity = Vector3.zero;
        public float showInfoDist = 20F;
        public float hideInfoDist = 25F;

        private TMP_Text? nameText;
        private bool nameTextShown = false, initialized = false;
        private CornClient? game;
        private Entity? entity;

        public Entity Entity
        {
            get {
                return entity!;
            }

            set {
                entity = value;

                transform.position = CoordConvert.MC2Unity(entity.Location);

                UpdateDisplayName();
            }
        }

        public void Unload()
        {
            Destroy(this.gameObject);
        }

        public void MoveTo(Vector3 position)
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

            if (entity!.CustomName is not null)
                nameText!.text = $"#{entity.ID} {entity.CustomName}";
            else if (entity.Name is not null)
                nameText!.text = $"#{entity.ID} {entity.Name}";
            else
                nameText!.text = $"#{entity.ID} {entity.Type}";
            
        }

        void Start()
        {
            // Get game instance
            game = CornClient.Instance;

            EnsureInitialized();

        }

        public float lerpParam = 0.1F;

        void Update()
        {
            // Update position
            var cameraPos = game!.GetCameraPosition();
            var dist2Cam  = (cameraPos - transform.position).magnitude;

            if (targetPosition is not null)
            {
                if (dist2Cam > 10F || (targetPosition.Value - transform.position).sqrMagnitude > MOVE_THERESHOLD) // Treat as teleport
                    transform.position = targetPosition.Value;
                else
                {
                    // Smoothly move to current position
                    //transform.position = Vector3.SmoothDamp(transform.position, targetPosition.Value, ref currentVelocity, lerpParam);
                    
                    //transform.position = Vector3.Lerp(transform.position, targetPosition.Value, lerpParam * Time.deltaTime);
                    transform.position = Vector3.Lerp(transform.position, targetPosition.Value, lerpParam);
                    
                    //transform.position = targetPos;
                }
            }

            // Update info plate
            if (nameTextShown)
            {
                if (dist2Cam > hideInfoDist) // Hide info plate
                {
                    nameText!.enabled = false;
                    nameTextShown = false;
                }
                else // Update info plate rotation
                {
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
        
    }
}