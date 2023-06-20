#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

using MinecraftClient.Event;
using MinecraftClient.Mapping;

namespace MinecraftClient.Control
{
    public abstract class PlayerController : MonoBehaviour
    {
        public enum WeaponState
        {
            Hold,
            Mount
        }

        [SerializeField] public PlayerMeleeAttack? meleeAttack;
        public PlayerMeleeAttack MeleeAttack => meleeAttack!;

        [SerializeField] protected PlayerAbility? ability;
        [SerializeField] public Transform? cameraRef;
        [SerializeField] public Transform? visualTransform;
        [SerializeField] public PhysicMaterial? physicMaterial;
        [HideInInspector] public bool UseRootMotion = false;

        public PlayerAbility Ability => ability!;
        protected CornClient? client;
        protected CameraController? cameraController;
        protected Rigidbody? playerRigidbody;
        public Rigidbody PlayerRigidbody => playerRigidbody!;
        protected Collider? playerCollider;
        public Collider PlayerCollider => playerCollider!;
        protected IPlayerState CurrentState = PlayerStates.IDLE;
        protected readonly PlayerUserInputData inputData = new();
        protected PlayerUserInput? userInput;
        protected PlayerStatusUpdater? statusUpdater;
        public PlayerStatus? Status => statusUpdater?.Status;

        private Action<GameModeUpdateEvent>? gameModeCallback;

        public virtual void Initialize(CornClient client, CameraController camController)
        {
            this.client = client;
            this.cameraController = camController;
        }

        void Start()
        {
            playerRigidbody = GetComponent<Rigidbody>();

            statusUpdater = GetComponent<PlayerStatusUpdater>();
            userInput = GetComponent<PlayerUserInput>();

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

        public virtual void SetClientEntity(Entity clientEntity) { }

        protected void SetGameMode(GameMode gameMode)
        {
            switch (gameMode)
            {
                case GameMode.Survival:
                case GameMode.Creative:
                case GameMode.Adventure:
                    Status!.Spectating = false;
                    CheckMovement();
                    break;
                case GameMode.Spectator:
                    Status!.Spectating = true;
                    CheckMovement();
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

            Status.GravityDisabled = true;
        }

        protected void EnableEntity()
        {
            // Update components state...
            playerCollider!.enabled = true;
            playerRigidbody!.useGravity = true;

            Status!.GravityDisabled = false;
        }
        
        public delegate void WeaponStateEventHandler(WeaponState weaponState);
        public event WeaponStateEventHandler? OnWeaponStateChanged;
        public void ChangeWeaponState(WeaponState weaponState) => OnWeaponStateChanged?.Invoke(weaponState);

        public virtual void CrossFadeState(string stateName, float time = 0.2F, int layer = 0, float timeOffset = 0F) { }
        public virtual void RandomizeMirroredFlag() { }

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

            visualTransform!.eulerAngles = new(0F, attackYaw, 0F);
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
 
        private void CheckMovement()
        {
            if (!client!.IsMovementReady()) // Movement is not ready, disable player entity
            {
                if (!Status!.GravityDisabled) // If player entity is not disabled yet
                {
                    // Disable it
                    DisableEntity();
                    // Re-sync player position
                    transform.position = client.GetPosition();
                }
            }
            else // Movement is now ready
            {
                if (Status!.GravityDisabled && !Status.Spectating) // Player entity was previously disabled, and this player is not in spectator mode
                {
                    // Enable it back
                    EnableEntity();
                }

                if (!Status!.GravityDisabled && Status.Spectating) // Player entity was not disabled, but this player is in spectator mode
                {
                    // Disable entity
                    DisableEntity();
                }
            }
        }

        protected void PreLogicalUpdate(float interval)
        {
            // Check if movement is available
            CheckMovement();

            // Update user input
            userInput!.UpdateInputs(inputData, client!.Perspective);

            // Update player status (in water, grounded, etc)
            if (!Status!.Spectating)
                statusUpdater!.UpdatePlayerStatus(client!.GetWorld(), visualTransform!.forward);
            
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

            Status!.CurrentVisualYaw = visualTransform!.eulerAngles.y;

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
            visualTransform!.eulerAngles = new(0F, Status.CurrentVisualYaw, 0F);
        }

        protected void PostLogicalUpdate()
        {
            // Tell server our current position
            Vector3 newPosition;

            if (CurrentState is ForceMoveState)
            // Use move origin as the player location to tell to server, to
            // prevent sending invalid positions during a force move operation
                newPosition = ((ForceMoveState) CurrentState).GetFakePlayerOffset();
            else
                newPosition = transform.position;

            // Update client player data
            var newYaw = visualTransform!.eulerAngles.y - 90F;
            var newPitch = 0F;
            
            client!.UpdatePlayerStatus(newPosition, newYaw, newPitch, Status!.Grounded);
        }

        protected virtual void LogicalUpdate(float interval)
        {
            PreLogicalUpdate(interval);
            
            // Visual updates...

            PostLogicalUpdate();
        }

        protected virtual void PhysicalUpdate(float interval)
        {
            var info = Status!;

            if (info.MoveVelocity != Vector3.zero)
            {
                // The player is actively moving
                playerRigidbody!.AddForce((info.MoveVelocity - playerRigidbody!.velocity) * interval * 10F, ForceMode.VelocityChange);
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
            visualTransform!.eulerAngles = new(0F, yaw, 0F);
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
            string targetBlockInfo = string.Empty;

            var velocity = playerRigidbody?.velocity;
            // Visually swap xz velocity to fit vanilla
            var veloInfo = $"Vel:\t{velocity?.z:0.00}\t{velocity?.y:0.00}\t{velocity?.x:0.00}\n({velocity?.magnitude:0.000})";

            string statusInfo;

            if (statusUpdater?.Status.Spectating ?? true)
                statusInfo = string.Empty;
            else
                statusInfo = statusUpdater!.Status.ToString();
            
            return $"State:\t{CurrentState}\n{veloInfo}\n{statusInfo}";
        }

    }
}
