#nullable enable
using UnityEngine;
using MinecraftClient.Mapping;

namespace MinecraftClient.Control
{
    [RequireComponent(typeof (Rigidbody))]
    public class PlayerController : MonoBehaviour
    {
        private CornClient? game;
        private Rigidbody? rigidBody;
        private BoxCollider? boxCollider;

        public float walkSpeed = 1F;
        public float runSpeed  = 2F;
        public float jumpSpeed = 4F;

        private LayerMask interactionLayer, movementLayer, entityLayer;

        private GameMode gameMode = 0;
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
                    if (game!.LocationReceived)
                        EnableEntity();
                    else
                        DisableEntity();
                }
            }
        }

        private bool entityDisabled = false;
        public bool EntityDisabled { get { return entityDisabled; } }

        private CameraController? camControl;
        private Transform? visualTransform, blockSelectionTransform;
        private MeshRenderer? blockSelection;

        public void DisableEntity()
        {
            entityDisabled = true;
            // Update components state...
            boxCollider!.enabled = false;
            rigidBody!.velocity = Vector3.zero;
            rigidBody!.useGravity = false;

        }

        public void EnableEntity()
        {
            // Update and control...
            entityDisabled = false;
            // Update components state...
            boxCollider!.enabled = true;
            rigidBody!.useGravity = true;
        }

        public void SetPosition(Location pos)
        {
            transform.position = CoordConvert.MC2Unity(pos);
            Debug.Log($"Position set to {transform.position}");

            if (gameMode == GameMode.Spectator) // Spectating
                DisableEntity();
            else
                EnableEntity();
        }

        public Location GetLocation()
        {
            return CoordConvert.Unity2MC(transform.position);
        }

        public void Tick(float interval, float horInput, float verInput, bool walkMode, bool attack, bool up, bool down)
        {
            // Update block selection
            var viewRay = camControl!.ActiveCamera.ViewportPointToRay(new(0.5F, 0.5F, 0F));

            RaycastHit viewHit;
            if (Physics.Raycast(viewRay.origin, viewRay.direction, out viewHit, 10F, interactionLayer))
            {
                targetPos = viewHit.point;
                targetDir = viewHit.normal;
            }
            else
                targetPos = targetDir = null;

            if (targetPos is not null && targetDir is not null)
            {
                Vector3 offseted  = onSurface(targetPos.Value) ? targetPos.Value - targetDir.Value * 0.5F : targetPos.Value;
                Vector3 selection = new(Mathf.Floor(offseted.x), Mathf.Floor(offseted.y), Mathf.Floor(offseted.z));
                blockSelectionTransform!.position = selection;

                targetBlockPos = CoordConvert.Unity2MC(selection);
                blockSelection!.enabled = true;
            }
            else
            {
                targetBlockPos = null;
                blockSelection!.enabled = false;
            }

            if (entityDisabled)
                TickSpectator(interval, horInput, verInput, walkMode, attack, up, down);
            else
                TickNormal(interval, horInput, verInput, walkMode, attack, up, down);
            
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
            CornClient.Instance.SyncLocation(CoordConvert.Unity2MC(transform.position), visualTransform!.eulerAngles.y - 90F, 0F);
        }

        private float camRotation = 0F;
        public bool IsOnGround { get { return centerDownDist < 0.4F; } }
        private float centerDownDist, frontDownDist;
        private Vector3? targetPos = null, targetDir = null;
        private Location? targetBlockPos = null;

        private void TickNormal(float interval, float horInput, float verInput, bool walkMode, bool attack, bool up, bool down)
        {
            if (targetBlockPos is not null)
            {
                if (attack)
                    game!.DigBlock(targetBlockPos.Value, true, false);
                // else if (use) TODO Implement
            }
            
            // Update player rotation
            var moveVelocity = Vector3.zero;

            // Update player state
            var center = transform.position + transform.up * 0.2F;
            var front  = center + GetAxisAlignedOrientation(visualTransform!.forward) * 0.5F;

            RaycastHit centerDownHit, frontDownHit;
            if (Physics.Raycast(center, -transform.up, out centerDownHit, 1F, movementLayer))
                centerDownDist = centerDownHit.distance;
            else centerDownDist = 1F;

            Debug.DrawRay(center, transform.up * -1F, Color.cyan);

            if (Physics.Raycast(front,  -transform.up, out frontDownHit, 1F, movementLayer))
                frontDownDist = frontDownHit.distance;
            else frontDownDist = 1F;

            Debug.DrawRay(front,  transform.up * -1F, Color.cyan);

            // Check horizontal movement...
            if (Mathf.Abs(verInput) > 0F || Mathf.Abs(horInput) > 0F)
            {
                // Calculate current velocity...
                moveVelocity = GetMoveVelocity(horInput, verInput, walkMode ? walkSpeed : runSpeed);

                // Check auto-jump
                // Clamp delta to [0, 0.8], and amplify to [0, 1]
                float slope = Mathf.Clamp01((centerDownDist - frontDownDist) * 1.25F);
                if (IsOnGround && slope > 0F) // Amplify and apply auto-jump speed
                    moveVelocity.y = jumpSpeed * Mathf.Sqrt(slope);
                else
                    moveVelocity.y = rigidBody!.velocity.y;
            }
            else
            {
                moveVelocity.y = rigidBody!.velocity.y;
            }

            // Check vertical movement...
            if (IsOnGround)
            {
                if (up) // Jump up
                    moveVelocity.y = jumpSpeed;
            }

            // Apply movement...
            rigidBody!.velocity = moveVelocity;

            // Tell server our current position
            CornClient.Instance.SyncLocation(CoordConvert.Unity2MC(transform.position), visualTransform!.eulerAngles.y - 90F, 0F);
        }

        Vector3 GetAxisAlignedOrientation(Vector3 original)
        {
            if (Mathf.Abs(original.x) > Mathf.Abs(original.z))
                return original.x > 0F ? Vector3.right   : Vector3.left;
            else
                return original.z > 0F ? Vector3.forward : Vector3.back;
        }

        Vector3 GetMoveVelocity(float horInput, float verInput, float speed)
        {
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

            float newRotation = camControl!.transform.rotation.eulerAngles.y + camRotation;
            visualTransform!.transform.eulerAngles = new Vector3(0F, newRotation, 0F);
            
            var dir = Quaternion.AngleAxis(visualTransform.transform.eulerAngles.y, Vector3.up) * Vector3.forward;

            return dir * speed;
        }

        public void Start()
        {
            interactionLayer  = LayerMask.GetMask("Interaction"); // ~LayerMask.GetMask("Player", "Ignore Raycast");
            movementLayer     = LayerMask.GetMask("Movement");

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

        private bool onGridEdge(float value)
        {
            var delta = value - Mathf.Floor(value);
            return delta < 0.01F || delta > 0.99F;
        }

        private bool onSurface(Vector3 point)
        {
            return onGridEdge(point.x) || onGridEdge(point.y) || onGridEdge(point.z);
        }

        public string GetDebugInfo()
        {
            string targetBlockInfo = string.Empty;

            if (targetBlockPos is not null)
            {
                var targetBlockState = game!.GetWorld()?.GetBlock(targetBlockPos.Value).State;
                if (targetBlockState is not null)
                    targetBlockInfo = targetBlockState.ToString();
            }

            if (entityDisabled)
                return $"{gameMode}\nPosition:\t{transform.position.x.ToString("#.##")}\t{transform.position.y.ToString("#.##")}\t{transform.position.z.ToString("#.##")}\nIn water:\t{false}\nTarget Block:\t{targetBlockPos}\n{targetBlockInfo}";
            else
                return $"{gameMode}\nPosition:\t{transform.position.x.ToString("#.##")}\t{transform.position.y.ToString("#.##")}\t{transform.position.z.ToString("#.##")}\nGrounded:\t{IsOnGround}\t{centerDownDist.ToString("#.##")} {frontDownDist.ToString("#.##")}\nIn water:\t{false}\nTarget Block:\t{targetBlockPos}\n{targetBlockInfo}";

        }

    }
}
