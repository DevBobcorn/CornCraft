using System;
using System.Linq;
using UnityEngine;

using CraftSharp.Event;
using CraftSharp.Rendering;
using KinematicCharacterController;

namespace CraftSharp.Control
{
    [RequireComponent(typeof (PlayerStatusUpdater))]
    public class PlayerController : MonoBehaviour, ICharacterController
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
        [SerializeField] private KinematicCharacterMotor m_Motor;
        private KinematicCharacterMotor Motor => m_Motor;

        // Input System Fields & Methods
        public PlayerActions Actions { get; private set; }

        public void EnableInput() => Actions?.Enable();
        public void DisableInput() => Actions?.Disable();

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
            Motor.CharacterController = this;

            // Set stamina to max value
            Status.StaminaLeft = m_AbilityConfig.MaxStamina;
            // And broadcast current stamina
            EventManager.Instance.Broadcast<StaminaUpdateEvent>(new(Status.StaminaLeft, m_AbilityConfig.MaxStamina));
            // Initialize health value
            EventManager.Instance.Broadcast<HealthUpdateEvent>(new(20F, true));

            // Register gamemode events for updating gamemode
            gameModeCallback = e => SetGameMode(e.GameMode);
            EventManager.Instance.Register(gameModeCallback);
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
                    IPlayerState initState = Status.Flying ? PlayerStates.AIRBORNE : PlayerStates.GROUNDED;
                    Status.Spectating = false;

                    if (CurrentState != initState) // Update initial player state
                    {
                        ChangeToState(initState);
                    }

                    // Update components state...
                    Motor.Capsule.enabled = true;
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
                    Motor.Capsule.enabled = false;
                    Status.GravityScale = 0F;

                    // Reset velocity to zero TODO: Check
                    Motor.BaseVelocity = Vector3.zero;

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
            Motor.enabled = false;
        }

        public void EnablePhysics()
        {
            Status.PhysicsDisabled = false;
            Motor.enabled = true;
        }
        
        public void ToggleSneaking()
        {
            Status.Sneaking = !Status.Sneaking;
            CornApp.Notify(Translations.Get($"gameplay.control.sneaking_{(Status.Sneaking ? "started" : "stopped")}"));
        }

        public void ClimbOverBarrier(float barrierDist, float barrierHeight, bool walkUp, bool fromLiquid)
        {
            Vector2 extraOffset = AbilityConfig.ClimbOverExtraOffset;

            if (fromLiquid) extraOffset.x += 0.1F;
            var forwardDir = GetMovementOrientation() * Vector3.forward;
            var offset = forwardDir * barrierDist + barrierHeight * Motor.CharacterUp;

            StartForceMoveOperation("Climb over barrier (TransformDisplacement)",
                new ForceMoveOperation[] {
                    new(offset, barrierHeight * 0.3F,
                        exit: (info, _, _) =>
                        {
                            info.Grounded = true;
                        },
                        update: (_, _, inputData, info, _, _) =>
                        {
                            info.Moving = inputData.Locomotion.Movement.IsPressed();
                            // Don't terminate till the time ends
                            return false;
                        }
                    )
                } );
        }

        private void StartForceMoveOperation(string stateName, ForceMoveOperation[] ops)
        {
            // Set it as pending state, this will be set as active state upon next logical update
            pendingState = new ForceMoveState(stateName, ops);
        }

        public void ChangeToState(IPlayerState state)
        {
            var prevState = CurrentState;

            //Debug.Log($"Exit state [{CurrentState}]");
            CurrentState.OnExit(state, m_StatusUpdater.Status, Motor, this);

            // Exit previous state and enter this state
            CurrentState = state;
            
            //Debug.Log($"Enter state [{CurrentState}]");
            CurrentState.OnEnter(prevState, m_StatusUpdater.Status, Motor, this);
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

        /// <summary>
        /// Update player status
        /// </summary>
        private void UpdatePlayerStatus()
        {
            if (!Status.EntityDisabled)
            {
                m_StatusUpdater.UpdatePlayerStatus(Motor, GetMovementOrientation());
            }
        }

        public void BeforeCharacterUpdate(float deltaTime)
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
            UpdatePlayerStatus();

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

            CurrentState.UpdateBeforeMotor(deltaTime, Actions!, status, Motor, this);
        }

        public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            var upward = m_InitialUpward;
            var forward = Quaternion.AngleAxis(Status.CurrentVisualYaw, upward) * m_InitialForward;

            currentRotation = Quaternion.LookRotation(forward, upward);
        }

        public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            var status = m_StatusUpdater.Status;

            float prevStamina = status.StaminaLeft;
            
            // Update player physics and transform using updated current state
            CurrentState.UpdateMain(ref currentVelocity, deltaTime, Actions!, status, Motor, this);

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (prevStamina != status.StaminaLeft) // Broadcast current stamina if changed
            {
                EventManager.Instance.Broadcast<StaminaUpdateEvent>(new(status.StaminaLeft, m_AbilityConfig.MaxStamina));
            }

            // Visual updates... Don't pass the velocity by ref here, just the value
            OnPlayerUpdate?.Invoke(currentVelocity, deltaTime, Status);
        }

        public void AfterCharacterUpdate(float deltaTime)
        {
            var newLocation = CoordConvert.Unity2MC(_worldOriginOffset, transform.position);

            // Update values to send to server
            Location2Send = new(
                Math.Round(newLocation.X, 2),
                Math.Round(newLocation.Y, 2),
                Math.Round(newLocation.Z, 2)
            );

            if (m_PlayerRender)
            {
                // Update client player data
                MCYaw2Send = Status.CurrentVisualYaw - 90F; // Coordinate system conversion
                Pitch2Send = 0F;
            }
            
            IsGrounded2Send = Status.GroundCheck;
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

            Motor.SetPosition(updatedPosition);

            return playerPosDelta;
        }

        public void SetLocationFromServer(Location loc, bool reset = false, float mcYaw = 0F)
        {
            if (reset) // Reset motor velocity TODO: Check
            {
                Motor.BaseVelocity = Vector3.zero;
            }

            var newUnityYaw = mcYaw + 90F; // Coordinate system conversion
            var newRotation = Quaternion.Euler(0F, newUnityYaw, 0F);

            Status.TargetVisualYaw = newUnityYaw;
            Status.CurrentVisualYaw = newUnityYaw;

            // Update current location and yaw
            Motor.SetPositionAndRotation(CoordConvert.MC2Unity(_worldOriginOffset, loc), newRotation);

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

            return $"State: {CurrentState}\n{statusInfo}";
        }

        // Misc methods from Kinematic Character Controller

        public void PostGroundingUpdate(float deltaTime)
        {
            // Do nothing
        }

        public bool IsColliderValidForCollisions(Collider coll)
        {
            if (!m_StatusUpdater)
            {
                return true;
            }
            return !m_StatusUpdater.Status.Spectating && !CurrentState.IgnoreCollision();
        }

        public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
            // Do nothing
        }

        public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
            // Do nothing
        }

        public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
        {
            // Do nothing
        }

        public void OnDiscreteCollisionDetected(Collider hitCollider)
        {
            // Do nothing
        }
    }
}
