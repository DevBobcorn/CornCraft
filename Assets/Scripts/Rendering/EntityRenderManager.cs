#nullable enable
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using MinecraftClient.Mapping;

namespace MinecraftClient.Rendering
{
    public class EntityRenderManager : MonoBehaviour
    {
        [SerializeField] private GameObject? defaultPrefab;

        [SerializeField] private GameObject? serverPlayerPrefab;
        [SerializeField] private GameObject? serverSlimPlayerPrefab;

        [SerializeField] private GameObject? skeletonPrefab;
        [SerializeField] private GameObject? witherSkeletonPrefab;
        [SerializeField] private GameObject? strayPrefab;
        [SerializeField] private GameObject? zombiePrefab;
        [SerializeField] private GameObject? huskPrefab;
        [SerializeField] private GameObject? drownedPrefab;
        [SerializeField] private GameObject? creeperPrefab;
        [SerializeField] private GameObject? pigPrefab;
        [SerializeField] private GameObject? cowPrefab;
        [SerializeField] private GameObject? mooshroomPrefab;
        [SerializeField] private GameObject? sheepPrefab;
        [SerializeField] private GameObject? itemPrefab;
        [SerializeField] private GameObject? arrowPrefab;
        [SerializeField] private GameObject? experienceOrbPrefab;

        private readonly Dictionary<ResourceLocation, GameObject?> entityPrefabs = new();
        private GameObject? GetPrefabForType(ResourceLocation type) => entityPrefabs.GetValueOrDefault(type, defaultPrefab);

        private readonly Dictionary<int, EntityRender> entityRenders = new();
        
        // Entity Id => Square distance to player
        private readonly Dictionary<int, float> nearbyEntities = new();

        private const float NEARBY_THERESHOLD_INNER = 100F; // 10 * 10
        private const float NEARBY_THERESHOLD_OUTER = 121F; // 11 * 11

        public string GetDebugInfo() => $"Ent: {entityRenders.Count}";

        public void AddEntityRender(Entity entity)
        {
            if (entityRenders.ContainsKey(entity.ID))
                return;

            GameObject? entityPrefab;

            if (entity.Type.EntityId == EntityType.PLAYER_ID) // TODO Apply right model
            {
                entityPrefab = serverSlimPlayerPrefab;
            }
            else
                entityPrefab = GetPrefabForType(entity.Type.EntityId);

            if (entityPrefab is not null)
            {
                var entityObj    = GameObject.Instantiate(entityPrefab);
                var entityRender = entityObj!.GetComponent<EntityRender>();

                entityRender.Entity = entity;
                entityRenders.Add(entity.ID, entityRender);

                entityObj.name = $"{entity.ID} {entity.Type}";
                entityObj.transform.parent = transform;

                // Initialize the entity
                entityRender.Initialize(entity.Type, entity);
            }
        }

        public void RemoveEntityRenders(int[] entityIds)
        {
            foreach (var id in entityIds)
            {
                if (entityRenders.ContainsKey(id))
                {
                    entityRenders[id].Unload();
                    entityRenders.Remove(id);
                }

                if (nearbyEntities.ContainsKey(id))
                    nearbyEntities.Remove(id);
            }

        }

        public void MoveEntityRender(int entityId, Location location)
        {
            if (entityRenders.ContainsKey(entityId))
                entityRenders[entityId].MoveTo(CoordConvert.MC2Unity(location));
        }

        public void RotateEntityRender(int entityId, float yaw, float pitch)
        {
            if (entityRenders.ContainsKey(entityId))
                entityRenders[entityId].RotateTo(yaw, pitch);
        }

        public void RotateEntityRenderHead(int entityId, float headYaw)
        {
            if (entityRenders.ContainsKey(entityId))
                entityRenders[entityId].RotateHeadTo(headYaw);
        }

        public void UnloadEntityRenders()
        {
            entityRenders.Clear();
            nearbyEntities.Clear();
        }

        public void ReloadEntityRenders()
        {
            var ids = entityRenders.Keys.ToArray();

            foreach (var id in ids)
                entityRenders[id].Unload();

            entityRenders.Clear();
            nearbyEntities.Clear();

        }

        public Dictionary<int, float> GetNearbyEntities() => nearbyEntities;

        public Vector3? GetAttackTarget(Vector3 playerPos)
        {
            if (nearbyEntities.Count == 0) // Nothing to attack
                return null;
            
            Vector3? targetPos = null;
            float minDist = float.MaxValue;

            foreach (var pair in nearbyEntities)
            {
                if (pair.Value < minDist)
                {
                    var render = GetEntityRender(pair.Key);

                    if (render!.Entity.Type.ContainsItem) // Not a valid target
                        continue;

                    var pos = render.transform.position;
                    
                    if (pair.Value <= 16F && pos.y - playerPos.y < 2F)
                        targetPos = pos;
                }
            }

            return targetPos;
        }

        public EntityRender? GetEntityRender(int entityId)
        {
            if (entityRenders.ContainsKey(entityId))
                return entityRenders[entityId];
            
            return null;
        }

        void Start()
        {
            // Clear loaded things
            entityPrefabs.Clear();

            // Register entity render prefabs ===========================================
            // Hostile Mobs
            entityPrefabs.Add(EntityType.SKELETON_ID,          skeletonPrefab);
            entityPrefabs.Add(EntityType.WITHER_SKELETON_ID,   witherSkeletonPrefab);
            entityPrefabs.Add(EntityType.STRAY_ID,             strayPrefab);
            entityPrefabs.Add(EntityType.ZOMBIE_ID,            zombiePrefab);
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
            entityPrefabs.Add(EntityType.EXPERIENCE_BOTTLE_ID, experienceOrbPrefab);
            // ...

            foreach (var prefabItem in entityPrefabs)
            {
                if (prefabItem.Value is null)
                    Debug.LogWarning($"Prefab for entity type {prefabItem.Key} is not assigned!");
                
            }

        }

        void Update()
        {
            var client = CornApp.CurrentClient;

            if (client is null) // Game is not ready, cancel update
                return;

            foreach (var render in entityRenders.Values)
            {
                // Call managed update
                render.ManagedUpdate(client!.GetTickMilSec());

                // Update entities around the player
                float dist = (render.transform.position - client.GetPosition()).sqrMagnitude;
                int entityId = render.Entity.ID;

                bool inDict = nearbyEntities.ContainsKey(entityId);

                if (dist < NEARBY_THERESHOLD_INNER) // Add entity to dictionary
                {
                    if (inDict)
                        nearbyEntities[entityId] = dist;
                    else
                        nearbyEntities.Add(entityId, dist);
                }
                else if (dist > NEARBY_THERESHOLD_OUTER) // Remove entity from dictionary
                {
                    if (inDict)
                        nearbyEntities.Remove(entityId);
                }
                else // Update entity's distance to the player if it is in the dictionary, otherwise do nothing
                {
                    if (inDict)
                        nearbyEntities[entityId] = dist;
                }

            }
        }

    }
}