using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

using CraftSharp.Event;
using CraftSharp.Inventory;
using CraftSharp.Inventory.Recipe;
using CraftSharp.Protocol.Handlers.StructuredComponents.Components;
using CraftSharp.Protocol.Handlers.StructuredComponents.Core;
using CraftSharp.Protocol.Message;
using CraftSharp.Rendering;

namespace CraftSharp.UI
{
    [RequireComponent(typeof (CanvasGroup))]
    public class InventoryScreen : BaseScreen
    {
        private static readonly ResourceLocation SPEED_ID = new("speed");
        private static readonly ResourceLocation HASTE_ID = new("haste");
        private static readonly ResourceLocation RESISTANCE_ID = new("resistance");
        private static readonly ResourceLocation JUMP_BOOST_ID = new("jump_boost");
        private static readonly ResourceLocation STRENGTH_ID = new("strength");
        private static readonly ResourceLocation REGENERATION_ID = new("regeneration");
        
        // UI controls and objects
        [SerializeField] private TMP_Text inventoryTitleText;
        [SerializeField] private Button closeButton;
        [SerializeField] private Animator screenAnimator;
        [SerializeField] private GameObject inventorySlotPrefab;
        [SerializeField] private GameObject inventoryInputPrefab;
        [SerializeField] private GameObject inventoryLabelPrefab;
        [SerializeField] private GameObject inventoryButtonPrefab;
        [SerializeField] private GameObject inventorySpritePrefab;
        [SerializeField] private GameObject inventoryScrollViewPrefab;
        [SerializeField] private InventoryBackground inventoryBackground;
        [SerializeField] private GameObject leftScrollViewObject;
        [SerializeField] private RectTransform listPanel;
        [SerializeField] private GameObject rightScrollViewObject;
        [SerializeField] private IconSpritePanel mobEffectsPanel;
        [SerializeField] private VerticalLayoutGroup inventoryPanelLayoutGroup;
        [SerializeField] private RectTransform inventoryPanel, workPanel, backpackPanel, hotbarPanel;
        [SerializeField] private RectTransform cursorRect;
        [SerializeField] private InventoryItemSlot cursorSlot;
        [SerializeField] private RectTransform cursorTextPanel;
        [SerializeField] private TMP_Text cursorText;
        [SerializeField] private InventoryItemSlot[] backpackSlots;
        [SerializeField] private InventoryItemSlot[] hotbarSlots;
        [SerializeField] private float inventorySlotSize = 90F;
        [SerializeField] private TMP_Text propertyPreviewText;

        private RectTransform screenRect;

        private readonly Dictionary<int, InventoryItemSlot> currentSlots = new();
        private readonly Dictionary<int, InventoryInput> currentInputs = new();
        private readonly Dictionary<int, InventoryButton> currentButtons = new();
        private readonly Dictionary<int, InventoryScrollView> currentScrollViews = new();

        // (cur_value_property, max_value_property, sprite_type, sprite_image)
        private readonly List<(string, string, SpriteType, Image)> currentFilledSprites = new();
        // (flipbook_index_property, sprite_type, sprite_image)
        private readonly List<(string, SpriteType, Image)> currentPropertyFlipbookSprites = new();
        // (flipbook_timer, sprite_type, sprite_image)
        private readonly List<(SpriteType.FlipbookTimer, SpriteType, Image)> currentTimerFlipbookSprites = new();
        // (content_property, text_handler, label_text)
        private readonly List<(string, Func<short, string>, TMP_Text)> currentPropertyLabels = new();
        // (predicate_type, predicate, inventory_fragment)
        private readonly List<(InventoryType.PredicateType, InventoryPropertyPredicate, MonoBehaviour)> propertyDependents = new();
        
        private readonly Dictionary<ResourceLocation, string> mobEffectsNames = new();
        private readonly Dictionary<ResourceLocation, int> mobEffectsCurrentTicks = new();

        private bool isActive = false;

        private int activeInventoryId = -1; // -1 for none
        private InventoryData activeInventoryData = null;
        private readonly Dictionary<string, short> propertyTable = new();
        private int fixedValuePropertyCount = 0;
        private readonly HashSet<string> updatedPropertyNames = new();
        
#nullable enable

        private Action<MobEffectUpdateEvent>? mobEffectUpdateCallback;
        private Action<MobEffectRemovalEvent>? mobEffectRemovalCallback;
        private Action<TickSyncEvent>? tickSyncCallback;
        private Action<InventorySlotUpdateEvent>? slotUpdateCallback;
        private Action<InventoryItemsUpdateEvent>? itemsUpdateCallback;
        private Action<InventoryPropertyUpdateEvent>? propertyUpdateCallback;

#nullable disable
        
        private bool pointerIsDown = false;
        private int dragStartSlot = -1;
        private bool dragging = false;
        private readonly HashSet<int> draggedSlots = new();
        private PointerEventData.InputButton mouseButton = PointerEventData.InputButton.Left;

        public void SetActiveInventory(InventoryData inventoryData)
        {
            EnsureInitialized();

            activeInventoryId = inventoryData.Id;
            activeInventoryData = inventoryData;

            propertyTable.Clear();
            fixedValuePropertyCount = 0;
            
            var inventoryType = inventoryData.Type;

            inventoryTitleText.text = inventoryData.Title;
            Debug.Log($"Set inventory: [{activeInventoryId}] {inventoryData.Title}");
            
            // Update work panel height
            workPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
                inventoryType.WorkPanelHeight * inventorySlotSize);

            var totalHeight = (inventoryType.WorkPanelHeight + 4) * inventorySlotSize +
                2 * inventoryPanelLayoutGroup.spacing +
                inventoryPanelLayoutGroup.padding.top + inventoryPanelLayoutGroup.padding.bottom;
            
            // Update total height of all columns
            inventoryPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, totalHeight);
            
            if (inventoryType.ListPanelWidth > 0)
            {
                // TODO: Initialize trade list
                Debug.Log("Initialize trade list");

                var leftRect = leftScrollViewObject.GetComponent<RectTransform>();
                
                // Show list panel
                leftRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
                    inventoryType.ListPanelWidth * inventorySlotSize);
                leftRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, totalHeight);
                leftScrollViewObject.SetActive(true);
            }
            else
            {
                // Hide list panel
                leftScrollViewObject.SetActive(false);
            }
            
            var rightRect = rightScrollViewObject.GetComponent<RectTransform>();
            rightRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, totalHeight);
            
            // Populate work panel controls, except slots
            CreateLayout(workPanel, inventoryType.WorkPanelLayout, false);

            // Populate work panel slots
            for (int i = 0; i < inventoryType.PrependSlotCount; i++)
            {
                var slotInfo = inventoryType.GetWorkPanelSlotInfo(i);
                
                CreateSlot(workPanel, i, slotInfo.PosX, slotInfo.PosY, slotInfo.TypeId, slotInfo.PreviewItemStack,
                    slotInfo.PlaceholderTypeId, $"Slot [{i}] (Work Prepend) [{slotInfo.TypeId}]");
            }
            
            var workMainStart = inventoryType.PrependSlotCount;
            for (int i = workMainStart; i < workMainStart + inventoryType.MainSlotWidth * inventoryType.MainSlotHeight; i++)
            {
                var slotInfo = inventoryType.GetWorkPanelSlotInfo(i);
                
                CreateSlot(workPanel, i, slotInfo.PosX, slotInfo.PosY, slotInfo.TypeId, slotInfo.PreviewItemStack,
                    slotInfo.PlaceholderTypeId, $"Slot [{workMainStart + i}] (Work Main) [{slotInfo.TypeId}]");
            }

            if (inventoryType.HasBackpackSlots)
            {
                var backpackStart = inventoryData.GetFirstBackpackSlot();
                
                // Initialize backpack slots
                for (int i = 0; i < backpackSlots.Length; i++)
                {
                    backpackSlots[i].UpdateItemStack(inventoryData.Items.GetValueOrDefault(backpackStart + i));
                    backpackSlots[i].gameObject.name = $"Slot [{backpackStart + i}] (Backpack)";
                    
                    SetupSlot(backpackStart + i, backpackSlots[i]);
                }
                backpackPanel.gameObject.SetActive(true);
            }
            else
            {
                backpackPanel.gameObject.SetActive(false);
            }
            
            if (inventoryType.HasHotbarSlots)
            {
                var hotbarStart = inventoryData.GetFirstHotbarSlot();
                
                // Initialize hotbar slots
                for (int i = 0; i < hotbarSlots.Length; i++)
                {
                    hotbarSlots[i].UpdateItemStack(inventoryData.Items.GetValueOrDefault(hotbarStart + i));
                    hotbarSlots[i].gameObject.name = $"Slot [{hotbarStart + i}] (Hotbar)";

                    SetupSlot(hotbarStart + i, hotbarSlots[i]);
                }
                hotbarPanel.gameObject.SetActive(true);
            }
            else
            {
                hotbarPanel.gameObject.SetActive(false);
            }
            
            var workAppendStart = inventoryType.SlotCount - inventoryType.AppendSlotCount;
            for (int i = workAppendStart; i < workAppendStart + inventoryType.AppendSlotCount; i++)
            {
                var slotInfo = inventoryType.GetWorkPanelSlotInfo(i);
                
                CreateSlot(workPanel, i, slotInfo.PosX, slotInfo.PosY, slotInfo.TypeId, slotInfo.PreviewItemStack,
                    slotInfo.PlaceholderTypeId, $"Slot [{i}] (Work Append) [{slotInfo.TypeId}]");
            }

            // Initialize cursor slot
            currentSlots[-1] = cursorSlot;
            SetupSlot(-1, cursorSlot);
            
            // Initialize player inventory slots (these won't be sent because client already have them)
            if (activeInventoryId == 0)
            {
                currentSlots[0].UpdateItemStack(activeInventoryData.Items.GetValueOrDefault(0)); // Output
                currentSlots[1].UpdateItemStack(activeInventoryData.Items.GetValueOrDefault(1));
                currentSlots[2].UpdateItemStack(activeInventoryData.Items.GetValueOrDefault(2));
                currentSlots[3].UpdateItemStack(activeInventoryData.Items.GetValueOrDefault(3));
                currentSlots[4].UpdateItemStack(activeInventoryData.Items.GetValueOrDefault(4));
                currentSlots[5].UpdateItemStack(activeInventoryData.Items.GetValueOrDefault(5)); // Head
                currentSlots[6].UpdateItemStack(activeInventoryData.Items.GetValueOrDefault(6)); // Chest
                currentSlots[7].UpdateItemStack(activeInventoryData.Items.GetValueOrDefault(7)); // Legs
                currentSlots[8].UpdateItemStack(activeInventoryData.Items.GetValueOrDefault(8)); // Feet
                
                currentSlots[45].UpdateItemStack(activeInventoryData.Items.GetValueOrDefault(45)); // Offhand
            }

            // Hide cursor text
            cursorTextPanel.gameObject.SetActive(false);
            
            // Clear property preview text
            if (propertyPreviewText)
            {
                propertyPreviewText.text = string.Empty;
            }
        }
        
        private bool TryAddFixedValueProperty(string fixedValue, out string propertyName)
        {
            propertyName = $"fixed_value_{fixedValuePropertyCount}";
            
            if (short.TryParse(fixedValue, out var shortValue))
            {
                if (propertyTable.TryAdd(propertyName, shortValue))
                {
                    Debug.Log($"Add fixed value property {propertyName}: {shortValue}");
                    fixedValuePropertyCount++;
                    return true;
                }
            }
            return false;
        }

        private InventoryItemSlot CreateSlot(RectTransform parent, int slotId, float x, float y, ResourceLocation typeId,
                ItemStack previewItem, ResourceLocation? placeholderSpriteTypeId, string slotName)
        {
            var slotObj = Instantiate(inventorySlotPrefab, parent);
            var slot = slotObj.GetComponent<InventoryItemSlot>();
            var rectTransform = slotObj.GetComponent<RectTransform>();
            
            rectTransform.anchoredPosition = new Vector2(x * inventorySlotSize, y * inventorySlotSize);
            slotObj.name = slotName;

            if (placeholderSpriteTypeId.HasValue) // Set placeholder sprite
            {
                var placeholderSpriteType = SpriteTypePalette.INSTANCE.GetById(
                    placeholderSpriteTypeId.Value);
                var placeholderImage = slot.GetPlaceholderImage();

                slot.SetPlaceholderSprite(placeholderSpriteType.Sprite);

                SetupSprite(placeholderSpriteType, placeholderImage, null, null, null);
            }

            if (typeId == InventorySlotType.SLOT_TYPE_PREVIEW_ID && previewItem is not null)
            {
                slot.UpdateItemStack(previewItem);

                // Hide border to avoid confusion with interactable slots
                slot.SetSlotBorderVisibility(false);
            }

            SetupSlot(slotId, slot);

            return slot;
        }

        private void SetupSlot(int slotId, InventoryItemSlot slot)
        {
            currentSlots[slotId] = slot;
            var slotInfo = activeInventoryData.Type.GetWorkPanelSlotInfo(slotId);
            var slotType = InventorySlotTypePalette.INSTANCE.GetById(slotInfo.TypeId);

            var game = CornApp.CurrentClient;
            if (!game) return;

            slot.SetPointerDownHandler(button =>
            {
                if (!game) return;
                
                pointerIsDown = true;
                mouseButton = button;

                var target = activeInventoryData.Items.GetValueOrDefault(slotId);
                var cursor = game.GetInventory(0)?.Items.GetValueOrDefault(-1);
                
                // Draggable slot has to be either empty or have the exact same item as cursor item.
                // And also, to start dragging, cursor item shouldn't be empty
                var canBeUsedAsDragStart = cursor is not null && cursor.Count > 1 && slotType.PlacePredicate(cursor)
                                            && (target is null || InventoryData.CheckStackable(target, cursor));
                dragStartSlot = canBeUsedAsDragStart ? slotId : -1;
            });
            
            slot.SetPointerUpHandler(button =>
            {
                if (!pointerIsDown || button != mouseButton || !game) return;

                if (dragging)
                {
                    var action = mouseButton switch
                    {
                        PointerEventData.InputButton.Left => InventoryActionType.EndDragLeft,
                        PointerEventData.InputButton.Right => InventoryActionType.EndDragRight,
                        PointerEventData.InputButton.Middle => InventoryActionType.EndDragMiddle,
                        _ => throw new ArgumentOutOfRangeException()
                    };
                    game.DoInventoryAction(activeInventoryId, slotId, action);
                }
                else // Click
                {
                    var shiftIsDown = Keyboard.current.shiftKey.isPressed;

                    var action = mouseButton switch
                    {
                        PointerEventData.InputButton.Left => shiftIsDown ? InventoryActionType.ShiftClick : InventoryActionType.LeftClick,
                        PointerEventData.InputButton.Right => shiftIsDown ? InventoryActionType.ShiftRightClick : InventoryActionType.RightClick,
                        PointerEventData.InputButton.Middle => InventoryActionType.MiddleClick,
                        _ => throw new ArgumentOutOfRangeException()
                    };
                    game.DoInventoryAction(activeInventoryId, slotId, action);
                    cursorTextPanel.gameObject.SetActive(false);
                }
                
                pointerIsDown = false;
                dragging = false;

                foreach (var draggedSlot in draggedSlots)
                {
                    currentSlots[draggedSlot].Dragged = false;
                }
                
                draggedSlots.Clear();
            });
            
            slot.SetHoverHandler(() =>
            {
                if (!game) return;
                
                if (pointerIsDown)
                {
                    var target = activeInventoryData.Items.GetValueOrDefault(slotId);

                    // Drag start slot has to be either empty or have the exact same item as cursor item.
                    // And also, to start dragging, cursor item shouldn't be empty
                    if (dragStartSlot < 0)
                    {
                        var cursor = game.GetInventory(0)?.Items.GetValueOrDefault(-1);
                        if (cursor is not null && cursor.Count > 1 && slotType.MaxCount == int.MaxValue &&
                            slotType.PlacePredicate(cursor) &&
                            (target is null || InventoryData.CheckStackable(target, cursor)))
                        {
                            dragStartSlot = slotId;
                        }
                    }
                    
                    if (!dragging && dragStartSlot >= 0 && slotId != dragStartSlot) // Start dragging
                    {
                        dragging = true;
                        currentSlots[dragStartSlot].Dragged = true;
                        draggedSlots.Clear();
                        draggedSlots.Add(dragStartSlot);
                        
                        var action = mouseButton switch
                        {
                            PointerEventData.InputButton.Left => InventoryActionType.StartDragLeft,
                            PointerEventData.InputButton.Right => InventoryActionType.StartDragRight,
                            PointerEventData.InputButton.Middle => InventoryActionType.StartDragMiddle,
                            _ => throw new ArgumentOutOfRangeException()
                        };
                        game.DoInventoryAction(activeInventoryId, dragStartSlot, action);
                        action = mouseButton switch
                        {
                            PointerEventData.InputButton.Left => InventoryActionType.AddDragLeft,
                            PointerEventData.InputButton.Right => InventoryActionType.AddDragRight,
                            PointerEventData.InputButton.Middle => InventoryActionType.AddDragMiddle,
                            _ => throw new ArgumentOutOfRangeException()
                        };
                        game.DoInventoryAction(activeInventoryId, dragStartSlot, action);
                        cursorTextPanel.gameObject.SetActive(false);
                    }
                    
                    if (dragging && !draggedSlots.Contains(slotId) && slotType.MaxCount == int.MaxValue &&
                        game.CheckAddDragged(target, slotType.PlacePredicate)) // Add this slot
                    {
                        currentSlots[slotId].Dragged = true;
                        draggedSlots.Add(slotId);
                        var action = mouseButton switch
                        {
                            PointerEventData.InputButton.Left => InventoryActionType.AddDragLeft,
                            PointerEventData.InputButton.Right => InventoryActionType.AddDragRight,
                            PointerEventData.InputButton.Middle => InventoryActionType.AddDragMiddle,
                            _ => throw new ArgumentOutOfRangeException()
                        };
                        game.DoInventoryAction(activeInventoryId, slotId, action);
                    }
                }
            });

            slot.SetCursorTextHandler(UpdateCursorText);
        }

        private void CreateLayout(RectTransform parent, InventoryType.InventoryLayoutInfo layoutInfo, bool createSlots)
        {
            // Populate layout sprites
            if (layoutInfo.SpriteInfo is not null)
                foreach (var spriteInfo in layoutInfo.SpriteInfo)
                {
                    var spriteType = SpriteTypePalette.INSTANCE.GetById(spriteInfo.TypeId);
                    var sprite = CreateSprite(parent, spriteInfo.PosX, spriteInfo.PosY, spriteInfo.Width, spriteInfo.Height,
                        spriteType, spriteInfo.CurFillProperty, spriteInfo.MaxFillProperty, spriteInfo.FlipIdxProperty);
                    RegisterPropertyDependent(spriteInfo, sprite);
                }

            // Populate layout labels
            if (layoutInfo.LabelInfo is not null)
                foreach (var labelInfo in layoutInfo.LabelInfo)
                {
                    var label = CreateLabel(parent, labelInfo.PosX, labelInfo.PosY, labelInfo.Width, labelInfo.Height,
                        labelInfo.Alignment, labelInfo.TextTranslationKey, labelInfo.ContentProperty);
                    RegisterPropertyDependent(labelInfo, label);
                }

            // Populate layout inputs
            if (layoutInfo.InputInfo is not null)
                foreach (var (inputId, inputInfo) in layoutInfo.InputInfo)
                {
                    var input = CreateInput(parent, inputId, inputInfo.PosX, inputInfo.PosY,
                        inputInfo.Width, inputInfo.PlaceholderTranslationKey, $"Input [{inputId}]");
                    RegisterPropertyDependent(inputInfo, input);
                    input.SetHintTranslationFromInfo(inputInfo);
                }

            // Populate layout buttons
            if (layoutInfo.ButtonInfo is not null)
                foreach (var (buttonId, buttonInfo) in layoutInfo.ButtonInfo)
                {
                    var button = CreateButton(parent, buttonId, buttonInfo.PosX, buttonInfo.PosY,
                        buttonInfo.Width, buttonInfo.Height, buttonInfo.LayoutInfo, $"Button [{buttonId}]");
                    RegisterPropertyDependent(buttonInfo, button);
                    button.SetHintTranslationFromInfo(buttonInfo);
                }
            
            // Populate layout scroll views
            if (layoutInfo.ScrollViewInfo is not null)
                foreach (var (scrollViewId, scrollViewInfo) in layoutInfo.ScrollViewInfo)
                {
                    var scrollView = CreateScrollView(parent, scrollViewId, scrollViewInfo.PosX, scrollViewInfo.PosY,
                        scrollViewInfo.Width, scrollViewInfo.Height, $"Scroll View [{scrollViewId}]");
                    RegisterPropertyDependent(scrollViewInfo, scrollView);
                }

            // Populate layout slots
            if (createSlots && layoutInfo.SlotInfo is not null)
                foreach (var (slotId, slotInfo) in layoutInfo.SlotInfo)
                {
                    var slot = CreateSlot(parent, slotId, slotInfo.PosX, slotInfo.PosY, slotInfo.TypeId,
                        slotInfo.PreviewItemStack, slotInfo.PlaceholderTypeId, $"Slot [{slotId}] (Nested)");
                    RegisterPropertyDependent(slotInfo, slot);
                    slot.SetHintTranslationFromInfo(slotInfo);
                }
        }

        private TMP_Text CreateLabel(RectTransform parent, float x, float y, float w, float h, InventoryType.LabelAlignment alignment, string translationKey, string contentProperty)
        {
            var labelObj = Instantiate(inventoryLabelPrefab, parent);
            var labelText = labelObj.GetComponent<TMP_Text>();
            var rectTransform = labelObj.GetComponent<RectTransform>();

            rectTransform.anchoredPosition = new Vector2(x * inventorySlotSize, y * inventorySlotSize);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w * inventorySlotSize);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h * inventorySlotSize);

            labelText.text = translationKey is not null ? Translations.Get(translationKey) : string.Empty;
            labelText.horizontalAlignment = alignment switch
            {
                InventoryType.LabelAlignment.Left => HorizontalAlignmentOptions.Left,
                InventoryType.LabelAlignment.Center => HorizontalAlignmentOptions.Center,
                InventoryType.LabelAlignment.Right => HorizontalAlignmentOptions.Right,
                _ => throw new InvalidDataException($"Label alignment {alignment} is not defined!"),
            };

            SetupLabel(labelText, contentProperty);

            return labelText;
        }

        private void SetupLabel(TMP_Text labelText, string contentProperty)
        {
            if (contentProperty is not null)
            {
                if (contentProperty.Contains('~'))
                {
                    var parts = contentProperty.Split('~', 2);
                    Func<short, string> textConverter = parts[1] switch
                    {
                        "enchantment_magic" => x => $"UwU {x}",
                        _ => x => x.ToString()
                    };
                    currentPropertyLabels.Add((parts[0], textConverter, labelText));
                }
                else
                {
                    currentPropertyLabels.Add((contentProperty, x => x.ToString(), labelText));
                }
            }
        }

        private InventoryInput CreateInput(RectTransform parent, int inputId, float x, float y, float w,
                string translationKey, string inputName)
        {
            var inputObj = Instantiate(inventoryInputPrefab, parent);
            var input = inputObj.GetComponent<InventoryInput>();
            var rectTransform = inputObj.GetComponent<RectTransform>();

            inputObj.name = inputName;
            rectTransform.anchoredPosition = new Vector2(x * inventorySlotSize, y * inventorySlotSize);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w * inventorySlotSize);

            input.SetPlaceholderText(translationKey is not null ? Translations.Get(translationKey) : string.Empty);

            SetupInput(inputId, input);

            return input;
        }

        private void SetupInput(int inputId, InventoryInput input)
        {
            currentInputs[inputId] = input;

            input.SetValueChangeHandler(str => HandleInputValueChange(inputId, str));

            input.SetCursorTextHandler(UpdateCursorText);
        }

        private InventoryButton CreateButton(RectTransform parent, int buttonId, float x, float y, float w, float h,
                InventoryType.InventoryLayoutInfo layoutInfo, string buttonName)
        {
            var buttonObj = Instantiate(inventoryButtonPrefab, parent);
            var button = buttonObj.GetComponent<InventoryButton>();
            var rectTransform = buttonObj.GetComponent<RectTransform>();

            buttonObj.name = buttonName;
            rectTransform.anchoredPosition =
                new Vector2(x * inventorySlotSize, y * inventorySlotSize);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w * inventorySlotSize);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h * inventorySlotSize);

            // Create nested layout
            CreateLayout(rectTransform, layoutInfo, true);

            SetupButton(buttonId, button);

            return button;
        }

        private void SetupButton(int buttonId, InventoryButton button)
        {
            currentButtons[buttonId] = button;

            button.SetClickHandler(() => HandleButtonClick(buttonId));

            button.SetCursorTextHandler(UpdateCursorText);
        }
        
        private Image CreateSprite(RectTransform parent, float x, float y, float w, float h, SpriteType spriteType, string curFillProp, string maxFillProp, string flipIdxProp)
        {
            var spriteObj = Instantiate(inventorySpritePrefab, parent);
            var spriteImage = spriteObj.GetComponent<Image>();
            var rectTransform = spriteObj.GetComponent<RectTransform>();

            SpriteType.SetupSpriteImage(spriteType, spriteImage);
            
            rectTransform.anchoredPosition = new Vector2(x * inventorySlotSize, y * inventorySlotSize);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w * inventorySlotSize);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h * inventorySlotSize);
            
            spriteObj.name = $"Sprite [{spriteType.TypeId}]";

            SetupSprite(spriteType, spriteImage, curFillProp, maxFillProp, flipIdxProp);

            return spriteImage;
        }

        private void SetupSprite(SpriteType spriteType, Image spriteImage, string curFillProp, string maxFillProp, string flipIdxProp)
        {
            if (spriteType.ImageType == SpriteType.SpriteImageType.Filled)
            {
                if (curFillProp is null || maxFillProp is null)
                {
                    Debug.LogWarning($"Filled sprite properties for sprite {spriteType.TypeId} is not specified!");
                }
                else
                {
                    if (!activeInventoryData.Type.PropertySlots.ContainsKey(curFillProp))
                    {
                        if (TryAddFixedValueProperty(curFillProp, out var fixedValuePropName))
                        {
                            curFillProp = fixedValuePropName;
                        }
                    }
                    if (!activeInventoryData.Type.PropertySlots.ContainsKey(maxFillProp))
                    {
                        if (TryAddFixedValueProperty(maxFillProp, out var fixedValuePropName))
                        {
                            maxFillProp = fixedValuePropName;
                        }
                    }
                    // Add it to the list
                    currentFilledSprites.Add((curFillProp, maxFillProp, spriteType, spriteImage));

                    Debug.Log($"Filled sprite: {spriteType}, CurProp: {curFillProp}, MaxProp: {maxFillProp}");
                }
            }
            
            if (spriteType.FlipbookSprites is null)
            {
                Debug.LogWarning($"Flipbook sprite for {spriteType.TypeId} is null!");
            }
            else if (spriteType.FlipbookSprites.Length > 0)
            {
                if (flipIdxProp is not null) // Use property to control flipbook frame
                {
                    currentPropertyFlipbookSprites.Add((flipIdxProp, spriteType, spriteImage));
                }
                else // Use a timer to control flipbook frame
                {
                    currentTimerFlipbookSprites.Add((new(), spriteType, spriteImage));
                }
            }
        }

        private InventoryScrollView CreateScrollView(RectTransform parent, int scrollViewId, float x, float y, float w, float h, string scrollViewName)
        {
            var scrollViewObj = Instantiate(inventoryScrollViewPrefab, parent);
            var scrollView = scrollViewObj.GetComponent<InventoryScrollView>();
            var rectTransform = scrollViewObj.GetComponent<RectTransform>();

            scrollViewObj.name = scrollViewName;
            
            rectTransform.anchoredPosition = new Vector2(x * inventorySlotSize, y * inventorySlotSize);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w * inventorySlotSize);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h * inventorySlotSize);
            
            SetupScrollView(scrollViewId, scrollView);

            return scrollView;
        }
        
        private void SetupScrollView(int scrollViewId, InventoryScrollView scrollView)
        {
            currentScrollViews[scrollViewId] = scrollView;
        }
        
        private void RegisterPropertyDependent(InventoryType.InventoryFragmentInfo fragmentInfo, MonoBehaviour inventoryFragment)
        {
            foreach (var (predicateType, predicate) in fragmentInfo.Predicates)
            {
                propertyDependents.Add((predicateType, predicate, inventoryFragment));
            }
        }

        private void UpdateCursorText(string str)
        {
            var game = CornApp.CurrentClient;
            if (!game) return;
            var cursorHasItem = game.GetInventory(0)?.Items.ContainsKey(-1) ?? true;
            
            if (string.IsNullOrEmpty(str) || cursorHasItem)
            {
                cursorTextPanel.gameObject.SetActive(false);
            }
            else
            {
                cursorText.text = str;
                cursorTextPanel.gameObject.SetActive(true);
                
                LayoutRebuilder.ForceRebuildLayoutImmediate(cursorTextPanel);
            }
        }

        private void HandleButtonClick(int buttonId)
        {
            // Handle button click TODO: Move to separate class
            if (activeInventoryData.Type.TypeId == InventoryType.BEACON_ID)
            {
                if (buttonId is >= 0 and <= 4 &&
                    propertyTable.TryGetValue("first_potion_effect", out var primaryId) &&
                    propertyTable.TryGetValue("second_potion_effect_index", out var secondaryIdIndex))
                {
                    primaryId = buttonId switch
                    {
                        0 => (short) MobEffectPalette.INSTANCE.GetNumIdById(SPEED_ID),
                        1 => (short) MobEffectPalette.INSTANCE.GetNumIdById(HASTE_ID),
                        2 => (short) MobEffectPalette.INSTANCE.GetNumIdById(RESISTANCE_ID),
                        3 => (short) MobEffectPalette.INSTANCE.GetNumIdById(JUMP_BOOST_ID),
                        4 => (short) MobEffectPalette.INSTANCE.GetNumIdById(STRENGTH_ID),
                        // ReSharper disable once UnreachableSwitchArmDueToIntegerAnalysis
                        _ => primaryId
                    };
                    var propertyId = activeInventoryData.Type.PropertySlots["first_potion_effect"];
                    EventManager.Instance.Broadcast(new InventoryPropertyUpdateEvent(activeInventoryId, propertyId, primaryId));
                    
                    if (secondaryIdIndex == 1) // Level-up button selected, also update secondary effect id
                    {
                        propertyId = activeInventoryData.Type.PropertySlots["second_potion_effect"];
                        EventManager.Instance.Broadcast(new InventoryPropertyUpdateEvent(activeInventoryId, propertyId, primaryId));
                    }
                }
                
                if (buttonId is >= 5 and <= 6 &&
                    propertyTable.TryGetValue("first_potion_effect", out primaryId) &&
                    propertyTable.TryGetValue("second_potion_effect", out var secondaryId))
                {
                    secondaryId = buttonId switch
                    {
                        5 => (short) MobEffectPalette.INSTANCE.GetNumIdById(REGENERATION_ID),
                        6 => primaryId, // Level-up primary effect
                        // ReSharper disable once UnreachableSwitchArmDueToIntegerAnalysis
                        _ => secondaryId
                    };
                    var propertyId = activeInventoryData.Type.PropertySlots["second_potion_effect"];
                    EventManager.Instance.Broadcast(new InventoryPropertyUpdateEvent(activeInventoryId, propertyId, secondaryId));
                }

                if (buttonId == 7 &&
                    propertyTable.TryGetValue("first_potion_effect", out primaryId) &&
                    propertyTable.TryGetValue("second_potion_effect", out secondaryId))
                {
                    var game = CornApp.CurrentClient;
                    if (!game) return;
                    
                    game.SetBeaconEffects(primaryId, secondaryId);
                    CloseInventory();
                }

                if (buttonId == 8)
                {
                    CloseInventory();
                }
            }

            if (activeInventoryData.Type.TypeId == InventoryType.ENCHANTMENT_ID)
            {
                var game = CornApp.CurrentClient;
                if (!game) return;

                game.DoInventoryButtonClick(activeInventoryId, buttonId);
            }

            if (activeInventoryData.Type.TypeId == InventoryType.STONECUTTER_ID)
            {
                var game = CornApp.CurrentClient;
                if (!game) return;
                
                game.DoInventoryButtonClick(activeInventoryId, buttonId);
                
                EventManager.Instance.Broadcast<InventoryPropertyUpdateEvent>(
                    new(activeInventoryId, 0, (short) buttonId));
            }
            
            if (activeInventoryData.Type.TypeId == InventoryType.LOOM_ID)
            {
                var game = CornApp.CurrentClient;
                if (!game) return;

                if (currentButtons.Count > 1) // i.e. Not using a pattern item
                {
                    game.DoInventoryButtonClick(activeInventoryId, buttonId);
                
                    EventManager.Instance.Broadcast<InventoryPropertyUpdateEvent>(
                        new(activeInventoryId, 0, (short) buttonId));
                }
            }
        }

        private void HandleInputValueChange(int inputId, string text)
        {
            // Handle input value change TODO: Move to separate class
            if (activeInventoryData.Type.TypeId == InventoryType.ANVIL_ID)
            {
                if (inputId == 0)
                {
                    if (text.Length <= 35)
                    {
                        var game = CornApp.CurrentClient;
                        if (!game) return;

                        game.SetAnvilRenameText(text);
                    }
                }
            }
        }
        
        private void HandleSlotChange(int slotId, ItemStack itemStack)
        {
            // Handle slot change TODO: Move to separate class
            if (activeInventoryData.Type.TypeId == InventoryType.BEACON_ID)
            {
                if (slotId == 0) // Beacon activation item slot
                {
                    SetPseudoProperty("activation_item_ready", itemStack is null ? (short) 0 : (short) 1);
                    // Update property-dependent fragments
                    UpdatePredicateDependents();
                }
            }

            if (activeInventoryData.Type.TypeId == InventoryType.ENCHANTMENT_ID)
            {
                if (slotId == 1) // Lapis Lazuli item slot (Base item slot change will trigger property update)
                {
                    UpdateEnchantmentOptions();
                    // Update property-dependent fragments
                    UpdatePredicateDependents();
                }
            }

            if (activeInventoryData.Type.TypeId == InventoryType.STONECUTTER_ID)
            {
                if (slotId == 0) // Stonecutter base item slot
                {
                    var scrollView = currentScrollViews[0];
                    var grid = scrollView.ItemGridLayoutGroup;
                    var gridRectTransform = grid.GetComponent<RectTransform>();
                    
                    // Clear current list first
                    scrollView.ClearAllItems();
                    currentButtons.Clear();
                    propertyDependents.Clear();
                    
                    if (itemStack is null) return;
                    
                    var game = CornApp.CurrentClient;
                    if (!game) return;

                    var recipes = game.GetReceivedRecipes(RecipeTypePalette.STONECUTTING)
                        .Select(r => (StonecuttingExtraData) r)
                        .Where(r => r.Ingredient.Any(x => x!.ItemType == itemStack.ItemType))
                        // Sort by translation key of result items
                        .OrderBy(x => x.Result.ItemType.ItemId.GetTranslationKey("block"));

                    if (!propertyTable.TryGetValue("selected_recipe", out var currentSelected))
                    {
                        currentSelected = -1;
                    }

                    int index = 0;
                    foreach (var recipe in recipes)
                    {
                        var nestedLayout = new InventoryType.InventoryLayoutInfo(
                            null, new()
                            {
                                [1000 + index] = new InventoryType.InventorySlotInfo(
                                    0, 0, InventorySlotType.SLOT_TYPE_PREVIEW_ID, recipe.Result, null)
                            }, null, null, null);

                        var recipeButton = CreateButton(gridRectTransform, index, 0, 0, 1, 1, nestedLayout, $"Recipe [{index}]");
                        propertyDependents.Add((InventoryType.PredicateType.Selected,
                            new InventoryPropertyPredicate(new()
                            {
                                ["selected_recipe"] = (InventoryPropertyPredicate.Operator.EQUAL, index.ToString())
                            }), recipeButton));
                        recipeButton.HintTranslationKey = recipe.Result.ItemType.ItemId.GetTranslationKey("block");
                        recipeButton.MarkCursorTextDirty();

                        if (currentSelected == index) recipeButton.Selected = true;

                        index++;
                    }
                }
            }
            
            if (activeInventoryData.Type.TypeId == InventoryType.LOOM_ID)
            {
                if (slotId is >= 0 and < 3) // Loom input item slot
                {
                    var scrollView = currentScrollViews[0];
                    var grid = scrollView.ItemGridLayoutGroup;
                    var gridRectTransform = grid.GetComponent<RectTransform>();
                    
                    // Clear current list first
                    scrollView.ClearAllItems();
                    currentButtons.Clear();
                    propertyDependents.Clear();
                    
                    var bannerItem = currentSlots[0].GetItemStack();
                    var dyeItem = currentSlots[1].GetItemStack();
                    var bannerPatternItem = currentSlots[2].GetItemStack();

                    if (bannerItem is null || dyeItem is null)
                    {
                        return;
                    }
                    
                    var game = CornApp.CurrentClient;
                    if (!game) return;

                    var usingPatternItem = bannerPatternItem is not null;
                    ResourceLocation[] patterns;

                    if (usingPatternItem) // Use the pattern specified by pattern item
                    {
                        var patternId = BannerPatternType.GetIdFromItemId(bannerPatternItem.ItemType.ItemId);
                        if (patternId == ResourceLocation.INVALID)
                        {
                            return;
                        }
                        
                        patterns = new[] { patternId };
                    }
                    else // Use default pattern list
                    {
                        // TODO: Set this value based on protocol version
                        patterns = BannerPatternType.GetDefaultPatternList();
                    }

                    if (!propertyTable.TryGetValue("selected_pattern", out var currentSelected))
                    {
                        currentSelected = usingPatternItem ? (short) 0 : (short) -1;
                    }

                    var existingPatterns = GetBannerPatterns(bannerItem);
                    var patternColor = CommonColorsHelper.GetCommonColor(
                        dyeItem.ItemType.ItemId.Path[..^"_dye".Length]);

                    int index = 1; // Banner button index starts from 1
                    foreach (var pattern in patterns)
                    {
                        var nestedLayout = new InventoryType.InventoryLayoutInfo(
                            null, null, null, null, null);

                        var previewSeq = new BannerPatternSequence(existingPatterns
                            .Concat(new [] { new BannerPatternRecord(pattern, patternColor) }).ToArray());

                        var patternButton = CreateButton(gridRectTransform, index, 0, 0, 1, 1, nestedLayout, $"Pattern [{index}]");
                        propertyDependents.Add((InventoryType.PredicateType.Selected,
                            new InventoryPropertyPredicate(new()
                            {
                                ["selected_pattern"] = (InventoryPropertyPredicate.Operator.EQUAL, index.ToString())
                            }), patternButton));
                        patternButton.MarkCursorTextDirty();

                        var spriteObj = new GameObject("Banner Pattern Sprite");
                        spriteObj.transform.SetParent(patternButton.transform);
                        spriteObj.transform.localPosition = Vector3.zero;
                        spriteObj.transform.localScale = Vector3.one;
                        var spriteImage = spriteObj.AddComponent<Image>();
                        
                        game.EntityMaterialManager.ApplyBannerTexture(previewSeq, texture =>
                        {
                            var sprite = EntityMaterialManager.CreateSpriteFromTexturePart(
                                texture, 1 * texture.width / 64, 1 * texture.height / 64,
                                20 * texture.width / 64, 40 * texture.height / 64);
                
                            spriteImage.sprite = sprite;
                            spriteImage.rectTransform.sizeDelta = new Vector2(40, 80);
                        });

                        if (currentSelected == index) patternButton.Selected = true;

                        index++;
                    }
                }
                else if (slotId == 3) // Loom output item slot
                {
                    // Update output preview
                    
                }
            }
        }
        
        private void SetPseudoProperty(string propertyName, short propertyValue)
        {
            propertyTable[propertyName] = propertyValue;
            
            updatedPropertyNames.Add(propertyName);
            //Debug.Log($"Setting property [{activeInventoryId}]/[pseudo] {propertyName} to {propertyValue}");
        }

        private static string GetEnchantmentHoverText(int cost, string enchantmentTranslationKey, short enchantmentLevel, short xpLevelRequirement, int playerXpLevel, int lapisCount)
        {
            var enchantmentName = ChatParser.TranslateString(enchantmentTranslationKey);
            var enchantmentLevelText = ChatParser.TranslateString($"enchantment.level.{enchantmentLevel}");

            var enchantmentClue = ChatParser.TranslateString("container.enchant.clue", new() { $"{enchantmentName} {enchantmentLevelText}" });
            string additionalInfoCode, additionalInfo;

            if (xpLevelRequirement > playerXpLevel)
            {
                // Append unavailable hint (Red)
                additionalInfoCode = "§c";
                additionalInfo = ChatParser.TranslateString("container.enchant.level.requirement", new() { xpLevelRequirement.ToString() });
            }
            else
            {
                var lapisCostText = cost > 1
                    ? ChatParser.TranslateString("container.enchant.lapis.many", new() { cost.ToString() })
                    : ChatParser.TranslateString("container.enchant.lapis.one");
                var levelCostText = cost > 1
                    ? ChatParser.TranslateString("container.enchant.level.many", new() { cost.ToString() })
                    : ChatParser.TranslateString("container.enchant.level.one");

                // Append enchantment cost (Grey if affordable, Red otherwise)
                additionalInfoCode = lapisCount >= cost ? "§7" : "§c";
                var additionalInfoCode2 = playerXpLevel >= cost ? "§7" : "§c";
                
                additionalInfo = $"{lapisCostText}\n{additionalInfoCode2}{levelCostText}";
            }

            return TMPConverter.MC2TMP($"§7{enchantmentClue}\n\n{additionalInfoCode}{additionalInfo}");
        }

        private void UpdateEnchantmentOptions()
        {
            if (
                propertyTable.TryGetValue("xp_level_top",     out var topXpLevelRequirement) &&
                propertyTable.TryGetValue("xp_level_middle",  out var middleXpLevelRequirement) &&
                propertyTable.TryGetValue("xp_level_bottom",  out var bottomXpLevelRequirement) &&
                propertyTable.TryGetValue("enchantment_seed", out var enchantmentSeed) &&
                propertyTable.TryGetValue("enchantment_id_top",    out var topEnchantmentId) &&
                propertyTable.TryGetValue("enchantment_id_middle", out var middleEnchantmentId) &&
                propertyTable.TryGetValue("enchantment_id_bottom", out var bottomEnchantmentId) &&
                propertyTable.TryGetValue("enchantment_level_top",    out var topEnchantmentLevel) &&
                propertyTable.TryGetValue("enchantment_level_middle", out var middleEnchantmentLevel) &&
                propertyTable.TryGetValue("enchantment_level_bottom", out var bottomEnchantmentLevel))
            {
                var topEnchantment    = EnchantmentTypePalette.INSTANCE.GetByNumId(topEnchantmentId);
                var middleEnchantment = EnchantmentTypePalette.INSTANCE.GetByNumId(middleEnchantmentId);
                var bottomEnchantment = EnchantmentTypePalette.INSTANCE.GetByNumId(bottomEnchantmentId);

                var game = CornApp.CurrentClient;
                if (!game) return;

                var playerXpLevel = game.GameMode == GameMode.Creative ? 99 : game.ExperienceLevel;
                var lapisCount = game.GameMode == GameMode.Creative ? 64 : activeInventoryData.Items.TryGetValue(1, out var lapisSlot) ? lapisSlot.Count : 0;

                SetPseudoProperty("enchantment_enabled_top",    playerXpLevel >= topXpLevelRequirement    && playerXpLevel >= 1 && lapisCount >= 1 ? (short) 1 : (short) 0);
                SetPseudoProperty("enchantment_enabled_middle", playerXpLevel >= middleXpLevelRequirement && playerXpLevel >= 2 && lapisCount >= 2 ? (short) 1 : (short) 0);
                SetPseudoProperty("enchantment_enabled_bottom", playerXpLevel >= bottomXpLevelRequirement && playerXpLevel >= 3 && lapisCount >= 3 ? (short) 1 : (short) 0);
                
                currentButtons[0].HintStringOverride = GetEnchantmentHoverText(1, topEnchantment.TranslationKey,    topEnchantmentLevel,    topXpLevelRequirement,    playerXpLevel, lapisCount);
                currentButtons[0].MarkCursorTextDirty();
                currentButtons[1].HintStringOverride = GetEnchantmentHoverText(2, middleEnchantment.TranslationKey, middleEnchantmentLevel, middleXpLevelRequirement, playerXpLevel, lapisCount);
                currentButtons[1].MarkCursorTextDirty();
                currentButtons[2].HintStringOverride = GetEnchantmentHoverText(3, bottomEnchantment.TranslationKey, bottomEnchantmentLevel, bottomXpLevelRequirement, playerXpLevel, lapisCount);
                currentButtons[2].MarkCursorTextDirty();
            }
        }

        private static List<BannerPatternRecord> GetBannerPatterns(ItemStack bannerItem)
        {
            var idPath = bannerItem.ItemType.ItemId.Path;
            var colorName = idPath[..^"_banner".Length];
            var baseColor = CommonColorsHelper.GetCommonColor(colorName);

            var patternRecords = new List<BannerPatternRecord>
            {
                // Base pattern is specified by the block/item id
                new(BannerPatternType.BASE_ID, baseColor)
            };

            if (bannerItem.TryGetComponent<BannerPatternsComponent>(
                    StructuredComponentIds.BANNER_PATTERNS_ID, out var bannerPatternsComp))
            {
                foreach (var patternData in bannerPatternsComp.Layers)
                {
                    // Encoded as enum int (probably as a string)
                    var color = patternData.DyeColor;
                    ResourceLocation patternId;
            
                    if (patternData.PatternType > 0) // Given as an id
                    {
                        patternId = BannerPatternType.GetIdFromIndex(patternData.PatternType);
                    }
                    else if (patternData.PatternType == 0) // Given as an inline definition
                    {
                        patternId = patternData.AssetId!.Value;
                        var translationKey = patternData.TranslationKey!;
                        var newEntry = new BannerPatternType(patternId, translationKey);
                        BannerPatternPalette.INSTANCE.AddOrUpdateEntry(patternId, newEntry);
                    }
                    else
                    {
                        patternId = ResourceLocation.INVALID;
                        Debug.LogWarning("Unexpected pattern type: " + patternData.PatternType);
                    }
                
                    patternRecords.Add(new(patternId, color));
                }
            }

            return patternRecords;
        }
        
        private void UpdatePseudoProperties(string propertyName, short value)
        {
            // Handle property change TODO: Move to separate class
            if (activeInventoryData.Type.TypeId == InventoryType.BEACON_ID)
            {
                if (propertyName == "first_potion_effect")
                {
                    var firstEffect = MobEffectPalette.INSTANCE.GetByNumId(value);
                    //Debug.Log($"First effect: {firstEffect.GetDescription()}");
                    short firstIndex = -1;
                    if (firstEffect.MobEffectId == SPEED_ID) firstIndex = 0;
                    else if (firstEffect.MobEffectId == HASTE_ID) firstIndex = 1;
                    else if (firstEffect.MobEffectId == RESISTANCE_ID) firstIndex = 2;
                    else if (firstEffect.MobEffectId == JUMP_BOOST_ID) firstIndex = 3;
                    else if (firstEffect.MobEffectId == STRENGTH_ID) firstIndex = 4;

                    SetPseudoProperty("first_potion_effect_index", firstIndex);
                }

                if (propertyName == "second_potion_effect")
                {
                    var secondEffect = MobEffectPalette.INSTANCE.GetByNumId(value);
                    //Debug.Log($"Second effect: {secondEffect.GetDescription()}");
                    short secondIndex = -1;
                    if (secondEffect.MobEffectId == REGENERATION_ID) secondIndex = 0;
                    else if (secondEffect.MobEffectId == SPEED_ID || secondEffect.MobEffectId == HASTE_ID ||
                             secondEffect.MobEffectId == RESISTANCE_ID || secondEffect.MobEffectId == JUMP_BOOST_ID ||
                             secondEffect.MobEffectId == STRENGTH_ID) secondIndex = 1;

                    SetPseudoProperty("second_potion_effect_index", secondIndex);
                }
            }
            
            if (activeInventoryData.Type.TypeId == InventoryType.BREWING_STAND_ID)
            {
                if (propertyName == "brew_time")
                {
                    var curBubbleProgress = propertyTable.GetValueOrDefault("bubble_progress", (short) 0);
                    SetPseudoProperty("bubble_progress", (short) (value > 1 ? (curBubbleProgress + 1) % 40 : 0));
                    
                    SetPseudoProperty("arrow_progress", (short) (400 - value));
                }
            }
        
            if (activeInventoryData.Type.TypeId == InventoryType.ENCHANTMENT_ID)
            {
                // We got the last property for enchantment
                if (propertyName == "enchantment_level_bottom" && value != -1)
                {
                    UpdateEnchantmentOptions();
                }
            }
        }

        private void UpdatePredicateDependents()
        {
            // TODO: Update only dependents affected by the change
            foreach (var (predicateType, predicate, inventoryFragment) in propertyDependents)
            {
                var predicateResult = predicate.Check(propertyTable);

                switch (predicateType)
                {
                    case InventoryType.PredicateType.Visible:
                        inventoryFragment.gameObject.SetActive(predicateResult);
                        break;
                    case InventoryType.PredicateType.Enabled:
                        if (inventoryFragment is InventoryInteractable inventoryInteractable1)
                        {
                            inventoryInteractable1.Enabled = predicateResult;
                        }
                        else
                        {
                            Debug.LogWarning($"Cannot set enabled status for inventory fragment {inventoryFragment.gameObject.name}!");
                        }
                        break;
                    case InventoryType.PredicateType.Selected:
                        if (inventoryFragment is InventoryInteractable inventoryInteractable2)
                        {
                            inventoryInteractable2.Selected = predicateResult;
                        }
                        else
                        {
                            Debug.LogWarning($"Cannot set enabled status for inventory fragment {inventoryFragment.gameObject.name}!");
                        }
                        break;
                    default:
                        Debug.LogWarning($"Cannot set unknown status {predicateType} for inventory fragment {inventoryFragment.gameObject.name}!");
                        break;
                }
            }
            
            // Update property preview text (if present)
            if (propertyPreviewText)
            {
                propertyPreviewText.text = string.Join('\n', propertyTable.Select(x => $"{x.Key}: {x.Value, 3}"));
            }
        }

        public override bool IsActive
        {
            set {
                isActive = value;
                screenAnimator.SetBool(SHOW_HASH, isActive);
                
                if (isActive)
                {
                    // Enable actions
                    BaseActions?.Enable();
                }
                else
                {
                    // Disable actions
                    BaseActions.Disable();
                }
            }

            get => isActive;
        }
        
        private void OnDestroy()
        {
            if (mobEffectUpdateCallback is not null)
                EventManager.Instance.Unregister(mobEffectUpdateCallback);
            if (mobEffectRemovalCallback is not null)
                EventManager.Instance.Unregister(mobEffectRemovalCallback);
            if (tickSyncCallback is not null)
                EventManager.Instance.Unregister(tickSyncCallback);
            if (slotUpdateCallback is not null)
                EventManager.Instance.Unregister(slotUpdateCallback);
            if (itemsUpdateCallback is not null)
                EventManager.Instance.Unregister(itemsUpdateCallback);
            if (propertyUpdateCallback is not null)
                EventManager.Instance.Unregister(propertyUpdateCallback);
        }

        public override bool ReleaseCursor()
        {
            return true;
        }

        public override bool ShouldPauseControllerInput()
        {
            return true;
        }

        private void CloseInventory()
        {
            var client = CornApp.CurrentClient;
            if (activeInventoryData is null || !client) return;

            if (activeInventoryId != 0) // Don't close player inventory
            {
                client.CloseInventory(activeInventoryId);
                
                EventManager.Instance.Broadcast(new InventoryCloseEvent(activeInventoryId));
            }
            
            client.ScreenControl.TryPopScreen();
            
            // Clear all item slots (which includes all backpack and hotbar slots which will be reused)
            foreach (var slot in currentSlots.Values)
            {
                slot.UpdateItemStack(null);
                slot.SetCursorTextHandler(null);
                slot.SetHoverHandler(null);
                slot.SetPointerDownHandler(null);
                slot.SetPointerUpHandler(null);
            }
            
            // Destroy all controls under work panel
            foreach (Transform t in workPanel)
            {
                Destroy(t.gameObject);
            }
            
            currentSlots.Clear();
            currentButtons.Clear();
            currentInputs.Clear();
            currentScrollViews.Clear();
            currentFilledSprites.Clear();
            currentTimerFlipbookSprites.Clear();
            currentPropertyFlipbookSprites.Clear();
            currentPropertyLabels.Clear();
            
            propertyDependents.Clear();
            propertyTable.Clear();
            
            fixedValuePropertyCount = 0;
            activeInventoryId = -1;
            activeInventoryData = null;
        }

        protected override void Initialize()
        {
            // Initialize controls and add listeners
            closeButton.onClick.AddListener(CloseInventory);
            screenRect = GetComponent<RectTransform>();
            
            inventoryBackground.SetEnterHandler(() =>
            {
                var game = CornApp.CurrentClient;
                if (!game) return;
                
                var cursor = game.GetInventory(0)?.Items.GetValueOrDefault(-1);
                if (cursor is null) return;
                
                if (cursorRect && cursorRect.TryGetComponent<Image>(out var image))
                {
                    image.enabled = true; // Display cursor frame
                }
            });
            
            inventoryBackground.SetExitHandler(() =>
            {
                var game = CornApp.CurrentClient;
                if (!game) return;
                
                if (cursorRect && cursorRect.TryGetComponent<Image>(out var image))
                {
                    image.enabled = false; // Hide cursor frame
                }
            });
            
            inventoryBackground.SetClickHandler(button =>
            {
                if (button == PointerEventData.InputButton.Middle) return;
                
                var game = CornApp.CurrentClient;
                if (!game) return;
                
                var cursor = game.GetInventory(0)?.Items.GetValueOrDefault(-1);
                if (cursor is null) return;

                try
                {
                    game.DoInventoryAction(activeInventoryId, -1,
                        button == PointerEventData.InputButton.Left ?
                            InventoryActionType.LeftClickDropOutside : InventoryActionType.RightClickDropOutside);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    throw;
                }
            });
            
            mobEffectUpdateCallback = e =>
            {
                var effectId = e.Effect.EffectId;
                var spriteTypeId = new ResourceLocation(
                    CornApp.RESOURCE_LOCATION_NAMESPACE, $"gui_mob_effect_{effectId.Path}");

                if (e.Effect.ShowIcon)
                {
                    mobEffectsCurrentTicks[effectId] = e.Effect.Duration;
                    mobEffectsPanel.AddIconSprite(effectId, spriteTypeId);
                    var seconds = e.Effect.Duration / 20;
                    mobEffectsPanel.UpdateIconFill(effectId, 0F); // Fill is not used for this view
                    
                    var effectName = ChatParser.TranslateString(effectId.GetTranslationKey("effect"));
                    
                    var potencyText = ChatParser.TranslateString($"potion.potency.{e.Effect.Amplifier}");
                    if (potencyText != string.Empty)
                        effectName = ChatParser.TranslateString("potion.withAmplifier", new List<string> { effectName, potencyText });
                    
                    mobEffectsNames[effectId] = effectName;
                    mobEffectsPanel.UpdateIconText(effectId, $"{effectName}\n<color=#AAAAAA>{Mathf.Min(seconds / 60, 99):D02}:{seconds % 60:D02}</color>");

                    // Show mob effects panel
                    rightScrollViewObject.SetActive(true);
                }
            };
            
            mobEffectRemovalCallback = e =>
            {
                var effectId = MobEffectPalette.INSTANCE.GetIdByNumId(e.EffectId);
                
                mobEffectsPanel.RemoveIconSprite(effectId);
                mobEffectsCurrentTicks.Remove(effectId);
                mobEffectsNames.Remove(effectId);
                
                if (mobEffectsCurrentTicks.Count == 0)
                {
                    rightScrollViewObject.SetActive(false);
                    // Setting the parent inactive will disable the fade out animator, and some items might already been
                    // unregistered, preventing remaining items to be removed, so we need to destroy these manually.
                    mobEffectsPanel.DestroyAllChildren();
                }
            };
            
            tickSyncCallback = e =>
            {
                if (mobEffectsCurrentTicks.Count > 0)
                {
                    foreach (var effectId in mobEffectsCurrentTicks.Keys.ToArray())
                    {
                        var updatedTicks = Mathf.Max(0, mobEffectsCurrentTicks[effectId] - e.PassedTicks);
                        mobEffectsCurrentTicks[effectId] = updatedTicks;
                        var blink = updatedTicks < 200; // Roughly 10 seconds at 20TPS
                        mobEffectsPanel.UpdateIconBlink(effectId, blink, blink ? 200F / Mathf.Max(40, updatedTicks) : 1F);
                        var seconds = updatedTicks / 20;
                        
                        mobEffectsPanel.UpdateIconText(effectId, $"{mobEffectsNames[effectId]}\n<color=#AAAAAA>{Mathf.Min(seconds / 60, 99):D02}:{seconds % 60:D02}</color>");
                    }
                }
            };
            
            slotUpdateCallback = e =>
            {
                if (!IsActive) return;
                if (dragging && !e.FromClient) return; // We'll handle this just fine
                
                if (e.InventoryId == activeInventoryId)
                {
                    if (currentSlots.TryGetValue(e.SlotId, out var slot))
                    {
                        var itemActuallyChanged = slot.UpdateItemStack(e.ItemStack);
                        if (itemActuallyChanged)
                        {
                            // Collect updated property names
                            updatedPropertyNames.Clear();
                        
                            // Handle change with custom logic
                            HandleSlotChange(e.SlotId, e.ItemStack);

                            if (e.SlotId == -1) // Update cursor slot
                            {
                                if ((e.ItemStack is null || e.ItemStack.Count <= 0) && cursorRect && cursorRect.TryGetComponent<Image>(out var image))
                                {
                                    image.enabled = false; // Hide cursor frame
                                }
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Slot {e.SlotId} is not available!");
                    }
                }
                else if (e.InventoryId == 0)
                {
                    if (e.SlotId == -1) // Update cursor slot
                    {
                        currentSlots[-1].UpdateItemStack(e.ItemStack);

                        if ((e.ItemStack is null || e.ItemStack.Count <= 0) && cursorRect && cursorRect.TryGetComponent<Image>(out var image))
                        {
                            image.enabled = false; // Hide cursor frame
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Unhandled update Inventory [0]/[{e.SlotId}], Item {e.ItemStack}");
                    }
                }
                else // Not current inventory, and not player inventory either
                {
                    Debug.LogWarning($"Invalid Inventory [{e.InventoryId}]/[{e.SlotId}], Item {e.ItemStack}");
                }
            };
            
            itemsUpdateCallback = e =>
            {
                if (!IsActive) return;
                if (dragging) return; // We'll handle this just fine
                
                if (e.InventoryId == activeInventoryId)
                {
                    foreach (var (slotId, itemStack) in e.Items)
                    {
                        if (currentSlots.TryGetValue(slotId, out var slot))
                        {
                            var itemActuallyChanged = slot.UpdateItemStack(itemStack);
                            if (itemActuallyChanged)
                            {
                                // Collect updated property names
                                updatedPropertyNames.Clear();
                        
                                // Handle change with custom logic
                                HandleSlotChange(slotId, itemStack);
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"Slot {slotId} is not available!");
                        }
                    }
                }
                else // Not current inventory
                {
                    Debug.LogWarning($"Invalid inventory id: {e.InventoryId}");
                }
            };
            
            propertyUpdateCallback = e =>
            {
                if (!IsActive) return;
                if (e.InventoryId == activeInventoryId)
                {
                    var propertyName = activeInventoryData.Type.PropertyNames.GetValueOrDefault(e.Property, $"prop_{e.Property}");
                    
                    Debug.Log($"Setting property [{activeInventoryId}]/[{e.Property}] {propertyName} to {e.Value}");
                    propertyTable[propertyName] = e.Value;
                    
                    // Collect updated property names
                    updatedPropertyNames.Clear();
                    updatedPropertyNames.Add(propertyName);

                    // Update pseudo properties
                    UpdatePseudoProperties(propertyName, e.Value);

                    // Update property-dependent fragments
                    UpdatePredicateDependents();
                    
                    // Update property labels
                    foreach (var (contPropName, textConverter, labelText) in currentPropertyLabels)
                    {
                        if (updatedPropertyNames.Contains(contPropName)) // This one needs to be updated
                        {
                            if (propertyTable.TryGetValue(contPropName, out var contPropValue))
                            {
                                labelText.text = textConverter(contPropValue);
                            }
                            else
                            {
                                Debug.LogWarning($"Failed to get property slot for {contPropName}, check inventory definition");
                            }
                        }
                    }
                    
                    // Update property flipbook sprites
                    foreach (var (propName, spriteType, spriteImage) in currentPropertyFlipbookSprites)
                    {
                        if (propertyTable.TryGetValue(propName, out var propValue) &&
                            propValue >= 0 && propValue < spriteType.FlipbookSprites.Length)
                        {
                            spriteImage.overrideSprite = spriteType.FlipbookSprites[propValue];
                        }
                    }
                    
                    // Update filled sprites
                    foreach (var (curPropName, maxPropName, spriteType, spriteImage) in currentFilledSprites)
                    {
                        if (updatedPropertyNames.Contains(curPropName) || updatedPropertyNames.Contains(maxPropName)) // This one needs to be updated
                        {
                            if (propertyTable.TryGetValue(curPropName, out var curPropValue) &&
                                propertyTable.TryGetValue(maxPropName, out var maxPropValue))
                            {
                                SpriteType.UpdateFilledSpriteImage(spriteType, spriteImage, curPropValue, maxPropValue);
                            }
                            else
                            {
                                Debug.LogWarning($"Failed to get property slot for {curPropName} or {maxPropName}, check inventory definition");
                            }
                        }
                    }
                }
                else // Not current inventory
                {
                    Debug.LogWarning($"Invalid inventory id: {e.InventoryId}, property {e.Property}");
                }
            };
            
            EventManager.Instance.Register(mobEffectUpdateCallback);
            EventManager.Instance.Register(mobEffectRemovalCallback);
            EventManager.Instance.Register(tickSyncCallback);
            EventManager.Instance.Register(slotUpdateCallback);
            EventManager.Instance.Register(itemsUpdateCallback);
            EventManager.Instance.Register(propertyUpdateCallback);
        }

        public override void UpdateScreen()
        {
            if (BaseActions.Interaction.CloseScreen.WasPressedThisFrame())
            {
                CloseInventory();
            }

            var game = CornApp.CurrentClient;
            if (!game) return;

            // Update cursor slot position
            var mousePos = Mouse.current.position.value;
            
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                screenRect, mousePos, game.UICamera, out Vector2 newPos);
            
            var tipPos = new Vector2(
                Mathf.Min(screenRect.rect.width / 2 - cursorTextPanel.rect.width, newPos.x),
                Mathf.Max(cursorTextPanel.rect.height - screenRect.rect.height / 2, newPos.y) );
            
            newPos = transform.TransformPoint(newPos);
            tipPos = transform.TransformPoint(tipPos);

            // Don't modify z coordinate
            cursorRect.position = new Vector3(newPos.x, newPos.y, cursorRect.position.z);
            cursorTextPanel.position = new Vector3(tipPos.x, tipPos.y, cursorTextPanel.position.z);

            if (currentTimerFlipbookSprites.Count > 0) // Update timer flipbook sprites
            {
                foreach (var tuple in currentTimerFlipbookSprites)
                {
                    var (timer, spriteType, spriteImage) = tuple;
                    
                    timer.Time += Time.deltaTime;
                    while (timer.Time > spriteType.FlipbookInterval)
                    {
                        timer.Time -= spriteType.FlipbookInterval;
                        timer.Frame = (timer.Frame + 1) % spriteType.FlipbookSprites.Length;
                    }
                    
                    spriteImage.overrideSprite = spriteType.FlipbookSprites[timer.Frame];
                }
            }
        }
    }
}