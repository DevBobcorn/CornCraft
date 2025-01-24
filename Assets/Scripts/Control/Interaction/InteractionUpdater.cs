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
        private Action<ToolInteractionUpdateEvent>? toolInteractionUpdateCallback;

        private readonly Dictionary<BlockLoc, List<InteractionInfo>> blockInteractionInfos = new();

        private readonly InteractionId interactionId = new();

        private LocalToolInteractionInfo? lastToolInteractionInfo;
        private Item? currentItem;

        public Direction? TargetDirection { get; private set; } = Direction.Down;
        public BlockLoc? TargetBlockLoc { get; private set; } = null;

        private void UpdateBlockSelection(Ray? viewRay)
        {
            if (viewRay is null || client == null) return;

            if (Physics.Raycast(viewRay.Value.origin, viewRay.Value.direction, out RaycastHit viewHit, 10F, blockSelectionLayer))
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

                TargetBlockLoc = CoordConvert.Unity2MC(client.WorldOriginOffset, unityBlockPos).GetBlockLoc();
                var block = client.ChunkRenderManager.GetBlock(TargetBlockLoc.Value);

                if (blockSelectionBox == null)
                {
                    blockSelectionBox = Instantiate(blockSelectionFramePrefab)!.GetComponent<BlockSelectionBox>();

                    blockSelectionBox!.transform.SetParent(transform, false);
                }

                blockSelectionBox.UpdateShape(block.State.Shape);

                blockSelectionBox.transform.position = unityBlockPos;
            }
            else
            {
                TargetBlockLoc = null;

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

            foreach (var blockLoc in blockInteractionInfos.Keys.ToList()) // ToList because collection may change
            {
                if (playerBlockLoc.SqrDistanceTo(blockLoc) > BLOCK_INTERACTION_RADIUS_SQR_PLUS)
                {
                    RemoveBlockInteraction<ViewInteractionInfo>(blockLoc, info =>
                    {
                        EventManager.Instance.Broadcast<InteractionRemoveEvent>(new(info.Id));
                    });
                    //Debug.Log($"Rem: [{blockLoc}]");
                }

                // Update tool interactions (these are not bound by interaction radius)
                if (blockInteractionInfos.TryGetValue(blockLoc, out var infos)) // Check if the entry is still present
                {
                    foreach (var info in infos.OfType<ToolInteractionInfo>().ToList()) // ToList because collection may change
                    {
                        if (!info.UpdateInteraction(client))
                        {
                            RemoveBlockInteraction<ToolInteractionInfo>(blockLoc);
                        }
                    }
                }
            }

            // Append new available view interactions
            foreach (var offset in validOffsets)
            {
                var blockLoc = playerBlockLoc + offset;
                var block = chunksManager.GetBlock(blockLoc);

                if (table.TryGetValue(block.StateId, out InteractionDefinition? newInteractionDefinition))
                {
                    var newViewInteraction = newInteractionDefinition?.Get<ViewInteraction>();
                    if (newViewInteraction is null) continue;

                    var prevInfo = GetBlockInteraction<ViewInteractionInfo>(blockLoc)?.FirstOrDefault();
                    var newInfo = new ViewInteractionInfo(interactionId.AllocateID(), blockLoc, block.BlockId, newViewInteraction);

                    if (prevInfo is not null)
                    {
                        var prevDefinition = prevInfo.Definition;
                        if (prevDefinition != newViewInteraction) // Update this interaction
                        {
                            RemoveBlockInteraction<ViewInteractionInfo>(blockLoc, info =>
                            {
                                EventManager.Instance.Broadcast<InteractionRemoveEvent>(new(info.Id));
                            });
                            AddBlockInteraction(blockLoc, newInfo, info =>
                            {
                                EventManager.Instance.Broadcast<InteractionAddEvent>(new(info.Id, false, info));
                            });
                            //Debug.Log($"Upd: [{blockLoc}] {prevDefinition.Identifier} => {newDefinition.Identifier}");
                        }
                        // Otherwise leave it unchanged
                    }
                    else // Add this interaction
                    {
                        AddBlockInteraction(blockLoc, newInfo, info =>
                        {
                            EventManager.Instance.Broadcast<InteractionAddEvent>(new(info.Id, false, info));
                        });
                        //Debug.Log($"Add: [{blockLoc}] {newDefinition.Identifier}");
                    }
                }
                else
                {
                    if (blockInteractionInfos.ContainsKey(blockLoc))
                    {
                        RemoveBlockInteraction<ViewInteractionInfo>(blockLoc, info =>
                        {
                            EventManager.Instance.Broadcast<InteractionRemoveEvent>(new(info.Id));
                        });
                        //Debug.Log($"Rem: [{blockLoc}]");
                    }
                }
            }
        }

        private void AddBlockInteraction<T>(BlockLoc location, T info, Action<T>? onCreated = null) where T : InteractionInfo
        {
            if (!blockInteractionInfos.TryGetValue(location, out List<InteractionInfo>? interactionInfos))
            {
                interactionInfos = new List<InteractionInfo>();
                blockInteractionInfos[location] = interactionInfos;
            }

            interactionInfos.Add(info);

            onCreated?.Invoke(info);
        }

        private IEnumerable<T>? GetBlockInteraction<T>(BlockLoc location) where T : InteractionInfo
        {
            return blockInteractionInfos.TryGetValue(location, out List<InteractionInfo> interactionInfos)
                ? interactionInfos.OfType<T>() : null;
        }

        private void RemoveBlockInteraction<T>(BlockLoc location, Action<T>? onRemoved = null) where T : InteractionInfo
        {
            if (blockInteractionInfos.TryGetValue(location, out List<InteractionInfo> interactionInfos))
            {
                interactionInfos.RemoveAll(interactionInfo =>
                {
                    if (interactionInfo is not T matchedInfo) return false;

                    interactionId.ReleaseID(matchedInfo.Id);

                    onRemoved?.Invoke(matchedInfo);

                    return true;
                });

                if (!interactionInfos.Any()) blockInteractionInfos.Remove(location);
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
            toolInteractionUpdateCallback = e =>
            {
                // Must keep only one at a time.
                var toolInteractionInfo = GetBlockInteraction<ToolInteractionInfo>(e.Location)?.FirstOrDefault();

                if (toolInteractionInfo is not null)
                {
                    // Update the process
                    toolInteractionInfo.Progress = e.Progress;
                }
            };
            EventManager.Instance.Register(toolInteractionUpdateCallback);
        }

        private void StartDiggingProcess(Block block, BlockLoc blockLoc, Direction direction, PlayerStatus status)
        {
            var definition = InteractionManager.INSTANCE.InteractionTable
                .GetValueOrDefault(block.StateId)?
                .Get<ToolInteraction>();
            
            if (lastToolInteractionInfo is not null)
            {
                lastToolInteractionInfo.CancelInteraction();
                EventManager.Instance.Broadcast<InteractionRemoveEvent>(new(lastToolInteractionInfo.Id));
            }

            lastToolInteractionInfo = new LocalToolInteractionInfo(interactionId.AllocateID(), blockLoc, direction,
                currentItem, block.State.Hardness, status.Floating, status.Grounded, definition);
            
            //Debug.Log($"Created {lastToolInteractionInfo.GetHashCode()} at {blockLoc}");

            AddBlockInteraction(blockLoc, lastToolInteractionInfo, info =>
            {
                EventManager.Instance.Broadcast<InteractionAddEvent>(new(info.Id, true, info));
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
                    var curToolInteractionInfo = GetBlockInteraction<LocalToolInteractionInfo>(TargetBlockLoc.Value)?.FirstOrDefault();
                    var block = client.ChunkRenderManager.GetBlock(TargetBlockLoc.Value);
                    var status = playerController.Status;
                    if (curToolInteractionInfo is not null)
                    {
                        if (curToolInteractionInfo.State == ToolInteractionState.Completed)
                        {
                            lastToolInteractionInfo = null;
                        }
                    }
                    else if (!block.State.NoSolidMesh)
                    {
                        StartDiggingProcess(block, TargetBlockLoc.Value, TargetDirection.Value, status);
                    }
                }
                else
                {
                    if (lastToolInteractionInfo is not null)
                    {
                        lastToolInteractionInfo.CancelInteraction();
                        EventManager.Instance.Broadcast<InteractionRemoveEvent>(new(lastToolInteractionInfo.Id));
                        
                        lastToolInteractionInfo = null;
                    }
                }
            }
            else
            {
                if (lastToolInteractionInfo is not null)
                {
                    lastToolInteractionInfo.CancelInteraction();
                    EventManager.Instance.Broadcast<InteractionRemoveEvent>(new(lastToolInteractionInfo.Id));
                    
                    lastToolInteractionInfo = null;
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