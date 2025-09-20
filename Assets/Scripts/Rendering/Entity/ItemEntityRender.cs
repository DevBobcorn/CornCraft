using System.Collections.Generic;
using CraftSharp.Resource;
using UnityEngine;

namespace CraftSharp.Rendering
{
    public class ItemEntityRender : EntityRender
    {
        public GameObject ItemObject;
        
        #nullable enable

        private ItemStack? currentItemStack;

        public ItemStack? GetItemStack()
        {
            return currentItemStack;
        }

        public override void UpdateMetadata(Dictionary<int, object?> updatedMeta)
        {
            base.UpdateMetadata(updatedMeta);

            // Update entity item
            if (Type.MetaSlotByName.TryGetValue("data_item", out var metaSlot1)
                && Type.MetaEntries[metaSlot1].DataType == EntityMetadataType.Slot)
            {
                if (Metadata!.TryGetValue(metaSlot1, out var value) && value is ItemStack itemStack)
                {
                    currentItemStack = itemStack;

                    if (ItemObject)
                    {
                        if (ItemMeshBuilder.BuildItemGameObject(ItemObject, itemStack, DisplayPosition.Ground, false)) // If built object is not empty
                        {
                            var objTransform = ItemObject.transform;
                            objTransform.localScale = Vector3.one;
                            objTransform.localPosition = new Vector3(0F, 0.75F, 0F);
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Item entity prefab object not assigned!");
                    }
                }
            }
        }

        private void Update()
        {
            var client = CornApp.CurrentClient;
            if (!client) return;

            var cameraTransform = client.CameraController.RenderCamera.transform;
            
            Vector3 directionToTarget = transform.position - cameraTransform.position;
            // Ignore vertical distance
            directionToTarget.y = 0;
            
            if (directionToTarget != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                VisualTransform.localRotation = Quaternion.AngleAxis(90F, Vector3.up) * targetRotation;
            }
        }
    }
}