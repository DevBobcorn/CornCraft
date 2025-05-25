using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        private static readonly HashSet<ResourceLocation> POTION_ITEM_IDS = new()
        {
            new("potion"),
            new("splash_potion"),
            new("lingering_potion"),
            new("tipped_arrow")
        };
        
        public static string GetItemDisplayText(ItemStack? itemStack)
        {
            if (itemStack == null || itemStack.ItemType.ItemId == Item.AIR_ID)
            {
                return string.Empty;
            }
            
            var itemId = itemStack.ItemType.ItemId;

            // Block items might use block translation key
            var text = getDisplayName() ?? ( ChatParser.TryTranslateString(itemStack.ItemType.ItemId.GetTranslationKey("item"), out var translated) ?
                translated : ChatParser.TranslateString(itemStack.ItemType.ItemId.GetTranslationKey("block")) );
            
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
                text = TMPConverter.MC2TMP($"{colorPrefix}{text}");
            }

            if (POTION_ITEM_IDS.Contains(itemId))
            {
                var effectInstances = new List<MobEffectInstance>();
                
                // Default effects for potion
                if (itemStack.NBT is not null && itemStack.NBT.TryGetValue("Potion", out var value))
                {
                    var potionId = ResourceLocation.FromString((string) value);
                    var potion = PotionPalette.INSTANCE.GetById(potionId);

                    effectInstances.AddRange(potion.Effects);
                } 
                
                // Custom effects overrides
                if (itemStack.NBT is not null && (itemStack.NBT.TryGetValue("CustomPotionEffects ", out value) ||
                                                  itemStack.NBT.TryGetValue("custom_potion_effects", out value)))
                {
                    var effectList = (object[]) value;

                    effectInstances.AddRange(effectList.Select(
                        x => GetEffectInstanceFromNBT((Dictionary<string, object>) x)) );
                }

                if (effectInstances.Count > 0)
                {
                    var effectNames = new StringBuilder();
                    
                    foreach (var instance in effectInstances)
                    {
                        var effect = MobEffectPalette.INSTANCE.GetById(instance.EffectId);
                        var effectName = ChatParser.TranslateString(instance.EffectId.GetTranslationKey("effect"));
                        
                        var potencyText = ChatParser.TranslateString($"potion.potency.{instance.Amplifier}");
                        if (potencyText != string.Empty)
                            effectName = ChatParser.TranslateString("potion.withAmplifier", new List<string> { effectName, potencyText });
                        
                        if (!effect.Instant)
                        {
                            var seconds = instance.Duration / 20;
                            effectName = ChatParser.TranslateString("potion.withDuration",
                                new List<string> { effectName, $"{Mathf.Min(seconds / 60, 99):D02}:{seconds % 60:D02}" });
                        }

                        effectNames.Append('\n');
                        effectNames.Append(effect.Category switch
                        {
                            MobEffectCategory.Beneficial => "§9", // Blue
                            MobEffectCategory.Harmful => "§c", // Red
                            _ => "§7" // Grey
                        });
                        effectNames.Append(effectName);
                    }
                    text += TMPConverter.MC2TMP(effectNames.ToString());
                    
                    var effectModifiers = new StringBuilder();
                    var hasModifiers = false;

                    foreach (var instance in effectInstances)
                    {
                        var effect = MobEffectPalette.INSTANCE.GetById(instance.EffectId);
                        if (effect.Modifiers.Length > 0)
                        {
                            foreach (var modifier in effect.Modifiers)
                            {
                                effectModifiers.Append('\n');
                                var opTranslationKey = modifier.Value switch
                                {
                                    > 0 => $"attribute.modifier.plus.{(int) modifier.Operation}",
                                    < 0 => $"attribute.modifier.take.{(int) modifier.Operation}",
                                    _ => $"attribute.modifier.equals.{(int) modifier.Operation}"
                                };
                                var opValue = modifier.Operation switch
                                {
                                    MobAttributeModifier.Operations.AddValue => ((int)(modifier.Value * (1 + instance.Amplifier))).ToString(),
                                    _ => ((int) (modifier.Value * (1 + instance.Amplifier) * 100)).ToString()
                                };
                                var opAttrName = ChatParser.TranslateString($"attribute.name.{modifier.Attribute}");
                                effectModifiers.Append(ChatParser.TranslateString(opTranslationKey, new List<string> { opValue, opAttrName }));
                            }
                            
                            hasModifiers = true;
                        }
                    }

                    if (hasModifiers)
                    {
                        text += TMPConverter.MC2TMP($"\n\n{ChatParser.TranslateString("potion.whenDrank")}{effectModifiers}");
                    }
                }
                else // No effects
                {
                    text += TMPConverter.MC2TMP($"\n§7{ChatParser.TranslateString("effect.none")}"); // Grey
                }
            }
                
            if (itemStack.Lores is not null && itemStack.Lores.Length > 0)
                text += '\n' + string.Join("\n", itemStack.Lores.Select(x => x.ToString()));
                
            return text;

            string? getDisplayName()
            {
                if (POTION_ITEM_IDS.Contains(itemId))
                {
                    // Check potion NBTs https://minecraft.wiki/w/Item_format/Before_1.20.5#Potion_Effects
                    var baseTranslationKey = itemStack.ItemType.ItemId.GetTranslationKey("item");
                    
                    if (itemStack.NBT is not null && itemStack.NBT.TryGetValue("Potion", out var value))
                    {
                        var potionId = ResourceLocation.FromString((string) value);
                        var potionTranslationKey = potionId.Path;
                        
                        if (potionTranslationKey.StartsWith("strong_")) // Remove Enhanced (Level II) Prefix
                            potionTranslationKey = potionTranslationKey.Remove(0, "strong_".Length);
                        
                        if (potionTranslationKey.StartsWith("long_")) // Remove Extended Prefix
                            potionTranslationKey = potionTranslationKey.Remove(0, "long_".Length);
                        
                        return ChatParser.TranslateString($"{baseTranslationKey}.effect.{potionTranslationKey}");
                    }
                    return ChatParser.TranslateString($"{baseTranslationKey}.effect.empty"); // Uncraftable potion
                }
                
                var displayNameJson = itemStack.DisplayName;
                if (string.IsNullOrEmpty(displayNameJson)) return null;
                
                var formattedName = ChatParser.ParseText(displayNameJson);
                return TMPConverter.MC2TMP($"§o{formattedName}§r"); // Make the name italic
            }

            static MobEffectInstance GetEffectInstanceFromNBT(Dictionary<string, object> x)
            {
                var effectId = ResourceLocation.FromString((string) x["id"]);
                int amplifier = x.TryGetValue("amplifier", out var value) ? (int) value : 0; // 0 (Level I) by default
                int duration = x.TryGetValue("duration", out value) ? (int) value : 1; // 1 tick by default

                return new MobEffectInstance(effectId, amplifier, duration, false, true, true);
            };
        }

        protected override void UpdateCursorText()
        {
            cursorTextDirty = false;

            if (HintTranslationKey is not null)
            {
                cursorText = Translations.Get(HintTranslationKey);
                return;
            }

            // Update item cursor text
            cursorText = GetItemDisplayText(itemStack);
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
            
            if (_slotAnimator) // For hotbar slots
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

        public void SetPointerUpHandler(Action<PointerEventData.InputButton> handler)
        {
            pointerUpHandler = handler;
        }
        
        public void SetPointerDownHandler(Action<PointerEventData.InputButton> handler)
        {
            pointerDownHandler = handler;
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