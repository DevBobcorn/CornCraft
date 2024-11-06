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
        [SerializeField] private GameObject blockSelectionFramePrefab;

        private GameObject blockSelectionFrame;

        public readonly Dictionary<BlockLoc, BlockInteractionInfo> blockInteractionInfos = new();
        public Direction? TargetDirection = Direction.Down;
        public BlockLoc? TargetBlockLoc = null;
        private BaseCornClient client;
        private CameraController cameraController;

        private int nextNumeralID = 1;

        private Coroutine DiggingCoroutine;
        private BlockLoc? DiggingBlockLoc = null;

        private void UpdateBlockSelection(Ray? viewRay)
        {
            if (viewRay is null || client == null)
            {
                return;
            }
            
            Vector3? castResultPos;
            Vector3? castSurfaceDir;
            
            if (Physics.Raycast(viewRay.Value.origin, viewRay.Value.direction, out RaycastHit viewHit, 10F, blockSelectionLayer))
            {
                castResultPos = viewHit.point;
                castSurfaceDir = viewHit.normal;

                TargetDirection = castSurfaceDir.Value switch
                {
                    { x: 0, y: 0, z: 1 } => Direction.North,
                    { x: 0, y: 0, z: -1 } => Direction.South,
                    { x: -1, y: 0, z: 0 } => Direction.West,
                    { x: 1, y: 0, z: 0 } => Direction.East,
                    { x: 0, y: 1, z: 0 } => Direction.Up,
                    { x: 0, y: -1, z: 0 } => Direction.Down,
                    _ => null
                };
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

                    blockSelectionFrame.transform.SetParent(transform, false);
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
            var table = InteractionManager.INSTANCE.BlockInteractionTable;

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

                        if (table.TryGetValue(stateId, out BlockInteractionDefinition newDef))
                        {
                            if (blockInteractionInfos.ContainsKey(blockLoc))
                            {
                                var prevDef = blockInteractionInfos[blockLoc].GetDefinition();
                                if (prevDef.Identifier != newDef.Identifier) // Update this interaction
                                {
                                    RemoveBlockInteraction(blockLoc);
                                    AddBlockInteraction(blockLoc, block.BlockId, newDef);
                                    //Debug.Log($"Upd: [{blockLoc}] {prevDef.Identifier} => {newDef.Identifier}");
                                }
                                // Otherwise leave it unchanged
                            }
                            else // Add this interaction
                            {
                                AddBlockInteraction(blockLoc, block.BlockId, newDef);
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

        private void AddBlockInteraction(BlockLoc location, ResourceLocation blockId, BlockInteractionDefinition def)
        {
            var info = new BlockInteractionInfo(nextNumeralID, location, blockId, def);
            blockInteractionInfos.Add(location, info);

            nextNumeralID++;

            EventManager.Instance.Broadcast<InteractionAddEvent>(new(info.Id, info));
        }

        private void RemoveBlockInteraction(BlockLoc location)
        {
            if (blockInteractionInfos.ContainsKey(location))
            {
                blockInteractionInfos.Remove(location, out BlockInteractionInfo info);
                
                EventManager.Instance.Broadcast<InteractionRemoveEvent>(new(info.Id));
            }
        }

        private void StartDiggingProgress(float digTime, Action onComplete)
        {
            if (DiggingBlockLoc != null && DiggingBlockLoc != TargetBlockLoc)
                StopCoroutine(DiggingCoroutine);

            DiggingBlockLoc = TargetBlockLoc;
            DiggingCoroutine = StartCoroutine(DigProgressCoroutine());

            IEnumerator DigProgressCoroutine()
            {
                Debug.Log($"Digging starting: {digTime}");
                float timeElapsed = 0f;

                while (timeElapsed < digTime)
                {
                    if (DiggingBlockLoc == null) yield break;

                    timeElapsed += Time.deltaTime;

                    yield return null;
                }

                DiggingBlockLoc = null;
                Debug.Log($"Digging finished: {digTime}");
                onComplete.Invoke();
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

        public void Initialize(BaseCornClient client, CameraController camController)
        {
            this.client = client;
            this.cameraController = camController;
        }

        void Update()
        {
            if (cameraController != null && cameraController.IsAiming)
            {
                // Update target block selection
                UpdateBlockSelection(cameraController!.GetPointerRay());

                // Handle digging
                if (TargetBlockLoc != null && TargetDirection != null)
                {
                    var time = client.DigBlock(TargetBlockLoc.Value, TargetDirection.Value);

                    if (time > 0)
                    {
                        StartDiggingProgress(time, () =>
                            client.DigBlock(TargetBlockLoc.Value, TargetDirection.Value, BaseCornClient.DiggingStatus.Finished));
                    }
                }
            }
            else
            {
                DiggingBlockLoc = null;
                TargetBlockLoc = null;

                if (blockSelectionFrame != null && blockSelectionFrame.activeSelf)
                {
                    blockSelectionFrame.SetActive(false);
                }
            }

            if (client != null)
            {
                // Update player interactions
                UpdateBlockInteractions(client.ChunkRenderManager);
            }
        }
    }
}