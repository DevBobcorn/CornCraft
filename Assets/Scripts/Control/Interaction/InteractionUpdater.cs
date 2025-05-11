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
using CraftSharp.Resource;

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
        private const int MAX_TARGET_DISTANCE = 8;
        private const int BLOCK_INTERACTION_RADIUS = 2;
        private const float BLOCK_INTERACTION_RADIUS_SQR = BLOCK_INTERACTION_RADIUS * BLOCK_INTERACTION_RADIUS; // BLOCK_INTERACTION_RADIUS ^ 2
        private const float BLOCK_INTERACTION_RADIUS_SQR_PLUS = (BLOCK_INTERACTION_RADIUS + 0.5F) * (BLOCK_INTERACTION_RADIUS + 0.5F); // (BLOCK_INTERACTION_RADIUS + 0.5f) ^ 2

        private const float CREATIVE_INSTA_BREAK_COOLDOWN = 0.3F;
        private const float MINIMUM_INSTA_BREAK_COOLDOWN = 0.05F;

        private const float PLACE_BLOCK_COOLDOWN = 0.2F;
        
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

        private Action<HeldItemUpdateEvent>? heldItemChangeCallback;
        private Action<TriggerInteractionExecutionEvent>? triggerInteractionExecutionEvent;
        private Action<HarvestInteractionUpdateEvent>? harvestInteractionUpdateCallback;

        private readonly Dictionary<int, InteractionInfo> interactionInfos = new();
        private readonly Dictionary<BlockLoc, List<BlockTriggerInteractionInfo>> blockTriggerInteractionInfos = new();

        private readonly InteractionId interactionId = new();

        private LocalHarvestInteractionInfo? lastHarvestInteractionInfo;
        private ItemStack? currentItemStack;
        private ItemActionType currentActionType = ItemActionType.None;

        public Direction? TargetDirection { get; private set; }
        public BlockLoc? TargetBlockLoc { get; private set; }
        public Location? TargetExactLoc { get; private set; }
        
        private float instaBreakCooldown;
        private float placeBlockCooldown;

        private void UpdateBlockSelection(Ray? viewRay)
        {
            if (viewRay is null || !client) return;

            if (placeBlockCooldown >= 0F) // Ongoing block placement cooldown
            {
                // Don't update block selection block location
                
            }
            else if (Physics.Raycast(viewRay.Value.origin, viewRay.Value.direction, out RaycastHit viewHit, MAX_TARGET_DISTANCE, blockSelectionLayer, QueryTriggerInteraction.Collide))
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

                TargetExactLoc = CoordConvert.Unity2MC(client.WorldOriginOffset, viewHit.point);
                var newBlockLoc = CoordConvert.Unity2MC(client.WorldOriginOffset, unityBlockPos).GetBlockLoc();
                var block = client.ChunkRenderManager.GetBlock(newBlockLoc);

                // Create selection box if not present
                if (!blockSelectionBox)
                {
                    blockSelectionBox = Instantiate(blockSelectionFramePrefab)!.GetComponent<BlockSelectionBox>();
                    blockSelectionBox!.transform.SetParent(transform, false);
                }

                // Update target location if changed
                if (TargetBlockLoc != newBlockLoc)
                {
                    TargetBlockLoc = newBlockLoc;
                    blockSelectionBox.transform.position = unityBlockPos;

                    EventManager.Instance.Broadcast(new TargetBlockLocUpdateEvent(newBlockLoc));
                }

                // Update shape even if target location is not changed (the block itself may change)
                blockSelectionBox.UpdateShape(block.State.Shape);
            }
            else
            {
                // Update target location if changed
                if (TargetBlockLoc is not null)
                {
                    TargetBlockLoc = null;
                    TargetExactLoc = null;

                    EventManager.Instance.Broadcast(new TargetBlockLocUpdateEvent(null));
                }

                // Clear shape if selection box is created
                if (blockSelectionBox)
                {
                    blockSelectionBox.ClearShape();
                }
            }

            return;

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

        private void UpdateBlockPlacementPrediction(BlockLoc newBlockLoc, ResourceLocation blockId, Direction targetDirection, float cameraYaw, float cameraPitch, bool clickedTopHalf)
        {
            if (!client) return;

            while (cameraYaw >= 360F) cameraYaw -= 360F;
            while (cameraYaw < 0F) cameraYaw += 360F;
            var cameraYawDir = cameraYaw switch
            {
                > 315 or <= 45 => Direction.East,
                > 45 and <= 135 => Direction.South,
                > 135 and <= 225 => Direction.West,
                > 225 and <= 315 => Direction.North,
                _ => throw new InvalidDataException($"Invalid yaw angle: {cameraYaw}!")
            };

            // Create selection box if not present
            if (!blockSelectionBox)
            {
                blockSelectionBox = Instantiate(blockSelectionFramePrefab)!.GetComponent<BlockSelectionBox>();
                blockSelectionBox!.transform.SetParent(transform, false);
            }

            var palette = BlockStatePalette.INSTANCE;
            var propTable = palette.GetBlockProperties(blockId);
            var predicateProps = palette.GetDefault(blockId).Properties
                // Make a copy of default property dictionary
                .ToDictionary(entry => entry.Key, entry => entry.Value);

            if (propTable.TryGetValue("facing", out var possibleValues))
            {
                if (possibleValues.Contains("up") && cameraPitch <= -44)
                {
                    predicateProps["facing"] = "up";
                }
                else if (possibleValues.Contains("down") && cameraPitch >= 44)
                {
                    predicateProps["facing"] = "down";
                }
                else
                {
                    predicateProps["facing"] = cameraYawDir switch
                    {
                        Direction.North => "north",
                        Direction.East => "east",
                        Direction.South => "south",
                        Direction.West => "west",
                        _ => throw new InvalidDataException($"Undefined direction {targetDirection}!")
                    };
                }
            }

            if (propTable.TryGetValue("half", out possibleValues))
            {
                if (possibleValues.Contains("lower"))
                {
                    predicateProps["half"] = "lower";
                }
                else if (possibleValues.Contains("bottom"))
                {
                    predicateProps["half"] = clickedTopHalf ? "top" : "bottom";
                }
            }

            if (propTable.TryGetValue("axis", out possibleValues))
            {
                predicateProps["axis"] = targetDirection switch
                {
                    Direction.North => "z",
                    Direction.East => "x",
                    Direction.South => "z",
                    Direction.West => "x",
                    Direction.Up => possibleValues.Contains("y") ? "y" : (cameraYawDir == Direction.South || cameraYawDir == Direction.North ? "z" : "x"),
                    Direction.Down => possibleValues.Contains("y") ? "y" : (cameraYawDir == Direction.South || cameraYawDir == Direction.North ? "z" : "x"),
                    _ => throw new InvalidDataException($"Undefined direction {targetDirection}!")
                };
            }

            var (predictedStateId, predictedBlockState) = palette.GetBlockStateWithProperties(blockId, predicateProps);
            //Debug.Log($"Predicted block state: {predictedBlockState}");

            //EventManager.Instance.Broadcast(new BlockPredictionEvent(newBlockLoc, (ushort) predictedStateId));

            TargetBlockLoc = newBlockLoc;
            blockSelectionBox.transform.position = CoordConvert.MC2Unity(client.WorldOriginOffset, newBlockLoc.ToLocation());
            blockSelectionBox.UpdateShape(predictedBlockState.Shape);

            EventManager.Instance.Broadcast(new TargetBlockLocUpdateEvent(newBlockLoc));
        }

        private void UpdateBlockInteractions(ChunkRenderManager chunksManager)
        {
            var playerBlockLoc = client!.GetCurrentLocation().GetBlockLoc();
            var table = InteractionManager.INSTANCE.InteractionTable;

            if (!client) return;

            foreach (var blockLoc in blockTriggerInteractionInfos.Keys.ToList()
                         .Where(blockLoc => playerBlockLoc.SqrDistanceTo(blockLoc) > BLOCK_INTERACTION_RADIUS_SQR_PLUS)
                         .Where(blockLoc => blockLoc != TargetBlockLoc))
            {
                RemoveBlockTriggerInteractionsAt(blockLoc, info =>
                {
                    EventManager.Instance.Broadcast<InteractionRemoveEvent>(new(info.Id));
                });
                //Debug.Log($"Rem: [{blockLoc}]");
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
            return blockTriggerInteractionInfos.GetValueOrDefault(blockLoc);
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

        private void AbortDiggingBlockIfPresent()
        {
            if (lastHarvestInteractionInfo is not null)
            {
                lastHarvestInteractionInfo.CancelInteraction();
                EventManager.Instance.Broadcast<InteractionRemoveEvent>(new(lastHarvestInteractionInfo.Id));
                
                lastHarvestInteractionInfo = null;

                if (blockSelectionBox)
                {
                    blockSelectionBox.ClearBreakMesh();
                }
            }
        }

        private static void InstaBreak(BaseCornClient client, BlockLoc targetBlockLoc, Direction targetDirection)
        {
            var block = client.ChunkRenderManager.GetBlock(targetBlockLoc);
            var blockColor = client.ChunkRenderManager.GetBlockColor(block.StateId, targetBlockLoc);

            // Send digging packets
            Task.Run(() => {
                client.DigBlock(targetBlockLoc, targetDirection, DiggingStatus.Started);
                client.DigBlock(targetBlockLoc, targetDirection, DiggingStatus.Finished);
            });

            //EventManager.Instance.Broadcast(new BlockPredictionEvent(targetBlockLoc, 0));

            EventManager.Instance.Broadcast(new ParticlesEvent(CoordConvert.MC2Unity(client.WorldOriginOffset, targetBlockLoc.ToCenterLocation()),
                ParticleTypePalette.INSTANCE.GetNumIdById(BLOCK_PARTICLE_ID), new BlockParticleExtraDataWithColor(block.StateId, blockColor), 16));
        }

        private static BlockLoc PlaceBlock(BaseCornClient client, BlockLoc targetBlockLoc, float inBlockX, float inBlockY, float inBlockZ, Direction targetDirection)
        {
            var placeBlockLoc = GetPlaceBlockLoc(targetBlockLoc, targetDirection);

            client.PlaceBlock(targetBlockLoc, targetDirection, inBlockX, inBlockY, inBlockZ, Inventory.Hand.MainHand);

            return placeBlockLoc;
        }

        public void SetControllers(BaseCornClient curClient, CameraController curCameraController, PlayerController curPlayerController)
        {
            client = curClient;
            cameraController = curCameraController;

            if (playerController != curPlayerController)
            {
                playerController = curPlayerController;

                curPlayerController.Actions.Interaction.ChargedAttack.performed += _ =>
                {
                    var status = curPlayerController.Status;

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
                            status.AttackStatus.CurrentChargedAttack = curPlayerController.AbilityConfig.RangedBowAttack_Charged;

                            // Update player state
                            curPlayerController.ChangeToState(PlayerStates.RANGED_AIM);
                        }
                    }
                    else // Check digging block
                    {
                        if (TargetBlockLoc is not null && TargetDirection is not null &&
                            curClient.GameMode != GameMode.Creative)
                        {
                            curPlayerController.ChangeToState(PlayerStates.DIGGING_AIM);
                        }
                    }
                };

                curPlayerController.Actions.Interaction.NormalAttack.performed += _ =>
                {
                    var status = curPlayerController.Status;

                    if (TargetBlockLoc is not null && TargetDirection is not null && instaBreakCooldown <= 0F)
                    {
                        var block = client.ChunkRenderManager.GetBlock(TargetBlockLoc.Value);

                        // Check if we can initiate insta-break (if creative mode or break time is short enough)
                        StartDiggingProcess(block, TargetBlockLoc.Value, TargetDirection.Value, status, true);
                    }
                    else if (currentActionType == ItemActionType.Sword)
                    {
                        if (status.AttackStatus.AttackCooldown <= 0F)
                        {
                            if (curPlayerController.CurrentState != PlayerStates.MELEE)
                            {
                                // Specify attack data to use
                                status.AttackStatus.CurrentStagedAttack = curPlayerController.AbilityConfig.MeleeSwordAttack_Staged;

                                // Update player state
                                curPlayerController.ChangeToState(PlayerStates.MELEE);
                            }
                            else if (curPlayerController.CurrentState is MeleeState melee &&
                                    curPlayerController.Status.AttackStatus.AttackCooldown <= 0F)
                            {
                                melee.SetNextAttackFlag();
                            }
                        }
                    }
                };

                curPlayerController.Actions.Interaction.UseChargedItem.performed += _ =>
                {
                    var status = curPlayerController.Status;

                    if (currentActionType == ItemActionType.Bow)
                    {
                        if (status.AttackStatus.AttackCooldown <= 0F)
                        {
                            // Specify attack data to use
                            status.AttackStatus.CurrentChargedAttack = curPlayerController.AbilityConfig.RangedBowAttack_Charged;

                            // Update player state
                            curPlayerController.ChangeToState(PlayerStates.RANGED_AIM);
                        }
                    }
                };

                curPlayerController.Actions.Interaction.UseNormalItem.performed += _ =>
                {
                    if (TargetBlockLoc is not null && TargetDirection is not null && TargetExactLoc is not null)
                    {
                        if (blockTriggerInteractionInfos.ContainsKey(TargetBlockLoc.Value)) // Check if target block is interactable
                        {
                            if (placeBlockCooldown < 0F)
                            {
                                placeBlockCooldown = PLACE_BLOCK_COOLDOWN;
                                var inBlockLoc = TargetExactLoc.Value - TargetBlockLoc.Value.ToLocation();

                                // Interact with target block
                                PlaceBlock(curClient, TargetBlockLoc.Value, (float) inBlockLoc.X, (float) inBlockLoc.Y, (float) inBlockLoc.Z, TargetDirection.Value);
                            }
                        }
                        else if (currentActionType == ItemActionType.Block) // Check if holding a block item
                        {
                            if (placeBlockCooldown < 0F)
                            {
                                var cameraYaw = curClient.GetCameraYaw();
                                var cameraPitch = curClient.GetCameraPitch();

                                var inBlockLoc = TargetExactLoc.Value - TargetBlockLoc.Value.ToLocation();
                                var placeLoc = PlaceBlock(curClient, TargetBlockLoc.Value, (float) inBlockLoc.X, (float) inBlockLoc.Y, (float) inBlockLoc.Z, TargetDirection.Value);

                                inBlockLoc = TargetExactLoc.Value - placeLoc.ToLocation();
                                var clickedTopHalf = inBlockLoc.Y >= 0.5;

                                placeBlockCooldown = PLACE_BLOCK_COOLDOWN;
                                UpdateBlockPlacementPrediction(placeLoc, currentItemStack!.ItemType.ItemBlock!.Value, TargetDirection.Value, cameraYaw, cameraPitch, clickedTopHalf);
                            }
                        }
                    }
                    else
                    {
                        curClient.UseItemOnMainHand();
                    }
                };
            
                curPlayerController.Actions.Interaction.PickTargetItem.performed += _ =>
                {
                    // TODO: Implement
                };
            }
        }

        private bool AffectsAttackBehaviour(ItemActionType itemType)
        {
            return itemType switch
            {
                ItemActionType.Bow => true,
                ItemActionType.Crossbow => true,
                ItemActionType.Trident => true,
                ItemActionType.Shield => true,

                ItemActionType.Shears => true,
                ItemActionType.Axe => true,
                ItemActionType.Pickaxe => true,
                ItemActionType.Sword => true,
                ItemActionType.Shovel => true,
                ItemActionType.Hoe => true,
                ItemActionType.Brush => true,

                _ => false
            };
        }

        private void Start()
        {
            heldItemChangeCallback = e =>
            {
                if (playerController)
                {
                    if (e.HotbarSlotChanged || AffectsAttackBehaviour(e.ActionType) || AffectsAttackBehaviour(currentActionType))
                    {
                        // Exit attack state when active item action type is changed
                        if (currentActionType != e.ActionType)
                        {
                            playerController.Status!.Attacking = false;
                        }
                    }
                    
                    playerController.ChangeCurrentItem(e.ItemStack, e.ActionType);
                }

                currentItemStack = e.ItemStack;
                currentActionType = e.ActionType;
            };
            EventManager.Instance.Register(heldItemChangeCallback);

            harvestInteractionUpdateCallback = e =>
            {
                var harvestInteractionInfo = GetInteraction<HarvestInteractionInfo>(e.InteractionId);
                if (harvestInteractionInfo is not null)
                {
                    // Update progress bar
                    harvestInteractionInfo.Progress = e.Progress;

                    // Update box selection
                    if (blockSelectionBox)
                    {
                        if (e.Status == DiggingStatus.Finished || e.Progress >= 1F) // Digging complete
                        {
                            blockSelectionBox.ClearBreakMesh();
                        }
                        else
                        {
                            blockSelectionBox.UpdateBreakStage(Mathf.Clamp((int) (e.Progress * 10), 0, 9));
                        }
                    }
                }
            };
            EventManager.Instance.Register(harvestInteractionUpdateCallback);

            triggerInteractionExecutionEvent = e =>
            {
                var triggerInteractionInfo = GetInteraction<InteractionInfo>(e.InteractionId);

                if (triggerInteractionInfo != null && client)
                {
                    triggerInteractionInfo.UpdateInteraction(client);
                }
            };
            EventManager.Instance.Register(triggerInteractionExecutionEvent);
        }

        private void StartDiggingProcess(Block block, BlockLoc blockLoc, Direction direction, PlayerStatus status, bool instaBreakOnly = false)
        {
            var definition = InteractionManager.INSTANCE.InteractionTable
                .GetValueOrDefault(block.StateId)?.Get<HarvestInteraction>();
            var blockState = block.State;

            if (blockState.BlockId == BlockState.AIR_ID)
            {
                return; // Possibly the other part of a double block
            }
            
            if (definition is null)
            {
                Debug.LogWarning($"Harvest interaction for {blockState} is not registered.");
                return;
            }

            if (!client) return;
            
            // Abort previous digging interaction
            AbortDiggingBlockIfPresent();

            if (client.GameMode == GameMode.Creative)
            {
                InstaBreak(client, blockLoc, direction);
                instaBreakCooldown = CREATIVE_INSTA_BREAK_COOLDOWN;

                if (blockSelectionBox)
                {
                    blockSelectionBox.ClearBreakMesh();
                }

                return;
            }

            var newHarvestInteractionInfo = new LocalHarvestInteractionInfo(interactionId.AllocateID(), block, blockLoc, direction,
                currentItemStack, blockState.Hardness, status.Floating, status.Grounded, definition);

            // If this duration is too short, use insta break instead
            if (newHarvestInteractionInfo.Duration <= CREATIVE_INSTA_BREAK_COOLDOWN)
            {
                InstaBreak(client, blockLoc, direction);
                instaBreakCooldown = Mathf.Max(MINIMUM_INSTA_BREAK_COOLDOWN, newHarvestInteractionInfo.Duration);

                if (blockSelectionBox)
                {
                    blockSelectionBox.ClearBreakMesh();
                }
            }
            else // Use regular digging, with a progress bar
            {
                if (instaBreakOnly)
                {
                    //Debug.Log($"Duration is {newHarvestInteractionInfo.Duration}, can't start insta-break! Target: {blockState}, Hardness: {blockState.Hardness}");
                    return; // Duration is too long for insta-break, can't do it
                }

                if (blockSelectionBox)
                {
                    var offsetType = ResourcePackManager.Instance.StateModelTable[block.StateId].OffsetType;
                    var posOffset = ChunkRenderBuilder.GetBlockOffsetInBlock(offsetType,
                            blockLoc.X >> 4, blockLoc.Z >> 4, blockLoc.X & 0xF, blockLoc.Z & 0xF);

                    blockSelectionBox.UpdateBreakMesh(blockState, posOffset, 0b111111, 0);
                }

                lastHarvestInteractionInfo = newHarvestInteractionInfo;

                AddInteraction(lastHarvestInteractionInfo.Id, lastHarvestInteractionInfo, info =>
                {
                    EventManager.Instance.Broadcast<InteractionAddEvent>(new(info.Id, true, true, info));
                });
            }
        }

        private void Update()
        {
            instaBreakCooldown -= Time.deltaTime;
            placeBlockCooldown -= Time.deltaTime;

            if (cameraController && cameraController.IsAimingOrLocked)
            {
                UpdateBlockSelection(cameraController.GetPointerRay());

                if (TargetBlockLoc is not null && TargetDirection is not null && TargetExactLoc is not null &&
                    playerController && client)
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
                        // Check continuous insta-break
                        if (playerController.Actions.Interaction.NormalAttack.IsPressed() && lastHarvestInteractionInfo is null)
                        {
                            if (instaBreakCooldown <= 0F) // Cooldown for insta-break
                            {
                                // Check if we can initiate insta-break (if creative mode or break time is short enough)
                                StartDiggingProcess(block, TargetBlockLoc.Value, TargetDirection.Value, status, true);
                            }
                        }
                        else
                        {
                            // Not in digging state, abort
                            AbortDiggingBlockIfPresent();
                        }
                    }
                    
                    // Check continuous block placement
                    if (currentActionType == ItemActionType.Block &&
                        playerController.Actions.Interaction.UseNormalItem.IsPressed() &&
                        !blockTriggerInteractionInfos.ContainsKey(TargetBlockLoc.Value))
                    {
                        if (placeBlockCooldown <= 0F) // Cooldown for placing block
                        {
                            var cameraYaw = client.GetCameraYaw();
                            var cameraPitch = client.GetCameraPitch();

                            var inBlockLoc = TargetExactLoc.Value - TargetBlockLoc.Value.ToLocation();
                            var placeLoc = PlaceBlock(client, TargetBlockLoc.Value, (float) inBlockLoc.X, (float) inBlockLoc.Y, (float) inBlockLoc.Z, TargetDirection.Value);

                            inBlockLoc = TargetExactLoc.Value - placeLoc.ToLocation();
                            var clickedTopHalf = inBlockLoc.Y >= 0.5;

                            placeBlockCooldown = PLACE_BLOCK_COOLDOWN;
                            UpdateBlockPlacementPrediction(placeLoc, currentItemStack!.ItemType.ItemBlock!.Value, TargetDirection.Value, cameraYaw, cameraPitch, clickedTopHalf);
                        }
                    }
                }
                else
                {
                    // Target is gone, abort digging
                    AbortDiggingBlockIfPresent();
                }
            }
            else // Not aiming, clear digging status
            {
                AbortDiggingBlockIfPresent();
                
                TargetBlockLoc = null;

                if (blockSelectionBox)
                {
                    blockSelectionBox.ClearShape();
                }
            }
        }

        private void LateUpdate()
        {
            if (client)
            {
                // Update block interactions
                UpdateBlockInteractions(client.ChunkRenderManager);
            }
        }

        private void OnDestroy()
        {
            if (heldItemChangeCallback is not null)
                EventManager.Instance.Unregister(heldItemChangeCallback);
            
            if (triggerInteractionExecutionEvent is not null)
                EventManager.Instance.Unregister(triggerInteractionExecutionEvent);
            
            if (harvestInteractionUpdateCallback is not null)
                EventManager.Instance.Unregister(harvestInteractionUpdateCallback);
        }
    }
}