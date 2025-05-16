using System;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Unity.Mathematics;
using TMPro;

using CraftSharp.Rendering;
using CraftSharp.Resource;
using CraftSharp.Protocol;

namespace CraftSharp.UI
{
    public class InventoryItemSlot : InventoryInteractable
    {
        private static readonly int SELECTED_HASH = Animator.StringToHash("Selected");

        [SerializeField] private GameObject modelObject;
        [SerializeField] private TMP_Text itemText;
        [SerializeField] private MeshFilter itemMeshFilter;
        [SerializeField] private MeshRenderer itemMeshRenderer;
        [SerializeField] private Transform slotCenterRef;
        [SerializeField] private Sprite hoveredSprite;
        [SerializeField] private Sprite draggedSprite;
        [SerializeField] private Sprite disabledSprite;
        [SerializeField] private TMP_Text keyHintText;
        [SerializeField] private Image placeholderImage;
        [SerializeField] private Image slotImage;
        
        [SerializeField] private RectTransform damageBarTransform;
        [SerializeField] private Image damageBarFillImage;

        [SerializeField] private float fullItemScale = 60F;

        private Animator _slotAnimator;

        #nullable enable

        // Use null for empty items
        private ItemStack? itemStack = null;
        private bool hasVisibleItem = false;

        private bool hovered = false;
        private bool dragged = false;

        public bool Dragged
        {
            get => dragged;
            set
            {
                dragged = value;
                slotImage.overrideSprite = _enabled ? dragged ? draggedSprite : hovered || _selected ? hoveredSprite : null : disabledSprite;
            }
        }

        public override bool Selected
        {
            get => _selected;
            set
            {
                _selected = value;
                slotImage.overrideSprite = _enabled ? dragged ? draggedSprite : hovered || _selected ? hoveredSprite : null : disabledSprite;
            }
        }

        private void Awake()
        {
            _slotAnimator = GetComponent<Animator>();
        }

        public void SetKeyHint(string keyHint)
        {
            keyHintText.text = keyHint;
        }

        public void SetPlaceholderSprite(Sprite? sprite)
        {
            placeholderImage.sprite = sprite;
            
            if (hasVisibleItem)
                HidePlaceholderImage();
            else
                ShowPlaceholderImage();
        }

        protected override void UpdateCursorText()
        {
            cursorTextDirty = false;

            // Update item cursor text
            if (itemStack == null || itemStack.ItemType.ItemId == Item.AIR_ID)
            {
                cursorText = string.Empty;
            }
            else
            {
                // Block items might use block translation key
                cursorText = ChatParser.TryTranslateString(itemStack.ItemType.ItemId.GetTranslationKey("item"), out var translated) ?
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
                    cursorText = TMPConverter.MC2TMP($"{colorPrefix}{cursorText}");
                }
                
                if (itemStack.Lores is not null && itemStack.Lores.Length > 0)
                    cursorText += '\n' + string.Join("\n", itemStack.Lores.Select(x => x.ToString()));
            }
        }

        public void UpdateItemStack(ItemStack? newItemStack)
        {
            itemStack = newItemStack;
            var newItemType = newItemStack?.ItemType ?? Item.NULL;
            
            // Update item mesh
            UpdateItemMesh();
            
            // Update damage bar image
            var damage = newItemStack?.Damage ?? 0;
            
            if (!newItemType.IsDepletable || damage == 0)
            {
                damageBarTransform.gameObject.SetActive(false);
            }
            else
            {
                
                var maxDamage = (float) newItemType.MaxDurability; // TODO: Check enchantment
                
                damageBarFillImage.fillAmount = Mathf.Clamp01(1F - damage / maxDamage);
                var hue = Mathf.Lerp(0.33333334F, 0F, damage / maxDamage);
                damageBarFillImage.color = Color.HSVToRGB(hue, 1f, 1f);
                
                damageBarTransform.gameObject.SetActive(true);
            }
            
            cursorTextDirty = true;
        }

        #nullable disable

        public void SetSlotItemScale(float scale)
        {
            slotCenterRef.transform.localScale = new Vector3(scale, scale, scale) * fullItemScale;
        }

        public void SlotPointerEnter()
        {
            hovered = true;
            slotImage.overrideSprite = Enabled ? Dragged || Selected ? draggedSprite : hoveredSprite : disabledSprite;
            
            if (_slotAnimator) // For hotbar slots
                _slotAnimator.SetBool(SELECTED_HASH, true);
            
            if (cursorTextDirty)
            {
                // Update only when needed
                UpdateCursorText();
            }
            
            cursorTextHandler?.Invoke(cursorText);
            hoverHandler?.Invoke();
        }

        public void SlotPointerExit()
        {
            hovered = false;
            slotImage.overrideSprite = Enabled ? Dragged || Selected ? draggedSprite : null : disabledSprite;
            
            if (_slotAnimator)
                _slotAnimator.SetBool(SELECTED_HASH, false);
            
            cursorTextHandler?.Invoke(string.Empty);
        }

        private void ShowPlaceholderImage()
        {
            if (!placeholderImage || !placeholderImage.sprite) return;
            placeholderImage.gameObject.SetActive(true);
        }
        
        private void HidePlaceholderImage()
        {
            if (!placeholderImage) return;
            placeholderImage.gameObject.SetActive(false);
        }
        
        private Action<PointerEventData.InputButton> pointerUpHandler;
        private Action<PointerEventData.InputButton> pointerDownHandler;
        private Action hoverHandler;

        public void SetPointerUpHandler(Action<PointerEventData.InputButton> handler)
        {
            pointerUpHandler = handler;
        }
        
        public void SetPointerDownHandler(Action<PointerEventData.InputButton> handler)
        {
            pointerDownHandler = handler;
        }
        
        public void SetHoverHandler(Action handler)
        {
            hoverHandler = handler;
        }
        
        public void SlotPointerDown(BaseEventData data)
        {
            if (data is PointerEventData pointerData)
            {
                pointerDownHandler?.Invoke(pointerData.button);
            }
            else
            {
                Debug.LogWarning("Event data is not pointer data!");
            }
        }
        
        public void SlotPointerUp(BaseEventData data)
        {
            if (data is PointerEventData pointerData)
            {
                pointerUpHandler?.Invoke(pointerData.button);
            }
            else
            {
                Debug.LogWarning("Event data is not pointer data!");
            }
        }

        private void UpdateItemMesh()
        {
            var result = ItemMeshBuilder.BuildItem(itemStack, true);

            if (result != null) // If build succeeded
            {
                itemMeshFilter.sharedMesh = result.Value.mesh;
                itemMeshRenderer.sharedMaterial = result.Value.material;

                // Handle GUI display transform
                bool hasGUITransform = result.Value.transforms.TryGetValue(DisplayPosition.GUI, out float3x3 t);
                // Make use of the debug text
                itemText.text = itemStack!.Count > 1 ? itemStack.Count.ToString() : string.Empty;

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
                
                hasVisibleItem = true;
                HidePlaceholderImage();
            }
            else // If build failed (item is empty or invalid)
            {
                itemMeshFilter.sharedMesh = null;
                itemText.text = string.Empty;

                hasVisibleItem = false;
                ShowPlaceholderImage();
            }
        }
    }
}