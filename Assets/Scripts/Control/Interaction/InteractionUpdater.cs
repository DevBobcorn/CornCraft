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
using CraftSharp.Protocol.Handlers;
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
        private const int MAX_INTERACTION_DISTANCE = 5;
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

        private readonly List<Vector3Int> rayCells = new();
        private static Ray ray;
        
        public Direction? TargetDirection { get; private set; }
        public BlockLoc? TargetBlockLoc { get; private set; }
        public Location? TargetExactLoc { get; private set; }
        
        private float instaBreakCooldown;
        private float placeBlockCooldown;

        private void UpdateBlockSelection(Ray? viewRay, float maxDistance)
        {
            if (viewRay is null || !client) return;
            
            ray = viewRay.Value;
            Raycaster.RaycastGridCells(viewRay.Value.origin, viewRay.Value.direction, maxDistance, rayCells);

            if (placeBlockCooldown >= 0F) // Ongoing block placement cooldown
            {
                // Don't update block selection block location
                
            }
            else if (client.ChunkRenderManager.RaycastBlocks(rayCells, ray, out var aabbInfo, out var blockInfo))
            {
                TargetDirection = aabbInfo.direction;
                TargetExactLoc = blockInfo.ExactLoc;

                // Create selection box if not present
                if (!blockSelectionBox)
                {
                    blockSelectionBox = Instantiate(blockSelectionFramePrefab)!.GetComponent<BlockSelectionBox>();
                    blockSelectionBox!.transform.SetParent(transform, false);
                }

                // Update target location if changed
                if (TargetBlockLoc != blockInfo.BlockLoc)
                {
                    TargetBlockLoc = blockInfo.BlockLoc;
                    blockSelectionBox.transform.position = blockInfo.CellPos;

                    EventManager.Instance.Broadcast(new TargetBlockLocUpdateEvent(blockInfo.BlockLoc));
                }

                // Update shape even if target location is not changed (the block itself may change)
                blockSelectionBox.UpdateShape(blockInfo.BlockState.Shape);
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
        }

        private static readonly ResourceLocation COCOA_ID = new("cocoa");

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

            // See https://bugs.mojang.com/browse/MC/issues/MC-193943
            // Bell & Cocoa: Don't invert
            // Other blocks: Invert if wall-attached
            bool invertFacing = true;
            
            if (propTable.ContainsKey("attachment")) // Used by bell
            {
                predicateProps["attachment"] = targetDirection switch
                {
                    Direction.Up => "floor",
                    Direction.Down => "ceiling",
                    _ => "single_wall" // 
                };
                invertFacing = false;
            }
            
            if (propTable.ContainsKey("face"))
            {
                predicateProps["face"] = targetDirection switch
                {
                    Direction.Up => "floor",
                    Direction.Down => "ceiling",
                    _ => "wall"
                };
                invertFacing = targetDirection is not Direction.Up and not Direction.Down; // Invert if wall-attached
            }

            if (propTable.TryGetValue("facing", out var possibleValues))
            {
                if (blockId == COCOA_ID) invertFacing = false;
                
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
                    predicateProps["facing"] = targetDirection switch
                    {
                        Direction.North => invertFacing ? "north" : "south",
                        Direction.East => invertFacing ? "east" : "west",
                        Direction.South => invertFacing ? "south" : "north",
                        Direction.West => invertFacing ? "west" : "east",
                        _ => cameraYawDir switch
                        {
                            Direction.North => invertFacing ? "south" : "north",
                            Direction.East => invertFacing ? "west" : "east",
                            Direction.South => invertFacing ? "north" : "south",
                            Direction.West => invertFacing ? "east" : "west",
                            _ => throw new InvalidDataException($"Undefined direction {targetDirection}!")
                        }
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

            // Doesn't seem to work very well
            //EventManager.Instance.Broadcast(new BlockPredictionEvent(newBlockLoc, predictedStateId));

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

            EventManager.Instance.Broadcast(new BlockPredictionEvent(targetBlockLoc, 0));

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

                playerController.Actions.Interaction.ChargedAttack.performed += _ =>
                {
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
                    var status = playerController.Status;

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
                            if (playerController.CurrentState != PlayerStates.MELEE)
                            {
                                // Specify attack data to use
                                status.AttackStatus.CurrentStagedAttack = playerController.AbilityConfig.MeleeSwordAttack_Staged;

                                // Update player state
                                playerController.ChangeToState(PlayerStates.MELEE);
                            }
                            else if (playerController.CurrentState is MeleeState melee &&
                                    playerController.Status.AttackStatus.AttackCooldown <= 0F)
                            {
                                melee.SetNextAttackFlag();
                            }
                        }
                    }
                };

                playerController.Actions.Interaction.UseChargedItem.performed += _ =>
                {
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
                    if (TargetBlockLoc is not null && TargetDirection is not null && TargetExactLoc is not null)
                    {
                        if (blockTriggerInteractionInfos.ContainsKey(TargetBlockLoc.Value)) // Check if target block is interactable
                        {
                            if (placeBlockCooldown < 0F)
                            {
                                placeBlockCooldown = PLACE_BLOCK_COOLDOWN;
                                var inBlockLoc = TargetExactLoc.Value - TargetBlockLoc.Value.ToLocation();

                                // Interact with target block
                                PlaceBlock(client, TargetBlockLoc.Value, (float) inBlockLoc.X, (float) inBlockLoc.Y, (float) inBlockLoc.Z, TargetDirection.Value);
                            }
                        }
                        else if (currentActionType == ItemActionType.Block) // Check if holding a block item
                        {
                            if (placeBlockCooldown < 0F)
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
                        client.UseItemOnMainHand();
                    }
                };
            
                playerController.Actions.Interaction.PickTargetItem.performed += _ =>
                {
                    //if (client.GetProtocolVersion() < ProtocolMinecraft.MC_1_21_4_Version)
                    if (TargetBlockLoc is not null)
                    {
                        var playerInventory = client.GetInventory(0);
                        var block = client.ChunkRenderManager.GetBlock(TargetBlockLoc.Value);
                        var itemToPick = ItemPalette.INSTANCE.GetItemForBlock(block.BlockId);
                        var itemStackToPick = new ItemStack(itemToPick, 1);
                        
                        if (playerInventory is not null && itemToPick != Item.UNKNOWN)
                        {
                            var matchedHotbarSlot = playerInventory.MatchItemStackInHotbar(itemStackToPick);
                            if (matchedHotbarSlot >= 0) // We already have this item in hotbar, just change our slot
                            {
                                client.ChangeHotbarSlot(matchedHotbarSlot);
                            }
                            else // Swap from backpack if we can find any, or get one if in Creative mode
                            {
                                var matchedBackpackSlot =
                                    playerInventory.MatchItemStackInHotbarAndBackpack(itemStackToPick);
                                
                                if (matchedBackpackSlot >= 0) // Swap from backpack
                                {
                                    client.PickItem(matchedBackpackSlot);
                                    //Debug.Log($"Slot to use for storing swaped item: {matchedBackpackSlot}, Swaped item: {itemToPick}");
                                }
                                else if (client.GameMode == GameMode.Creative) // Get one new, for free!
                                {
                                    var hotbarSlotToUse = playerInventory.GetSuitableSlotInHotbar(client.CurrentHotbarSlot);
                                    var slotToUse = playerInventory.GetFirstHotbarSlot() + hotbarSlotToUse;
                                    
                                    var emptyBackpackAndHotbarSlots = playerInventory.GetEmptySlots(9, 36);
                                    var oldItemStack = playerInventory.Items.GetValueOrDefault(slotToUse);
                                    if (oldItemStack is not null && emptyBackpackAndHotbarSlots.Length > 0) // Put old item stack to somewhere else
                                    {
                                        client.DoCreativeGive(emptyBackpackAndHotbarSlots[0], oldItemStack.ItemType, oldItemStack.Count, oldItemStack.NBT);
                                        //Debug.Log($"Slot to use for storing old item: {emptyBackpackAndHotbarSlots[0]}, Old item: {oldItemStack}");
                                    }
                                    
                                    client.DoCreativeGive(slotToUse, itemToPick, itemStackToPick.Count, itemStackToPick.NBT);
                                    client.ChangeHotbarSlot((short) hotbarSlotToUse);
                                    EventManager.Instance.Broadcast(new HeldItemUpdateEvent(hotbarSlotToUse, true, itemStackToPick, itemToPick.ActionType));
                                    
                                    //Debug.Log($"Slot to use for storing picked item: {hotbarSlotToUse}, Picked item: {itemToPick}");
                                }
                            }
                        }
                    }
                    // TODO: Implement for 1.21.4+
                };

                playerController.Actions.Interaction.ToggleAimingLock.performed += _ =>
                {
                    playerController.ToggleAimingLock();
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
                UpdateBlockSelection(cameraController.GetPointerRay(), MAX_INTERACTION_DISTANCE);

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