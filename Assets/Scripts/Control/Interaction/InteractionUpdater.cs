#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using CraftSharp.Event;
using CraftSharp.Rendering;
using System.IO;
using System.Threading.Tasks;

namespace CraftSharp.Control
{
    public class InteractionId
    {
        private readonly BitArray usage = new(int.MaxValue);
        private int currentId = 0;

        public int AllocateID()
        {
            while (currentId < usage.Length)
            {
                if (!usage[currentId])
                {
                    usage[currentId] = true;
                    return currentId++;
                }

                currentId++;
            }

            return -1;
        }

        public void ReleaseID(int id)
        {
            if (id >= 0 && id < usage.Length)
                usage[id] = false;
        }
    }

    public class InteractionUpdater : MonoBehaviour
    {
        public static readonly ResourceLocation BLOCK_PARTICLE_ID = new("block");
        public const int MAX_TARGET_DISTANCE = 8;
        public const int BLOCK_INTERACTION_RADIUS = 3;
        public const float BLOCK_INTERACTION_RADIUS_SQR = 9.0F; // BLOCK_INTERACTION_RADIUS ^ 2
        public const float BLOCK_INTERACTION_RADIUS_SQR_PLUS = 12.25F; // (BLOCK_INTERACTION_RADIUS + 0.5f) ^ 2

        public const float INSTA_BREAK_COOLDOWN = -0.3F;
        public const float PLACE_BLOCK_COOLDOWN = -0.2F;
        
        private static readonly List<BlockLoc> validOffsets = ComputeOffsets();

        private static List<BlockLoc> ComputeOffsets()
        {
            var offsets = new List<BlockLoc>();
            for (int x = -BLOCK_INTERACTION_RADIUS; x <= BLOCK_INTERACTION_RADIUS; x++)
                for (int y = -BLOCK_INTERACTION_RADIUS; y <= BLOCK_INTERACTION_RADIUS; y++)
                    for (int z = -BLOCK_INTERACTION_RADIUS; z <= BLOCK_INTERACTION_RADIUS; z++)
                        if (x * x + y * y + z * z <= BLOCK_INTERACTION_RADIUS_SQR)
                            offsets.Add(new BlockLoc(x, y, z));
            return offsets;
        }

        [SerializeField] private LayerMask blockSelectionLayer;
        [SerializeField] private GameObject? blockSelectionFramePrefab;

        private BlockSelectionBox? blockSelectionBox;

        private BaseCornClient? client;
        private CameraController? cameraController;
        private PlayerController? playerController;

        private Action<HeldItemChangeEvent>? heldItemCallback;
        private Action<TriggerInteractionExecutionEvent>? triggerInteractionExecutionEvent;
        private Action<HarvestInteractionUpdateEvent>? harvestInteractionUpdateCallback;

        private readonly Dictionary<int, InteractionInfo> interactionInfos = new();
        private readonly Dictionary<BlockLoc, List<BlockTriggerInteractionInfo>> blockTriggerInteractionInfos = new();

        private readonly InteractionId interactionId = new();

        private LocalHarvestInteractionInfo? lastHarvestInteractionInfo;
        private Item? currentItem;

        public Direction? TargetDirection { get; private set; } = Direction.Down;
        public BlockLoc? TargetBlockLoc { get; private set; } = null;
        
        private float instaBreakCooldown = 0F;
        private float placeBlockCooldown = 0F;

        private void UpdateBlockSelection(Ray? viewRay)
        {
            if (viewRay is null || client == null) return;

            if (Physics.Raycast(viewRay.Value.origin, viewRay.Value.direction, out RaycastHit viewHit, MAX_TARGET_DISTANCE, blockSelectionLayer))
            {
                Vector3 normal = viewHit.normal.normalized;
                TargetDirection = GetDirectionFromNormal(normal);

                Vector3 offseted = PointOnCubeSurface(viewHit.point)
                    ? viewHit.point - normal * 0.5f
                    : viewHit.point;

                Vector3 unityBlockPos = new(
                    Mathf.FloorToInt(offseted.x),
                    Mathf.FloorToInt(offseted.y),
                    Mathf.FloorToInt(offseted.z)
                );

                var newTargetLoc = CoordConvert.Unity2MC(client.WorldOriginOffset, unityBlockPos).GetBlockLoc();
                var block = client.ChunkRenderManager.GetBlock(newTargetLoc);

                // Create selection box if not present
                if (blockSelectionBox == null)
                {
                    blockSelectionBox = Instantiate(blockSelectionFramePrefab)!.GetComponent<BlockSelectionBox>();
                    blockSelectionBox!.transform.SetParent(transform, false);
                }

                // Update target location if changed
                if (TargetBlockLoc != newTargetLoc)
                {
                    TargetBlockLoc = newTargetLoc;
                    blockSelectionBox.transform.position = unityBlockPos;

                    EventManager.Instance.Broadcast(new TargetBlockLocChangeEvent(newTargetLoc));
                }

                // Update shape even if target location is not changed (the block itself may change)
                blockSelectionBox.UpdateShape(block.State.Shape);
            }
            else
            {
                // Update target location if changed
                if (TargetBlockLoc != null)
                {
                    TargetBlockLoc = null;

                    EventManager.Instance.Broadcast(new TargetBlockLocChangeEvent(null));
                }

                // Clear shape if selection box is created
                if (blockSelectionBox != null)
                {
                    blockSelectionBox.ClearShape();
                }
            }
            
            static Direction GetDirectionFromNormal(Vector3 normal)
            {
                float absX = Mathf.Abs(normal.x);
                float absY = Mathf.Abs(normal.y);
                float absZ = Mathf.Abs(normal.z);

                if (absX >= absY && absX >= absZ)
                    return normal.x > 0 ? Direction.South : Direction.North;
                if (absY >= absX && absY >= absZ)
                    return normal.y > 0 ? Direction.Up : Direction.Down;

                return normal.z > 0 ? Direction.East : Direction.West;
            }

            static bool PointOnCubeSurface(Vector3 point)
            {
                Vector3 delta = new(
                    point.x - Mathf.Floor(point.x),
                    point.y - Mathf.Floor(point.y),
                    point.z - Mathf.Floor(point.z)
                );

                return delta.x is < 0.01f or > 0.99f ||
                       delta.y is < 0.01f or > 0.99f ||
                       delta.z is < 0.01f or > 0.99f;
            }
        }

        private void UpdateBlockInteractions(ChunkRenderManager chunksManager)
        {
            var playerBlockLoc = client!.GetCurrentLocation().GetBlockLoc();
            var table = InteractionManager.INSTANCE.InteractionTable;

            if (client == null) return;

            foreach (var blockLoc in blockTriggerInteractionInfos.Keys.ToList()) // ToList because collection may change
            {
                // Remove trigger interactions which are too far from player
                if (playerBlockLoc.SqrDistanceTo(blockLoc) > BLOCK_INTERACTION_RADIUS_SQR_PLUS)
                {
                    if (blockLoc != TargetBlockLoc) // Make an exception for target location
                    {
                        RemoveBlockTriggerInteractionsAt(blockLoc, info =>
                        {
                            EventManager.Instance.Broadcast<InteractionRemoveEvent>(new(info.Id));
                        });
                        //Debug.Log($"Rem: [{blockLoc}]");
                    }
                }
            }

            // Update harvest interactions (these are not bound by interaction radius)
            if (lastHarvestInteractionInfo != null)
            {
                if (lastHarvestInteractionInfo.Location != TargetBlockLoc || !lastHarvestInteractionInfo.UpdateInteraction(client))
                {
                    RemoveInteraction<HarvestInteractionInfo>(lastHarvestInteractionInfo.Id, info =>
                    {
                        EventManager.Instance.Broadcast<InteractionRemoveEvent>(new(info.Id));
                    });

                    lastHarvestInteractionInfo = null;
                }
            }

            var availableBlockLocs = validOffsets.Select(offset => offset + playerBlockLoc);
            if (TargetBlockLoc != null)
            {
                availableBlockLocs = availableBlockLocs.Append(TargetBlockLoc.Value);
            }

            // Append new available trigger interactions
            foreach (var blockLoc in availableBlockLocs)
            {
                var block = chunksManager.GetBlock(blockLoc);

                if (table.TryGetValue(block.StateId, out InteractionDefinition? newInteractionDefinition))
                {
                    var newTriggerInteraction = newInteractionDefinition?.Get<TriggerInteraction>();
                    if (newTriggerInteraction is null) continue;

                    var prevInfo = GetBlockTriggerInteractionsAt(blockLoc)?.FirstOrDefault();
                    var newInfo = new BlockTriggerInteractionInfo(interactionId.AllocateID(), block, blockLoc, block.BlockId, newTriggerInteraction);

                    if (prevInfo is not null)
                    {
                        var prevDefinition = prevInfo.Definition;
                        if (prevDefinition != newTriggerInteraction) // Update this interaction
                        {
                            RemoveBlockTriggerInteractionsAt(blockLoc, info =>
                            {
                                EventManager.Instance.Broadcast<InteractionRemoveEvent>(new(info.Id));
                            });
                            AddBlockTriggerInteractionAt(blockLoc, newInfo, info =>
                            {
                                // Select this new item if it is at target location
                                EventManager.Instance.Broadcast<InteractionAddEvent>(new(info.Id, TargetBlockLoc == blockLoc, false, info));
                            });
                            //Debug.Log($"Upd: [{blockLoc}] {prevDefinition.Identifier} => {newDefinition.Identifier}");
                        }
                        // Otherwise leave it unchanged
                    }
                    else // Add this interaction
                    {
                        AddBlockTriggerInteractionAt(blockLoc, newInfo, info =>
                        {
                            // Select this new item if it is at target location
                            EventManager.Instance.Broadcast<InteractionAddEvent>(new(info.Id, TargetBlockLoc == blockLoc, false, info));
                        });
                        //Debug.Log($"Add: [{blockLoc}] {newDefinition.Identifier}");
                    }
                }
                else
                {
                    if (blockTriggerInteractionInfos.ContainsKey(blockLoc))
                    {
                        RemoveBlockTriggerInteractionsAt(blockLoc, info =>
                        {
                            EventManager.Instance.Broadcast<InteractionRemoveEvent>(new(info.Id));
                        });
                        //Debug.Log($"Rem: [{blockLoc}]");
                    }
                }
            }
        }

        private void AddInteraction<T>(int id, T info, Action<T>? onCreated = null) where T : InteractionInfo
        {
            interactionInfos.Add(id, info);

            onCreated?.Invoke(info);
        }

        private void AddBlockTriggerInteractionAt(BlockLoc location, BlockTriggerInteractionInfo info, Action<BlockTriggerInteractionInfo>? onCreated = null)
        {
            if (!blockTriggerInteractionInfos.TryGetValue(location, out var infosAtLoc))
            {
                infosAtLoc = new List<BlockTriggerInteractionInfo>();
                blockTriggerInteractionInfos[location] = infosAtLoc;
            }

            infosAtLoc.Add(info);

            AddInteraction(info.Id, info, onCreated);
        }

        private T? GetInteraction<T>(int id) where T : InteractionInfo
        {
            return interactionInfos.TryGetValue(id, out var interactionInfo) ? (T) interactionInfo : null;
        }

        private IEnumerable<BlockTriggerInteractionInfo>? GetBlockTriggerInteractionsAt(BlockLoc blockLoc)
        {
            return blockTriggerInteractionInfos.TryGetValue(blockLoc, out var infos) ? infos : null;
        }

        private void RemoveInteraction<T>(int id, Action<T>? onRemoved = null) where T : InteractionInfo
        {
            if (interactionInfos.Remove(id, out var removedInfo))
            {
                interactionId.ReleaseID(id);
                onRemoved?.Invoke((T) removedInfo);
            }
        }

        private void RemoveBlockTriggerInteractionsAt(BlockLoc blockLoc, Action<BlockTriggerInteractionInfo>? onEachRemoved = null)
        {
            if (blockTriggerInteractionInfos.TryGetValue(blockLoc, out var infosAtLoc))
            {
                infosAtLoc.RemoveAll(interactionInfo =>
                {
                    RemoveInteraction(interactionInfo.Id, onEachRemoved);

                    return true;
                });

                // Remove this entry
                blockTriggerInteractionInfos.Remove(blockLoc);
            }
        }

        private static BlockLoc GetPlaceBlockLoc(BlockLoc targetLoc, Direction targetDir)
        {
            return targetDir switch
            {
                Direction.Down  => targetLoc.Down(),
                Direction.Up    => targetLoc.Up(),
                Direction.South => targetLoc.South(),
                Direction.North => targetLoc.North(),
                Direction.East  => targetLoc.East(),
                Direction.West  => targetLoc.West(),
                _               => throw new InvalidDataException($"Invalid direction {targetDir}"),
            };
        }

        private void AbortDiggingBlock()
        {
            if (lastHarvestInteractionInfo is not null)
            {
                lastHarvestInteractionInfo.CancelInteraction();
                EventManager.Instance.Broadcast<InteractionRemoveEvent>(new(lastHarvestInteractionInfo.Id));
                
                lastHarvestInteractionInfo = null;
            }
        }

        private static void CreativeInstaBreak(BaseCornClient client, BlockLoc targetBlockLoc, Direction targetDirection)
        {
            var block = client.ChunkRenderManager.GetBlock(targetBlockLoc);

            EventManager.Instance.Broadcast(new ParticlesEvent(CoordConvert.MC2Unity(client.WorldOriginOffset, targetBlockLoc.ToCenterLocation()),
                    ParticleTypePalette.INSTANCE.GetNumIdById(BLOCK_PARTICLE_ID), new BlockParticleExtraData(block.StateId), 16));
            
            // Send digging packets
            Task.Run(() => {
                client.DigBlock(targetBlockLoc, targetDirection, DiggingStatus.Started);
                client.DigBlock(targetBlockLoc, targetDirection, DiggingStatus.Finished);
            });
        }

        private static void PlaceBlock(BaseCornClient client, BlockLoc targetBlockLoc, Direction targetDirection)
        {
            var placeBlockLoc = GetPlaceBlockLoc(targetBlockLoc, targetDirection);

            client.PlaceBlock(placeBlockLoc, targetDirection, Inventory.Hand.MainHand);
        }

        public void Initialize(BaseCornClient client, CameraController camController, PlayerController playerController)
        {
            this.client = client;
            this.cameraController = camController;
            this.playerController = playerController;

            playerController.Actions.Interaction.ChargedAttack.performed += _ =>
            {
                var currentActionType = playerController.CurrentActionType;
                var status = playerController.Status;

                if (currentActionType == ItemActionType.Sword)
                {
                    if (status.AttackStatus.AttackCooldown <= 0F)
                    {
                        // TODO: Implement
                    }
                }
                else if (currentActionType == ItemActionType.Bow)
                {
                    if (status.AttackStatus.AttackCooldown <= 0F)
                    {
                        // Specify attack data to use
                        status.AttackStatus.CurrentChargedAttack = playerController.AbilityConfig.RangedBowAttack_Charged;

                        // Update player state
                        playerController.ChangeToState(PlayerStates.RANGED_AIM);
                    }
                }
                else // Check digging block
                {
                    if (TargetBlockLoc is not null && TargetDirection is not null &&
                        client.GameMode != GameMode.Creative)
                    {
                        playerController.ChangeToState(PlayerStates.DIGGING_AIM);
                    }
                }
            };

            playerController.Actions.Interaction.NormalAttack.performed += _ =>
            {
                var currentActionType = playerController.CurrentActionType;
                var status = playerController.Status;

                if (TargetBlockLoc is not null && TargetDirection is not null &&
                    client.GameMode == GameMode.Creative)
                {
                    if (instaBreakCooldown < INSTA_BREAK_COOLDOWN)
                    {
                        CreativeInstaBreak(client, TargetBlockLoc.Value, TargetDirection.Value);

                        instaBreakCooldown = 0F;
                    }
                }
                else if (currentActionType == ItemActionType.Sword)
                {
                    if (status.AttackStatus.AttackCooldown <= 0F)
                    {
                        // Specify attack data to use
                        status.AttackStatus.CurrentStagedAttack = playerController.AbilityConfig.MeleeSwordAttack_Staged;

                        // Update player state
                        playerController.ChangeToState(PlayerStates.MELEE);
                    }
                }
            };

            playerController.Actions.Interaction.UseChargedItem.performed += _ =>
            {
                var currentActionType = playerController.CurrentActionType;
                var status = playerController.Status;

                if (currentActionType == ItemActionType.Bow)
                {
                    if (status.AttackStatus.AttackCooldown <= 0F)
                    {
                        // Specify attack data to use
                        status.AttackStatus.CurrentChargedAttack = playerController.AbilityConfig.RangedBowAttack_Charged;

                        // Update player state
                        playerController.ChangeToState(PlayerStates.RANGED_AIM);
                    }
                }
            };

            playerController.Actions.Interaction.UseNormalItem.performed += _ =>
            {
                var currentActionType = playerController.CurrentActionType;
                var status = playerController.Status;

                if (playerController.CurrentActionType == ItemActionType.Block)
                {
                    if (TargetBlockLoc is not null && TargetDirection is not null)
                    {
                        if (placeBlockCooldown < PLACE_BLOCK_COOLDOWN)
                        {
                            PlaceBlock(client, TargetBlockLoc.Value, TargetDirection.Value);

                            placeBlockCooldown = 0F;
                        }
                    }
                }
                else
                {
                    client.UseItemOnMainHand();
                }
            };
        }

        void Start()
        {
            heldItemCallback = e => currentItem = e.ItemStack?.ItemType;
            EventManager.Instance.Register(heldItemCallback);

            harvestInteractionUpdateCallback = e =>
            {
                var harvestInteractionInfo = GetInteraction<HarvestInteractionInfo>(e.InteractionId);
                if (harvestInteractionInfo is not null)
                {
                    // Update the process
                    harvestInteractionInfo.Progress = e.Progress;
                }
            };
            EventManager.Instance.Register(harvestInteractionUpdateCallback);

            triggerInteractionExecutionEvent = e =>
            {
                var triggerInteractionInfo = GetInteraction<InteractionInfo>(e.InteractionId);

                if (triggerInteractionInfo != null && client != null)
                {
                    triggerInteractionInfo.UpdateInteraction(client);
                }
            };
            EventManager.Instance.Register(triggerInteractionExecutionEvent);
        }

        private void StartDiggingProcess(Block block, BlockLoc blockLoc, Direction direction, PlayerStatus status)
        {
            var definition = InteractionManager.INSTANCE.InteractionTable
                .GetValueOrDefault(block.StateId)?
                .Get<HarvestInteraction>();
            
            if (definition is null)
            {
                Debug.LogWarning($"Harvest interaction for {block.State} is not registered.");
                return;
            }
            
            // Abort previous digging interaction
            AbortDiggingBlock();

            lastHarvestInteractionInfo = new LocalHarvestInteractionInfo(interactionId.AllocateID(), block, blockLoc, direction,
                currentItem, block.State.Hardness, status.Floating, status.Grounded, definition);
            
            //Debug.Log($"Created {lastHarvestInteractionInfo.GetHashCode()} at {blockLoc}");

            AddInteraction(lastHarvestInteractionInfo.Id, lastHarvestInteractionInfo, info =>
            {
                EventManager.Instance.Broadcast<InteractionAddEvent>(new(info.Id, true, true, info));
            });
        }

        void Update()
        {
            instaBreakCooldown -= Time.deltaTime;
            placeBlockCooldown -= Time.deltaTime;

            if (cameraController != null && cameraController.IsAimingOrLocked)
            {
                UpdateBlockSelection(cameraController.GetPointerRay());

                if (TargetBlockLoc is not null && TargetDirection is not null &&
                    playerController != null && client != null)
                {
                    var block = client.ChunkRenderManager.GetBlock(TargetBlockLoc.Value);
                    var status = playerController.Status;

                    if (playerController.CurrentState is DiggingAimState) // Digging right now (without Creative Mode insta-break)
                    {
                        if (lastHarvestInteractionInfo is not null)
                        {
                            // Remove if digging interaction is completed
                            if (lastHarvestInteractionInfo.State == HarvestInteractionState.Completed)
                            {
                                lastHarvestInteractionInfo = null;
                            }
                        }
                        else if (!block.State.NoSolidMesh) // Start regular digging process
                        {
                            StartDiggingProcess(block, TargetBlockLoc.Value, TargetDirection.Value, status);
                        }
                    }
                    else
                    {
                        // Not in digging state, abort
                        AbortDiggingBlock();
                    }
                    
                    // Check continuous insta-break
                    if (client.GameMode == GameMode.Creative &&
                             playerController.Actions.Interaction.NormalAttack.IsPressed())
                    {
                        if (instaBreakCooldown <= INSTA_BREAK_COOLDOWN) // Cooldown for Creative Mode insta-break
                        {
                            CreativeInstaBreak(client, TargetBlockLoc.Value, TargetDirection.Value);

                            instaBreakCooldown = 0F;
                        }
                    }
                    
                    // Check continuous block placement
                    if (playerController.CurrentActionType == ItemActionType.Block &&
                             playerController.Actions.Interaction.UseNormalItem.IsPressed())
                    {
                        if (placeBlockCooldown <= PLACE_BLOCK_COOLDOWN) // Cooldown for placing block
                        {
                            PlaceBlock(client, TargetBlockLoc.Value, TargetDirection.Value);

                            placeBlockCooldown = 0F;
                        }
                    }
                }
                else
                {
                    // Target is gone, abort digging
                    AbortDiggingBlock();
                }
            }
            else // Not aiming, clear digging status
            {
                AbortDiggingBlock();
                
                TargetBlockLoc = null;

                if (blockSelectionBox != null)
                {
                    blockSelectionBox.ClearShape();
                }
            }
        }

        private void LateUpdate()
        {
            if (client != null)
            {
                // Update block interactions
                UpdateBlockInteractions(client.ChunkRenderManager);
            }
        }

        private void OnDestroy()
        {
            if (heldItemCallback is not null)
                EventManager.Instance.Unregister(heldItemCallback);
        }
    }
}