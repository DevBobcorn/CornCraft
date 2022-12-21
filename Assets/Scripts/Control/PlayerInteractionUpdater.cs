#nullable enable
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MinecraftClient.Event;
using MinecraftClient.Mapping;

namespace MinecraftClient.Control
{
    public class PlayerInteractionUpdater : MonoBehaviour
    {
        [SerializeField] public LayerMask BlockSelectionLayer;

        public readonly Dictionary<int, BlockInteractionInfo> interactionInfos = new();
        public Location? TargetLocation = null;

        public void UpdateBlockSelection(Ray? viewRay)
        {
            if (viewRay is null)
                return;

            RaycastHit viewHit;

            Vector3? castResultPos  = null;
            Vector3? castSurfaceDir = null;

            if (Physics.Raycast(viewRay.Value.origin, viewRay.Value.direction, out viewHit, 10F, BlockSelectionLayer))
            {
                castResultPos  = viewHit.point;
                castSurfaceDir = viewHit.normal;
            }
            else
                castResultPos = castSurfaceDir = null;

            if (castResultPos is not null && castSurfaceDir is not null)
            {
                Vector3 offseted = PointOnCubeSurface(castResultPos.Value) ?
                        castResultPos.Value - castSurfaceDir.Value * 0.5F : castResultPos.Value;
                
                Vector3 selection = new(Mathf.Floor(offseted.x), Mathf.Floor(offseted.y), Mathf.Floor(offseted.z));

                TargetLocation = CoordConvert.Unity2MC(selection);
            }
            else
                TargetLocation = null;

        }

        public const int INTERACTION_RADIUS = 3;
        public const int INTERACTION_RADIUS_SQR = INTERACTION_RADIUS * INTERACTION_RADIUS;

        public const int INTERACTION_RADIUS_SQR_PLUS = INTERACTION_RADIUS * INTERACTION_RADIUS + INTERACTION_RADIUS;

        public void UpdateInteractions(World world)
        {
            var playerLoc = CoordConvert.Unity2MC(transform.position).ToFloor();
            var table = BlockInteractionManager.INSTANCE.InteractionTable;

            // Remove expired interactions
            var ids = interactionInfos.Keys.ToArray();

            foreach (var id in ids)
            {
                var loc = interactionInfos[id].Location;
                var sqrDist = playerLoc.SqrDistanceTo(loc);

                if (sqrDist > INTERACTION_RADIUS_SQR_PLUS)
                {
                    RemoveInteraction(id); // Remove this one
                    continue;
                }
                
                var block = world.GetBlock(loc);

                if (!table.ContainsKey(block.StateId))
                    RemoveInteraction(id); // Remove this one
                
            }

            // Append new available interactions
            for (int x = -INTERACTION_RADIUS;x <= INTERACTION_RADIUS;x++)
                for (int y = -INTERACTION_RADIUS;y <= INTERACTION_RADIUS;y++)
                    for (int z = -INTERACTION_RADIUS;z <= INTERACTION_RADIUS;z++)
                    {
                        if (x * x + y * y + z * z > INTERACTION_RADIUS_SQR)
                            continue;
                        
                        var loc = playerLoc + new Location(x, y, z);
                        // Hash locations in a 16*16*16 area
                        int locHash = loc.ChunkBlockX + (loc.ChunkBlockY << 4) + (loc.ChunkBlockZ << 8);
                        
                        var block = world.GetBlock(loc);

                        BlockInteractionDefinition? newDef;
                        
                        if (table.TryGetValue(block.StateId, out newDef))
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
                                AddInteraction(locHash, loc, newDef);
                            
                        }

                    }
        }

        private void AddInteraction(int id, Location loc, BlockInteractionDefinition def)
        {
            BlockInteractionInfo info = new(id, loc, def);
            interactionInfos.Add(id, info);

            EventManager.Instance.Broadcast<InteractionAddEvent>(new(id, info));

            //Debug.Log($"Add {id} {def.Hint} at {loc}");
        }

        private void RemoveInteraction(int id)
        {
            if (interactionInfos.ContainsKey(id))
            {
                var info = interactionInfos[id];
                interactionInfos.Remove(id);
                
                EventManager.Instance.Broadcast<InteractionRemoveEvent>(new(id));

                //Debug.Log($"Remove {id} {info.GetHint()} at {info.Location}");
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

    }
}