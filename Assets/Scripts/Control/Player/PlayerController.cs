using System;
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

        public ItemActionType CurrentActionType { get; private set; } = ItemActionType.None;

        // AbilityConfig & Skill Fields
        [SerializeField] private PlayerAbilityConfig m_AbilityConfig;
        [SerializeField] private PlayerSkillItemConfig m_SkillItemConfig;

        public PlayerAbilityConfig AbilityConfig => m_AbilityConfig;
        public PlayerSkillItemConfig SkillItemConfig => m_SkillItemConfig;

        // Status Fields
        [SerializeField] private PlayerStatusUpdater m_StatusUpdater;
        public PlayerStatus Status => m_StatusUpdater == null ? null : m_StatusUpdater.Status;
        private bool _usingAnimator = false;

        /// <summary>
        /// Whether player root motion should be applied.
        /// </summary>
        public bool UseRootMotion { get; set; } = false;

        /// <summary>
        /// Whether to countervail animator scale when applying root motion displacement. Only uniform scale is supported.
        /// </summary>
        public bool IgnoreAnimatorScale { get; set; } = false;

        /// <summary>
        /// Root motion position delta. Reset after applied.
        /// </summary>
        public Vector3 RootMotionPositionDelta { get; set; } = Vector3.zero;

        /// <summary>
        /// Root motion rotation delta. Reset after applied.
        /// </summary>
        public Quaternion RootMotionRotationDelta { get; set; } = Quaternion.identity;

        // Camera & Player Render Fields
        private CameraController m_CameraController;
        private Transform m_CameraRef;
        private EntityRender m_PlayerRender;
        [SerializeField] private Vector3 m_InitialUpward = Vector3.up;
        [SerializeField] private Vector3 m_InitialForward = Vector3.forward;
        [SerializeField] private KinematicCharacterMotor m_Motor;
        public KinematicCharacterMotor Motor => m_Motor;

        // Input System Fields & Methods
        private PlayerActions m_PlayerActions;
        public PlayerActions Actions => m_PlayerActions;

        public void EnableInput() => m_PlayerActions?.Enable();
        public void DisableInput() => m_PlayerActions?.Disable();

        // Player State Fields

        #nullable enable

        private IPlayerState? _currentState = PlayerStates.GROUNDED;
        private IPlayerState? _pendingState = null;

        #nullable disable

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

        public void SwitchPlayerRenderFromPrefab(Entity entity, GameObject renderPrefab)
        {
            GameObject renderObj;
            if (renderPrefab.TryGetComponent(out Animator _)) // Model prefab, wrap it up
            {
                renderObj = AnimatorEntityRender.CreateFromModel(renderPrefab);
            }
            else // Player render prefab, just instantiate
            {
                renderObj = GameObject.Instantiate(renderPrefab);
            }
            renderObj!.name = $"Player Entity ({renderPrefab.name})";

            SwitchPlayerRender(entity, renderObj);
        }

        private void SwitchPlayerRender(Entity entity, GameObject renderObj)
        {
            var prevRender = m_PlayerRender;

            if (prevRender != null)
            {
                // Unload and then destroy previous render object, if present
                prevRender.Unload();
            }

            // Clear existing event subscriptions
            OnPlayerUpdate = null;
            
            // Update controller's player render
            if (renderObj.TryGetComponent<EntityRender>(out m_PlayerRender))
            {
                // Initialize head yaw to look forward
                m_PlayerRender.HeadYaw.Value = Entity.GetHeadYawFromByte(127); // i.e. -90F
                m_PlayerRender.UUID = entity.UUID;
                m_PlayerRender.transform.SetParent(transform, false);

                // Initialize materials (This requires metadata to be present)
                if (renderObj.TryGetComponent(out EntityMaterialAssigner materialControl))
                {
                    materialControl.InitializeMaterials(entity.Type, m_PlayerRender.GetControlVariables(), entity.Metadata);
                }

                // Destroy these colliders, so that they won't affect our movement
                foreach (var collider in m_PlayerRender.GetComponentsInChildren<Collider>())
                {
                    Destroy(collider);
                }

                // Initialize player entity render (originOffset not used here)
                m_PlayerRender.Initialize(entity, Vector3Int.zero);
                // Workaround: This value should not be applied to entity render for client player
                m_PlayerRender.VisualTransform.localRotation = Quaternion.identity;

                // Update render gameobject layer (do this last to ensure all children are present)
                foreach (var child in renderObj.GetComponentsInChildren<Transform>())
                {
                    child.gameObject.layer = this.gameObject.layer;
                }

                var riggedRender = m_PlayerRender as PlayerEntityRiggedRender;
                if (riggedRender != null) // If player render is rigged render
                {
                    // Additionally, update player state machine for rigged rendersInitialize
                    OnPlayerUpdate += (velocity, _, status) =>
                    {
                        // Update player render velocity
                        m_PlayerRender.SetVisualMovementVelocity(velocity, Motor.CharacterUp);
                        // Upload animator state machine parameters
                        riggedRender.UpdateAnimator(status);
                        // Update render
                        m_PlayerRender.UpdateAnimation(0.05F);
                    };
                    // Initialize current item held by player
                    // TODO: Remove direct reference to client
                    var activeItem = CornApp.CurrentClient!.GetActiveItem();
                    riggedRender.InitializeActiveItem(activeItem,
                            PlayerActionHelper.GetItemActionType(activeItem));
                    // Set animator flag
                    _usingAnimator = true;
                }
                else // Player render is vanilla/entity render
                {
                    OnPlayerUpdate += (velocity, _, _) =>
                    {
                        // Update player render velocity
                        m_PlayerRender.SetVisualMovementVelocity(velocity, Motor.CharacterUp);
                        // Update render
                        m_PlayerRender.UpdateAnimation(0.05F);
                    };
                    // Reset animator flag
                    _usingAnimator = false;
                }
                // Setup camera ref and use it
                m_CameraRef = m_PlayerRender.SetupCameraRef();
                // Reset player render local position
                m_PlayerRender!.transform.localPosition = Vector3.zero;
            }
            else
            {
                Debug.LogWarning("Player render not found in game object!");
                // Use own transform
                m_CameraRef = transform;
                // Reset animator flag
                _usingAnimator = false;
            }

            if (m_CameraController != null)
            {
                m_CameraController.SetTarget(m_CameraRef);
            }
        }

        public void HandleCameraControllerSwitch(CameraController cameraController)
        {
            m_CameraController = cameraController;

            if (m_CameraRef != null)
            {
                cameraController.transform.rotation = Quaternion.LookRotation(m_InitialForward, m_InitialUpward);
                cameraController.SetTarget(m_CameraRef);
            }
            else
            {
                Debug.LogWarning("Camera ref is not present when switching to a new camera controller.");
            }
        }

        #nullable enable

        private Action<GameModeUpdateEvent>? gameModeCallback;
        private Action<HeldItemChangeEvent>? heldItemCallback;

        public delegate void ItemStateEventHandler(CurrentItemState weaponState);
        public event ItemStateEventHandler? OnItemStateChanged;
        public void ChangeItemState(CurrentItemState itemState) => OnItemStateChanged?.Invoke(itemState);

        public delegate void ItemStackEventHandler(ItemStack? item, ItemActionType actionType, PlayerSkillItemConfig? config);
        public event ItemStackEventHandler? OnCurrentItemChanged;

        public delegate void CrossFadeStateEventHandler(string stateName, float time, int layer, float timeOffset);
        public event CrossFadeStateEventHandler? OnCrossFadeState;
        public void StartCrossFadeState(string stateName, float time = 0.2F, int layer = 0, float timeOffset = 0F)
        {
            OnCrossFadeState?.Invoke(stateName, time, layer, timeOffset);
        }

        public delegate void OverrideStateEventHandler(AnimationClip dummyClip, AnimationClip animationClip);
        public event OverrideStateEventHandler? OnOverrideState;
        public void OverrideStateAnimation(AnimationClip dummyClip, AnimationClip animationClip)
        {
            OnOverrideState?.Invoke(dummyClip, animationClip);
        }

        public event Action? OnRandomizeMirroredFlag;
        public void RandomizeMirroredFlag() => OnRandomizeMirroredFlag?.Invoke();

        public event Action? OnMeleeDamageStart;
        public void MeleeDamageStart() => OnMeleeDamageStart?.Invoke();

        public event Action? OnMeleeDamageEnd;
        public void MeleeDamageEnd() => OnMeleeDamageEnd?.Invoke();

        // Used only by player renders, will be cleared and reassigned upon player render update
        private delegate void PlayerUpdateEventHandler(Vector3 velocity, float interval, PlayerStatus status);
        private event PlayerUpdateEventHandler? OnPlayerUpdate;

        #nullable disable

        void Start()
        {
            m_PlayerActions = new PlayerActions();
            m_PlayerActions.Enable();

            Motor.CharacterController = this;

            // Set stamina to max value
            Status!.StaminaLeft = m_AbilityConfig!.MaxStamina;
            // And broadcast current stamina
            EventManager.Instance.Broadcast<StaminaUpdateEvent>(new(Status.StaminaLeft, m_AbilityConfig!.MaxStamina));
            // Initialize health value
            EventManager.Instance.Broadcast<HealthUpdateEvent>(new(20F, true));

            // Register gamemode events for updating gamemode
            gameModeCallback = (e) => SetGameMode(e.GameMode);
            EventManager.Instance.Register(gameModeCallback);

            // Register hotbar item event for updating item visuals
            heldItemCallback = (e) =>
            {
                OnCurrentItemChanged?.Invoke(e.ItemStack, e.ActionType, null);
                // Exit attack state when active item is changed
                Status!.Attacking = false;
                CurrentActionType = e.ActionType;
            };
            EventManager.Instance.Register(heldItemCallback);

            // Initialize player state (idle on start)
            Status.Grounded = true;
            _currentState.OnEnter(PlayerStates.PRE_INIT, Status, Motor, this);
        }

        void OnDestroy()
        {
            m_PlayerActions?.Disable();

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
            Motor.enabled = false;
        }

        public void EnablePhysics()
        {
            Status!.PhysicsDisabled = false;
            Motor.enabled = true;
        }

        protected void DisableEntity()
        {
            // Update components state...
            Motor.Capsule.enabled = false;
            Status!.GravityScale = 0F;

            // Reset velocity to zero TODO: Check
            Motor.BaseVelocity = Vector3.zero;

            // Reset player status
            Status.Grounded = false;
            Status.InLiquid = false;
            Status.Clinging = false;
            Status.Sprinting = false;

            Status.EntityDisabled = true;

            // Hide entity render
            if (m_PlayerRender != null)
            {
                m_PlayerRender.gameObject.SetActive(false);
            }
        }

        protected void EnableEntity()
        {
            // Update components state...
            Motor.Capsule!.enabled = true;
            Status!.GravityScale = 1F;

            Status.EntityDisabled = false;

            // Show entity render
            if (m_PlayerRender != null)
            {
                m_PlayerRender.gameObject.SetActive(true);
            }
        }
        
        public void ToggleWalkMode()
        {
            Status!.WalkMode = !Status.WalkMode;
            CornApp.Notify(Translations.Get($"gameplay.control.switch_to_{(Status.WalkMode ? "walk" : "rush")}"));
        }

        public void ClimbOverBarrier(float barrierDist, float barrierHeight, bool walkUp, bool fromLiquid)
        {
            if (_usingAnimator && !walkUp)
            {
                Vector2 extraOffset;

                if (m_PlayerRender is PlayerEntityRiggedRender riggedRender)
                {
                    extraOffset = riggedRender.GetClimbOverOffset();
                }
                else
                {
                    extraOffset = AbilityConfig.ClimbOverExtraOffset;
                }

                if (fromLiquid)
                {
                    extraOffset.x += 0.1F;
                }

                var offset = (barrierHeight * 0.5F - 0.5F + extraOffset.y) * Motor.CharacterUp + extraOffset.x * Motor.CharacterForward;
                
                StartForceMoveOperation("Climb over barrier (RootMotion)",
                        new ForceMoveOperation[] {
                                new(offset, AbilityConfig.ClimbOverMoveTime,
                                    init: (info, motor, player) =>
                                    {
                                        player.RandomizeMirroredFlag();
                                        player.StartCrossFadeState(PlayerAbilityConfig.CLIMB_1M, 0.1F);
                                        info.PlayingRootMotion = true;
                                        player.UseRootMotion = true;
                                        player.IgnoreAnimatorScale = true;
                                    },
                                    update: (interval, curTime, inputData, info, motor, player) => false
                                ),
                                new(offset, AbilityConfig.ClimbOverTotalTime - AbilityConfig.ClimbOverMoveTime - AbilityConfig.ClimbOverCheckExit,
                                    update: (interval, curTime, inputData, info, motor, player) => false
                                ),
                                new(Vector3.zero, AbilityConfig.ClimbOverCheckExit,
                                    exit: (info, motor, player) =>
                                    {
                                        info.Grounded = true;
                                        player.UseRootMotion = false;
                                        info.PlayingRootMotion = false;
                                        StartCrossFadeState(GroundedState.GetEntryAnimatorStateName(info));
                                    },
                                    update: (interval, curTime, inputData, info, motor, player) =>
                                    {
                                        info.Moving = inputData.Gameplay.Movement.IsPressed();
                                        // Terminate force move action if almost finished and player is eager to move
                                        return info.Moving;
                                    }
                                )
                        } );
            }
            else
            {
                var forwardDir = GetTargetOrientation() * Vector3.forward;
                var offset = forwardDir * barrierDist + barrierHeight * Motor.CharacterUp;

                StartForceMoveOperation("Climb over barrier (Direct)",
                        new ForceMoveOperation[] {
                                new(offset, barrierHeight * 0.3F,
                                    exit: (info, motor, player) =>
                                    {
                                        info.Grounded = true;
                                    },
                                    update: (interval, curTime, inputData, info, motor, player) =>
                                    {
                                        info.Moving = inputData.Gameplay.Movement.IsPressed();
                                        // Don't terminate till the time ends
                                        return false;
                                    }
                                )
                        } );
            }
        }

        public void StartForceMoveOperation(string name, ForceMoveOperation[] ops)
        {
            // Set it as pending state, this will be set as active state upon next logical update
            _pendingState = new ForceMoveState(name, ops);
        }

        private void ChangeToState(IPlayerState state)
        {
            var prevState = _currentState;

            //Debug.Log($"Exit state [{_currentState}]");
            _currentState.OnExit(state, m_StatusUpdater!.Status, Motor, this);

            // Exit previous state and enter this state
            _currentState = state;
            
            //Debug.Log($"Enter state [{_currentState}]");
            _currentState.OnEnter(prevState, m_StatusUpdater!.Status, Motor, this);
        }

        public void AttachVisualFX(GameObject fxObj)
        {
            if (m_PlayerRender != null)
            {
                fxObj.transform.SetParent(m_PlayerRender.VisualTransform);
            }
            else
            {
                Debug.LogWarning("Trying to attach vfx object to empty player render!");
            }
        }

        public bool TryStartNormalAttack(IPlayerState attackState, PlayerStagedSkill skillData)
        {
            if (Status!.AttackStatus.AttackCooldown <= 0F)
            {
                // Specify attack data to use
                Status.AttackStatus.CurrentStagedAttack = skillData;

                // Update player state
                ChangeToState(attackState);
                
                return false;
            }
            
            return false;
        }

        public bool TryStartChargedAttack(IPlayerState attackState, PlayerChargedSkill skillData)
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
                    // Specify attack data to use
                    Status.AttackStatus.CurrentChargedAttack = skillData;

                    // Update player state
                    ChangeToState(attackState);

                    return true;
                }
                
                return false;
            }
            
            return false;
        }

        public void StartAiming()
        {
            if (m_CameraController != null)
            {
                // Align target visual yaw with camera, immediately
                Status!.TargetVisualYaw = m_CameraController.GetYaw();

                m_CameraController.EnableAimingCamera(true);
            }
        }

        public void StopAiming()
        {
            if (m_CameraController != null)
            {
                m_CameraController.EnableAimingCamera(false);
            }
        }

        public Quaternion GetCurrentOrientation()
        {
            var upward = m_InitialUpward;
            var forward = Quaternion.AngleAxis(Status!.CurrentVisualYaw, upward) * m_InitialForward;

            return Quaternion.LookRotation(forward, upward);
        }

        public Quaternion GetTargetOrientation()
        {
            var upward = m_InitialUpward;
            var forward = Quaternion.AngleAxis(Status!.TargetVisualYaw, upward) * m_InitialForward;

            return Quaternion.LookRotation(forward, upward);
        }

        /// <summary>
        /// Update player status
        /// </summary>
        public void UpdatePlayerStatus()
        {
            if (!Status!.EntityDisabled)
            {
                m_StatusUpdater!.UpdatePlayerStatus(Motor, GetTargetOrientation());
            }
        }

        public void BeforeCharacterUpdate(float deltaTime)
        {
            var status = m_StatusUpdater!.Status;

            // Update target player visual yaw before updating player status
            var horInput = m_PlayerActions!.Gameplay.Movement.ReadValue<Vector2>();
            if (horInput != Vector2.zero)
            {
                var userInputYaw = GetYawFromVector2(horInput);
                status.TargetVisualYaw = m_CameraController!.GetYaw() + userInputYaw;
            }

            // Update player status (in water, grounded, etc)
            UpdatePlayerStatus();

            // Update current player state
            if (_pendingState != null) // Change to pending state if present
            {
                ChangeToState(_pendingState);
                _pendingState = null;
            }
            else if (_currentState.ShouldExit(m_PlayerActions!, status))
            {
                // Try to exit current state and enter another one
                foreach (var state in PlayerStates.STATES)
                {
                    if (state != _currentState && state.ShouldEnter(m_PlayerActions!, status))
                    {
                        ChangeToState(state);
                        break;
                    }
                }
            }

            _currentState.UpdateBeforeMotor(deltaTime, m_PlayerActions!, status, Motor, this);
        }

        public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            var upward = m_InitialUpward;
            var forward = Quaternion.AngleAxis(Status!.CurrentVisualYaw, upward) * m_InitialForward;

            currentRotation = Quaternion.LookRotation(forward, upward);
        }

        public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            var status = m_StatusUpdater!.Status;

            float prevStamina = status.StaminaLeft;
            
            // Update player physics and transform using updated current state
            _currentState.UpdateMain(ref currentVelocity, deltaTime, m_PlayerActions!, status, Motor, this);

            // Broadcast current stamina if changed
            if (prevStamina != status.StaminaLeft)
            {
                EventManager.Instance.Broadcast<StaminaUpdateEvent>(new(status.StaminaLeft, m_AbilityConfig!.MaxStamina));
            }
            

            // Handle root motion
            if (UseRootMotion)
            {
                // Rotation (yaw only)
                var rootMotionYawDelta = RootMotionRotationDelta.eulerAngles.y;
                status.TargetVisualYaw += rootMotionYawDelta;
                status.CurrentVisualYaw += rootMotionYawDelta;
                // Position
                currentVelocity += RootMotionPositionDelta / deltaTime;
            }

            // Visual updates... Don't pass the velocity by ref here, just the value
            OnPlayerUpdate?.Invoke(currentVelocity, deltaTime, Status!);
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

            if (m_PlayerRender != null)
            {
                // Update client player data
                MCYaw2Send = Status!.CurrentVisualYaw - 90F; // Coordinate system conversion
                Pitch2Send = 0F;
            }

            if (m_CameraController != null)
            {
                if (m_CameraController.IsAiming)
                {
                    // Align target visual yaw with camera, immediately
                    Status!.TargetVisualYaw = m_CameraController.GetYaw();
                }
            }

            // Reset root motion deltas
            RootMotionPositionDelta = Vector3.zero;
            RootMotionRotationDelta = Quaternion.identity;
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

            Status!.TargetVisualYaw = newUnityYaw;
            Status!.CurrentVisualYaw = newUnityYaw;

            // Update current location and yaw
            Motor.SetPositionAndRotation(CoordConvert.MC2Unity(_worldOriginOffset, loc), newRotation);

            // Update local data
            Location2Send = loc;
            MCYaw2Send = mcYaw;
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
            /*
            string statusInfo;

            if (m_StatusUpdater!.Status.Spectating)
                statusInfo = string.Empty;
            else
                statusInfo = m_StatusUpdater!.Status.ToString();
            
            return $"ActionType:\t{CurrentActionType}\nState:\t{_currentState}\n{statusInfo}";
            */

            return $"ActionType:\t{CurrentActionType}\nState:\t{_currentState}";
        }

        // Misc methods from Kinematic Character Controller

        public void PostGroundingUpdate(float deltaTime)
        {
            // Do nothing
        }

        public bool IsColliderValidForCollisions(Collider coll)
        {
            if (m_StatusUpdater == null)
            {
                return true;
            }
            return !m_StatusUpdater.Status.Spectating && !_currentState.IgnoreCollision();
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
