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

        private void AddForEntity(int entityId, EntityRender render)
        {
            if (!render || entityFloatingUIs.ContainsKey(entityId)) return;
            
            var infoTagPrefab = render.FloatingInfoPrefab;
            if (!infoTagPrefab) return;

            // Make a new floating UI here...
            var fUIObj = Instantiate(infoTagPrefab, render.InfoAnchor, false);

            var fUI = fUIObj.GetComponent<FloatingUI>();
            fUI.SetInfo(render);

            entityFloatingUIs.Add(entityId, fUI);
        }

        public void RemoveForEntity(int entityId)
        {
            if (entityFloatingUIs.ContainsKey(entityId))
            {
                var target = entityFloatingUIs[entityId];

                if (target) // Delay removal
                {
                    target.Destroy(() => entityFloatingUIs.Remove(entityId));
                }
                else // Remove immediately
                {
                    entityFloatingUIs.Remove(entityId);
                }
            }
        }

        private void Update()
        {
            var client = CornApp.CurrentClient;
            if (!client) return;
            
            var entityManager = client.EntityRenderManager;
            var validTagOwners = entityManager.GetNearbyEntityIds().Keys.ToList();

            if (validTagOwners.Any())
            {
                var prevTagOwners = entityFloatingUIs.Keys.ToArray();

                foreach (var entityId in prevTagOwners)
                {
                    if (!validTagOwners.Contains(entityId)) // Remove this tag
                        RemoveForEntity(entityId);

                    validTagOwners.Remove(entityId);
                }

                foreach (var entityId in validTagOwners)
                {
                    var render = entityManager.GetEntityRender(entityId);

                    if (render && render.FloatingInfoPrefab)
                    {
                        AddForEntity(entityId, render);
                        //Debug.Log($"Adding floating UI for #{validTagOwners[i]}");
                    }
                }
            }

            var camController = client.CameraController;
            var nullKeyList = new List<int>();

            foreach (var item in entityFloatingUIs)
            {
                if (!item.Value)
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
                target.localScale = new Vector3(scale, scale, 1F);
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