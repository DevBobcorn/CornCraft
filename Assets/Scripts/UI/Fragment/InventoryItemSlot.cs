using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Unity.Mathematics;
using TMPro;

using CraftSharp.Inventory;
using CraftSharp.Protocol.Handlers.StructuredComponents.Components;
using CraftSharp.Protocol.Handlers.StructuredComponents.Components.Subcomponents;
using CraftSharp.Protocol.Handlers.StructuredComponents.Core;
using CraftSharp.Protocol.Message;
using CraftSharp.Rendering;
using CraftSharp.Resource;

namespace CraftSharp.UI
{
    public class InventoryItemSlot : InventoryInteractable
    {
        private static readonly int SELECTED_HASH = Animator.StringToHash("Selected");

        [SerializeField] private GameObject modelObject;
        [SerializeField] private TMP_Text itemText;
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
        private ItemStack? currentItemStack = null;
        private bool hasVisibleItem = false;

        private bool hovered = false;
        private bool dragged = false;

        public ItemStack? GetItemStack()
        {
            return currentItemStack;
        }

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

        public Image GetPlaceholderImage()
        {
            return placeholderImage;
        }

        public void SetPlaceholderSprite(Sprite? sprite)
        {
            placeholderImage.sprite = sprite;
            
            if (hasVisibleItem)
                HidePlaceholderImage();
            else
                ShowPlaceholderImage();
        }
        
        private static readonly Color LOW_OPACITY = new(1F, 1F, 1F, 0.25F);

        public void SetSlotBorderVisibility(bool visible)
        {
            slotImage.raycastTarget = visible;
            slotImage.color = visible ? Color.white : LOW_OPACITY;
        }
        
        public static string GetItemDisplayText(ItemStack? itemStack)
        {
            if (itemStack == null || itemStack.ItemType.ItemId == Item.AIR_ID)
            {
                return string.Empty;
            }
            
            var text = new StringBuilder(GetDisplayNameOrDefault(itemStack));
            
            var rarity = itemStack.Rarity;
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
                text = new StringBuilder(TMPConverter.MC2TMP($"{colorPrefix}{text}"));
            }

            if (itemStack.IsEnchanted)
            {
                foreach (var enchantment in itemStack.Enchantments)
                {
                    var enchantmentType = EnchantmentTypePalette.INSTANCE.GetById(enchantment.EnchantmentId);
                    var levelText = ChatParser.TranslateString($"enchantment.level.{enchantment.Level}");
                    text.Append(TMPConverter.MC2TMP($"\n§7{ChatParser.TranslateString(enchantmentType.TranslationKey)} {levelText}"));
                }
            }
            
            if (itemStack.TryGetComponent<PotionContentsComponent>(
                    StructuredComponentIds.POTION_CONTENTS_ID, out var potionContentsComp))
            {
                var effectInstances = new List<MobEffectInstance>();

                // Default effects for potion
                if (potionContentsComp.HasPotionId)
                {
                    var potion = PotionPalette.INSTANCE.GetById(potionContentsComp.PotionId);
                    effectInstances.AddRange(potion.Effects);
                }

                // Custom effects overrides
                effectInstances.AddRange(potionContentsComp.CustomEffects.Select(MobEffectInstance.FromComponent));

                if (effectInstances.Count > 0)
                {
                    var effectNames = new StringBuilder();

                    foreach (var instance in effectInstances)
                    {
                        var effect = MobEffectPalette.INSTANCE.GetById(instance.EffectId);
                        var effectName = ChatParser.TranslateString(instance.EffectId.GetTranslationKey("effect"));

                        var potencyText = ChatParser.TranslateString($"potion.potency.{instance.Amplifier}");
                        if (potencyText != string.Empty)
                            effectName = ChatParser.TranslateString("potion.withAmplifier",
                                new List<string> { effectName, potencyText });

                        if (!effect.Instant)
                        {
                            var seconds = instance.Duration / 20;
                            effectName = ChatParser.TranslateString("potion.withDuration",
                                new List<string>
                                    { effectName, $"{Mathf.Min(seconds / 60, 99):D02}:{seconds % 60:D02}" });
                        }

                        effectNames.Append('\n').Append(effect.Category switch
                        {
                            MobEffectCategory.Beneficial => "§9", // Blue
                            MobEffectCategory.Harmful => "§c", // Red
                            _ => "§7" // Grey
                        }).Append(effectName);
                    }

                    text.Append(TMPConverter.MC2TMP(effectNames.ToString()));

                    var effectModifiers = new StringBuilder();
                    var hasModifiersWhenDrank = false;

                    foreach (var instance in effectInstances)
                    {
                        var effect = MobEffectPalette.INSTANCE.GetById(instance.EffectId);
                        if (effect.Modifiers.Length > 0)
                        {
                            effectModifiers.Append(getAttributeModifiers(effect.Modifiers, 1 + instance.Amplifier));
                            hasModifiersWhenDrank = true;
                        }
                    }

                    if (hasModifiersWhenDrank)
                    {
                        text.Append(TMPConverter.MC2TMP(
                            $"\n\n§5{ChatParser.TranslateString("potion.whenDrank")}{effectModifiers}"));
                    }
                }
                else // No effects
                {
                    text.Append(TMPConverter.MC2TMP($"\n§7{ChatParser.TranslateString("effect.none")}")); // Grey
                }
            }

            if (itemStack.TryGetComponent<ContainerComponent>(
                    StructuredComponentIds.CONTAINER_ID, out var containerComp) && containerComp.Items.Any())
            {
                int count = 0, remaining = containerComp.Items.Count;
                
                foreach (var containedItemStack in containerComp.Items)
                {
                    var containedItemDisplayName = GetDisplayNameOrDefault(containedItemStack);
                    text.Append('\n').Append(containedItemDisplayName).Append($" x{containedItemStack.Count}");

                    count++;
                    remaining--;
                    // In case of exactly 6 item stacks, just display the 6th instead the 'xxx more' text
                    if (count == 5 && remaining > 1)
                    {
                        break;
                    }
                }

                if (remaining > 0)
                {
                    text.Append("\n<i>").Append(ChatParser.TranslateString("container.shulkerBox.more",
                        new List<string> { remaining.ToString() })).Append("</i>");
                }
            }
            
            if (itemStack.TryGetComponent<AttributeModifiersComponent>(
                    StructuredComponentIds.ATTRIBUTE_MODIFIERS_ID, out var attributeModifiersComp) &&
                attributeModifiersComp is { ShowInTooltip: true, NumberOfModifiers: > 0 })
            {
                var groupedBySlot = attributeModifiersComp.Modifiers.GroupBy(x => x.Slot);
                
                foreach (IGrouping<EquipmentSlot, AttributeModifierSubComponent> group in groupedBySlot)
                {
                    var slotTranslationKey = $"item.modifiers.{group.Key.GetEquipmentSlotName()}";
                    text.Append(TMPConverter.MC2TMP($"\n\n§7{ChatParser.TranslateString(slotTranslationKey)}"));
                    text.Append(getAttributeModifiers(group.Select(MobAttributeModifier.FromComponent).ToArray(), 1));
                }
            }
            
            if (itemStack.TryGetComponent<BannerPatternsComponent>(
                    StructuredComponentIds.BANNER_PATTERNS_ID, out var bannerPatternsComponent))
            {
                foreach (var patternData in bannerPatternsComponent.Layers)
                {
                    // Encoded as enum int (probably as a string)
                    var colorName = patternData.DyeColor.GetName();
                    string translationKey;
                        
                    if (patternData.PatternType > 0) // Given as an id
                    {
                        var patternId = BannerPatternType.GetIdFromIndex(patternData.PatternType);
                        translationKey = $"block.{patternId.Namespace}.banner.{patternId.Path}";
                    }
                    else if (patternData.PatternType == 0) // Given as an inline definition
                    {
                        translationKey = patternData.TranslationKey!;
                    }
                    else
                    {
                        translationKey = $"block.{ResourceLocation.INVALID.Namespace}.banner.{ResourceLocation.INVALID.Path}";
                    }
                    
                    text.Append(TMPConverter.MC2TMP($"\n§7{ChatParser.TranslateString($"{translationKey}.{colorName}")}"));
                }
            }
            
            if (itemStack.Lores is not null && itemStack.Lores.Count > 0)
            {
                text.Append('\n').Append(string.Join("\n", itemStack.Lores.Select(x => x.ToString())));
            }

            /*
            if (itemStack.Components.Count > 0) // For debugging item components
            {
                text = itemStack.Components.Aggregate(text, (current, component)
                    => current.Append(TMPConverter.MC2TMP($"\n§2{component.Value}")));
            }
            */

            var itemComponentRegistry = ItemPalette.INSTANCE.ComponentRegistry;

            if (itemStack.ReceivedComponentsToAdd is not null && itemStack.ReceivedComponentsToAdd.Count > 0) // For debugging item components
            {
                text = itemStack.ReceivedComponentsToAdd.Aggregate(text, (current, pair)
                    => current.Append(TMPConverter.MC2TMP($"\n§9{itemComponentRegistry.GetIdByNumId(pair.Key)} ({pair.Value.Length} bytes)"))); // Blue
            }

            if (itemStack.ReceivedComponentsToRemove is not null && itemStack.ReceivedComponentsToRemove.Count > 0) // For debugging item components
            {
                text = itemStack.ReceivedComponentsToRemove.Aggregate(text, (current, numId)
                    => current.Append(TMPConverter.MC2TMP($"\n§c{itemComponentRegistry.GetIdByNumId(numId)}"))); // Red
            }
            
            return text.ToString();

            string getAttributeModifiers(MobAttributeModifier[] modifiers, int multiplier)
            {
                var modifierText = new StringBuilder();
                
                foreach (var modifier in modifiers)
                {
                    if (modifier.Value == 0) continue;

                    var displayCalculatedValue = itemStack.ItemType.ActionType is
                        ItemActionType.Axe or ItemActionType.Hoe or ItemActionType.Pickaxe or
                        ItemActionType.Sword or ItemActionType.Shovel or ItemActionType.Trident;
                    var opAttrName = ChatParser.TranslateString($"attribute.name.{modifier.Attribute.Path}");
                    
                    modifierText.Append('\n');
                    string opColor, opTranslationKey, opValue;

                    if (displayCalculatedValue)
                    {
                        opColor = "§2"; // Dark Green
                        opTranslationKey = $"attribute.modifier.equals.{(int)modifier.Operation}";
                        var valueCalculated = (float) modifier.Value;
                        opValue = modifier.Operation switch
                        {
                            MobAttributeModifier.Operations.AddValue => $"({valueCalculated * multiplier})",
                            _ => $"??? + {valueCalculated * multiplier * 100}"
                        };
                    }
                    else
                    {
                        opColor = modifier.Value > 0 ? "§9" : "§c"; // Blue or Red
                        opTranslationKey = modifier.Value > 0 ?
                            $"attribute.modifier.plus.{(int)modifier.Operation}" :
                            $"attribute.modifier.take.{(int)modifier.Operation}";
                        var valueAbs = (float) (modifier.Value < 0 ? -modifier.Value : modifier.Value);
                        opValue = modifier.Operation switch
                        {
                            MobAttributeModifier.Operations.AddValue => $"{Mathf.CeilToInt(valueAbs * multiplier)}", // TODO: Check rounding
                            _ => $"{valueAbs * multiplier * 100}"
                        };
                    }
                    
                    var line = ChatParser.TranslateString(opTranslationKey, new List<string> { opValue, opAttrName });

                    modifierText.Append(opColor).Append(line);
                }
                return TMPConverter.MC2TMP(modifierText.ToString());
            }
        }

        public static string GetDisplayNameOrDefault(ItemStack itemStack)
        {
            // Block items might use block translation key
            return GetDisplayName(itemStack) ??
                   (ChatParser.TryTranslateString(itemStack.ItemType.ItemId.GetTranslationKey("item"), out var translated)
                       ? translated : ChatParser.TranslateString(itemStack.ItemType.ItemId.GetTranslationKey("block")));
        }

        private static string? GetDisplayName(ItemStack itemStack)
        {
            if (itemStack.TryGetComponent<PotionContentsComponent>(
                    StructuredComponentIds.POTION_CONTENTS_ID, out var potionContentsComp2))
            {
                var baseTranslationKey = itemStack.ItemType.ItemId.GetTranslationKey("item");
                    
                if (potionContentsComp2.HasPotionId)
                {
                    var potionTranslationKey = potionContentsComp2.PotionId.Path;
                        
                    if (potionTranslationKey.StartsWith("strong_")) // Remove Enhanced (Level II) Prefix
                        potionTranslationKey = potionTranslationKey["strong_".Length..];
                        
                    if (potionTranslationKey.StartsWith("long_")) // Remove Extended Prefix
                        potionTranslationKey = potionTranslationKey["long_".Length..];
                        
                    return ChatParser.TranslateString($"{baseTranslationKey}.effect.{potionTranslationKey}");
                }
                return ChatParser.TranslateString($"{baseTranslationKey}.effect.empty"); // Uncraftable potion
            }
                
            var displayNameJson = itemStack.CustomName;
            if (string.IsNullOrEmpty(displayNameJson)) return null;
                
            var formattedName = ChatParser.ParseText(displayNameJson);
            return TMPConverter.MC2TMP($"§o{formattedName}§r"); // Make the name italic
        }

        protected override void UpdateCursorText()
        {
            cursorTextDirty = false;

            if (HintStringOverride is not null)
            {
                cursorText = HintStringOverride;
                return;
            }

            if (HintTranslationKey is not null)
            {
                cursorText = Translations.Get(HintTranslationKey);
                return;
            }

            // Update item cursor text
            cursorText = GetItemDisplayText(currentItemStack);
        }

        /// <summary>
        /// Update slot item display, and returns if item is actually changed
        /// </summary>
        public bool UpdateItemStack(ItemStack? newItemStack)
        {
            // Update item count text separately
            var oldItemStackCount = currentItemStack?.Count ?? 0;
            var newItemStackCount = newItemStack?.Count ?? 0;
            itemText.text = newItemStackCount > 1 ? newItemStackCount.ToString() : string.Empty;
            
            // Check whether item data is updated, count change is NOT considered as data change
            bool itemDataUnchanged;
            if (currentItemStack is null)
            {
                itemDataUnchanged = newItemStack is null;
            }
            else // Current item stack is not null
            {
                itemDataUnchanged = newItemStack is not null && InventoryData.CheckStackable(currentItemStack, newItemStack);
            }
            
            //Debug.Log($"{gameObject.name} Previous: {currentItemStack}, New: {newItemStack}, Data changed: {!itemDataUnchanged}");
            currentItemStack = newItemStack;

            // If no item data is changed, there's no need to update item mesh, item damage or display text
            if (itemDataUnchanged)
            {
                return newItemStackCount != oldItemStackCount;
            }
            
            // Update item mesh
            UpdateItemMesh();
            
            // Update damage bar image
            var damage = newItemStack?.Damage ?? 0;
            
            if (newItemStack is null || !newItemStack.IsDamageable || damage == 0)
            {
                if (damageBarTransform && damageBarTransform.gameObject)
                    damageBarTransform.gameObject.SetActive(false);
            }
            else
            {
                var maxDamage = (float) newItemStack.MaxDamage; // TODO: Check enchantment

                if (damageBarFillImage)
                {
                    damageBarFillImage.fillAmount = Mathf.Clamp01(1F - damage / maxDamage);
                    var hue = Mathf.Lerp(0.33333334F, 0F, damage / maxDamage);
                    damageBarFillImage.color = Color.HSVToRGB(hue, 1f, 1f);
                }
                
                if (damageBarTransform && damageBarTransform.gameObject)
                    damageBarTransform.gameObject.SetActive(true);
            }
            
            cursorTextDirty = true;
            
            return true;
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
            if (ItemMeshBuilder.BuildItemGameObject(modelObject, currentItemStack, DisplayPosition.GUI, true)) // If build succeeded
            {
                foreach (Transform t in modelObject.GetComponentsInChildren<Transform>(true))
                {
                    t.gameObject.layer = gameObject.layer; // Make sure all children are in UI layer
                }
                
                hasVisibleItem = true;
                HidePlaceholderImage();
            }
            else // If build failed (item is empty or invalid)
            {
                hasVisibleItem = false;
                ShowPlaceholderImage();
            }
        }
    }
}