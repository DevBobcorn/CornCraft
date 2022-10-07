#nullable enable
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using MinecraftClient.Mapping;

namespace MinecraftClient.Rendering
{
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

        private static Dictionary<EntityType, GameObject?> entityPrefabs = new();

        private static GameObject? GetPrefabForType(EntityType type) => entityPrefabs.GetValueOrDefault(type, placeboEntityPrefab);

        private Dictionary<int, EntityRender> entities = new();

        public string GetDebugInfo()
        {
            return $"Ent: {entities.Count}";
        }

        public void AddEntity(Entity entity)
        {
            var entityPrefab = GetPrefabForType(entity.Type);

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
            }
        }

        public void MoveEntity(int entityId, Location location)
        {
            if (entities.ContainsKey(entityId))
                entities[entityId].MoveTo(CoordConvert.MC2Unity(location));
            
        }

        public void RotateEntity(int entityId, float yaw, float pitch)
        {
            if (entities.ContainsKey(entityId))
                entities[entityId].RotateTo(AngleConvert.MCYaw2Unity(yaw), AngleConvert.MC2Unity(pitch));
            
        }

        public void UnloadEntities()
        {
            entities.Clear();

            // Reset instance
            instance = null;
        }

        public void ReloadEntities()
        {
            var ids = entities.Keys.ToArray();

            foreach (var id in ids)
                entities[id].Unload();

            entities.Clear();

        }

        void Start()
        {
            placeboEntityPrefab = Resources.Load<GameObject>("Prefabs/Entity/Placebo Entity");

            // Clear loaded things
            entityPrefabs.Clear();

            // Add specific entity prefabs TODO Expand
            entityPrefabs.Add(EntityType.Skeleton, Resources.Load<GameObject>("Prefabs/Entity/Zombie/Zombie Entity"));
            entityPrefabs.Add(EntityType.Stray, Resources.Load<GameObject>("Prefabs/Entity/Zombie/Zombie Entity"));

            entityPrefabs.Add(EntityType.Zombie, Resources.Load<GameObject>("Prefabs/Entity/Zombie/Zombie Entity"));
            entityPrefabs.Add(EntityType.Husk, Resources.Load<GameObject>("Prefabs/Entity/Zombie/Husk Entity"));
            entityPrefabs.Add(EntityType.Drowned, Resources.Load<GameObject>("Prefabs/Entity/Zombie/Drowned Entity"));

            entityPrefabs.Add(EntityType.Pig, Resources.Load<GameObject>("Prefabs/Entity/Pig Entity"));
            entityPrefabs.Add(EntityType.Cow, Resources.Load<GameObject>("Prefabs/Entity/Pig Entity"));
            entityPrefabs.Add(EntityType.Sheep, Resources.Load<GameObject>("Prefabs/Entity/Pig Entity"));
            entityPrefabs.Add(EntityType.Goat, Resources.Load<GameObject>("Prefabs/Entity/Pig Entity"));

            foreach (var prefabItem in entityPrefabs)
            {
                if (prefabItem.Value is null)
                    Debug.LogWarning($"Prefab for entity type {prefabItem.Key} is not properly assigned!");
            }

            game = CornClient.Instance;

        }

        private float tickMilSec;

        void Update()
        {
            tickMilSec = (float)(1D / game!.GetServerTPS());

            // Call managed update
            var cameraPos = game!.GetCameraPosition();

            foreach (var entity in entities.Values)
                entity.ManagedUpdate(cameraPos, tickMilSec);

        }

    }
}