using System;
using UnityEngine;
using UnityEngine.UI;
using Unity.Mathematics;
using TMPro;

using CraftSharp.Rendering;
using CraftSharp.Resource;
using CraftSharp.Protocol;
using System.Linq;

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
        private string _slotCursorText = string.Empty;

        #nullable enable

        // Use null for empty items
        private ItemStack? itemStack = null;
        private bool cursorTextDirty = false;

        private void Awake()
        {
            _slotAnimator = GetComponent<Animator>();
            _slotImage = GetComponent<Image>();
        }

        public void SetKeyHint(string keyHint)
        {
            keyHintText.text = keyHint;
        }

        private void UpdateCursorText()
        {
            cursorTextDirty = false;

            // Update item cursor text
            if (itemStack == null || itemStack.ItemType.ItemId == Item.AIR_ID)
            {
                _slotCursorText = string.Empty;
            }
            else
            {
                // Block items might use block translation key
                _slotCursorText = ChatParser.TryTranslateString(itemStack.ItemType.ItemId.GetTranslationKey("item"), out var translated) ?
                    translated : ChatParser.TranslateString(itemStack.ItemType.ItemId.GetTranslationKey("block"));
                
                // TODO: Also check item enchantments
                var rarity = itemStack.ItemType.Rarity;
                
                if (rarity != ItemRarity.Common)
                {
                    var colorPrefix = rarity switch
                    {
                        ItemRarity.Uncommon => "§e", // Yellow
                        ItemRarity.Rare => "§b", // Aqua
                        ItemRarity.Epic => "§d", // Light Purple
                        _ => string.Empty
                    };
                    // Make sure TMP color tag is closed
                    _slotCursorText = TMPConverter.MC2TMP($"{colorPrefix}{_slotCursorText}");
                }
                
                if (itemStack.Lores is not null && itemStack.Lores.Length > 0)
                    _slotCursorText += '\n' + string.Join("\n", itemStack.Lores.Select(x => x.ToString()));
            }
        }

        public void UpdateItemStack(ItemStack? newItemStack)
        {
            itemStack = newItemStack;
            // Update item mesh
            UpdateItemMesh();
            
            cursorTextDirty = true;
        }

        #nullable disable

        public void SetSlotItemScale(float scale)
        {
            slotCenterRef.transform.localScale = new Vector3(scale, scale, scale) * fullItemScale;
        }

        public void SelectSlot()
        {
            _slotImage.overrideSprite = selectedSprite;
            if (_slotAnimator)
                _slotAnimator.SetBool(SELECTED_HASH, true);
            
            if (cursorTextDirty)
            {
                // Update only when needed
                UpdateCursorText();
            }
            
            cursorTextHandler?.Invoke(_slotCursorText);
        }

        public void DeselectSlot()
        {
            _slotImage.overrideSprite = null;
            if (_slotAnimator)
                _slotAnimator.SetBool(SELECTED_HASH, false);
            
            cursorTextHandler?.Invoke(string.Empty);
        }

        private Action clickHandler;
        private Action<string> cursorTextHandler;

        public void SetClickHandler(Action handler)
        {
            clickHandler = handler;
        }

        public void SetCursorTextHandler(Action<string> handler)
        {
            cursorTextHandler = handler;
        }

        public void ClickSlot()
        {
            clickHandler?.Invoke();
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