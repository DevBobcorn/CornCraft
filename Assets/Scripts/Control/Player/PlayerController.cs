#nullable enable
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

        public PlayerStagedSkill? MeleeSwordAttack;
        public PlayerChargedSkill? RangedBowAttack;
        [SerializeField] private CameraController? _cameraController;
        [SerializeField] private PlayerStatusUpdater? _statusUpdater;
        [SerializeField] private PlayerAbility? _ability;
        
        [SerializeField] private Vector3 _initialUpward = Vector3.up;
        [SerializeField] private Vector3 _initialForward = Vector3.forward;

        public PlayerSkillItemConfig? SkillItemConf;
        public Transform? CameraRef;
        [SerializeField] private KinematicCharacterMotor? _motor;
        public KinematicCharacterMotor Motor => _motor!;
        
        private EntityRender? _playerRender;
        private PlayerActions? _playerActions;
        public PlayerActions Actions => _playerActions!;

        public void EnableInput() => _playerActions?.Enable();
        public void DisableInput() => _playerActions?.Disable();

        public PlayerAbility Ability => _ability!;
        private IPlayerState _currentState = PlayerStates.GROUNDED;
        private IPlayerState? _pendingState = null;

        private bool _usingAnimator = false;

        /// <summary>
        /// Whether player root motion should be applied.
        /// </summary>
        public bool UseRootMotion { get; set; } = false;

        /// <summary>
        /// Whether to countervail animator scale when applying root motion displacement.
        /// <br>
        /// Only uniform scale is supported.
        /// </summary>
        public bool IgnoreAnimatorScale { get; set; } = false;
        public Vector3 RootMotionPositionDelta { get; set; } = Vector3.zero;
        public Quaternion RootMotionRotationDelta { get; set; } = Quaternion.identity;

        public PlayerStatus? Status => _statusUpdater == null ? null : _statusUpdater.Status;

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
            var prevRender = _playerRender;

            if (prevRender != null)
            {
                // Unload and then destroy previous render object, if present
                prevRender.Unload();
            }

            // Clear existing event subscriptions
            OnPlayerUpdate = null;
            
            // Update controller's player render
            if (renderObj.TryGetComponent<EntityRender>(out _playerRender))
            {
                // Initialize head yaw to look forward
                _playerRender.HeadYaw.Value = Entity.GetHeadYawFromByte(127); // i.e. -90F
                _playerRender.transform.SetParent(transform, false);

                // Destroy these colliders, so that they won't affect our movement
                foreach (var collider in _playerRender.GetComponentsInChildren<Collider>())
                {
                    Destroy(collider);
                }

                // Initialize player entity render (originOffset not used here)
                _playerRender.Initialize(entity, Vector3Int.zero);
                // Workaround: This value should not be applied to entity render for client player
                _playerRender.VisualTransform.localRotation = Quaternion.identity;

                // Update render gameobject layer (do this last to ensure all children are present)
                foreach (var child in renderObj.GetComponentsInChildren<Transform>())
                {
                    child.gameObject.layer = this.gameObject.layer;
                }

                var riggedRender = _playerRender as PlayerEntityRiggedRender;
                if (riggedRender != null) // If player render is rigged render
                {
                    // Additionally, update player state machine for rigged rendersInitialize
                    OnPlayerUpdate += (velocity, _, status) =>
                    {
                        // Update player render velocity
                        _playerRender.SetVisualMovementVelocity(velocity, Motor.CharacterUp);
                        // Upload animator state machine parameters
                        riggedRender.UpdateAnimator(status);
                        // Update render
                        _playerRender.UpdateAnimation(0.05F);
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
                        _playerRender.SetVisualMovementVelocity(velocity, Motor.CharacterUp);
                        // Update render
                        _playerRender.UpdateAnimation(0.05F);
                    };
                    // Reset animator flag
                    _usingAnimator = false;
                }

                CameraRef = _playerRender.SetupCameraRef();
                _cameraController!.SetTarget(CameraRef);

                _playerRender!.transform.localPosition = Vector3.zero;
            }
            else
            {
                Debug.LogWarning("Player render not found in game object!");
                // Use own transform
                _cameraController!.SetTarget(transform);
                // Reset animator flag
                _usingAnimator = false;
            }
        }

        private Action<GameModeUpdateEvent>? gameModeCallback;
        private Action<HeldItemChangeEvent>? heldItemCallback;

        void Start()
        {
            _playerActions = new PlayerActions();
            _playerActions.Enable();

            Motor.CharacterController = this;

            // Initialize camera orientation, this has nothing to do with player yaw, just initial orientation
            _cameraController!.transform.rotation = Quaternion.LookRotation(_initialForward, _initialUpward);

            // Set stamina to max value
            Status!.StaminaLeft = _ability!.MaxStamina;
            // And broadcast current stamina
            EventManager.Instance.Broadcast<StaminaUpdateEvent>(new(Status.StaminaLeft, _ability!.MaxStamina));
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
            _currentState.OnEnter(Status, Motor, this);
        }

        void OnDestroy()
        {
            _playerActions?.Disable();

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
            if (_playerRender != null)
            {
                _playerRender.gameObject.SetActive(false);
            }
        }

        protected void EnableEntity()
        {
            // Update components state...
            Motor.Capsule!.enabled = true;
            Status!.GravityScale = 1F;

            Status.EntityDisabled = false;

            // Show entity render
            if (_playerRender != null)
            {
                _playerRender.gameObject.SetActive(true);
            }
        }
        
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

        public event Action<string>? OnJumpRequest;
        public void StartJump(string stateName) => OnJumpRequest?.Invoke(stateName);

        public void ToggleWalkMode()
        {
            Status!.WalkMode = !Status.WalkMode;
            CornApp.Notify(Translations.Get($"gameplay.control.switch_to_{(Status.WalkMode ? "walk" : "rush")}"));
        }

        public void ClimbOverBarrier(float barrierDist, float barrierHeight, bool walkUp)
        {
            if (_usingAnimator && barrierHeight > 0.6F)
            {
                PlayerEntityRiggedRender? riggedRender;
                Vector2 extraOffset;

                if ((riggedRender = _playerRender as PlayerEntityRiggedRender) != null)
                {
                    extraOffset = riggedRender.GetClimberOverOffset();
                }
                else
                {
                    extraOffset = Ability.ClimbOverExtraOffset;
                }

                var offset = (barrierHeight * 0.5F - 0.5F + extraOffset.y) * Motor.CharacterUp + extraOffset.x * Motor.CharacterForward;
                
                StartForceMoveOperation("Climb over barrier (RootMotion)",
                        new ForceMoveOperation[] {
                                new(offset, Ability.ClimbOverMoveTime,
                                    init: (info, motor, player) =>
                                    {
                                        player.RandomizeMirroredFlag();
                                        player.StartCrossFadeState(PlayerAbility.CLIMB_1M, 0.1F);
                                        info.PlayingRootMotion = true;
                                        player.UseRootMotion = true;
                                        player.IgnoreAnimatorScale = true;
                                    },
                                    update: (interval, curTime, inputData, info, motor, player) => false
                                ),
                                new(offset, Ability.ClimbOverTotalTime - Ability.ClimbOverMoveTime - Ability.ClimbOverCheckExit,
                                    update: (interval, curTime, inputData, info, motor, player) => false
                                ),
                                new(Vector3.zero, Ability.ClimbOverCheckExit,
                                    exit: (info, motor, player) =>
                                    {
                                        info.Grounded = true;
                                        info.TimeSinceGrounded = Mathf.Max(0F, info.TimeSinceGrounded);
                                        player.UseRootMotion = false;
                                        info.PlayingRootMotion = false;
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
                                        info.TimeSinceGrounded = Mathf.Max(0F, info.TimeSinceGrounded);
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
            //Debug.Log($"Exit state [{_currentState}]");
            _currentState.OnExit(_statusUpdater!.Status, Motor, this);

            // Exit previous state and enter this state
            _currentState = state;
            
            //Debug.Log($"Enter state [{_currentState}]");
            _currentState.OnEnter(_statusUpdater!.Status, Motor, this);
        }

        public void AttachVisualFX(GameObject fxObj)
        {
            if (_playerRender != null)
            {
                fxObj.transform.SetParent(_playerRender.VisualTransform);
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
                    _currentState.OnExit(Status, Motor, this);
                    // Specify attack data to use
                    Status.AttackStatus.CurrentStagedAttack = MeleeSwordAttack;
                    // Enter attack state
                    _currentState = PlayerStates.MELEE;
                    _currentState.OnEnter(_statusUpdater!.Status, Motor, this);
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
                    _currentState.OnExit(Status, Motor, this);
                    // Specify attack data to use
                    Status.AttackStatus.CurrentChargedAttack = RangedBowAttack;
                    // Enter attack state
                    _currentState = PlayerStates.RANGED_AIM;
                    _currentState.OnEnter(_statusUpdater!.Status, Motor, this);
                    return true;
                }
                
                return false;
            }
            
            return false;
        }

        public void StartAiming()
        {
            AlignVisualYawToCamera();
            _cameraController!.EnableAimingCamera(true);
        }

        public void StopAiming()
        {
            _cameraController!.EnableAimingCamera(false);
        }

        /// <summary>
        /// Align player orientation with camera, immediately
        /// </summary>
        private void AlignVisualYawToCamera()
        {
            Status!.TargetVisualYaw = _cameraController!.GetYaw();
        }

        public Quaternion GetCurrentOrientation()
        {
            var upward = _initialUpward;
            var forward = Quaternion.AngleAxis(Status!.CurrentVisualYaw, upward) * _initialForward;

            return Quaternion.LookRotation(forward, upward);
        }

        public Quaternion GetTargetOrientation()
        {
            var upward = _initialUpward;
            var forward = Quaternion.AngleAxis(Status!.TargetVisualYaw, upward) * _initialForward;

            return Quaternion.LookRotation(forward, upward);
        }

        /// <summary>
        /// Update player status
        /// </summary>
        public void UpdatePlayerStatus()
        {
            if (!Status!.EntityDisabled)
            {
                _statusUpdater!.UpdatePlayerStatus(Motor, GetTargetOrientation());
            }
        }

        public void BeforeCharacterUpdate(float deltaTime)
        {
            var status = _statusUpdater!.Status;

            // Update target player visual yaw before updating player status
            var horInput = _playerActions!.Gameplay.Movement.ReadValue<Vector2>();
            if (horInput != Vector2.zero)
            {
                var userInputYaw = GetYawFromVector2(horInput);
                status.TargetVisualYaw = _cameraController!.GetYaw() + userInputYaw;
            }

            // Update player status (in water, grounded, etc)
            UpdatePlayerStatus();

            // Update current player state
            if (_pendingState != null) // Change to pending state if present
            {
                ChangeToState(_pendingState);
                _pendingState = null;
            }
            else if (_currentState.ShouldExit(_playerActions!, status))
            {
                // Try to exit current state and enter another one
                foreach (var state in PlayerStates.STATES)
                {
                    if (state != _currentState && state.ShouldEnter(_playerActions!, status))
                    {
                        ChangeToState(state);
                        break;
                    }
                }
            }

            _currentState.UpdateBeforeMotor(deltaTime, _playerActions!, status, Motor, this);
        }

        public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            var upward = _initialUpward;
            var forward = Quaternion.AngleAxis(Status!.CurrentVisualYaw, upward) * _initialForward;

            currentRotation = Quaternion.LookRotation(forward, upward);
        }

        public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            var status = _statusUpdater!.Status;

            float prevStamina = status.StaminaLeft;
            
            // Update player physics and transform using updated current state
            _currentState.UpdateMain(ref currentVelocity, deltaTime, _playerActions!, status, Motor, this);

            // Broadcast current stamina if changed
            if (prevStamina != status.StaminaLeft)
            {
                EventManager.Instance.Broadcast<StaminaUpdateEvent>(new(status.StaminaLeft, _ability!.MaxStamina));
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

            if (_playerRender != null)
            {
                // Update client player data
                MCYaw2Send = Status!.CurrentVisualYaw - 90F; // Coordinate system conversion
                Pitch2Send = 0F;
            }

            if (_cameraController!.IsAiming)
            {
                AlignVisualYawToCamera();
            }

            // Reset root motion deltas
            RootMotionPositionDelta = Vector3.zero;
            RootMotionRotationDelta = Quaternion.identity;
        }

        // Used only by player renders, will be cleared and reassigned upon player render update
        private delegate void PlayerUpdateEventHandler(Vector3 velocity, float interval, PlayerStatus status);
        private event PlayerUpdateEventHandler? OnPlayerUpdate;

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
            string statusInfo;

            if (_statusUpdater!.Status.Spectating)
                statusInfo = string.Empty;
            else
                statusInfo = _statusUpdater!.Status.ToString();
            
            return $"ActionType:\t{CurrentActionType}\nState:\t{_currentState}\n{statusInfo}";
        }

        // Misc methods from Kinematic Character Controller

        public void PostGroundingUpdate(float deltaTime)
        {
            // Do nothing
        }

        public bool IsColliderValidForCollisions(Collider coll)
        {
            if (_statusUpdater == null)
            {
                return true;
            }
            return !_statusUpdater.Status.Spectating && !_currentState.IgnoreCollision();
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
