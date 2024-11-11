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

        private GameObject? blockSelectionFrame;

        private BaseCornClient? client;
        private CameraController? cameraController;
        private PlayerController? playerController;

        private Action<HeldItemChangeEvent>? heldItemCallback;
        private Action<ToolInteractionEvent>? toolInteractionCallback;

        private Dictionary<BlockLoc, List<InteractionInfo>> blockInteractionInfos = new();

        private InteractionId interactionId = new();

        private LocalToolInteractionInfo? lastInteractionInfo;
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

                Vector3 unityBlockPos = new Vector3(
                    Mathf.FloorToInt(offseted.x),
                    Mathf.FloorToInt(offseted.y),
                    Mathf.FloorToInt(offseted.z)
                );

                TargetBlockLoc = CoordConvert.Unity2MC(client.WorldOriginOffset, unityBlockPos).GetBlockLoc();

                if (blockSelectionFrame == null)
                {
                    blockSelectionFrame = GameObject.Instantiate(blockSelectionFramePrefab);

                    blockSelectionFrame!.transform.SetParent(transform, false);
                }
                else if (!blockSelectionFrame.activeSelf)
                {
                    blockSelectionFrame.SetActive(true);
                }

                blockSelectionFrame.transform.position = unityBlockPos;
            }
            else
            {
                TargetBlockLoc = null;

                if (blockSelectionFrame != null && blockSelectionFrame.activeSelf)
                {
                    blockSelectionFrame.SetActive(false);
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
                Vector3 delta = new Vector3(
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

            foreach (var blockLoc in blockInteractionInfos.Keys.ToList())
            {
                if (playerBlockLoc.SqrDistanceTo(blockLoc) > BLOCK_INTERACTION_RADIUS_SQR_PLUS)
                {
                    RemoveBlockInteraction<ViewInteractionInfo>(blockLoc, info =>
                    {
                        if (info is ViewInteractionInfo viewInfo)
                            EventManager.Instance.Broadcast<ViewInteractionRemoveEvent>(new(viewInfo.Id));
                    });
                    RemoveBlockInteraction<ToolInteractionInfo>(blockLoc);
                    //Debug.Log($"Rem: [{blockLoc}]");
                }
                else
                {
                    // Update the interactions
                    if (client is null) return;

                    foreach (var interactionInfo in blockInteractionInfos[blockLoc].OfType<ToolInteractionInfo>())
                        if (!interactionInfo.RunInteraction(client).MoveNext())
                            RemoveBlockInteraction<ToolInteractionInfo>(blockLoc);
                }
            }

            // Append new available view interactions
            foreach (var offset in validOffsets)
            {
                var blockLoc = playerBlockLoc + offset;
                var block = chunksManager.GetBlock(blockLoc);

                if (table.TryGetValue(block.StateId, out InteractionDefinition? newInteractionDefinition))
                {
                    var newDefinition = newInteractionDefinition?.Get<ViewInteraction>();
                    if (newDefinition is null) continue;

                    var prevInfo = GetBlockInteraction<ViewInteractionInfo>(blockLoc)?.FirstOrDefault();
                    var newInfo = new ViewInteractionInfo(interactionId.AllocateID(), blockLoc, block.BlockId, newDefinition);
                    if (prevInfo is not null)
                    {
                        var prevDefinition = prevInfo.Definition;
                        if (prevDefinition != newDefinition) // Update this interaction
                        {
                            RemoveBlockInteraction<ViewInteractionInfo>(blockLoc, info =>
                            {
                                if (info is ViewInteractionInfo viewInfo)
                                    EventManager.Instance.Broadcast<ViewInteractionRemoveEvent>(new(viewInfo.Id));
                            });
                            AddBlockInteraction(blockLoc, newInfo, info =>
                            {
                                if (info is ViewInteractionInfo viewInfo)
                                    EventManager.Instance.Broadcast<ViewInteractionAddEvent>(new(viewInfo.Id, viewInfo));
                            });
                            //Debug.Log($"Upd: [{blockLoc}] {prevDefinition.Identifier} => {newDefinition.Identifier}");
                        }
                        // Otherwise leave it unchanged
                    }
                    else // Add this interaction
                    {
                        AddBlockInteraction(blockLoc, newInfo, info =>
                        {
                            if (info is ViewInteractionInfo viewInfo)
                                EventManager.Instance.Broadcast<ViewInteractionAddEvent>(new(viewInfo.Id, viewInfo));
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
                            if (info is ViewInteractionInfo viewInfo)
                                EventManager.Instance.Broadcast<ViewInteractionRemoveEvent>(new(viewInfo.Id));
                        });
                        //Debug.Log($"Rem: [{blockLoc}]");
                    }
                }
            }
        }

        private void AddBlockInteraction(BlockLoc location, InteractionInfo info, Action<InteractionInfo>? onCreated = null)
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
                ? interactionInfos.OfType<T>()
                : null;
        }

        private void RemoveBlockInteraction<T>(BlockLoc location, Action<InteractionInfo>? onRemoved = null) where T : InteractionInfo
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
            toolInteractionCallback = e =>
            {
                // Must keep only one at a time.
                var interactionInfo = GetBlockInteraction<GhostToolInteractionInfo>(e.Location)?.FirstOrDefault();

                if (interactionInfo is not null)
                {
                    // Update the process
                    interactionInfo.Progress = e.Progress;
                }
                else
                {
                    interactionInfo = new GhostToolInteractionInfo(interactionId.AllocateID(), e.Location, Direction.Down, null);

                    AddBlockInteraction(e.Location, interactionInfo);
                }
            };
            EventManager.Instance.Register(toolInteractionCallback);
        }

        void Update()
        {
            if (cameraController != null && cameraController.IsAiming)
            {
                UpdateBlockSelection(cameraController.GetPointerRay());

                if (TargetBlockLoc is not null && TargetDirection is not null &&
                    playerController is not null && client is not null)
                {
                    var info = GetBlockInteraction<LocalToolInteractionInfo>(TargetBlockLoc.Value)?.FirstOrDefault();
                    var block = client.ChunkRenderManager.GetBlock(TargetBlockLoc.Value);
                    var status = playerController.Status;
                    if (info is not null)
                    {
                        if (info.State == ToolInteractionState.Completed)
                        {
                            lastInteractionInfo = null;

                            if (blockSelectionFrame != null && blockSelectionFrame.activeSelf)
                                blockSelectionFrame.SetActive(false);
                        }
                        else if (TargetBlockLoc != lastInteractionInfo?.Location)
                        {
                            lastInteractionInfo?.CancelInteraction();
                            StartDiggingProcess(block, TargetBlockLoc.Value, TargetDirection.Value, status);
                        }
                    }
                    else
                    {
                        StartDiggingProcess(block, TargetBlockLoc.Value, TargetDirection.Value, status);
                    }
                }
                else
                {
                    lastInteractionInfo?.CancelInteraction();
                    lastInteractionInfo = null;
                }
            }
            else
            {
                lastInteractionInfo?.CancelInteraction();
                lastInteractionInfo = null;
                TargetBlockLoc = null;

                if (blockSelectionFrame != null && blockSelectionFrame.activeSelf)
                    blockSelectionFrame.SetActive(false);
            }

            return;

            void StartDiggingProcess(Block block, BlockLoc blockLoc, Direction direction, PlayerStatus status)
            {
                var definition = InteractionManager.INSTANCE.InteractionTable
                    .GetValueOrDefault(block.StateId)?
                    .Get<ToolInteraction>();

                lastInteractionInfo = new LocalToolInteractionInfo(interactionId.AllocateID(), blockLoc, direction,
                    currentItem, block.State.Hardness, status.Floating, status.Grounded, definition);

                AddBlockInteraction(blockLoc, lastInteractionInfo);
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