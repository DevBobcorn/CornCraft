using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using CraftSharp.Rendering;

namespace CraftSharp.UI
{
    public class FloatingUIManager : MonoBehaviour
    {
        private readonly Dictionary<int, FloatingUI> entityFloatingUIs = new();
        public AnimationCurve UIScaleCurve;

        public void AddForEntity(int entityId, EntityRender render)
        {
            if (render != null && !entityFloatingUIs.ContainsKey(entityId))
            {
                var infoTagPrefab = render.FloatingInfoPrefab;
                if (infoTagPrefab == null) return;

                // Make a new floating UI here...
                var fUIObj = Instantiate(infoTagPrefab);
                fUIObj.transform.SetParent(render.InfoAnchor, false);

                var fUI = fUIObj.GetComponent<FloatingUI>();
                fUI.SetInfo(render);

                entityFloatingUIs.Add(entityId, fUI);
            }
        }

        public void RemoveForEntity(int entityId)
        {
            if (entityFloatingUIs.ContainsKey(entityId))
            {
                var target = entityFloatingUIs[entityId];

                if (target != null) // Delay removal
                {
                    target.Destroy(() => entityFloatingUIs.Remove(entityId));
                }
                else // Remove immediately
                {
                    entityFloatingUIs.Remove(entityId);
                }
            }
        }

        void Update()
        {
            var client = CornApp.CurrentClient;
            if (client == null) return;
            
            var entityManager = client.EntityRenderManager;
            var validTagOwners = entityManager.GetNearbyEntities().Keys.ToList();

            if (validTagOwners is not null && validTagOwners.Any())
            {
                var tagOwners = entityFloatingUIs.Keys.ToArray();

                for (int i = 0;i < tagOwners.Length;i++)
                {
                    if (!validTagOwners.Contains(tagOwners[i])) // Remove this tag
                        RemoveForEntity(tagOwners[i]);

                    validTagOwners.Remove(tagOwners[i]);
                }

                for (int i = 0;i < validTagOwners.Count;i++)
                {
                    var render = entityManager.GetEntityRender(validTagOwners[i]);

                    if (render != null && render.FloatingInfoPrefab != null)
                    {
                        AddForEntity(validTagOwners[i], render);
                        //Debug.Log($"Adding floating UI for #{validTagOwners[i]}");
                    }
                }
            }

            var camController = client.CameraController;
            var nullKeyList = new List<int>();

            foreach (var item in entityFloatingUIs)
            {
                if (item.Value == null)
                {
                    nullKeyList.Add(item.Key);
                    continue;
                }

                var target = item.Value.transform;
                target.eulerAngles = camController.GetEulerAngles();
                var dist = (camController.GetPosition() - target.position).magnitude;
                var scale = UIScaleCurve.Evaluate(dist);
                // Countervail entity render scale (support uniform scale only)
                scale *= 1F / target.transform.parent.lossyScale.x;
                target.localScale = new(scale, scale, 1F);
            }

            if (nullKeyList.Any())
            {
                foreach (var entityId in nullKeyList)
                {
                    entityFloatingUIs.Remove(entityId);
                }
            }
        }
    }
}