using System.Collections.Generic;
using UnityEngine;

namespace CraftSharp.Rendering
{
    public class ItemEntityRender : EntityRender
    {
        public MeshFilter itemMeshFilter;
        public MeshRenderer itemMeshRenderer;
        
        #nullable enable

        private ItemStack? EntityItemStack;

        public override void UpdateMetadata(Dictionary<int, object?> updatedMeta)
        {
            base.UpdateMetadata(updatedMeta);

            // Update entity item
            if (Type.MetaSlotByName.TryGetValue("data_item", out var metaSlot1)
                && Type.MetaEntries[metaSlot1].DataType == EntityMetaDataType.Slot)
            {
                if (Metadata!.TryGetValue(metaSlot1, out var value) && value is ItemStack item)
                {
                    EntityItemStack = item;

                    if (itemMeshFilter && itemMeshRenderer)
                    {
                        var result = ItemMeshBuilder.BuildItem(item, false);

                        if (result != null) // If build succeeded
                        {
                            itemMeshFilter.sharedMesh = result.Value.mesh;
                            itemMeshRenderer.sharedMaterial = result.Value.material;

                            // Apply random rotation
                            var meshTransform = itemMeshRenderer.transform;
                            meshTransform.localEulerAngles = new(0F, (NumeralId * 350F) % 360F, 0F);
                            meshTransform.localScale = Vector3.one * 2F;
                            meshTransform.localPosition = new(0F, 0.75F, 0F);
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Item entity prefab components not assigned!");
                    }
                }
            }
        }
    }
}