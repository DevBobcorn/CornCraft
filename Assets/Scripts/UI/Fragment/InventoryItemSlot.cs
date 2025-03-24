using UnityEngine;
using UnityEngine.UI;
using Unity.Mathematics;
using TMPro;

using CraftSharp.Rendering;
using CraftSharp.Resource;

namespace CraftSharp.UI
{
    public class InventoryItemSlot : MonoBehaviour
    {
        private static readonly int SELECTED_HASH = Animator.StringToHash("Selected");

        [SerializeField] private GameObject modelObject;
        [SerializeField] private TMP_Text itemText;
        [SerializeField] private MeshFilter itemMeshFilter;
        [SerializeField] private MeshRenderer itemMeshRenderer;
        [SerializeField] private Transform slotCenterRef;
        [SerializeField] private Sprite selectedSprite;
        [SerializeField] private TMP_Text keyHintText;

        [SerializeField] private float fullItemScale = 60F;

        private Animator _slotAnimator;
        private Image _slotImage;

        #nullable enable

        // Use null for empty items
        private ItemStack? itemStack = null;

        private void Awake()
        {
            _slotAnimator = GetComponent<Animator>();
            _slotImage = GetComponent<Image>();
        }

        public void SetKeyHint(string keyHint)
        {
            keyHintText.text = keyHint;
        }

        public void UpdateItemStack(ItemStack? newItemStack)
        {
            itemStack = newItemStack;
            // Update item mesh
            UpdateItemMesh();
            // TODO: Visually emphasize the change
        }

        #nullable disable

        public void SetSlotItemScale(float scale)
        {
            slotCenterRef.transform.localScale = new Vector3(scale, scale, scale) * fullItemScale;
        }

        public void SelectSlot()
        {
            _slotImage.overrideSprite = selectedSprite;
            _slotAnimator.SetBool(SELECTED_HASH, true);
        }

        public void DeselectSlot()
        {
            _slotImage.overrideSprite = null;
            _slotAnimator.SetBool(SELECTED_HASH, false);
        }

        private void UpdateItemMesh()
        {
            var result = ItemMeshBuilder.BuildItem(itemStack, true);

            if (result != null) // If build suceeded
            {
                itemMeshFilter.sharedMesh = result.Value.mesh;
                itemMeshRenderer.sharedMaterial = result.Value.material;

                // Handle GUI display transform
                bool hasGUITransform = result.Value.transforms.TryGetValue(DisplayPosition.GUI, out float3x3 t);
                // Make use of the debug text
                itemText.text = itemStack.Count > 1 ? itemStack.Count.ToString() : string.Empty;

                if (hasGUITransform) // Apply specified local transform
                {
                    // Apply local translation, '1' in translation field means 0.1 unit in local space, so multiply with 0.1
                    modelObject.transform.localPosition = t.c0 * 0.1F;
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
                    modelObject.transform.localPosition = Vector3.zero;
                    // Apply local rotation
                    modelObject.transform.localEulerAngles = Vector3.zero;
                    // Apply local scale
                    modelObject.transform.localScale = Vector3.one;
                }
            }
            else // If build failed (item is empty or invalid)
            {
                itemMeshFilter.sharedMesh = null;
                itemText.text = string.Empty;
            }
        }
    }
}