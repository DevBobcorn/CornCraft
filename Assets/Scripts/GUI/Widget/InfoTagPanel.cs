#nullable enable
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using MinecraftClient.Rendering;

namespace MinecraftClient.UI
{
    public class InfoTagPanel : MonoBehaviour
    {
        private static readonly Vector3 HIDDEN_POS = new(0F, 0F, -1000F);
        private CornClient? game;
        private Dictionary<int, EntityInfoTag> infoTags = new();

        [SerializeField] public GameObject? npcInfoTagPrefab;
        [SerializeField] public GameObject? monsterInfoTagPrefab;

        [SerializeField] public AnimationCurve? tagScaleCurve;

        private GameObject? GetTagPrefab(EntityInfoTagType type)
        {
            return type switch
            {
                EntityInfoTagType.Monster => monsterInfoTagPrefab,
                EntityInfoTagType.NPC     => npcInfoTagPrefab,

                _                         => null
            };
        }

        public void AddTagInfo(int entityId, EntityRender? render)
        {
            if (render is not null && !infoTags.ContainsKey(entityId))
            {
                var infoTagPrefab = GetTagPrefab(EntityRenderManager.Instance.GetInfoTagTypeForType(render.Entity.Type.EntityId));

                if (infoTagPrefab is null)
                    return;

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
            var entityManager = game!.EntityRenderManager;

            // Add or remove info tags
            var tagOwners = infoTags.Keys.ToArray();
            var validTagOwners = entityManager?.GetNearbyEntities().ToList();

            if (validTagOwners is not null)
            {
                for (int i = 0;i < tagOwners.Length;i++)
                {
                    if (!validTagOwners.Contains(tagOwners[i])) // Remove this tag
                        RemoveTagInfo(tagOwners[i]);

                    validTagOwners.Remove(tagOwners[i]);
                }

                for (int i = 0;i < validTagOwners.Count;i++)
                {
                    AddTagInfo(validTagOwners[i], entityManager!.GetEntityRender(validTagOwners[i]));
                }
            }

            var camController = game!.CameraController;

            if (camController?.GetPosition() is not null) // Update existing info tags
            {
                var camPos = camController.GetPosition()!.Value;

                foreach (var pair in infoTags)
                {
                    var entity = entityManager!.GetEntityRender(pair.Key);

                    if (entity is null) // This entity is no longer there, also remove its tag
                    {
                        RemoveTagInfo(pair.Key);
                        continue;
                    }

                    pair.Value.UpdateInfo();

                    var tagTransform = pair.Value.transform;

                    // Update tag position
                    var screenPos = camController.GetTransfromScreenPos(entity.InfoAnchor);
                    tagTransform.position = screenPos ?? HIDDEN_POS;
                    
                    var scale = tagScaleCurve!.Evaluate(Vector3.Distance(camPos, entity.InfoAnchor.position));
                    tagTransform.localScale = new(scale, scale, scale);

                }
            }

        }

    }
}