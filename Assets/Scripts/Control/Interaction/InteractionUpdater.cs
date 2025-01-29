#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using CraftSharp.Event;
using CraftSharp.Rendering;

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
        public const int MAX_TARGET_DISTANCE = 8;
        public const int BLOCK_INTERACTION_RADIUS = 3;
        public const float BLOCK_INTERACTION_RADIUS_SQR = 9.0f; // BLOCK_INTERACTION_RADIUS ^ 2
        public const float BLOCK_INTERACTION_RADIUS_SQR_PLUS = 12.25f; // (BLOCK_INTERACTION_RADIUS + 0.5f) ^ 2
        
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
        private Action<HarvestInteractionUpdateEvent>? harvestInteractionUpdateCallback;

        private readonly Dictionary<int, InteractionInfo> interactionInfos = new();
        private readonly Dictionary<BlockLoc, List<BlockViewInteractionInfo>> blockViewInteractionInfos = new();

        private readonly InteractionId interactionId = new();

        private LocalHarvestInteractionInfo? lastHarvestInteractionInfo;
        private Item? currentItem;

        public Direction? TargetDirection { get; private set; } = Direction.Down;
        public BlockLoc? TargetBlockLoc { get; private set; } = null;

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
                    return normal.x > 0 ? Direction.East : Direction.West;
                if (absY >= absX && absY >= absZ)
                    return normal.y > 0 ? Direction.Up : Direction.Down;

                return normal.z > 0 ? Direction.North : Direction.South;
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

            foreach (var blockLoc in blockViewInteractionInfos.Keys.ToList()) // ToList because collection may change
            {
                // Remove view interactions which are too far from player
                if (playerBlockLoc.SqrDistanceTo(blockLoc) > BLOCK_INTERACTION_RADIUS_SQR_PLUS)
                {
                    if (blockLoc != TargetBlockLoc) // Make an exception for target location
                    {
                        RemoveBlockViewInteractionsAt(blockLoc, info =>
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
                    RemoveInteraction(lastHarvestInteractionInfo.Id, info =>
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

            // Append new available view interactions
            foreach (var blockLoc in availableBlockLocs)
            {
                var block = chunksManager.GetBlock(blockLoc);

                if (table.TryGetValue(block.StateId, out InteractionDefinition? newInteractionDefinition))
                {
                    var newViewInteraction = newInteractionDefinition?.Get<ViewInteraction>();
                    if (newViewInteraction is null) continue;

                    var prevInfo = GetBlockViewInteractionsAt(blockLoc)?.FirstOrDefault();
                    var newInfo = new BlockViewInteractionInfo(interactionId.AllocateID(), block, blockLoc, block.BlockId, newViewInteraction);

                    if (prevInfo is not null)
                    {
                        var prevDefinition = prevInfo.Definition;
                        if (prevDefinition != newViewInteraction) // Update this interaction
                        {
                            RemoveBlockViewInteractionsAt(blockLoc, info =>
                            {
                                EventManager.Instance.Broadcast<InteractionRemoveEvent>(new(info.Id));
                            });
                            AddBlockViewInteractionAt(blockLoc, newInfo, info =>
                            {
                                EventManager.Instance.Broadcast<InteractionAddEvent>(new(info.Id, false, false, info));
                            });
                            //Debug.Log($"Upd: [{blockLoc}] {prevDefinition.Identifier} => {newDefinition.Identifier}");
                        }
                        // Otherwise leave it unchanged
                    }
                    else // Add this interaction
                    {
                        AddBlockViewInteractionAt(blockLoc, newInfo, info =>
                        {
                            EventManager.Instance.Broadcast<InteractionAddEvent>(new(info.Id, false, false, info));
                        });
                        //Debug.Log($"Add: [{blockLoc}] {newDefinition.Identifier}");
                    }
                }
                else
                {
                    if (blockViewInteractionInfos.ContainsKey(blockLoc))
                    {
                        RemoveBlockViewInteractionsAt(blockLoc, info =>
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

        private void AddBlockViewInteractionAt(BlockLoc location, BlockViewInteractionInfo info, Action<BlockViewInteractionInfo>? onCreated = null)
        {
            if (!blockViewInteractionInfos.TryGetValue(location, out var infos))
            {
                infos = new List<BlockViewInteractionInfo>();
                blockViewInteractionInfos[location] = infos;
            }

            interactionInfos.Add(info.Id, info);
            infos.Add(info);

            onCreated?.Invoke(info);
        }

        private T? GetInteraction<T>(int id) where T : InteractionInfo
        {
            return interactionInfos.TryGetValue(id, out var interactionInfo) ? (T) interactionInfo : null;
        }

        private IEnumerable<BlockViewInteractionInfo>? GetBlockViewInteractionsAt(BlockLoc location)
        {
            return blockViewInteractionInfos.TryGetValue(location, out var infos) ? infos : null;
        }

        private void RemoveInteraction(int id, Action<InteractionInfo>? onRemoved = null)
        {
            if (interactionInfos.Remove(id, out var removedInfo))
            {
                interactionId.ReleaseID(removedInfo.Id);
                onRemoved?.Invoke(removedInfo);
            }
        }

        private void RemoveBlockViewInteractionsAt(BlockLoc location, Action<BlockViewInteractionInfo>? onEachRemoved = null)
        {
            if (blockViewInteractionInfos.TryGetValue(location, out var infos))
            {
                infos.RemoveAll(interactionInfo =>
                {
                    if (interactionInfo is not BlockViewInteractionInfo matchedInfo) return false;

                    interactionInfos.Remove(matchedInfo.Id);

                    interactionId.ReleaseID(matchedInfo.Id);
                    onEachRemoved?.Invoke(matchedInfo);

                    return true;
                });

                if (!infos.Any()) blockViewInteractionInfos.Remove(location);
            }
        }

        public void Initialize(BaseCornClient client, CameraController camController, PlayerController playerController)
        {
            this.client = client;
            this.cameraController = camController;
            this.playerController = playerController;
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
            
            if (lastHarvestInteractionInfo is not null)
            {
                lastHarvestInteractionInfo.CancelInteraction();
                EventManager.Instance.Broadcast<InteractionRemoveEvent>(new(lastHarvestInteractionInfo.Id));
            }

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
            if (cameraController != null && cameraController.IsAimingOrLocked)
            {
                UpdateBlockSelection(cameraController.GetPointerRay());

                if (TargetBlockLoc is not null && TargetDirection is not null &&
                    playerController != null && client != null && playerController.CurrentState is DiggingAimState)
                {
                    var block = client.ChunkRenderManager.GetBlock(TargetBlockLoc.Value);
                    var status = playerController.Status;
                    if (lastHarvestInteractionInfo is not null)
                    {
                        if (lastHarvestInteractionInfo.State == HarvestInteractionState.Completed)
                        {
                            lastHarvestInteractionInfo = null;
                        }
                    }
                    else if (!block.State.NoSolidMesh)
                    {
                        StartDiggingProcess(block, TargetBlockLoc.Value, TargetDirection.Value, status);
                    }
                }
                else
                {
                    if (lastHarvestInteractionInfo is not null)
                    {
                        lastHarvestInteractionInfo.CancelInteraction();
                        EventManager.Instance.Broadcast<InteractionRemoveEvent>(new(lastHarvestInteractionInfo.Id));
                        
                        lastHarvestInteractionInfo = null;
                    }
                }
            }
            else
            {
                if (lastHarvestInteractionInfo is not null)
                {
                    lastHarvestInteractionInfo.CancelInteraction();
                    EventManager.Instance.Broadcast<InteractionRemoveEvent>(new(lastHarvestInteractionInfo.Id));
                    
                    lastHarvestInteractionInfo = null;
                }
                
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