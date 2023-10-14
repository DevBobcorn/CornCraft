#nullable enable
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
        [SerializeField] private ChunkRenderManager? chunkRenderManager;
        [SerializeField] private EntityRenderManager? entityRenderManager;

        public readonly Dictionary<BlockLoc, BlockInteractionInfo> blockInteractionInfos = new();
        public BlockLoc? TargetBlockLoc = null;
        private BaseCornClient? client;
        private CameraController? cameraController;

        private int nextNumeralID = 1;

        private void UpdateBlockSelection(Ray? viewRay)
        {
            if (viewRay is null)
                return;
            
            Vector3? castResultPos;
            Vector3? castSurfaceDir;
            
            if (Physics.Raycast(viewRay.Value.origin, viewRay.Value.direction, out RaycastHit viewHit, 10F, blockSelectionLayer))
            {
                castResultPos = viewHit.point;
                castSurfaceDir = viewHit.normal;
            }
            else
                castResultPos = castSurfaceDir = null;

            if (castResultPos is not null && castSurfaceDir is not null)
            {
                Vector3 offseted = PointOnCubeSurface(castResultPos.Value) ?
                        castResultPos.Value - castSurfaceDir.Value * 0.5F : castResultPos.Value;

                TargetBlockLoc = new BlockLoc(
                        Mathf.FloorToInt(offseted.z),
                        Mathf.FloorToInt(offseted.y),
                        Mathf.FloorToInt(offseted.x));
            }
            else
            {
                TargetBlockLoc = null;
            }
        }

        public const int BLOCK_INTERACTION_RADIUS = 3;
        public const float BLOCK_INTERACTION_RADIUS_SQR = BLOCK_INTERACTION_RADIUS * BLOCK_INTERACTION_RADIUS;
        public const float BLOCK_INTERACTION_RADIUS_SQR_PLUS = (BLOCK_INTERACTION_RADIUS + 0.5F) * (BLOCK_INTERACTION_RADIUS + 0.5F);

        private void UpdateBlockInteractions(ChunkRenderManager chunksManager)
        {
            var playerBlockLoc = client!.GetLocation().GetBlockLoc();
            var table = BlockInteractionManager.INSTANCE.InteractionTable;

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
                        var stateId = chunksManager.GetBlock(blockLoc).StateId;

                        if (table.TryGetValue(stateId, out BlockInteractionDefinition? newDef))
                        {
                            if (blockInteractionInfos.ContainsKey(blockLoc))
                            {
                                var prevDef = blockInteractionInfos[blockLoc].Definition;
                                if (prevDef.Hint != newDef.Hint) // Update this interaction
                                {
                                    RemoveBlockInteraction(blockLoc);
                                    AddBlockInteraction(blockLoc, newDef);
                                    //Debug.Log($"Upd: [{blockLoc}] {prevDef.Hint} => {newDef.Hint}");
                                }
                                // Otherwise leave it unchanged
                            }
                            else // Add this interaction
                            {
                                AddBlockInteraction(blockLoc, newDef);
                                //Debug.Log($"Add: [{blockLoc}] {newDef.Hint}");
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

        private void AddBlockInteraction(BlockLoc location, BlockInteractionDefinition def)
        {
            var info = new BlockInteractionInfo(nextNumeralID, location, def);
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
            // Update target block selection
            UpdateBlockSelection(cameraController!.GetViewportCenterRay());

            // Update player interactions
            UpdateBlockInteractions(client!.ChunkRenderManager!);
        }
    }
}