using System;
using System.Linq;
using UnityEngine;

using CraftSharp.Event;
using CraftSharp.Rendering;

namespace CraftSharp.Control
{
    [RequireComponent(typeof (PlayerStatusUpdater))]
    public class PlayerController : MonoBehaviour
    {
        public enum CurrentItemState
        {
            HoldInMainHand,
            HoldInOffhand,
            Mount
        }

        // Player Config Fields
        [SerializeField] private PlayerAbilityConfig m_AbilityConfig;

        public PlayerAbilityConfig AbilityConfig => m_AbilityConfig;

        // Status Fields
        [SerializeField] private PlayerStatusUpdater m_StatusUpdater;
        public PlayerStatus Status => m_StatusUpdater.Status;

        // Camera & Player Render Fields
        private CameraController m_CameraController;
        private Transform m_FollowRef;
        private Transform m_AimingRef;
        private EntityRender m_PlayerRender;
        [SerializeField] private Vector3 m_InitialUpward = Vector3.up;
        [SerializeField] private Vector3 m_InitialForward = Vector3.forward;

        // Input System Fields & Methods
        public PlayerActions Actions { get; private set; }

        public void EnableInput() => Actions?.Enable();
        public void DisableInput() => Actions?.Disable();
        
        private Vector3 currentVelocity = Vector3.zero;

#nullable enable
        
        // Player State Fields
        private IPlayerState? pendingState;
        
#nullable disable
        
        public IPlayerState CurrentState { get; private set; } = PlayerStates.PRE_INIT;

        // Values for sending over to the server. Should only be set
        // from the unity thread and read from the network thread

        /// <summary>
        /// Player location for sending to the server
        /// </summary>
        public Location Location2Send { get; private set; }

        /// <summary>
        /// Player yaw for sending to the server
        /// This yaw value is stored in Minecraft coordinate system.
        /// Conversion is required when assigning to unity transform
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

        public void SwitchPlayerRenderFromPrefab(EntityData entity, GameObject renderPrefab)
        {
            var renderObj = Instantiate(renderPrefab);
            renderObj.name = $"Player Entity ({renderPrefab.name})";

            SwitchPlayerRender(entity, renderObj);
        }

        private void SwitchPlayerRender(EntityData entity, GameObject renderObj)
        {
            var prevRender = m_PlayerRender;

            if (prevRender)
            {
                // Unregister previous aiming mode change handler
                EventManager.Instance.Unregister<CameraAimingEvent>(prevRender.HandleAimingModeChange);
                
                // Unload and then destroy previous render object, if present
                prevRender.Unload();
            }

            // Clear existing event subscriptions
            OnPlayerUpdate = null;
            
            // Update controller's player render
            if (renderObj.TryGetComponent(out m_PlayerRender))
            {
                // Initialize head yaw to look forward
                if (m_PlayerRender is LivingEntityRender livingEntityRender)
                    livingEntityRender.HeadYaw.Value = EntityData.GetHeadYawFromByte(127); // i.e. -90F
                m_PlayerRender.UUID = entity.UUID;
                m_PlayerRender.transform.SetParent(transform, false);

                // Destroy these colliders, so that they won't affect our movement
                foreach (var playerCollider in m_PlayerRender.GetComponentsInChildren<Collider>())
                {
                    Destroy(playerCollider);
                }

                // Initialize player entity render (originOffset not used here)
                m_PlayerRender.Initialize(entity, Vector3Int.zero);
                // Workaround: This value should not be applied to entity render for client player
                m_PlayerRender.VisualTransform.localRotation = Quaternion.identity;

                // Update render gameobject layer (do this last to ensure all children are present)
                foreach (var child in renderObj.GetComponentsInChildren<Transform>())
                {
                    child.gameObject.layer = gameObject.layer;
                }

                OnPlayerUpdate += (velocity, _, _) =>
                {
                    // Update player render velocity
                    m_PlayerRender.SetVisualMovementVelocity(velocity);
                    // Update render
                    m_PlayerRender.UpdateAnimation(0.05F);
                };
                
                // Setup camera ref and use it
                m_FollowRef = m_PlayerRender.SetupCameraRef();
                m_AimingRef = m_PlayerRender.GetAimingRef();
                // Reset player render local position
                m_PlayerRender.transform.localPosition = Vector3.zero;
                
                // Initialize aiming mode state
                if (m_CameraController)
                {
                    // Create a dummy aiming mode change event
                    var aimingModeEvent = new CameraAimingEvent(m_CameraController.IsAimingOrLocked);
                    m_PlayerRender.HandleAimingModeChange(aimingModeEvent);
                }
                
                EventManager.Instance.Register<CameraAimingEvent>(m_PlayerRender.HandleAimingModeChange);
            }
            else
            {
                Debug.LogWarning("Player render not found in game object!");
                // Use own transform
                m_FollowRef = transform;
                m_AimingRef = transform;
            }

            if (m_CameraController)
            {
                m_CameraController.SetTargets(m_FollowRef, m_AimingRef);
            }
            
            // Re-initialize current state
            ChangeToState(CurrentState);
        }

        public void HandleCameraControllerSwitch(CameraController cameraController)
        {
            m_CameraController = cameraController;

            if (m_FollowRef && m_AimingRef)
            {
                cameraController.transform.rotation = Quaternion.LookRotation(m_InitialForward, m_InitialUpward);
                m_CameraController.SetTargets(m_FollowRef, m_AimingRef);
            }
            else
            {
                Debug.LogWarning("Camera ref is not present when switching to a new camera controller.");
            }
        }

#nullable enable

        private Action<GameModeUpdateEvent>? gameModeCallback;

        public delegate void ItemStateEventHandler(CurrentItemState weaponState);

        public event ItemStateEventHandler? OnItemStateChanged;
        
        public void ChangeItemState(CurrentItemState itemState)
        {
            OnItemStateChanged?.Invoke(itemState);
        }

        public delegate void ItemStackEventHandler(ItemStack? item, ItemActionType actionType);
        public event ItemStackEventHandler? OnCurrentItemChanged;
        public void ChangeCurrentItem(ItemStack? currentItem, ItemActionType actionType)
        {
            OnCurrentItemChanged?.Invoke(currentItem, actionType);
        }

        public event Action? OnRandomizeMirroredFlag;
        public void RandomizeMirroredFlag() => OnRandomizeMirroredFlag?.Invoke();

        // Used only by player renders, will be cleared and reassigned upon player render update
        private delegate void PlayerUpdateEventHandler(Vector3 velocity, float interval, PlayerStatus status);
        private event PlayerUpdateEventHandler? OnPlayerUpdate;

#nullable disable

        private void Awake()
        {
            if (Actions == null)
            {
                Actions = new PlayerActions();
                Actions.Enable();
            }
        }

        private void Start()
        {
            // Set stamina to max value
            Status.StaminaLeft = m_AbilityConfig.MaxStamina;
            // And broadcast current stamina
            EventManager.Instance.Broadcast(new StaminaUpdateEvent(Status.StaminaLeft, m_AbilityConfig.MaxStamina));
            // Initialize health value
            EventManager.Instance.Broadcast(new HealthUpdateEvent(20F, true));

            // Register gamemode events for updating gamemode
            gameModeCallback = e => SetGameMode(e.GameMode);
            EventManager.Instance.Register(gameModeCallback);
        }

        private void FixedUpdate()
        {
            var chunkRenderManager = CornApp.CurrentClient?.ChunkRenderManager;
            if (!chunkRenderManager) return;
            
            var terrainAABBs = chunkRenderManager.GetTerrainAABBs();
            
            BeforeCharacterUpdate(terrainAABBs);
            
            // Update player position
            CharacterUpdate(Time.fixedDeltaTime, terrainAABBs);
            
            AfterCharacterUpdate();
        }

        private void OnDestroy()
        {
            Actions?.Disable();

            if (gameModeCallback is not null)
                EventManager.Instance.Unregister(gameModeCallback);
        }

        private void SetGameMode(GameMode gameMode)
        {
            Status.GameMode = gameMode;

            switch (gameMode)
            {
                case GameMode.Survival:
                case GameMode.Creative:
                case GameMode.Adventure:
                    Status.Flying = gameMode == GameMode.Creative;
                    var initState = Status.Flying ? PlayerStates.AIRBORNE : PlayerStates.GROUNDED;
                    Status.Spectating = false;

                    if (CurrentState != initState) // Update initial player state
                    {
                        ChangeToState(initState);
                    }

                    // Update components state...
                    Status.GravityScale = 1F;

                    Status.EntityDisabled = false;

                    // Show entity render
                    if (m_PlayerRender)
                    {
                        m_PlayerRender.gameObject.SetActive(true);
                    }
                    break;
                case GameMode.Spectator:
                    Status.Spectating = true;
                    
                    if (CurrentState != PlayerStates.SPECTATE) // Update player state
                    {
                        ChangeToState(PlayerStates.SPECTATE);
                    }

                    // Update components state...
                    Status.GravityScale = 0F;

                    // Reset current velocity to zero
                    currentVelocity = Vector3.zero;

                    // Reset player status
                    Status.Grounded = false;
                    Status.InLiquid = false;
                    Status.Clinging = false;
                    Status.Sprinting = false;

                    Status.EntityDisabled = true;

                    // Hide entity render
                    if (m_PlayerRender)
                    {
                        m_PlayerRender.gameObject.SetActive(false);
                    }
                    break;
            }
        }

        public void DisablePhysics()
        {
            Status.PhysicsDisabled = true;
        }

        public void EnablePhysics()
        {
            Status.PhysicsDisabled = false;
        }
        
        public void ToggleSneaking()
        {
            Status.Sneaking = !Status.Sneaking;
            CornApp.Notify(Translations.Get($"gameplay.control.sneaking_{(Status.Sneaking ? "started" : "stopped")}"));
        }

        public void ChangeToState(IPlayerState state)
        {
            var prevState = CurrentState;

            //Debug.Log($"Exit state [{CurrentState}]");
            CurrentState.OnExit(state, m_StatusUpdater.Status, this);

            // Exit previous state and enter this state
            CurrentState = state;
            
            //Debug.Log($"Enter state [{CurrentState}]");
            CurrentState.OnEnter(prevState, m_StatusUpdater.Status, this);
        }

        public void UseAimingCamera(bool enable)
        {
            if (m_CameraController)
            {
                // Align target visual yaw with camera, immediately
                if (enable) Status.TargetVisualYaw = m_CameraController.GetYaw();

                m_CameraController.UseAimingCamera(enable);
            }
        }

        public bool IsUsingAimingCamera()
        {
            return m_CameraController && m_CameraController.IsAimingOrLocked;
        }

        public void ToggleAimingLock()
        {
            if (m_CameraController)
            {
                // Align target visual yaw with camera, immediately
                if (!m_CameraController.AimingLocked) Status.TargetVisualYaw = m_CameraController.GetYaw();

                m_CameraController.UseAimingLock(!m_CameraController.AimingLocked);
            }
        }

        public Quaternion GetMovementOrientation()
        {
            var upward = m_InitialUpward;
            var forward = Quaternion.AngleAxis(Status.MovementInputYaw, upward) * m_InitialForward;

            return Quaternion.LookRotation(forward, upward);
        }

        public void BeforeCharacterUpdate(UnityAABB[] aabbs)
        {
            var status = m_StatusUpdater.Status;

            // Update target player visual yaw before updating player status
            var horInput = Actions!.Locomotion.Movement.ReadValue<Vector2>();
            if (horInput != Vector2.zero)
            {
                var userInputYaw = GetYawFromVector2(horInput);
                status.TargetVisualYaw = m_CameraController.GetYaw() + userInputYaw;
                status.MovementInputYaw = status.TargetVisualYaw;
            }

            // Update target visual yaw if aiming
            if (m_CameraController && m_CameraController.IsAimingOrLocked)
            {
                status.TargetVisualYaw = m_CameraController.GetYaw();

                // Align player orientation with camera view (which is set as the target value)
                status.CurrentVisualYaw = status.TargetVisualYaw;
            }

            // Update player status (in water, grounded, etc)
            if (!Status.EntityDisabled)
            {
                m_StatusUpdater.UpdatePlayerStatus(GetMovementOrientation(), aabbs);
            }

            // Update current player state
            if (pendingState != null) // Change to pending state if present
            {
                ChangeToState(pendingState);
                pendingState = null;
            }
            else if (CurrentState.ShouldExit(Actions!, status))
            {
                // Try to exit current state and enter another one
                foreach (var state in PlayerStates.STATES.Where(
                             state => state != CurrentState && state.ShouldEnter(Actions!, status)))
                {
                    ChangeToState(state);
                    break;
                }
            }
        }

        public void CharacterUpdate(float deltaTime, UnityAABB[] aabbs)
        {
            var status = m_StatusUpdater.Status;
            var prevStamina = status.StaminaLeft;
            
            // Update player physics and transform using updated current state
            CurrentState.UpdateMain(ref currentVelocity, deltaTime, Actions!, status, this);
            
            // Update player rotation (yaw)
            if (m_PlayerRender)
            {
                m_PlayerRender.transform.eulerAngles = new Vector3(0F, status.CurrentVisualYaw, 0F);
            }

            if (status.PhysicsDisabled)
            {
                currentVelocity = Vector3.zero;
            }
            else
            {
                // Update player position using calculated velocity
                m_StatusUpdater.UpdatePlayerPosition(ref currentVelocity, deltaTime, aabbs);
            }

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (prevStamina != status.StaminaLeft) // Broadcast current stamina if changed
            {
                EventManager.Instance.Broadcast(new StaminaUpdateEvent(status.StaminaLeft, m_AbilityConfig.MaxStamina));
            }

            // Visual updates... Don't pass the velocity by ref here, just the value
            OnPlayerUpdate?.Invoke(currentVelocity, deltaTime, Status);
        }

        public void AfterCharacterUpdate()
        {
            var newLocation = CoordConvert.Unity2MC(_worldOriginOffset, transform.position);

            // Update values to send to server
            Location2Send = newLocation;

            if (m_PlayerRender)
            {
                // Update client player data
                MCYaw2Send = Status.CurrentVisualYaw - 90F; // Coordinate system conversion
                Pitch2Send = 0F;
            }
            
            IsGrounded2Send = Status.Grounded;
        }

        private Vector3Int _worldOriginOffset = Vector3Int.zero;

        /// <summary>
        /// Called when updating world origin offset to teleport the
        /// player seamlessly, returns actual position delta applied
        /// </summary>
        public Vector3 SetWorldOriginOffset(Vector3Int offset)
        {
            _worldOriginOffset = offset;

            // Recalculate position in Unity scene based on new world origin
            var updatedPosition = CoordConvert.MC2Unity(offset, Location2Send);
            var playerPosDelta = updatedPosition - transform.position;

            transform.position = updatedPosition;

            return playerPosDelta;
        }

        public void SetLocationFromServer(Location loc, bool reset = false, float mcYaw = 0F)
        {
            if (reset) // Reset current velocity
            {
                currentVelocity = Vector3.zero;
            }

            var newUnityYaw = mcYaw + 90F; // Coordinate system conversion

            Status.TargetVisualYaw = newUnityYaw;
            Status.CurrentVisualYaw = newUnityYaw;

            // Update current location and yaw
            transform.position = CoordConvert.MC2Unity(_worldOriginOffset, loc);
            m_PlayerRender.Yaw.Value = newUnityYaw;

            // Update local data
            Location2Send = loc;
            MCYaw2Send = mcYaw;
        }

        private static float GetYawFromVector2(Vector2 direction)
        {
            if (direction.y > 0F)
                return Mathf.Atan(direction.x / direction.y) * Mathf.Rad2Deg;
            if (direction.y < 0F)
                return Mathf.Atan(direction.x / direction.y) * Mathf.Rad2Deg + 180F;
            return direction.x > 0 ? 90F : 270F;
        }

        public string GetDebugInfo()
        {
            var statusInfo = Status.Spectating ? string.Empty : Status.ToString();

            return $"State: {CurrentState}\n{statusInfo}\nVelocity: {currentVelocity.x:0.00}\t{currentVelocity.y:0.00}\t{currentVelocity.z:0.00}";
        }
    }
}
