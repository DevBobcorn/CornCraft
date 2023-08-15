#nullable enable
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using CraftSharp.Rendering;

namespace CraftSharp.UI
{
    public class FloatingUIManager : MonoBehaviour
    {
        private Dictionary<int, FloatingUI> entityFloatingUIs = new();
        [SerializeField] public AnimationCurve? UIScaleCurve;

        public void AddForEntity(int entityId, EntityRender? render)
        {
            if (render?.Entity is not null && !entityFloatingUIs.ContainsKey(entityId))
            {
                var infoTagPrefab = render.FloatingInfoPrefab;
                if (infoTagPrefab == null) return;

                // Make a new floating UI here...
                var fUIObj = GameObject.Instantiate(infoTagPrefab);
                fUIObj!.transform.SetParent(render.InfoAnchor, true);
                fUIObj!.transform.localPosition = Vector3.zero;

                var fUI = fUIObj.GetComponent<FloatingUI>();
                fUI.SetInfo(render.Entity);

                entityFloatingUIs.Add(entityId, fUI);
            }
        }

        public void RemoveForEntity(int entityId)
        {
            if (entityFloatingUIs.ContainsKey(entityId))
            {
                var target = entityFloatingUIs[entityId];

                if (target != null)
                    target.Destroy(() => entityFloatingUIs.Remove(entityId));
                else
                    entityFloatingUIs.Remove(entityId);
            }
            
        }

        void Update()
        {
            var entityManager = CornApp.CurrentClient?.EntityRenderManager;
            var validTagOwners = entityManager?.GetNearbyEntities().Keys.ToList();

            if (validTagOwners is not null)
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
                    var render = entityManager!.GetEntityRender(validTagOwners[i]);

                    if (render?.FloatingInfoPrefab != null)
                    {
                        AddForEntity(validTagOwners[i], render);
                        //Debug.Log($"Adding floating UI for #{validTagOwners[i]}");
                    }
                    
                }
            }

            var camController = CornApp.CurrentClient?.CameraController;
            var viewDir = camController?.GetViewEularAngles();
            var camPos = camController?.GetPosition();

            if (viewDir is not null && camPos is not null)
            {
                foreach (var item in entityFloatingUIs)
                {
                    var target = item.Value.transform;
                    target.eulerAngles = viewDir.Value;
                    var scale = UIScaleCurve!.Evaluate((camPos.Value - target.position).magnitude);
                    target.localScale = new(scale, scale, 1F);

                }
            }

        }

    }
}