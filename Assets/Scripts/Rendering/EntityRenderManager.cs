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

    public class EntityRenderManager : MonoBehaviour
    {
        private static EntityRenderManager? instance;
        public static EntityRenderManager Instance
        {
            get {
                if (instance is null)
                {
                    instance = Component.FindObjectOfType<EntityRenderManager>();
                }
                
                return instance;
            }
        }

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

        private CornClient? game;

        private readonly Dictionary<EntityType, GameObject?> entityPrefabs = new();
        private readonly Dictionary<EntityType, EntityInfoTagType> infoTagTypes = new();

        private GameObject? GetPrefabForType(EntityType type) => entityPrefabs.GetValueOrDefault(type, defaultPrefab);
        public EntityInfoTagType GetInfoTagTypeForType(EntityType type) =>
                infoTagTypes.GetValueOrDefault(type, EntityInfoTagType.None);

        private readonly Dictionary<int, EntityRender> entities = new();
        private readonly HashSet<int> nearbyEntities = new();

        private const float NEARBY_THERESHOLD_INNER = 100F;
        private const float NEARBY_THERESHOLD_OUTER = 128F;

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
            game = CornClient.Instance;

            // Clear loaded things
            entityPrefabs.Clear();
            infoTagTypes.Clear();

            // Register entity render prefabs ===========================================
            // Hostile Mobs
            entityPrefabs.Add(EntityType.Skeleton,         skeletonPrefab);
            entityPrefabs.Add(EntityType.WitherSkeleton,   witherSkeletonPrefab);
            entityPrefabs.Add(EntityType.Stray,            strayPrefab);
            entityPrefabs.Add(EntityType.Zombie,           zombiePrefab);
            entityPrefabs.Add(EntityType.Husk,             huskPrefab);
            entityPrefabs.Add(EntityType.Drowned,          drownedPrefab);
            entityPrefabs.Add(EntityType.Creeper,          creeperPrefab);
            // ...
            // Passive Mobs
            entityPrefabs.Add(EntityType.Pig,              pigPrefab);
            entityPrefabs.Add(EntityType.Cow,              cowPrefab);
            entityPrefabs.Add(EntityType.Mooshroom,        mooshroomPrefab);
            entityPrefabs.Add(EntityType.Sheep,            sheepPrefab);
            // Neutral Mobs
            // ...
            // Miscellaneous Entities
            entityPrefabs.Add(EntityType.Item,             itemPrefab);
            // ...

            // Register entity info tag types ===========================================
            infoTagTypes.Add(EntityType.Skeleton,         EntityInfoTagType.Monster);
            infoTagTypes.Add(EntityType.WitherSkeleton,   EntityInfoTagType.Monster);
            infoTagTypes.Add(EntityType.Stray,            EntityInfoTagType.Monster);
            infoTagTypes.Add(EntityType.Zombie,           EntityInfoTagType.Monster);
            infoTagTypes.Add(EntityType.Husk,             EntityInfoTagType.Monster);
            infoTagTypes.Add(EntityType.Drowned,          EntityInfoTagType.Monster);
            infoTagTypes.Add(EntityType.Creeper,          EntityInfoTagType.Monster);
            // ...
            // Passive Mobs
            infoTagTypes.Add(EntityType.Villager, EntityInfoTagType.NPC);
            // ...
            // Neutral Mobs
            // ...
            // Miscellaneous Entities
            // ...

            foreach (var prefabItem in entityPrefabs)
            {
                if (prefabItem.Value is null)
                    Debug.LogWarning($"Prefab for entity type {prefabItem.Key} is not assigned!");
                
            }

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