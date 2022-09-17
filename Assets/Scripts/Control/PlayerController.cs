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
        private Transform visualTransform, blockSelectionTransform;
        private MeshRenderer blockSelection;
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
                if (targetBlockPos is not null)
                    game.DigBlock(targetBlockPos.Value, true, false);
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
            CornClient.Instance.SyncLocation(CoordConvert.Unity2MC(transform.position), visualTransform.eulerAngles.y - 90F, 0F);
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
            CornClient.Instance.SyncLocation(CoordConvert.Unity2MC(transform.position), visualTransform.eulerAngles.y - 90F, 0F);
        }

        Vector3 GetMoveVelocity(float horInput, float verInput, float speed)
        {
            float newRotation = camControl.transform.rotation.eulerAngles.y + camRotation;
            visualTransform.transform.eulerAngles = new Vector3(0F, newRotation, 0F);
            
            var dir = Quaternion.AngleAxis(visualTransform.transform.eulerAngles.y, Vector3.up) * Vector3.forward;

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
            terrainLayer  = LayerMask.GetMask("Terrain"); // ~LayerMask.GetMask("Player", "Ignore Raycast");
            entityLayer   = LayerMask.GetMask("Entity");
            camControl    = GameObject.FindObjectOfType<CameraController>();
            
            var blockSelectionPrefab = Resources.Load<GameObject>("Prefabs/Block Selection");
            var blockSelectionObj = GameObject.Instantiate(blockSelectionPrefab);
            blockSelection = blockSelectionObj.GetComponentInChildren<MeshRenderer>();
            blockSelection.enabled = false;

            
            visualTransform         = transform.Find("Visual");
            blockSelectionTransform = blockSelectionObj.transform;

            game = CornClient.Instance;

            boxCollider = transform.Find("Collider").GetComponent<BoxCollider>();
            rigidBody   = GetComponent<Rigidbody>();

        }

        private Vector3? targetPos = null, targetDir = null;
        private Location? targetBlockPos = null;

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

            if (targetPos is not null)
            {
                // Update block selection
                Vector3 offseted  = onSurface(targetPos.Value) ? targetPos.Value - targetDir.Value * 0.5F : targetPos.Value;
                Vector3 selection = new(Mathf.Floor(offseted.x), Mathf.Floor(offseted.y), Mathf.Floor(offseted.z));
                blockSelectionTransform.position = selection;

                targetBlockPos = CoordConvert.Unity2MC(selection);
                blockSelection.enabled = true;
            }
            else
            {
                targetBlockPos = null;
                blockSelection.enabled = false;
            }

        }

        private bool onGridEdge(float value)
        {
            var delta = value - Mathf.Floor(value);
            return delta < 0.01F || delta > 0.99F;
        }

        private bool onSurface(Vector3 point)
        {
            return onGridEdge(point.x) || onGridEdge(point.y) || onGridEdge(point.z);
        }

        void OnDrawGizmos()
        {
            if (targetPos is not null)
            {
                // Draw hit result
                Gizmos.color = Color.green;
                Gizmos.DrawRay(targetPos.Value, targetDir.Value);
            }
        }

        public string GetDebugInfo()
        {
            return $"{gameMode}\nPosition:\t{transform.position.x.ToString("#.##")}\t{transform.position.y.ToString("#.##")}\t{transform.position.z.ToString("#.##")}\nGrounded:\t{isOnGround}\nIn water:\t{isInWater}\nTarget Block:\t{targetBlockPos}";
        }

    }
}
