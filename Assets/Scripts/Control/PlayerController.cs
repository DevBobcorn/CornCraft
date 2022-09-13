using UnityEngine;
using MinecraftClient.Mapping;

namespace MinecraftClient.Control
{
    public class PlayerController : MonoBehaviour
    {
        private CornClient game;

        public float walkSpeed = 0.9F;
        public float runSpeed  = 4.2F;
        public float jumpSpeed = 4F;
        public float swimSpeed = 0.8F;
        public float swimFastSpeed = 3.2F;

        protected LayerMask checkLayer, entityLayer;
        protected GameMode gameMode = 0;
        public GameMode GameMode
        {
            get {
                return gameMode;
            }

            set {
                gameMode = value;

                if (gameMode == GameMode.Spectator) // Spectating
                {
                    DisableEntity();
                }
                else
                {
                    EnableEntity();
                }
            }
        }

        protected bool entityDisabled = false;
        public bool EntityDisabled { get { return entityDisabled; } }

        private CameraController camControl;
        private Transform visual;
        protected bool isMoving = false, isOnGround = false;
        protected float camRotation = 0F;
        public bool IsOnGround { get { return isOnGround; } }
        private bool isInWater = false;

        public virtual void DisableEntity()
        {
            entityDisabled = true;
            isMoving = false;
            // Reset movement params...
            isOnGround = true;
            isInWater = false;
        }

        public virtual void EnableEntity()
        {
            // Update and control...
            entityDisabled = false;
        }

        public virtual void SetPosition(Location pos)
        {
            transform.position = CoordConvert.MC2Unity(pos);
        }

        public float GetCursorRotation()
        {
            return 360F - transform.eulerAngles.y;
        }

        public void Tick(float interval, float horInput, float verInput, bool walkMode, bool attack, bool up, bool down)
        {
            if (entityDisabled)
            {
                TickSpectator(interval, horInput, verInput, walkMode, attack, up, down);
            }
            else
            {
                TickNormal(interval, horInput, verInput, walkMode, attack, up, down);
            }
        }

        private void TickSpectator(float interval, float horInput, float verInput, bool walkMode, bool attack, bool up, bool down)
        {
            // Update player rotation
            Vector3 moveVelocity = Vector3.zero;

            // Check horizontal movement...
            if (Mathf.Abs(verInput) > 0F || Mathf.Abs(horInput) > 0F)
            {
                // Calculate current velocity...
                moveVelocity = GetMoveVelocity(horInput, verInput, walkMode ? walkSpeed : runSpeed);
            }

            // Check vertical movement...
            if (up)
            {
                moveVelocity.y = walkMode ? walkSpeed : runSpeed;
            }
            else if (down)
            {
                moveVelocity.y = walkMode ? -walkSpeed : -runSpeed;
            }

            // Apply movement...
                transform.position += moveVelocity * interval;

            // No need to check position validity in spectator mode,
            // just tell server the new position...
            CornClient.Instance.SyncLocation(CoordConvert.Unity2MC(transform.position), visual.eulerAngles.y - 90F, 0F);
        }

        private void TickNormal(float interval, float horInput, float verInput, bool walkMode, bool attack, bool up, bool down)
        {
            // Update player rotation
            Vector3 moveVelocity = Vector3.zero;

            // Check horizontal movement...
            if (Mathf.Abs(verInput) > 0F || Mathf.Abs(horInput) > 0F)
            {
                // Calculate current velocity...
                moveVelocity = GetMoveVelocity(horInput, verInput, walkMode ? walkSpeed : runSpeed);
            }

            // Check vertical movement...
            if (up)
            {
                moveVelocity.y = walkMode ? walkSpeed : runSpeed;
            }
            else if (down)
            {
                moveVelocity.y = walkMode ? -walkSpeed : -runSpeed;
            }

            // Apply movement...
                transform.position += moveVelocity * interval;

            // No need to check position validity in spectator mode,
            // just tell server the new position...
            CornClient.Instance.SyncLocation(CoordConvert.Unity2MC(transform.position), visual.eulerAngles.y - 90F, 0F);
        }

        Vector3 GetMoveVelocity(float horInput, float verInput, float speed)
        {
            Quaternion orgVisualRotation = visual.rotation;

            float newRotation = camControl.transform.rotation.eulerAngles.y + camRotation;
            transform.eulerAngles = new Vector3(0F, newRotation, 0F);

            visual.rotation = orgVisualRotation;
            
            Vector3 dir = Quaternion.AngleAxis(transform.eulerAngles.y, Vector3.up) * Vector3.forward;

            // hor: x, ver: y
            Vector2 inputDirection = new Vector2(horInput, verInput);
            inputDirection.Normalize();
            if (inputDirection.y > 0F)
            {
                camRotation = Mathf.Atan(inputDirection.x / inputDirection.y) * Mathf.Rad2Deg;
            }
            else if (inputDirection.y < 0F)
            {
                camRotation = Mathf.Atan(inputDirection.x / inputDirection.y) * Mathf.Rad2Deg + 180F;
            }
            else
            {
                camRotation = inputDirection.x > 0 ? 90F : 270F;
            }

            return dir * speed;
        }

        public void Start()
        {
            checkLayer  = ~LayerMask.GetMask("Player", "Ignore Raycast");
            entityLayer = LayerMask.GetMask("Entity");
            camControl  = GameObject.FindObjectOfType<CameraController>();
            visual      = transform.Find("Visual");

            game = CornClient.Instance;

        }

        public string GetDebugInfo()
        {
            return $"{gameMode}\nPosition:\t{transform.position.x.ToString("#.##")}\t{transform.position.y.ToString("#.##")}\t{transform.position.z.ToString("#.##")}\nGrounded:\t{isOnGround}\nIn water:\t{isInWater}";
        }

    }
}
