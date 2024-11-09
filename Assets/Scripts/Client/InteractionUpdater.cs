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
    public class InteractionUpdater : MonoBehaviour
    {
        [SerializeField] private LayerMask blockSelectionLayer;
        [SerializeField] private GameObject? blockSelectionFramePrefab;

        private Action<ToolInteractionEvent>? toolInteractCallback;
        private ToolInteractionInfo? toolInteractInfo;

        private Action<HeldItemChangeEvent>? heldItemCallback;
        private Item? currentItem;

        private GameObject? blockSelectionFrame;

        public readonly Dictionary<BlockLoc, ViewInteractionInfo> blockInteractionInfos = new();
        public Direction? TargetDirection { get; private set; } = Direction.Down;
        public BlockLoc? TargetBlockLoc { get; private set; } = null;
        private BaseCornClient? client;
        private CameraController? cameraController;
        private PlayerController? playerController;

        private int nextNumeralID = 1;

        private Coroutine? diggingCoroutine;
        private BlockLoc? lastBlockLoc;

        private void UpdateBlockSelection(Ray? viewRay)
        {
            if (viewRay is null || client == null) return;
            
            Vector3? castResultPos;
            Vector3? castSurfaceDir;
            
            if (Physics.Raycast(viewRay.Value.origin, viewRay.Value.direction, out RaycastHit viewHit, 10F, blockSelectionLayer))
            {
                castResultPos = viewHit.point;
                castSurfaceDir = viewHit.normal;

                Vector3 normal = castSurfaceDir.Value.normalized;
                (float absX, float absY, float absZ) = (Mathf.Abs(normal.x), Mathf.Abs(normal.y), Mathf.Abs(normal.z));

                if (absX >= absY && absX >= absZ)
                    TargetDirection = normal.x > 0 ? Direction.East : Direction.West;
                else if (absY >= absX && absY >= absZ)
                    TargetDirection = normal.y > 0 ? Direction.Up : Direction.Down;
                else
                    TargetDirection = normal.z > 0 ? Direction.North : Direction.South;
            }
            else
                castResultPos = castSurfaceDir = null;

            if (castResultPos is not null && castSurfaceDir is not null)
            {
                Vector3 offseted = PointOnCubeSurface(castResultPos.Value) ?
                        castResultPos.Value - castSurfaceDir.Value * 0.5F : castResultPos.Value;

                int unityX = Mathf.FloorToInt(offseted.x);
                int unityY = Mathf.FloorToInt(offseted.y);
                int unityZ = Mathf.FloorToInt(offseted.z);
                var unityBlockPos = new Vector3(unityX, unityY, unityZ);

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
        }

        public const int BLOCK_INTERACTION_RADIUS = 3;
        public const float BLOCK_INTERACTION_RADIUS_SQR = BLOCK_INTERACTION_RADIUS * BLOCK_INTERACTION_RADIUS;
        public const float BLOCK_INTERACTION_RADIUS_SQR_PLUS = (BLOCK_INTERACTION_RADIUS + 0.5F) * (BLOCK_INTERACTION_RADIUS + 0.5F);

        private void UpdateBlockInteractions(ChunkRenderManager chunksManager)
        {
            var playerBlockLoc = client!.GetCurrentLocation().GetBlockLoc();
            var table = InteractionManager.INSTANCE.InteractionTable;

            // Remove expired interactions
            var blockLocs = blockInteractionInfos.Keys.ToArray();
            foreach (var blockLoc in blockLocs)
            {
                var sqrDist = playerBlockLoc.SqrDistanceTo(blockLoc);

                if (sqrDist > BLOCK_INTERACTION_RADIUS_SQR_PLUS)
                {
                    RemoveBlockInteraction(blockLoc); // Remove this one for being too far from the player
                    //Debug.Log($"Rem: [{blockLoc}]");
                    continue;
                }
            }

            // Append new available interactions
            for (int x = -BLOCK_INTERACTION_RADIUS;x <= BLOCK_INTERACTION_RADIUS;x++)
                for (int y = -BLOCK_INTERACTION_RADIUS;y <= BLOCK_INTERACTION_RADIUS;y++)
                    for (int z = -BLOCK_INTERACTION_RADIUS;z <= BLOCK_INTERACTION_RADIUS;z++)
                    {
                        if (x * x + y * y + z * z > BLOCK_INTERACTION_RADIUS_SQR)
                            continue;
                        
                        var blockLoc = playerBlockLoc + new BlockLoc(x, y, z);
                        var block = chunksManager.GetBlock(blockLoc);
                        var stateId = block.StateId;

                        if (table.TryGetValue(stateId, out InteractionDefinition? newDefinition))
                        {
                            var newInteraction = newDefinition?.Get<ViewInteraction>();
                            if (newInteraction is null) continue;

                            if (blockInteractionInfos.TryGetValue(blockLoc, out ViewInteractionInfo? interaction))
                            {
                                var prevInteraction = interaction.GetDefinition();
                                if (prevInteraction != newInteraction) // Update this interaction
                                {
                                    RemoveBlockInteraction(blockLoc);
                                    AddBlockInteraction(blockLoc, block.BlockId, newInteraction);
                                    //Debug.Log($"Upd: [{blockLoc}] {prevDef.Identifier} => {newDef.Identifier}");
                                }
                                // Otherwise leave it unchanged
                            }
                            else // Add this interaction
                            {
                                AddBlockInteraction(blockLoc, block.BlockId, newInteraction);
                                //Debug.Log($"Add: [{blockLoc}] {newDef.Identifier}");
                            }
                        }
                        else
                        {
                            if (blockInteractionInfos.ContainsKey(blockLoc))
                            {
                                RemoveBlockInteraction(blockLoc); // Remove this one for interaction no longer available
                                //Debug.Log($"Rem: [{blockLoc}]");
                            }
                        }
                    }
        }

        private void AddBlockInteraction(BlockLoc location, ResourceLocation blockId, ViewInteraction def)
        {
            var info = new ViewInteractionInfo(nextNumeralID, location, blockId, def);
            blockInteractionInfos.Add(location, info);

            nextNumeralID++;

            EventManager.Instance.Broadcast<ViewInteractionAddEvent>(new(info.Id, info));
        }

        private void RemoveBlockInteraction(BlockLoc location)
        {
            if (blockInteractionInfos.ContainsKey(location))
            {
                blockInteractionInfos.Remove(location, out ViewInteractionInfo info);
                
                EventManager.Instance.Broadcast<ViewInteractionRemoveEvent>(new(info.Id));
            }
        }

        private static bool PointOnGridEdge(float value)
        {
            var delta = value - Mathf.Floor(value);
            return delta < 0.01F || delta > 0.99F;
        }

        private static bool PointOnCubeSurface(Vector3 point)
        {
            return PointOnGridEdge(point.x) || PointOnGridEdge(point.y) || PointOnGridEdge(point.z);
        }

        private void StartDiggingProcess()
        {
            if (client is null || playerController is null || TargetBlockLoc is null || TargetDirection is null)
                return;

            var (isFloating, isGrounded)  = (playerController.Status.Floating, playerController.Status.Grounded);
            var block = client.ChunkRenderManager.GetBlock(TargetBlockLoc.Value);

            var definition = InteractionManager.INSTANCE.InteractionTable
                .GetValueOrDefault(block.StateId)?
                .Get<ToolInteraction>();

            toolInteractInfo = new ToolInteractionInfo(0, currentItem, block, TargetBlockLoc.Value,
                TargetDirection.Value, isFloating, isGrounded, definition);

            EventManager.Instance.Broadcast(new ToolInteractionEvent(currentItem, block.State, toolInteractInfo));

            if (diggingCoroutine != null) StopCoroutine(diggingCoroutine);

            diggingCoroutine = StartCoroutine(toolInteractInfo.RunInteraction(client));
        }

        private void CancelDiggingProcess()
        {
            if (toolInteractInfo != null)
            {
                toolInteractInfo.CancelInteraction();
                toolInteractInfo = null;
            }

            if (diggingCoroutine != null)
            {
                StopCoroutine(diggingCoroutine);
                diggingCoroutine = null;
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
        }

        void Update()
        {
            if (cameraController != null && cameraController.IsAiming)
            {
                UpdateBlockSelection(cameraController.GetPointerRay());

                if (TargetBlockLoc != null && TargetDirection != null)
                {
                    if (toolInteractInfo != null)
                    {
                        if (toolInteractInfo.State == ToolInteractionState.Completed)
                        {
                            toolInteractInfo = null;
                            diggingCoroutine = null;

                            if (blockSelectionFrame != null && blockSelectionFrame.activeSelf)
                                blockSelectionFrame.SetActive(false);
                        }
                        else if (TargetBlockLoc != lastBlockLoc)
                        {
                            CancelDiggingProcess();
                            StartDiggingProcess();
                        }
                    }
                    else
                    {
                        StartDiggingProcess();
                    }

                    lastBlockLoc = TargetBlockLoc;
                }
                else
                {
                    CancelDiggingProcess();
                }
            }
            else
            {
                CancelDiggingProcess();

                TargetBlockLoc = null;
                lastBlockLoc = null;

                if (blockSelectionFrame != null && blockSelectionFrame.activeSelf)
                    blockSelectionFrame.SetActive(false);
            }

            if (client != null)
            {
                // Update player interactions
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