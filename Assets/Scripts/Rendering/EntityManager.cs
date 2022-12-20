#nullable enable
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using MinecraftClient.Mapping;

namespace MinecraftClient.Rendering
{
    public enum EntityInfoTagType
    {
        NPC,
        Monster,
        None
    }

    public class EntityManager : MonoBehaviour
    {
        private static EntityManager? instance;
        public static EntityManager Instance
        {
            get {
                if (instance is null)
                {
                    instance = Component.FindObjectOfType<EntityManager>();
                }
                
                return instance;
            }
        }

        private CornClient? game;
        private static GameObject? placeboEntityPrefab;

        private static GameObject? serverDefoPlayerPrefab, serverSlimPlayerPrefab;

        private static readonly Dictionary<EntityType, GameObject?> entityPrefabs = new();
        private static readonly Dictionary<EntityType, EntityInfoTagType> infoTagTypes = new();

        private static GameObject? GetPrefabForType(EntityType type) => entityPrefabs.GetValueOrDefault(type, placeboEntityPrefab);
        public static EntityInfoTagType GetInfoTagTypeForType(EntityType type) =>
                infoTagTypes.GetValueOrDefault(type, EntityInfoTagType.None);

        private readonly Dictionary<int, EntityRender> entities = new();
        private readonly HashSet<int> nearbyEntities = new();

        private const float NEARBY_THERESHOLD_INNER = 256F;
        private const float NEARBY_THERESHOLD_OUTER = 300F;

        public string GetDebugInfo() => $"Ent: {entities.Count}";

        public void AddEntity(Entity entity)
        {
            if (entities.ContainsKey(entity.ID))
                return;

            GameObject? entityPrefab;

            if (entity.Type == EntityType.Player) // TODO Apply right model
            {
                entityPrefab = serverSlimPlayerPrefab;
            }
            else
                entityPrefab = GetPrefabForType(entity.Type);

            if (entityPrefab is not null)
            {
                var entityObj    = GameObject.Instantiate(entityPrefab);
                var entityRender = entityObj!.GetComponent<EntityRender>();

                entityRender.Entity = entity;
                entities.Add(entity.ID, entityRender);

                entityObj.name = $"{entity.ID} {entity.Type}";
                entityObj.transform.parent = transform;
            }
        }

        public void RemoveEntities(int[] entityIds)
        {
            foreach (var id in entityIds)
            {
                if (entities.ContainsKey(id))
                {
                    entities[id].Unload();
                    entities.Remove(id);
                }

                if (nearbyEntities.Contains(id))
                    nearbyEntities.Remove(id);
            }

        }

        public void MoveEntity(int entityId, Location location)
        {
            if (entities.ContainsKey(entityId))
                entities[entityId].MoveTo(CoordConvert.MC2Unity(location));
        }

        public void RotateEntity(int entityId, float yaw, float pitch, int flag)
        {
            if (entities.ContainsKey(entityId))
                entities[entityId].RotateTo(yaw, pitch);
        }

        public void UpdateEntityHeadYaw(int entityId, float headYaw)
        {
            if (entities.ContainsKey(entityId))
                entities[entityId].RotateHeadTo(headYaw);
        }

        public void UnloadEntities()
        {
            entities.Clear();
            nearbyEntities.Clear();

            // Reset instance
            instance = null;
        }

        public void ReloadEntities()
        {
            var ids = entities.Keys.ToArray();

            foreach (var id in ids)
                entities[id].Unload();

            entities.Clear();
            nearbyEntities.Clear();

        }

        public HashSet<int> GetNearbyEntities() => nearbyEntities;

        public EntityRender? GetEntity(int entityId)
        {
            if (entities.ContainsKey(entityId))
                return entities[entityId];
            
            return null;
        }

        void Start()
        {
            placeboEntityPrefab = Resources.Load<GameObject>("Prefabs/Entity/Cube Entity");

            serverDefoPlayerPrefab = Resources.Load<GameObject>("Prefabs/Player/Server Defo Player Entity");
            serverSlimPlayerPrefab = Resources.Load<GameObject>("Prefabs/Player/Server Slim Player Entity");

            // Clear loaded things
            entityPrefabs.Clear();
            infoTagTypes.Clear();

            // Add specific entity prefabs TODO Expand
            entityPrefabs.Add(EntityType.Skeleton, Resources.Load<GameObject>("Prefabs/Entity/Skeleton Entity"));
            entityPrefabs.Add(EntityType.WitherSkeleton, Resources.Load<GameObject>("Prefabs/Entity/Wither Skeleton Entity"));
            entityPrefabs.Add(EntityType.Stray, Resources.Load<GameObject>("Prefabs/Entity/Stray Entity"));

            entityPrefabs.Add(EntityType.Zombie, Resources.Load<GameObject>("Prefabs/Entity/Zombie Entity"));
            entityPrefabs.Add(EntityType.Husk, Resources.Load<GameObject>("Prefabs/Entity/Husk Entity"));
            entityPrefabs.Add(EntityType.Drowned, Resources.Load<GameObject>("Prefabs/Entity/Drowned Entity"));

            entityPrefabs.Add(EntityType.Creeper, Resources.Load<GameObject>("Prefabs/Entity/Creeper Entity"));

            entityPrefabs.Add(EntityType.Pig, Resources.Load<GameObject>("Prefabs/Entity/Pig Entity"));
            entityPrefabs.Add(EntityType.Cow, Resources.Load<GameObject>("Prefabs/Entity/Cow Entity"));
            entityPrefabs.Add(EntityType.Mooshroom, Resources.Load<GameObject>("Prefabs/Entity/Cow Entity"));
            entityPrefabs.Add(EntityType.Sheep, Resources.Load<GameObject>("Prefabs/Entity/Sheep Entity"));
            
            entityPrefabs.Add(EntityType.Goat, Resources.Load<GameObject>("Prefabs/Entity/Pig Entity"));

            // Register info tag types
            infoTagTypes.Add(EntityType.Skeleton, EntityInfoTagType.Monster);
            infoTagTypes.Add(EntityType.WitherSkeleton, EntityInfoTagType.Monster);
            infoTagTypes.Add(EntityType.Stray, EntityInfoTagType.Monster);

            infoTagTypes.Add(EntityType.Zombie, EntityInfoTagType.Monster);
            infoTagTypes.Add(EntityType.Husk, EntityInfoTagType.Monster);
            infoTagTypes.Add(EntityType.Drowned, EntityInfoTagType.Monster);

            infoTagTypes.Add(EntityType.Creeper, EntityInfoTagType.Monster);

            infoTagTypes.Add(EntityType.Villager, EntityInfoTagType.NPC);

            foreach (var prefabItem in entityPrefabs)
            {
                if (prefabItem.Value is null)
                    Debug.LogWarning($"Prefab for entity type {prefabItem.Key} is not properly assigned!");
            }

            game = CornClient.Instance;

        }

        void Update()
        {
            var playerPos = game!.PlayerController?.transform.position;

            foreach (var render in entities.Values)
            {
                // Call managed update
                render.ManagedUpdate(game!.GetTickMilSec());

                // Update entity set around the player
                if (playerPos is not null)
                {
                    float dist = (render.transform.position - playerPos.Value).sqrMagnitude;

                    if (dist < NEARBY_THERESHOLD_INNER)
                        nearbyEntities.Add(render.Entity.ID);
                    else if (dist > NEARBY_THERESHOLD_OUTER)
                        nearbyEntities.Remove(render.Entity.ID);
                    
                }


            }
        }

    }
}