using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using CraftSharp.Control;

namespace CraftSharp.Rendering
{
    public class EntityRenderManager : MonoBehaviour
    {
        #region GameObject Prefabs for each entity type
        [SerializeField] private GameObject defaultPrefab;

        [SerializeField] private GameObject serverPlayerPrefab;
        [SerializeField] private GameObject skeletonPrefab;
        [SerializeField] private GameObject witherSkeletonPrefab;
        [SerializeField] private GameObject strayPrefab;
        [SerializeField] private GameObject zombiePrefab;
        [SerializeField] private GameObject villagerPrefab;
        [SerializeField] private GameObject huskPrefab;
        [SerializeField] private GameObject drownedPrefab;
        [SerializeField] private GameObject creeperPrefab;
        [SerializeField] private GameObject pigPrefab;
        [SerializeField] private GameObject cowPrefab;
        [SerializeField] private GameObject mooshroomPrefab;
        [SerializeField] private GameObject sheepPrefab;
        [SerializeField] private GameObject itemPrefab;
        [SerializeField] private GameObject arrowPrefab;
        [SerializeField] private GameObject experienceOrbPrefab;
        [SerializeField] private GameObject thrownItemPrefab;
        #endregion

        private readonly Dictionary<ResourceLocation, GameObject> entityPrefabs = new();

        private GameObject GetPrefabForType(ResourceLocation type)
        {
            return entityPrefabs.GetValueOrDefault(type, defaultPrefab);
        }
        
        /// <summary>
        /// All entity renders in the current world
        /// </summary>
        private readonly Dictionary<int, EntityRender> entityRenders = new();
        
        // Squared distance range of an entity to be considered as "near" the player
        private const float NEARBY_THRESHOLD_INNER =  81F; //  9 *  9
        private const float NEARBY_THRESHOLD_OUTER = 100F; // 10 * 10

        /// <summary>
        /// A dictionary storing entities near the player
        /// Entity Id => Square distance to player
        /// </summary>
        private readonly Dictionary<int, float> nearbyEntities = new();

        public string GetDebugInfo()
        {
            return $"Entity Count: {entityRenders.Count}";
        }

        /// <summary>
        /// Check if the given id is occupied by an entity
        /// </summary>
        /// <param name="entityId">Numeral id of the entity</param>
        /// <returns></returns>
        public bool HasEntityRender(int entityId)
        {
            return entityRenders.ContainsKey(entityId);
        }

        /// <summary>
        /// Get entity render with a given numeral id
        /// </summary>
        /// <param name="entityId">Numeral id of the entity</param>
        /// <returns>Entity render with the id. Null if not present</returns>
        public EntityRender GetEntityRender(int entityId)
        {
            return entityRenders.GetValueOrDefault(entityId);
        }

        /// <summary>
        /// Create a new entity render from given entity data
        /// </summary>
        /// <param name="entitySpawn">Entity data</param>
        public void AddEntityRender(EntitySpawnData entitySpawn)
        {
            // If the numeral id is occupied by an entity already,
            // destroy this entity first
            if (entityRenders.ContainsKey(entitySpawn.Id))
            {
                if (entityRenders[entitySpawn.Id])
                {
                    entityRenders[entitySpawn.Id].Unload();
                }
                
                entityRenders.Remove(entitySpawn.Id);
                nearbyEntities.Remove(entitySpawn.Id);
            }

            var entityPrefab = entitySpawn.Type.TypeId == EntityType.PLAYER_ID ?
                serverPlayerPrefab : GetPrefabForType(entitySpawn.Type.TypeId);

            if (entityPrefab)
            {
                var entityObj = Instantiate(entityPrefab, transform, true);
                var entityRender = entityObj!.GetComponent<EntityRender>();

                entityRenders.Add(entitySpawn.Id, entityRender);

                entityObj.name = $"{entitySpawn.Id} {entitySpawn.Type}";

                // Initialize the entity
                entityRender.Initialize(entitySpawn, _worldOriginOffset);
            }
        }

        /// <summary>
        /// Unload(Clear) entities with given ids
        /// </summary>
        public void RemoveEntityRenders(int[] entityIds)
        {
            foreach (var id in entityIds)
            {
                if (entityRenders.ContainsKey(id))
                {
                    entityRenders[id].Unload();
                    entityRenders.Remove(id);
                }
                nearbyEntities.Remove(id);
            }
        }

        /// <summary>
        /// Move an entity render by given delta value
        /// </summary>
        /// <param name="entityId">Numeral id of the entity to move</param>
        /// <param name="delta">Location delta</param>
        public void MoveEntityRender(int entityId, Location delta)
        {
            if (entityRenders.TryGetValue(entityId, out var render))
            {
                // Delta value, world origin offset doesn't apply
                render.Position.Value += CoordConvert.MC2UnityDelta(delta);
            }
        }

        /// <summary>
        /// Set velocity for an entity render to given value
        /// </summary>
        /// <param name="entityId">Numeral id of the entity to set velocity for</param>
        /// <param name="velocity">New velocity</param>
        public void SetEntityReceivedVelocity(int entityId, float3 velocity)
        {
            if (entityRenders.TryGetValue(entityId, out var render))
            {
                // Velocity value, world origin offset doesn't apply
                render.SetReceivedVelocity(velocity.zyx);
            }
        }

        /// <summary>
        /// Teleport an entity render to given location
        /// </summary>
        /// <param name="entityId">Numeral id of the entity to move</param>
        /// <param name="location">New location</param>
        public void TeleportEntityRender(int entityId, Location location)
        {
            if (entityRenders.TryGetValue(entityId, out var render))
            {
                render.Position.Value = CoordConvert.MC2Unity(_worldOriginOffset, location);
            }
        }

        /// <summary>
        /// Update the yaw and pitch of a given entity
        /// </summary>
        /// <param name="entityId">Numeral id of the entity to rotate</param>
        /// <param name="yaw">Byte angle, conversion required</param>
        /// <param name="pitch">Byte angle, conversion required</param>
        public void RotateEntityRender(int entityId, byte yaw, byte pitch)
        {
            if (entityRenders.ContainsKey(entityId))
            {
                entityRenders[entityId].Yaw.Value = EntitySpawnData.GetYawFromByte(yaw);
                entityRenders[entityId].Pitch.Value = EntitySpawnData.GetPitchFromByte(pitch);
            }
        }

        /// <summary>
        /// Update the head yaw of a given entity
        /// </summary>
        /// <param name="entityId">Numeral id of the entity</param>
        /// <param name="headYaw">Byte angle, conversion required</param>
        public void RotateEntityRenderHead(int entityId, byte headYaw)
        {
            if (entityRenders.TryGetValue(entityId, out var render)
                && render is LivingEntityRender livingEntityRender)
            {
                livingEntityRender.HeadYaw.Value = EntitySpawnData.GetHeadYawFromByte(headYaw);
            }
        }

        /// <summary>
        /// Unload all entity renders in the world
        /// </summary>
        public void ReloadEntityRenders()
        {
            var ids = entityRenders.Keys.ToArray();
            foreach (var id in ids)
            {
                entityRenders[id].Unload();
            }
            entityRenders.Clear();
            nearbyEntities.Clear();
        }

        /// <summary>
        /// Get a list of ids of entities near the player
        /// </summary>
        /// <returns>A dictionary mapping nearby entity ids to the distance between the entity and client player</returns>
        public Dictionary<int, float> GetNearbyEntityIds()
        {
            return nearbyEntities;
        }
        
        /// <summary>
        /// Get a list entities near the player
        /// </summary>
        /// <returns>A dictionary mapping nearby entity ids to the distance between the entity and client player</returns>
        public Dictionary<EntityRender, float> GetNearbyEntities()
        {
            return nearbyEntities.ToDictionary(x => GetEntityRender(x.Key),
                x => x.Value);
        }

        private static Raycaster.AABBRaycastHit GetEmptyAabbHit() => new()
        {
            hit = false,
            point = Vector3.zero,
            direction = Direction.Up
        };

        /// <summary>
        /// Raycast against nearby entities using their AABBs and return the nearest hit, if any.
        /// </summary>
        /// <param name="ray">Ray in Unity world space.</param>
        /// <param name="aabbInfo">Hit info on the entity AABB.</param>
        /// <param name="entityRender">Entity render that was hit.</param>
        /// <returns>True if any nearby entity is hit by the ray.</returns>
        public bool RaycastNearbyEntities(Ray ray, out Raycaster.AABBRaycastHit aabbInfo, out EntityRender entityRender)
        {
            aabbInfo = GetEmptyAabbHit();
            entityRender = null;

            // Avoid invalid rays to prevent division by zero in the raycast utility
            if (ray.direction.sqrMagnitude < Mathf.Epsilon)
            {
                return false;
            }

            float nearestDistance = float.PositiveInfinity;

            foreach (var entityId in nearbyEntities.Keys)
            {
                if (!entityRenders.TryGetValue(entityId, out var render) || !render)
                    continue;

                var hit = Raycaster.RaycastAABB(ray, render.GetAABB());
                if (!hit.hit)
                    continue;

                var distance = Vector3.Distance(ray.origin, hit.point);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    aabbInfo = hit;
                    entityRender = render;
                }
            }

            return entityRender;
        }

        private Vector3Int _worldOriginOffset = Vector3Int.zero;

        public void SetWorldOriginOffset(Vector3 posDelta, Vector3Int offset)
        {
            _worldOriginOffset = offset;

            // Move all registered EntityRenders
            foreach (var render in entityRenders.Values)
            {
                render.TeleportByDelta(posDelta);
            }
        }

        private void Start()
        {
            // Clear loaded things
            entityPrefabs.Clear();

            // Register entity render prefabs ===========================================
            // Server player
            entityPrefabs.Add(EntityType.PLAYER_ID,            serverPlayerPrefab);
            // Hostile Mobs
            entityPrefabs.Add(EntityType.SKELETON_ID,          skeletonPrefab);
            entityPrefabs.Add(EntityType.WITHER_SKELETON_ID,   witherSkeletonPrefab);
            entityPrefabs.Add(EntityType.STRAY_ID,             strayPrefab);
            entityPrefabs.Add(EntityType.ZOMBIE_ID,            zombiePrefab);
            entityPrefabs.Add(EntityType.VILLAGER_ID,          villagerPrefab);
            entityPrefabs.Add(EntityType.HUSK_ID,              huskPrefab);
            entityPrefabs.Add(EntityType.DROWNED_ID,           drownedPrefab);
            entityPrefabs.Add(EntityType.CREEPER_ID,           creeperPrefab);
            // ...
            // Passive Mobs
            entityPrefabs.Add(EntityType.PIG_ID,               pigPrefab);
            entityPrefabs.Add(EntityType.COW_ID,               cowPrefab);
            entityPrefabs.Add(EntityType.MOOSHROOM_ID,         mooshroomPrefab);
            entityPrefabs.Add(EntityType.SHEEP_ID,             sheepPrefab);
            // Neutral Mobs
            // ...
            // Miscellaneous Entities
            entityPrefabs.Add(EntityType.ARROW_ID,             arrowPrefab);
            entityPrefabs.Add(EntityType.ITEM_ID,              itemPrefab);
            entityPrefabs.Add(EntityType.EXPERIENCE_ORB_ID,    experienceOrbPrefab);
            entityPrefabs.Add(EntityType.EXPERIENCE_BOTTLE_ID, thrownItemPrefab);
            entityPrefabs.Add(EntityType.EGG_ID,               thrownItemPrefab);
            entityPrefabs.Add(EntityType.ENDER_PEARL_ID,       thrownItemPrefab);
            entityPrefabs.Add(EntityType.FIREBALL_ID,          thrownItemPrefab);
            entityPrefabs.Add(EntityType.SMALL_FIREBALL_ID,    thrownItemPrefab);
            entityPrefabs.Add(EntityType.SNOWBALL_ID,          thrownItemPrefab);
            // ...

            foreach (var prefabItem in entityPrefabs)
            {
                if (!prefabItem.Value)
                {
                    Debug.LogWarning($"Prefab for entity type {prefabItem.Key} is not assigned!");
                }            
            }
        }

        private void Update()
        {
            var client = CornApp.CurrentClient;

            if (!client) // Game is not ready, cancel update
                return;

            var tickMilSec = client.GetTickMilSec();
            var cameraController = client.CameraController;
            
            if (!cameraController) // Camera is not ready, cancel update
                return;
            
            var cameraTransform = cameraController.RenderCamera.transform;

            foreach (var render in entityRenders.Values)
            {
                // Call managed update
                render.ManagedUpdate(tickMilSec, cameraTransform);

                // Update entities around the player
                float dist = (render.transform.position - client.GetPosition()).sqrMagnitude;
                int entityId = render.NumeralId;
                bool inNearbyDict = nearbyEntities.ContainsKey(entityId);

                if (dist < NEARBY_THRESHOLD_INNER) // Add entity to dictionary
                {
                    if (inNearbyDict)
                        nearbyEntities[entityId] = dist;
                    else
                        nearbyEntities.Add(entityId, dist);
                }
                else if (dist > NEARBY_THRESHOLD_OUTER) // Remove entity from dictionary
                {
                    if (inNearbyDict)
                        nearbyEntities.Remove(entityId);
                }
                else // Update entity's distance to the player if it is in the dictionary, otherwise do nothing
                {
                    if (inNearbyDict)
                        nearbyEntities[entityId] = dist;
                }
            }
        }
    }
}