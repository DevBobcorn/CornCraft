#nullable enable
using System;
using UnityEngine;

using CraftSharp.Event;
using CraftSharp.Rendering;

namespace CraftSharp.Control
{
    [RequireComponent(typeof (Rigidbody), typeof (PlayerStatusUpdater))]
    public class PlayerController : MonoBehaviour
    {
        public enum CurrentItemState
        {
            HoldInMainHand,
            HoldInOffhand,
            Mount
        }

        public ItemActionType CurrentActionType { get; private set; } = ItemActionType.None;

        [SerializeField] public PlayerStagedSkill? MeleeSwordAttack;
        [SerializeField] public PlayerChargedSkill? RangedBowAttack;

        [SerializeField] protected PlayerAbility? ability;
        [SerializeField] public PlayerSkillItemConfig? SkillItemConf;
        [SerializeField] protected CameraController? cameraController;
        [SerializeField] public Transform? CameraRef;
        [SerializeField] protected Rigidbody? playerRigidbody;
        [SerializeField] protected PlayerStatusUpdater? statusUpdater;
        
        private EntityRender? playerRender;
        [SerializeField] protected PhysicMaterial? physicMaterial;
        [SerializeField] public bool UseRootMotion = false;
        private PlayerActions? playerActions;
        public PlayerActions Actions => playerActions!;

        public void EnableInput() => playerActions!.Enable();
        public void DisableInput() => playerActions!.Disable();

        public PlayerAbility Ability => ability!;
        public Rigidbody PlayerRigidbody => playerRigidbody!;
        protected Collider? playerCollider;
        public Collider PlayerCollider => playerCollider!;
        protected IPlayerState currentState = PlayerStates.IDLE;
        protected IPlayerState? pendingState = null;
        public PlayerStatus? Status => statusUpdater?.Status;

        // Values for sending over to the server. Should only be set
        // from the unity thread and read from the network thread

        /// <summary>
        /// Player location for sending to the server
        /// </summary>
        public Location Location2Send { get; private set; }

        /// <summary>
        /// Player yaw for sending to the server
        /// This yaw value is stored in Minecraft coordinate system.
        /// Conversion is required when assigning to unity transfom
        /// </summary>
        public float MCYaw2Send { get; private set; }

        /// <summary>
        /// Player pitch for sending to the server
        /// </summary>
        public float Pitch2Send { get; private set; }

        /// <summary>
        /// Grounded flag for sending to the server
        /// </summary>
        public bool IsGrounded2Send { get; private set; }

        public Quaternion GetRotation()
        {
            if (playerRender != null) // If player render is present
            {
                return playerRender.VisualTransform.rotation;
            }
            return Quaternion.identity;
        }

        public Vector3 GetOrientation()
        {
            if (playerRender != null) // If player render is present
            {
                return playerRender.VisualTransform.forward;
            }
            return Vector3.forward;
        }

        public void UpdatePlayerRenderFromPrefab(Entity entity, GameObject renderPrefab)
        {
            GameObject renderObj;
            if (renderPrefab.GetComponent<Animator>() != null) // Model prefab, wrap it up
            {
                renderObj = AnimatorEntityRender.CreateFromModel(renderPrefab);
            }
            else // Player render prefab, just instantiate
            {
                renderObj = GameObject.Instantiate(renderPrefab);
            }
            renderObj!.name = $"Player Entity ({renderPrefab.name})";

            UpdatePlayerRender(entity, renderObj);
        }

        private void UpdatePlayerRender(Entity entity, GameObject renderObj)
        {
            var prevRender = playerRender;

            // Initialize and assign new visual gameobject
            // Clear existing event subscriptions
            OnLogicalUpdate = null;
            OnRandomizeMirroredFlag = null;
            OnMeleeDamageStart = null;
            OnMeleeDamageEnd = null;
            OnCurrentItemChanged = null;
            OnItemStateChanged = null;
            OnCrossFadeState = null;
            
            // Update controller's player render
            playerRender = renderObj.GetComponent<EntityRender>();

            if (playerRender != null)
            {
                // Initialize head yaw to look forward
                playerRender.HeadYaw.Value = Entity.GetHeadYawFromByte(127); // i.e. -90F
                playerRender.transform.SetParent(transform, false);

                // Destroy these colliders, so that they won't affect our movement
                foreach (var collider in playerRender.GetComponentsInChildren<Collider>())
                {
                    Destroy(collider);
                }

                // Initialize player entity render
                playerRender.Initialize(entity.Type, entity);

                // Update render gameobject layer (do this last to ensure all children are present)
                foreach (var child in renderObj.GetComponentsInChildren<Transform>())
                {
                    child.gameObject.layer = this.gameObject.layer;
                }

                var riggedRender = playerRender as PlayerEntityRiggedRender;
                if (riggedRender != null) // If player render is rigged render
                {
                    // Additionally, update player state machine for rigged rendersInitialize
                    OnLogicalUpdate += (interval, status, rigidbody) => riggedRender.UpdateStateMachine(status);
                    // Initialize current item held by player
                    // TODO: Remove direct reference to client
                    var activeItem = CornApp.CurrentClient?.GetActiveItem();
                    riggedRender.InitializeActiveItem(activeItem,
                            PlayerActionHelper.GetItemActionType(activeItem));
                }
                else // Player render is vanilla/entity render
                {
                    // Do nothing here...
                }

                // Update player render state machine
                OnLogicalUpdate += (interval, status, rigidbody) =>
                {
                    // Update player render velocity
                    playerRender.SetVisualMovementVelocity(rigidbody!.velocity);
                    // Update render
                    playerRender.UpdateAnimation(0.05F);
                };

                CameraRef = playerRender.SetupCameraRef(new(0F, 1.5F, 0F));
                cameraController!.SetTarget(CameraRef);
            }
            else
            {
                Debug.LogWarning("Player render not found in game object!");
                // Use own transform
                cameraController!.SetTarget(transform);
            }

            if (prevRender != null)
            {
                if (playerRender != null)
                {
                    playerRender.transform.rotation = prevRender.VisualTransform.rotation;
                }
                
                // Dispose previous render gameobject
                Destroy(prevRender.gameObject);
            }

            playerRender!.transform.localPosition = Vector3.zero;
        }

        private Action<GameModeUpdateEvent>? gameModeCallback;
        private Action<HeldItemChangeEvent>? heldItemCallback;

        void Start()
        {
            playerActions = new PlayerActions();
            playerActions.Enable();

            var boxcast = ability!.ColliderType == PlayerAbility.PlayerColliderType.Box;

            if (boxcast)
            {
                // Attach box collider
                var box = gameObject.AddComponent<BoxCollider>();
                var sideLength = ability.ColliderRadius * 2F;
                box.size = new(sideLength, ability.ColliderHeight, sideLength);
                box.center = new(0F, (ability.ColliderHeight / 2F) - 0.001F, 0F);

                statusUpdater!.GroundBoxcastHalfSize = new(ability.ColliderRadius, 0.01F, ability.ColliderRadius);

                playerCollider = box;
            }
            else
            {
                // Attach capsule collider
                var capsule = gameObject.AddComponent<CapsuleCollider>();
                capsule.height = ability.ColliderHeight;
                capsule.radius = ability.ColliderRadius;
                capsule.center = new(0F, (ability.ColliderHeight / 2F) - 0.001F, 0F);

                statusUpdater!.GroundSpherecastRadius = ability.ColliderRadius;
                statusUpdater.GroundSpherecastCenter = new(0F, ability.ColliderRadius + 0.05F, 0F);

                playerCollider = capsule;
            }

            playerCollider.material = physicMaterial;
            statusUpdater.UseBoxCastForGroundedCheck = boxcast;

            // Set stamina to max value
            Status!.StaminaLeft = ability!.MaxStamina;
            // And broadcast current stamina
            EventManager.Instance.Broadcast<StaminaUpdateEvent>(new(Status.StaminaLeft, ability!.MaxStamina));
            // Initialize health value
            EventManager.Instance.Broadcast<HealthUpdateEvent>(new(20F, true));

            // Register gamemode events for updating gamemode
            gameModeCallback = (e) => SetGameMode(e.GameMode);
            EventManager.Instance.Register(gameModeCallback);

            // Register hotbar item event for updating item visuals
            heldItemCallback = (e) =>
            {
                OnCurrentItemChanged?.Invoke(e.ItemStack, e.ActionType);
                // Exit attack state when active item is changed
                Status!.Attacking = false;
                CurrentActionType = e.ActionType;
            };
            EventManager.Instance.Register(heldItemCallback);

            // Initialize player state (idle on start)
            Status.Grounded = true;
            currentState.OnEnter(Status, playerRigidbody!, this);
        }

        void OnDestroy()
        {
            playerActions?.Disable();

            if (gameModeCallback is not null)
                EventManager.Instance.Unregister(gameModeCallback);
            
            if (heldItemCallback is not null)
                EventManager.Instance.Unregister(heldItemCallback);
        }

        protected void SetGameMode(GameMode gameMode)
        {
            switch (gameMode)
            {
                case GameMode.Survival:
                case GameMode.Creative:
                case GameMode.Adventure:
                    Status!.Spectating = false;
                    EnableEntity();
                    break;
                case GameMode.Spectator:
                    Status!.Spectating = true;
                    DisableEntity();
                    break;
            }
        }

        public void DisablePhysics()
        {
            Status!.PhysicsDisabled = true;
            playerRigidbody!.isKinematic = true;
        }

        public void EnablePhysics()
        {
            Status!.PhysicsDisabled = false;
            playerRigidbody!.isKinematic = false;
        }

        protected void DisableEntity()
        {
            // Update components state...
            playerCollider!.enabled = false;
            Status!.GravityScale = 0F;

            if (!playerRigidbody!.isKinematic) // If physics is enabled, reset velocity to zero
            {
                playerRigidbody.velocity = Vector3.zero;
            }

            // Reset player status
            Status.Grounded = false;
            Status.InLiquid  = false;
            Status.OnWall    = false;
            Status.Sprinting = false;

            Status.EntityDisabled = true;

            // Hide entity render
            playerRender?.gameObject.SetActive(false);
        }

        protected void EnableEntity()
        {
            // Update components state...
            playerCollider!.enabled = true;
            Status!.GravityScale = 1F;

            Status.EntityDisabled = false;

            // Show entity render
            playerRender?.gameObject.SetActive(true);
        }
        
        public delegate void ItemStateEventHandler(CurrentItemState weaponState);
        public event ItemStateEventHandler? OnItemStateChanged;
        public void ChangeItemState(CurrentItemState itemState) => OnItemStateChanged?.Invoke(itemState);

        public delegate void ItemStackEventHandler(ItemStack? item, ItemActionType actionType);
        public event ItemStackEventHandler? OnCurrentItemChanged;

        public delegate void CrossFadeStateEventHandler(string stateName, float time, int layer, float timeOffset);
        public event CrossFadeStateEventHandler? OnCrossFadeState;
        public void CrossFadeState(string stateName, float time = 0.2F, int layer = 0, float timeOffset = 0F)
        {
            OnCrossFadeState?.Invoke(stateName, time, layer, timeOffset);
        }

        public delegate void OverrideStateEventHandler(AnimationClip dummyClip, AnimationClip animationClip);
        public event OverrideStateEventHandler? OnOverrideState;
        public void OverrideState(AnimationClip dummyClip, AnimationClip animationClip)
        {
            OnOverrideState?.Invoke(dummyClip, animationClip);
        }

        public delegate void NoParamEventHandler();
        public event NoParamEventHandler? OnRandomizeMirroredFlag;
        public void RandomizeMirroredFlag() => OnRandomizeMirroredFlag?.Invoke();

        public event NoParamEventHandler? OnMeleeDamageStart;
        public void MeleeDamageStart() => OnMeleeDamageStart?.Invoke();

        public event NoParamEventHandler? OnMeleeDamageEnd;
        public void MeleeDamageEnd() => OnMeleeDamageEnd?.Invoke();

        public void ToggleWalkMode()
        {
            Status!.WalkMode = !Status.WalkMode;
            CornApp.Notify(Translations.Get($"gameplay.control.switch_to_{(Status.WalkMode ? "walk" : "rush")}"));
        }

        public void StartForceMoveOperation(string name, ForceMoveOperation[] ops)
        {
            // Set it as pending state, this will be set as active state upon next logical update
            pendingState = new ForceMoveState(name, ops);
        }

        private void ChangeToState(IPlayerState state)
        {
            //Debug.Log($"Exit state [{CurrentState}]");
            currentState.OnExit(statusUpdater!.Status, playerRigidbody!, this);

            // Exit previous state and enter this state
            currentState = state;
            
            //Debug.Log($"Enter state [{CurrentState}]");
            currentState.OnEnter(statusUpdater!.Status, playerRigidbody!, this);
        }

        public void AttachVisualFX(GameObject fxObj)
        {
            if (playerRender != null)
            {
                fxObj.transform.SetParent(playerRender.VisualTransform);
            }
            else
            {
                Debug.LogWarning("Trying to attach vfx object to empty player render!");
            }
        }

        public bool TryStartNormalAttack()
        {
            if (Status!.AttackStatus.AttackCooldown <= 0F)
            {
                if (CurrentActionType == ItemActionType.MeleeWeaponSword)
                {
                    currentState.OnExit(Status, playerRigidbody!, this);
                    // Specify attack data to use
                    Status.AttackStatus.CurrentStagedAttack = MeleeSwordAttack;
                    // Enter attack state
                    currentState = PlayerStates.MELEE;
                    currentState.OnEnter(statusUpdater!.Status, playerRigidbody!, this);
                    return true;
                }
                else if (CurrentActionType == ItemActionType.RangedWeaponBow)
                {
                    // TODO: Implement
                    return false;
                }
                
                return false;
            }
            
            return false;
        }

        public bool TryStartChargedAttack()
        {
            if (Status!.AttackStatus.AttackCooldown <= 0F)
            {
                if (CurrentActionType == ItemActionType.MeleeWeaponSword)
                {
                    // TODO: Implement
                    return false;
                }
                else if (CurrentActionType == ItemActionType.RangedWeaponBow)
                {
                    currentState.OnExit(Status, playerRigidbody!, this);
                    // Specify attack data to use
                    Status.AttackStatus.CurrentChargedAttack = RangedBowAttack;
                    // Enter attack state
                    currentState = PlayerStates.RANGED_AIM;
                    currentState.OnEnter(statusUpdater!.Status, playerRigidbody!, this);
                    return true;
                }
                
                return false;
            }
            
            return false;
        }

        public void StartAiming()
        {
            AlignVisualYawToCamera();
            cameraController!.EnableAimingCamera(true);
        }

        public void StopAiming()
        {
            cameraController!.EnableAimingCamera(false);
        }

        private void AlignVisualYawToCamera()
        {
            Status!.CurrentVisualYaw = cameraController!.GetYaw();
            playerRender!.VisualTransform.eulerAngles = new(0F, Status!.CurrentVisualYaw, 0F);

            //Debug.Log($"Aligning player yaw to camera ({Status!.CurrentVisualYaw}). Aiming: {cameraController!.IsAiming}");
        }

        public void UpdatePlayerStatus()
        {
            if (!Status!.EntityDisabled)
            {
                statusUpdater!.UpdatePlayerStatus(playerRigidbody!.velocity, GetOrientation());
            }
        }

        protected void PreLogicalUpdate(float interval)
        {
            // Update player status (in water, grounded, etc)
            UpdatePlayerStatus();
            
            var status = statusUpdater!.Status;

            // Update current player state
            if (pendingState != null) // Change to pending state if present
            {
                ChangeToState(pendingState);
                pendingState = null;
            }
            else if (currentState.ShouldExit(playerActions!, status))
            {
                // Try to exit current state and enter another one
                foreach (var state in PlayerStates.STATES)
                {
                    if (state != currentState && state.ShouldEnter(playerActions!, status))
                    {
                        ChangeToState(state);
                        break;
                    }
                }
            }

            if (playerRender != null)
            {
                Status!.CurrentVisualYaw = playerRender.VisualTransform.eulerAngles.y;
            }

            float prevStamina = status.StaminaLeft;
            var horInput = playerActions!.Gameplay.Movement.ReadValue<Vector2>();

            // Prepare current and target player visual yaw before updating it
            if (horInput != Vector2.zero)
            {
                var userInputYaw = GetYawFromVector2(horInput);
                //Status.TargetVisualYaw = inputData.CameraEularAngles.y + Status.UserInputYaw;
                Status!.TargetVisualYaw = cameraController!.GetYaw() + userInputYaw;
            }
            
            // Update player physics and transform using updated current state
            currentState.UpdatePlayer(interval, playerActions, status, playerRigidbody!, this);

            // Broadcast current stamina if changed
            if (prevStamina != status.StaminaLeft)
            {
                EventManager.Instance.Broadcast<StaminaUpdateEvent>(new(status.StaminaLeft, ability!.MaxStamina));
            }

            // Apply updated visual yaw to visual transform
            if (playerRender != null)
            {
                playerRender.VisualTransform.eulerAngles = new(0F, Status!.CurrentVisualYaw, 0F);
            }
        }

        protected void PostLogicalUpdate()
        {
            // Update values to send to server
            if (currentState is ForceMoveState state)
            {
                // Use move origin as the player location to tell to server, to
                // prevent sending invalid positions during a force move operation
                Location2Send = CoordConvert.Unity2MC(
                        state.GetFakePlayerOffset());
            }
            else
            {
                Location2Send = new(
                        Math.Round(transform.position.z, 2),
                        Math.Round(transform.position.y, 2),
                        Math.Round(transform.position.x, 2)
                );
            }

            if (playerRender != null)
            {
                // Update client player data
                MCYaw2Send = playerRender.VisualTransform.eulerAngles.y - 90F; // Coordinate system conversion
                Pitch2Send = 0F;
            }
        }

        // Used only by player renders, will be cleared and reassigned upon player render update
        private delegate void PlayerUpdateEventHandler(float interval, PlayerStatus status, Rigidbody rigidbody);
        private event PlayerUpdateEventHandler? OnLogicalUpdate;

        /// <summary>
        /// Logical update
        /// </summary>
        void Update()
        {
            PreLogicalUpdate(Time.unscaledDeltaTime);
            
            // Visual updates...
            OnLogicalUpdate?.Invoke(Time.unscaledDeltaTime, Status!, playerRigidbody!);

            PostLogicalUpdate();
        }

        /// <summary>
        /// Late logical update
        /// </summary>
        void LateUpdate()
        {
            if (cameraController!.IsAiming)
            {
                AlignVisualYawToCamera();
            }
        }

        /// <summary>
        /// Physics update
        /// </summary>
        void FixedUpdate()
        {
            var info = Status!;

            if (info.GravityScale != 0F) // Apply current gravity
            {
                Vector3 gravity = Physics.gravity * info.GravityScale;
                playerRigidbody!.AddForce(gravity, ForceMode.Acceleration);
            }

            if (info.MoveVelocity != Vector3.zero) // Add a force to change velocity of the rigidbody
            {
                // The player is actively moving
                playerRigidbody!.AddForce(10F * Time.fixedUnscaledDeltaTime * (info.MoveVelocity - playerRigidbody!.velocity), ForceMode.VelocityChange);
            }
            else
            {
                if (info.Spectating && !playerRigidbody!.isKinematic) // If physics is enabled, reset velocity to zero
                {
                    playerRigidbody!.velocity = Vector3.zero;
                }
                
                // Otherwise leave the player rigidbody untouched
            }
        }

        public void SetLocation(Location loc, bool reset = false, float mcYaw = 0F)
        {
            if (reset) // Reset rigidbody
            {
                playerRigidbody!.velocity = Vector3.zero;
            }
            
            playerRigidbody!.position = CoordConvert.MC2Unity(loc);
            // Update local data
            Location2Send = loc;
            MCYaw2Send = mcYaw;

            if (playerRender != null)
            {
                playerRender.VisualTransform.eulerAngles = new(0F, mcYaw + 90F, 0F); // Coordinate system conversion
            }
        }

        protected static float GetYawFromVector2(Vector2 direction)
        {
            if (direction.y > 0F)
                return Mathf.Atan(direction.x / direction.y) * Mathf.Rad2Deg;
            else if (direction.y < 0F)
                return Mathf.Atan(direction.x / direction.y) * Mathf.Rad2Deg + 180F;
            else
                return direction.x > 0 ? 90F : 270F;
        }

        public string GetDebugInfo()
        {
            string statusInfo;

            if (statusUpdater?.Status.Spectating ?? true)
                statusInfo = string.Empty;
            else
                statusInfo = statusUpdater!.Status.ToString();
            
            return $"ActionType:\t{CurrentActionType}\nState:\t{currentState}\n{statusInfo}";
        }
    }
}
