#nullable enable
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using MinecraftClient.Rendering;

namespace MinecraftClient.UI
{
    public class InfoTagPanel : MonoBehaviour
    {
        [SerializeField] private GameObject? infoTagPrefab;

        private CornClient? game;
        private Dictionary<int, EntityInfoTag> infoTags = new();

        public void AddTagInfo(int entityId, EntityRender? render)
        {
            if (render is not null && !infoTags.ContainsKey(entityId))
            {
                // Make a new notification here...
                var infoTagObj = GameObject.Instantiate(infoTagPrefab);
                infoTagObj!.transform.SetParent(transform, true);
                infoTagObj!.transform.localScale = Vector3.one;

                var infoTag = infoTagObj.GetComponent<EntityInfoTag>();

                infoTag.SetInfo(this, entityId, render);

                infoTags.Add(entityId, infoTag);
            }
        }

        public void RemoveTagInfo(int entityId)
        {
            if (infoTags.ContainsKey(entityId))
                infoTags[entityId].Remove();
            
        }

        public void ExpireTagInfo(int entityId)
        {
            if (infoTags.ContainsKey(entityId))
                infoTags.Remove(entityId);
            
        }

        void Start()
        {
            // First get the game instance
            game = CornClient.Instance;

            infoTags.Clear();

        }

        void Update()
        {
            var entityManager = game!.EntityManager;

            // Add or remove info tags
            var tagOwners = infoTags.Keys.ToArray();
            var validTagOwners = entityManager?.GetNearbyEntities().ToList();

            if (validTagOwners is not null)
            {
                for (int i = 0;i < tagOwners.Length;i++)
                {
                    if (!validTagOwners.Contains(tagOwners[i]))
                    {
                        // Remove this tag
                        RemoveTagInfo(tagOwners[i]);

                    }

                    validTagOwners.Remove(tagOwners[i]);
                }

                for (int i = 0;i < validTagOwners.Count;i++)
                {
                    AddTagInfo(validTagOwners[i], entityManager!.GetEntity(validTagOwners[i]));
                }
            }

            // Update existing info tags
            foreach (var pair in infoTags)
            {
                var entity = entityManager!.GetEntity(pair.Key);

                if (entity is null) // This entity is no longer there, also remove its tag
                {
                    RemoveTagInfo(pair.Key);
                    continue;
                }

                // Update tag position
                pair.Value.transform.position = 
                        game!.CameraController!.GetTransfromScreenPos(entity.InfoAnchor);
            }

        }

    }
}