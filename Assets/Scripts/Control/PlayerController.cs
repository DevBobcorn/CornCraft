#nullable enable
using System;
using UnityEngine;
using MinecraftClient.Event;
using MinecraftClient.Mapping;
using MinecraftClient.Rendering;

namespace MinecraftClient.Control
{
    [RequireComponent(typeof (Rigidbody), typeof (EntityRender))]
    [RequireComponent(typeof (PlayerStatusUpdater), typeof (PlayerUserInput))]
    public class PlayerController : MonoBehaviour, IPlayerController
    {
        [SerializeField] public PlayerAbility? playerAbility;
        [SerializeField] public Transform? cameraRef;

        private CornClient? game;
        private Rigidbody? playerRigidbody;
        private Collider? playerCollider;

        private bool entityDisabled = false;

        private readonly PlayerUserInputData inputData = new();
        private PlayerUserInput? userInput;
        private PlayerStatusUpdater? statusUpdater;
        public PlayerStatus Status { get => statusUpdater!.Status; }

        private IPlayerState CurrentState = PlayerStates.IDLE;

        private CameraController? camControl;
        private Transform? visualTransform;
        private EntityRender? playerRender;
        /* DISABLE START
        private PlayerAnimatorController? animatorController;
        // DISABLE END */
        private Entity fakeEntity = new(0, EntityType.Player, new());

        public void DisableEntity()
        {
            entityDisabled = true;
            // Update components state...
            playerCollider!.enabled = false;
            playerRigidbody!.velocity = Vector3.zero;
            playerRigidbody!.useGravity = false;

            Status.Spectating = true;
        }

        public void EnableEntity()
        {
            // Update and control...
            entityDisabled = false;
            // Update components state...
            playerCollider!.enabled = true;
            playerRigidbody!.useGravity = true;

            Status.Spectating = false;
        }

        public void ToggleWalkMode()
        {
            Status.WalkMode = !Status.WalkMode;
            CornClient.ShowNotification(Status.WalkMode ? "Switched to walk mode" : "Switched to rush mode");
        }

        private void CheckEntityEnabled()
        {
            if (game!.PlayerData.GameMode == GameMode.Spectator) // Spectating
                DisableEntity();
            else
                EnableEntity();
        }

        public void SetEntityId(int entityId)
        {
            fakeEntity.ID = entityId;
            // Reassign this entity to refresh
            playerRender!.Entity = fakeEntity;
        }

        public void SetLocation(Location loc)
        {
            // Immediately end force move operation
            Status.ForceMoveDestination = null;
            Status.ForceMoveOrigin = null;

            transform.position = CoordConvert.MC2Unity(loc);
            Debug.Log($"Position set to {transform.position}");

            CheckEntityEnabled();
        }

        public Location GetLocation() => CoordConvert.Unity2MC(transform.position);

        void Update() => ManagedUpdate(Time.deltaTime);

        public void ManagedUpdate(float interval)
        {
            // Update user input
            userInput!.UpdateInputs(inputData, game!.PlayerData.Perspective);

            // Update target block selection
            statusUpdater!.UpdateBlockSelection(camControl!.GetViewportCenterRay());

            // Update player status (in water, grounded, etc)
            statusUpdater.UpdatePlayerStatus(game!.GetWorld(), visualTransform!.forward);

            var status = statusUpdater.Status;

            // Update current player state
            if (CurrentState.ShouldExit(status))
            {
                // Try to exit current state and enter another one
                foreach (var state in PlayerStates.STATES)
                {
                    if (state != CurrentState && state.ShouldEnter(status))
                    {
                        // Exit previous state and enter this state
                        CurrentState = state;
                        break;
                    }
                }
            }

            Status.CurrentVisualYaw = visualTransform!.eulerAngles.y;

            float prevStamina = status.StaminaLeft;

            if (status.ForceMoveDestination is not null && status.ForceMoveOrigin is not null)
            {
                status.ForceMoveTimeCurrent = Mathf.Max(status.ForceMoveTimeCurrent - interval, 0F);
                var moveProgress = status.ForceMoveTimeCurrent / status.ForceMoveTimeTotal;
                var curPosition = Vector3.Lerp(status.ForceMoveDestination.Value, status.ForceMoveOrigin.Value, moveProgress);

                // Force grounded while doing the move
                status.Grounded = true;
                // Update player yaw
                status.CurrentVisualYaw = Mathf.LerpAngle(status.CurrentVisualYaw, status.TargetVisualYaw, playerAbility!.SteerSpeed * interval);

                //playerRigidbody!.MovePosition(curPosition);
                transform.position = curPosition;

                if (status.ForceMoveTimeCurrent == 0F) // End force move operation
                {
                    status.ForceMoveDestination = null;
                    status.ForceMoveOrigin = null;
                }
            }
            else
            {
                // Prepare current and target player visual yaw before updating it
                Status.UserInputYaw = AngleConvert.GetYawFromVector2(inputData.horInputNormalized);
                Status.TargetVisualYaw = camControl!.GetYaw() + Status.UserInputYaw;
                
                // Update player physics and transform using updated current state
                CurrentState.UpdatePlayer(interval, inputData, status, playerAbility!, playerRigidbody!);
            }

            // Broadcast current stamina if changed
            if (prevStamina != status.StaminaLeft)
                EventManager.Instance.Broadcast<StaminaUpdateEvent>(new(status.StaminaLeft, status.StaminaLeft >= playerAbility!.MaxStamina));

            // Update player animator
            /* DISABLE START
            animatorController!.UpdateStateMachine(status, playerRigidbody!.velocity);
            // DISABLE END */

            // Apply updated visual yaw to visual transform
            visualTransform!.eulerAngles = new(0F, Status.CurrentVisualYaw, 0F);

            // Apply current horizontal velocity to visual render
            var horizontalVelocity = playerRigidbody!.velocity;
            horizontalVelocity.y = 0; // TODO Check and improve
            playerRender!.SetCurrentVelocity(horizontalVelocity);

            // Tell server our current position
            Location rawLocation;
            
            if (status.ForceMoveDestination is null) // TODO Check and Improve
                rawLocation = CoordConvert.Unity2MC(transform.position);
            else // Use move destination as the player location to tell to server
            // This is done to prevent sending invalid positions during a force move operation
                rawLocation = CoordConvert.Unity2MC(status.ForceMoveDestination.Value);

            // Preprocess the location before sending it (to avoid position correction from server)
            if ((status.Grounded || status.CenterDownDist < 0.5F) && rawLocation.Y - (int)rawLocation.Y > 0.9D)
                rawLocation.Y = (int)rawLocation.Y + 1;

            CornClient.Instance.SyncLocation(rawLocation, visualTransform!.eulerAngles.y - 90F, 0F);

            // Update render
            var cameraPos = camControl.GetPosition();

            if (cameraPos is not null)
                playerRender!.UpdateInfoPlate(cameraPos.Value);
            
            playerRender!.UpdateAnimation(game!.GetTickMilSec());

        }

        private Action<PerspectiveUpdateEvent>? perspectiveCallback;
        private Action<GameModeUpdateEvent>? gameModeCallback;

        void Start()
        {
            camControl = GameObject.FindObjectOfType<CameraController>();
            game = CornClient.Instance;
            
            // Initialize player visuals
            visualTransform = transform.Find("Visual");
            playerRender    = GetComponent<EntityRender>();

            /* DISABLE START
            animatorController = GetComponentInChildren<PlayerAnimatorController>();
            // DISABLE END */

            fakeEntity.Name = game!.GetUsername();
            fakeEntity.ID   = 0;
            playerRender.Entity = fakeEntity;

            playerRigidbody   = GetComponent<Rigidbody>();

            statusUpdater = GetComponent<PlayerStatusUpdater>();
            userInput = GetComponent<PlayerUserInput>();

            perspectiveCallback = (e) => { };

            gameModeCallback = (e) => {
                if (e.GameMode != GameMode.Spectator && game!.LocationReceived)
                    EnableEntity();
                else
                    DisableEntity();
            };

            EventManager.Instance.Register(perspectiveCallback);
            EventManager.Instance.Register(gameModeCallback);

            if (playerAbility is null)
                Debug.LogError("Player ability not assigned!");
            else
            {
                var boxcast = playerAbility.ColliderType == PlayerAbility.PlayerColliderType.Box;

                if (boxcast)
                {
                    // Attach box collider
                    var box = gameObject.AddComponent<BoxCollider>();
                    var sideLength = playerAbility.ColliderRadius * 2F;
                    box.size = new(sideLength, playerAbility.ColliderHeight, sideLength);
                    box.center = new(0F, playerAbility.ColliderHeight / 2F, 0F);

                    statusUpdater.GroundBoxcastHalfSize = new(playerAbility.ColliderRadius, 0.01F, playerAbility.ColliderRadius);

                    playerCollider = box;
                }
                else
                {
                    // Attach capsule collider
                    var capsule = gameObject.AddComponent<CapsuleCollider>();
                    capsule.height = playerAbility.ColliderHeight;
                    capsule.radius = playerAbility.ColliderRadius;
                    capsule.center = new(0F, playerAbility.ColliderHeight / 2F, 0F);

                    statusUpdater.GroundSpherecastRadius = playerAbility.ColliderRadius;
                    statusUpdater.GroundSpherecastCenter = new(0F, playerAbility.ColliderRadius + 0.05F, 0F);

                    playerCollider = capsule;
                }

                statusUpdater.UseBoxCastForGroundedCheck = boxcast;
            }

        }

        void OnDestroy()
        {
            if (perspectiveCallback is not null)
                EventManager.Instance.Unregister(perspectiveCallback);

            if (gameModeCallback is not null)
                EventManager.Instance.Unregister(gameModeCallback);
        }

        public string GetDebugInfo()
        {
            string targetBlockInfo = string.Empty;
            var loc = CoordConvert.Unity2MC(transform.position);
            var world = game!.GetWorld();

            var status = statusUpdater!.Status;

            if (status.TargetBlockPos is not null)
            {
                var targetBlockState = world?.GetBlock(status.TargetBlockPos.Value).State;
                if (targetBlockState is not null)
                    targetBlockInfo = targetBlockState.ToString();
            }

            var velocity = playerRigidbody!.velocity;
            // Visually swap xz velocity to fit vanilla
            var veloInfo = $"Vel:\t{velocity.z:0.00}\t{velocity.y:0.00}\t{velocity.x:0.00}\n({velocity.magnitude:0.000})";

            if (entityDisabled)
                return $"Position:\t{loc}\nState:\t{CurrentState}\n{veloInfo}\nTarget Block:\t{status.TargetBlockPos}\n{targetBlockInfo}\nBiome:\n[{world?.GetBiomeId(loc)}] {world?.GetBiome(loc).GetDescription()}";
            else
                return $"Position:\t{loc}\nState:\t{CurrentState}\n{veloInfo}\n{status.ToString()}\nTarget Block:\t{status.TargetBlockPos}\n{targetBlockInfo}\nBiome:\n[{world?.GetBiomeId(loc)}] {world?.GetBiome(loc).GetDescription()}";

        }

    }
}
