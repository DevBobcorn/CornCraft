using UnityEngine;
using MinecraftClient.Mapping;

namespace MinecraftClient.Control
{
    [RequireComponent(typeof (Rigidbody))]
    public class PlayerController : MonoBehaviour
    {
        private CornClient game;
        private Rigidbody rigidBody;
        private BoxCollider boxCollider;

        public float walkSpeed = 1F;
        public float runSpeed  = 2F;
        public float jumpSpeed = 4F;

        protected LayerMask terrainLayer, entityLayer;

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
                    if (game.LocationReceived)
                        EnableEntity();
                    else
                        DisableEntity();
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
            // Update components state...
            boxCollider.enabled = false;
            rigidBody.velocity = Vector3.zero;
            rigidBody.useGravity = false;
        }

        public virtual void EnableEntity()
        {
            // Update and control...
            entityDisabled = false;
            // Update components state...
            boxCollider.enabled = true;
            rigidBody.useGravity = true;
        }

        public virtual void SetPosition(Location pos)
        {
            transform.position = CoordConvert.MC2Unity(pos);
            Debug.Log($"Position set to {transform.position}");

            if (gameMode == GameMode.Spectator) // Spectating
                DisableEntity();
            else
                EnableEntity();
        }

        public void Tick(float interval, float horInput, float verInput, bool walkMode, bool attack, bool up, bool down)
        {
            if (attack)
            {
                if (blockPos is not null)
                    game.DigBlock(blockPos.Value, true, false);
            }

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
            var moveVelocity = Vector3.zero;

            // Check horizontal movement...
            if (Mathf.Abs(verInput) > 0F || Mathf.Abs(horInput) > 0F)
            {
                // Calculate current velocity...
                moveVelocity = GetMoveVelocity(horInput, verInput, walkMode ? walkSpeed : runSpeed) * 4F;
            }

            // Check vertical movement...
            if (up)
                moveVelocity.y =  walkSpeed * 4F;
            else if (down)
                moveVelocity.y = -walkSpeed * 4F;

            // Apply movement...
            transform.position += moveVelocity * interval;

            // No need to check position validity in spectator mode,
            // just tell server the new position...
            CornClient.Instance.SyncLocation(CoordConvert.Unity2MC(transform.position), visual.eulerAngles.y - 90F, 0F);
        }

        private void TickNormal(float interval, float horInput, float verInput, bool walkMode, bool attack, bool up, bool down)
        {
            // Update player rotation
            var moveVelocity = Vector3.zero;

            // Check horizontal movement...
            if (Mathf.Abs(verInput) > 0F || Mathf.Abs(horInput) > 0F)
            {
                // Calculate current velocity...
                moveVelocity = GetMoveVelocity(horInput, verInput, walkMode ? walkSpeed : runSpeed);
            }

            // Check vertical movement...
            moveVelocity.y = up ? jumpSpeed : rigidBody.velocity.y;

            // Apply movement...
            rigidBody.velocity = moveVelocity;

            // Tell server our current position
            CornClient.Instance.SyncLocation(CoordConvert.Unity2MC(transform.position), visual.eulerAngles.y - 90F, 0F);
        }

        Vector3 GetMoveVelocity(float horInput, float verInput, float speed)
        {
            float newRotation = camControl.transform.rotation.eulerAngles.y + camRotation;
            visual.transform.eulerAngles = new Vector3(0F, newRotation, 0F);
            
            var dir = Quaternion.AngleAxis(visual.transform.eulerAngles.y, Vector3.up) * Vector3.forward;

            // hor: x, ver: y
            Vector2 inputDirection = new(horInput, verInput);
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
            terrainLayer = LayerMask.GetMask("Terrain"); // ~LayerMask.GetMask("Player", "Ignore Raycast");
            entityLayer  = LayerMask.GetMask("Entity");
            camControl   = GameObject.FindObjectOfType<CameraController>();
            visual       = transform.Find("Visual");

            game = CornClient.Instance;

            boxCollider = transform.Find("Collider").GetComponent<BoxCollider>();
            rigidBody   = GetComponent<Rigidbody>();

        }

        private Vector3? targetPos = null, targetDir = null;
        private Location? blockPos = null;

        void Update()
        {
            var ray = camControl.ActiveCamera.ViewportPointToRay(new(0.5F, 0.5F, 0F));

            RaycastHit hit;
            if (Physics.Raycast(ray.origin, ray.direction, out hit, 100F, terrainLayer))
            {
                targetPos = hit.point;
                targetDir = hit.normal;
            }
            else
            {
                targetPos = null;
                targetDir = null;
            }

        }

        void OnDrawGizmos()
        {
            if (targetPos is not null)
            {
                // Draw hit result
                Gizmos.color = Color.yellow;
                Gizmos.DrawCube(targetPos.Value, Vector3.one * 0.1F);
                Gizmos.color = Color.green;
                Gizmos.DrawRay(targetPos.Value, targetDir.Value);
                // Draw calculated block position
                var offseted = targetPos.Value - targetDir.Value * 0.5F;
                Vector3 pos = new(Mathf.Floor(offseted.x), Mathf.Floor(offseted.y), Mathf.Floor(offseted.z));
                blockPos = CoordConvert.Unity2MC(pos);
                Gizmos.color = Color.black;
                Gizmos.DrawWireCube(pos + Vector3.one * .5F, Vector3.one);

            }
            else
                blockPos = null;
        }

        public string GetDebugInfo()
        {
            return $"{gameMode}\nPosition:\t{transform.position.x.ToString("#.##")}\t{transform.position.y.ToString("#.##")}\t{transform.position.z.ToString("#.##")}\nGrounded:\t{isOnGround}\nIn water:\t{isInWater}\nTarget Block:\t{blockPos}";
        }

    }
}
