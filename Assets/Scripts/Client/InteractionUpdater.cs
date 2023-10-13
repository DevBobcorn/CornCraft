#nullable enable
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using CraftSharp.Event;
using CraftSharp.Interaction;

namespace CraftSharp.Control
{
    public class InteractionUpdater : MonoBehaviour
    {
        [SerializeField] public LayerMask BlockSelectionLayer;

        public readonly Dictionary<int, BlockInteractionInfo> interactionInfos = new();
        public BlockLoc? TargetBlockLoc = null;
        private BaseCornClient? client;
        private CameraController? cameraController;

        private void UpdateBlockSelection(Ray? viewRay)
        {
            if (viewRay is null)
                return;
            
            Vector3? castResultPos;
            Vector3? castSurfaceDir;
            
            if (Physics.Raycast(viewRay.Value.origin, viewRay.Value.direction, out RaycastHit viewHit, 10F, BlockSelectionLayer))
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

        public const int INTERACTION_RADIUS = 3;
        public const int INTERACTION_RADIUS_SQR = INTERACTION_RADIUS * INTERACTION_RADIUS;

        public const int INTERACTION_RADIUS_SQR_PLUS = INTERACTION_RADIUS * INTERACTION_RADIUS + INTERACTION_RADIUS;

        private void UpdateInteractions(World world)
        {
            var playerLoc = client!.GetLocation();
            var playerBlockLoc = playerLoc.GetBlockLoc();
            var table = BlockInteractionManager.INSTANCE.InteractionTable;

            // Remove expired interactions
            var ids = interactionInfos.Keys.ToArray();

            foreach (var id in ids)
            {
                var blockLoc = interactionInfos[id].Location;
                var sqrDist = playerLoc.SqrDistanceTo(blockLoc.ToLocation());

                if (sqrDist > INTERACTION_RADIUS_SQR_PLUS)
                {
                    RemoveInteraction(id); // Remove this one
                    continue;
                }
                
                var block = world.GetBlock(blockLoc);

                if (!table.ContainsKey(block.StateId))
                {
                    RemoveInteraction(id); // Remove this one
                }
            }

            // Append new available interactions
            for (int x = -INTERACTION_RADIUS;x <= INTERACTION_RADIUS;x++)
                for (int y = -INTERACTION_RADIUS;y <= INTERACTION_RADIUS;y++)
                    for (int z = -INTERACTION_RADIUS;z <= INTERACTION_RADIUS;z++)
                    {
                        if (x * x + y * y + z * z > INTERACTION_RADIUS_SQR)
                            continue;
                        
                        var loc = playerBlockLoc + new BlockLoc(x, y, z);
                        // Hash locations in a 16*16*16 area
                        int locHash = loc.GetChunkBlockX() + (loc.GetChunkBlockY() << 4) + (loc.GetChunkBlockZ() << 8);
                        var block = world.GetBlock(loc);

                        if (table.TryGetValue(block.StateId, out BlockInteractionDefinition? newDef))
                        {
                            if (interactionInfos.ContainsKey(locHash))
                            {
                                var def = interactionInfos[locHash].Definition;
                                if (def.GetHashCode() != newDef.GetHashCode())
                                {
                                    // Update this interaction
                                    RemoveInteraction(locHash);
                                    //Debug.Log($"Upd: {def.GetHashCode()} {def.Hint} {def.Type} => {newDef.GetHashCode()} {newDef.Hint} {newDef.Type}");
                                    AddInteraction(locHash, loc, newDef);
                                }
                                // Otherwise leave it unchanged

                            }
                            else // Add this interaction
                            {
                                AddInteraction(locHash, loc, newDef);
                            }
                        }
                    }
        }

        private void AddInteraction(int id, BlockLoc location, BlockInteractionDefinition def)
        {
            BlockInteractionInfo info = new(id, location, def);
            interactionInfos.Add(id, info);

            EventManager.Instance.Broadcast<InteractionAddEvent>(new(id, info));
        }

        private void RemoveInteraction(int id)
        {
            if (interactionInfos.ContainsKey(id))
            {
                interactionInfos.Remove(id);
                
                EventManager.Instance.Broadcast<InteractionRemoveEvent>(new(id));
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
            UpdateInteractions(client!.GetWorld());
        }
    }
}