#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

using MinecraftClient.Event;
using MinecraftClient.Mapping;
using MinecraftClient.Rendering;

namespace MinecraftClient.Control
{
    [RequireComponent(typeof (Rigidbody), typeof (PlayerStatusUpdater))]
    public class PlayerController : MonoBehaviour
    {
        public enum WeaponState
        {
            Hold,
            Mount
        }

        [SerializeField] public PlayerMeleeAttack? meleeAttack;
        public PlayerMeleeAttack MeleeAttack => meleeAttack!;

        [SerializeField] protected PlayerAbility? ability;
        [SerializeField] protected GameObject? meleeWeaponPrefab;
        [SerializeField] public Transform? cameraRef;
        [SerializeField] public EntityRender? playerRender;
        [SerializeField] protected PhysicMaterial? physicMaterial;
        [HideInInspector] public bool UseRootMotion = false;

        public PlayerAbility Ability => ability!;
        protected CornClient? client;
        protected CameraController? cameraController;
        protected Rigidbody? playerRigidbody;
        public Rigidbody PlayerRigidbody => playerRigidbody!;
        protected Collider? playerCollider;
        public Collider PlayerCollider => playerCollider!;
        protected IPlayerState CurrentState = PlayerStates.IDLE;
        protected PlayerStatusUpdater? statusUpdater;
        public PlayerStatus? Status => statusUpdater?.Status;
        public Transform visualTransform => playerRender!.VisualTransform;

        private Action<GameModeUpdateEvent>? gameModeCallback;

        public virtual void Initialize(CornClient client, CameraController camController)
        {
            this.client = client;
            // Generate a dummy player render first, this will be replaced later
            var dummyRenderObj = new GameObject("Dummy Player Render");
            dummyRenderObj.transform.SetParent(transform, false);
            playerRender = dummyRenderObj.AddComponent<EntityRender>();
            // Use dummy object's own transform as dummy visual transform
            playerRender.VisualTransform = playerRender.transform;
            // Assign current camera controller
            this.cameraController = camController;
        }

        protected GameObject CreateAnimatorRenderFromModel(GameObject visualPrefab)
        {
            var visualObj = GameObject.Instantiate(visualPrefab);
            visualObj.name = "Visual";

            var renderObj = new GameObject($"Player {visualPrefab.name} Entity");
            var render = renderObj.AddComponent<PlayerEntityRiggedRender>();
            render.VisualTransform = visualObj.transform;

            var infoAnchorObj = new GameObject("Info Anchor");
            infoAnchorObj.transform.SetParent(renderObj.transform, false);
            infoAnchorObj.transform.localPosition = new(0F, 2F, 0F);
            render.InfoAnchor = infoAnchorObj.transform;

            visualObj.transform.SetParent(renderObj.transform, false);

            return renderObj;
        }

        protected void SetPlayerRender(Entity entity, GameObject renderPrefab)
        {
            // Clear existing event subscriptions
            OnLogicalUpdate = null;
            OnRandomizeMirroredFlag = null;
            OnWeaponStateChanged = null;
            OnCrossFadeState = null;

            GameObject renderObj;

            if (renderPrefab.GetComponent<Animator>() != null) // Model prefab, wrap it up
            {
                renderObj = CreateAnimatorRenderFromModel(renderPrefab);
                
            }
            else // Player render prefab, just instantiate
            {
                renderObj = GameObject.Instantiate(renderPrefab);
            }

            renderObj!.name = $"Player Entity ({renderPrefab.name})";
            
            // Update controller's player render
            playerRender = renderObj.GetComponent<EntityRender>();
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
                // Additionally, update player state machine for rigged renders
                OnLogicalUpdate += (interval, status, rigidbody) => riggedRender.UpdateStateMachine(status);
                // Create melee weapon
                riggedRender.CreateWeapon(meleeWeaponPrefab!);
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

        }

        public void UpdatePlayerRender(Entity entity, GameObject renderPrefab)
        {
            var prevRender = playerRender;

            // Initialize and assign new visual gameobject
            SetPlayerRender(entity, renderPrefab);

            if (prevRender != null)
            {
                visualTransform.rotation = prevRender.VisualTransform.rotation;
                // Dispose previous render gameobject
                Destroy(prevRender.gameObject);
            }
        }

        void Start()
        {
            playerRigidbody = GetComponent<Rigidbody>();
            statusUpdater = GetComponent<PlayerStatusUpdater>();

            var boxcast = ability!.ColliderType == PlayerAbility.PlayerColliderType.Box;

            if (boxcast)
            {
                // Attach box collider
                var box = gameObject.AddComponent<BoxCollider>();
                var sideLength = ability.ColliderRadius * 2F;
                box.size = new(sideLength, ability.ColliderHeight, sideLength);
                box.center = new(0F, ability.ColliderHeight / 2F, 0F);

                statusUpdater!.GroundBoxcastHalfSize = new(ability.ColliderRadius, 0.01F, ability.ColliderRadius);

                playerCollider = box;
            }
            else
            {
                // Attach capsule collider
                var capsule = gameObject.AddComponent<CapsuleCollider>();
                capsule.height = ability.ColliderHeight;
                capsule.radius = ability.ColliderRadius;
                capsule.center = new(0F, ability.ColliderHeight / 2F, 0F);

                statusUpdater!.GroundSpherecastRadius = ability.ColliderRadius;
                statusUpdater.GroundSpherecastCenter = new(0F, ability.ColliderRadius + 0.05F, 0F);

                playerCollider = capsule;
            }

            playerCollider.material = physicMaterial;
            statusUpdater.UseBoxCastForGroundedCheck = boxcast;

            // Set stamina to max value
            Status!.StaminaLeft = ability!.MaxStamina;
            // And broadcast current stamina
            EventManager.Instance.Broadcast<StaminaUpdateEvent>(new(Status.StaminaLeft, true));
            // Initialize health value
            EventManager.Instance.Broadcast<HealthUpdateEvent>(new(20F, true));

            // Disable entity on start
            DisableEntity();

            // Register gamemode events for updating gamemode
            gameModeCallback = (e) => SetGameMode(e.GameMode);
            EventManager.Instance.Register(gameModeCallback);
        }

        protected void SetGameMode(GameMode gameMode)
        {
            switch (gameMode)
            {
                case GameMode.Survival:
                case GameMode.Creative:
                case GameMode.Adventure:
                    Status!.Spectating = false;
                    CheckEntityEnabled();
                    break;
                case GameMode.Spectator:
                    Status!.Spectating = true;
                    CheckEntityEnabled();
                    break;
            }
        }

        void OnDestroy()
        {
            if (gameModeCallback is not null)
                EventManager.Instance.Unregister(gameModeCallback);
        }

        protected void DisableEntity()
        {
            // Update components state...
            playerCollider!.enabled = false;
            playerRigidbody!.useGravity = false;

            // Reset player status
            playerRigidbody!.velocity = Vector3.zero;
            Status!.Grounded  = false;
            Status.InLiquid  = false;
            Status.OnWall    = false;
            Status.Sprinting = false;

            Status.EntityDisabled = true;

            // Hide entity render
            HideRender();
        }

        protected void EnableEntity()
        {
            // Update components state...
            playerCollider!.enabled = true;
            playerRigidbody!.useGravity = true;

            Status!.EntityDisabled = false;

            // Show entity render
            ShowRender();
        }

        protected virtual void HideRender()
        {
            playerRender?.gameObject.SetActive(false);
        }

        protected virtual void ShowRender()
        {
            playerRender?.gameObject.SetActive(true);
        }
        
        public delegate void WeaponStateEventHandler(WeaponState weaponState);
        public event WeaponStateEventHandler? OnWeaponStateChanged;
        public void ChangeWeaponState(WeaponState weaponState) => OnWeaponStateChanged?.Invoke(weaponState);

        public delegate void CrossFadeStateEventHandler(string stateName, float time, int layer, float timeOffset);
        public event CrossFadeStateEventHandler? OnCrossFadeState;
        public void CrossFadeState(string stateName, float time = 0.2F, int layer = 0, float timeOffset = 0F)
        {
            OnCrossFadeState?.Invoke(stateName, time, layer, timeOffset);
        }

        public delegate void NoParamEventHandler();
        public event NoParamEventHandler? OnRandomizeMirroredFlag;
        public void RandomizeMirroredFlag() => OnRandomizeMirroredFlag?.Invoke();

        public void ToggleWalkMode()
        {
            Status!.WalkMode = !Status.WalkMode;
            CornApp.Notify(Status.WalkMode ? "Switched to walk mode" : "Switched to rush mode");
        }

        public void StartForceMoveOperation(string name, ForceMoveOperation[] ops)
        {
            CurrentState.OnExit(statusUpdater!.Status, playerRigidbody!, this);
            // Enter a new force move state
            CurrentState = new ForceMoveState(name, ops);
            CurrentState.OnEnter(statusUpdater!.Status, playerRigidbody!, this);
        }

        public bool TryStartAttack()
        {
            if (statusUpdater!.Status.AttackStatus.AttackCooldown <= 0F)
            {
                CurrentState.OnExit(statusUpdater!.Status, playerRigidbody!, this);
                // Enter melee attack state
                CurrentState = PlayerStates.MELEE;

                CurrentState.OnEnter(statusUpdater!.Status, playerRigidbody!, this);

                return true;
            }
            
            return false;
        }

        public void TurnToAttackTarget()
        {
            Vector3? targetPos = client?.GetAttackTarget();

            if (targetPos is null) // Failed to get attack target's position, do nothing
                return;

            var posOffset = targetPos.Value - transform.position;

            var attackYaw = GetYawFromVector2(new(posOffset.x, posOffset.z));

            statusUpdater!.Status.TargetVisualYaw = attackYaw;
            statusUpdater!.Status.CurrentVisualYaw = attackYaw;

            visualTransform.eulerAngles = new(0F, attackYaw, 0F);
        }

        public void AttackDamage(bool enable)
        {
            if (statusUpdater!.Status.Attacking)
            {
                statusUpdater!.Status.AttackStatus.CausingDamage = enable;
            }
            else
            {
                Debug.LogWarning("Trying to toggle attack damage when not attacking!");
            }
        }

        public void DealDamage(List<AttackHitInfo> hitInfos)
        {
            foreach (var hitInfo in hitInfos)
            {
                // Send attack packets to server
                var entityId = hitInfo.EntityRender?.Entity?.ID;

                if (entityId is not null)
                    client!.InteractEntity(entityId.Value, 1);

            }
        }

        public void ClearAttackCooldown()
        {
            if (statusUpdater!.Status.Attacking)
            {
                statusUpdater!.Status.AttackStatus.AttackCooldown = 0F;
            }
            else
            {
                Debug.LogWarning("Trying to clear attack cooldown when not attacking!");
            }
        }
 
        private void CheckEntityEnabled()
        {
            if (!client!.IsMovementReady()) // Movement is not ready, disable player entity
            {
                // Set player velocity to zero to stop it from floating around
                playerRigidbody!.velocity = Vector3.zero;

                if (!Status!.EntityDisabled) // If player entity is not disabled yet
                {
                    // Disable it
                    DisableEntity();
                    // Re-sync player position
                    transform.position = client.GetPosition();
                }
            }
            else // Movement is now ready
            {
                if (Status!.EntityDisabled && !Status.Spectating) // Player entity was previously disabled, and this player is not in spectator mode
                {
                    // Enable it back
                    EnableEntity();
                }

                if (!Status!.EntityDisabled && Status.Spectating) // Player entity was not disabled, but this player is in spectator mode
                {
                    // Disable entity
                    DisableEntity();
                }
            }
        }

        protected void PreLogicalUpdate(float interval)
        {
            // Check if entity should be enabled
            CheckEntityEnabled();

            // Get input data from client
            var inputData = client!.InputData;

            // Update player status (in water, grounded, etc)
            if (!Status!.EntityDisabled)
                statusUpdater!.UpdatePlayerStatus(client!.GetWorld(), visualTransform.forward);
            
            var status = statusUpdater!.Status;

            // Update current player state
            if (CurrentState.ShouldExit(inputData, status))
            {
                // Try to exit current state and enter another one
                foreach (var state in PlayerStates.STATES)
                {
                    if (state != CurrentState && state.ShouldEnter(inputData, status))
                    {
                        CurrentState.OnExit(status, playerRigidbody!, this);

                        // Exit previous state and enter this state
                        CurrentState = state;
                        
                        CurrentState.OnEnter(status, playerRigidbody!, this);
                        break;
                    }
                }
            }

            Status!.CurrentVisualYaw = visualTransform.eulerAngles.y;

            float prevStamina = status.StaminaLeft;

            // Prepare current and target player visual yaw before updating it
            if (inputData.horInputNormalized != Vector2.zero)
            {
                Status.UserInputYaw = GetYawFromVector2(inputData.horInputNormalized);
                Status.TargetVisualYaw = cameraController!.GetViewEularAngles()?.y + Status.UserInputYaw ?? Status.TargetVisualYaw;
            }
            
            // Update player physics and transform using updated current state
            CurrentState.UpdatePlayer(interval, inputData, status, playerRigidbody!, this);

            // Broadcast current stamina if changed
            if (prevStamina != status.StaminaLeft)
                EventManager.Instance.Broadcast<StaminaUpdateEvent>(new(status.StaminaLeft, status.StaminaLeft >= ability!.MaxStamina));

            // Apply updated visual yaw to visual transform
            visualTransform.eulerAngles = new(0F, Status.CurrentVisualYaw, 0F);
        }

        protected void PostLogicalUpdate()
        {
            // Tell server our current position
            Vector3 newPosition;

            if (CurrentState is ForceMoveState state)
            // Use move origin as the player location to tell to server, to
            // prevent sending invalid positions during a force move operation
                newPosition = state.GetFakePlayerOffset();
            else
                newPosition = transform.position;

            // Update client player data
            var newYaw = visualTransform.eulerAngles.y - 90F;
            var newPitch = 0F;
            
            client!.UpdatePlayerStatus(newPosition, newYaw, newPitch, Status!.Grounded);
        }

        // Used only by player renders, will be cleared and reassigned upon player render update
        private delegate void PlayerUpdateEventHandler(float interval, PlayerStatus status, Rigidbody rigidbody);
        private event PlayerUpdateEventHandler? OnLogicalUpdate;
        void Update()
        {
            PreLogicalUpdate(Time.unscaledDeltaTime);
            
            // Visual updates...
            OnLogicalUpdate?.Invoke(Time.unscaledDeltaTime, Status!, playerRigidbody!);

            PostLogicalUpdate();
        }

        void FixedUpdate()
        {
            var info = Status!;

            if (info.MoveVelocity != Vector3.zero)
            {
                // The player is actively moving
                playerRigidbody!.AddForce((info.MoveVelocity - playerRigidbody!.velocity) * Time.fixedUnscaledDeltaTime * 10F, ForceMode.VelocityChange);
            }
            else
            {
                if (info.Spectating)
                    playerRigidbody!.velocity = Vector3.zero;
                
                // Otherwise leave the player rigidbody untouched
            }
        }

        public void SetLocation(Location loc, bool resetVelocity = false, float yaw = 0F)
        {
            if (playerRigidbody is null) return;

            if (resetVelocity) // Reset rigidbody velocity
                playerRigidbody.velocity = Vector3.zero;
            
            playerRigidbody.position = CoordConvert.MC2Unity(loc);
            visualTransform.eulerAngles = new(0F, yaw, 0F);
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
            
            return $"State:\t{CurrentState}\n{statusInfo}";
        }

    }
}
