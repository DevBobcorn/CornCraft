#nullable enable
using UnityEngine;
using Unity.Mathematics;
using TMPro;

using MinecraftClient.Inventory;
using MinecraftClient.Rendering;
using MinecraftClient.Resource;

namespace MinecraftClient.UI
{
    public class InventoryItemSlot : MonoBehaviour
    {
        [SerializeField] private GameObject? modelObject;
        [SerializeField] private TMP_Text? itemText;
        [SerializeField] private MeshFilter? itemMeshFilter;
        [SerializeField] private MeshRenderer? itemMeshRenderer;

        // Use null for empty items
        private ItemStack? itemStack = null;

        public void UpdateItemStack(ItemStack? newItemStack)
        {
            itemStack = newItemStack;
            // Update item mesh
            UpdateItemMesh();
        }

        private void UpdateItemMesh()
        {
            var result = ItemMeshBuilder.BuildItem(itemStack);

            if (result != null) // If build suceeded
            {
                itemMeshFilter!.sharedMesh = result.Value.mesh;
                itemMeshRenderer!.sharedMaterial = result.Value.material;

                // Handle GUI display transform
                bool hasGUITransform = result.Value.transforms.TryGetValue(DisplayPosition.GUI, out float3x3 t);
                // Make use of the debug text
                itemText!.text = itemStack!.Count > 1 ? itemStack.Count.ToString() : string.Empty;

                if (hasGUITransform) // Apply specified local transform
                {
                    // Apply local translation, '1' in translation field means 0.1 unit in local space, so multiply with 0.1
                    modelObject!.transform.localPosition = t.c0 * 0.1F;
                    // Apply local rotation
                    modelObject.transform.localEulerAngles = Vector3.zero;
                    // - MC ROT X
                    modelObject.transform.Rotate(Vector3.back, t.c1.x, Space.Self);
                    // - MC ROT Y
                    modelObject.transform.Rotate(Vector3.down, t.c1.y, Space.Self);
                    // - MC ROT Z
                    modelObject.transform.Rotate(Vector3.left, t.c1.z, Space.Self);
                    // Apply local scale
                    modelObject.transform.localScale = t.c2;
                }
                else // Apply uniform local transform
                {
                    // Apply local translation, set to zero
                    modelObject!.transform.localPosition = Vector3.zero;
                    // Apply local rotation
                    modelObject.transform.localEulerAngles = Vector3.zero;
                    // Apply local scale
                    modelObject.transform.localScale = Vector3.one;
                }
            }
            else // If build failed (item is empty or invalid)
            {
                itemMeshFilter!.sharedMesh = null;
                itemText!.text = string.Empty;
            }
        }
    }
}