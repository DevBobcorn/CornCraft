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

namespace CraftSharp.UI
{
    [RequireComponent(typeof (CanvasGroup))]
    public class InventoryScreen : BaseScreen
    {
        // UI controls and objects
        [SerializeField] private TMP_Text inventoryTitleText;
        [SerializeField] private Button closeButton;
        [SerializeField] private Animator screenAnimator;
        [SerializeField] private GameObject inventorySlotPrefab;
        [SerializeField] private GameObject inventoryInputPrefab;
        [SerializeField] private GameObject inventoryLabelPrefab;
        [SerializeField] private GameObject inventoryButtonPrefab;
        [SerializeField] private GameObject inventorySpritePrefab;
        [SerializeField] private RectTransform listPanel, workPanel, backpackPanel, hotbarPanel;
        [SerializeField] private InventoryItemSlot cursorSlot;
        [SerializeField] private RectTransform cursorTextPanel;
        [SerializeField] private TMP_Text cursorText;
        [SerializeField] private InventoryItemSlot[] backpackSlots;
        [SerializeField] private InventoryItemSlot[] hotbarSlots;
        [SerializeField] private float inventorySlotSize = 90F;
        
        [SerializeField] private TMP_Text propertyPreviewText;

        private readonly Dictionary<int, InventoryItemSlot> currentSlots = new();
        private readonly Dictionary<int, InventoryInput> currentInputs = new();
        private readonly Dictionary<int, InventoryButton> currentButtons = new();

        // (cur_value_property, max_value_property, sprite_type, sprite_image)
        private readonly List<(string, string, SpriteType, Image)> currentFilledSprites = new();
        // (flipbook_timer, sprite_type, sprite_image)
        private readonly List<(SpriteType.FlipbookTimer, SpriteType, Image)> currentFlipbookSprites = new();
        // (predicate_type, predicate, inventory_fragment)
        private readonly List<(InventoryType.PredicateType, InventoryPropertyPredicate, MonoBehaviour)> propertyDependents = new();

        private bool isActive = false;

        private int activeInventoryId = -1; // -1 for none
        private InventoryData activeInventoryData = null;
        private readonly Dictionary<string, short> propertyTable = new();
        
#nullable enable

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
            
            var inventoryType = inventoryData.Type;

            inventoryTitleText.text = inventoryData.Title;
            Debug.Log($"Set inventory: [{activeInventoryId}] {inventoryData.Title}");
            
            if (inventoryType.ListPanelWidth > 0)
            {
                // TODO: Initialize trade list
                Debug.Log("Initialize trade list");
                
                // Show list panel
                listPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
                    inventoryType.ListPanelWidth * inventorySlotSize);
                listPanel.gameObject.SetActive(true);
            }
            else
            {
                // Hide list panel
                listPanel.gameObject.SetActive(false);
            }
            
            // Update work panel height
            workPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
                inventoryType.WorkPanelHeight * inventorySlotSize);
            
            // Populate work panel controls, except slots
            CreateLayout(workPanel, inventoryType.WorkPanelLayout, false);

            // Populate work panel slots
            for (int i = 0; i < inventoryType.PrependSlotCount; i++)
            {
                var slotPos = inventoryType.GetInventorySlotPos(i);
                
                CreateSlot(workPanel, i, slotPos.x, slotPos.y, inventoryType.GetInventorySlotPreviewItem(i),
                    inventoryType.GetInventorySlotPlaceholderSpriteTypeId(i),
                    $"Slot [{i}] (Work Prepend) [{inventoryType.GetInventorySlotType(i).TypeId}]");
            }
            
            var workMainStart = inventoryType.PrependSlotCount;
            var workMainPosX = inventoryType.MainPosX;
            var workMainPosY = inventoryType.MainPosY;
            for (int y = 0, i = 0; y < inventoryType.MainSlotHeight; y++)
                for (int x = 0; x < inventoryType.MainSlotWidth; x++, i++)
                {
                    CreateSlot(workPanel, workMainStart + i, x + workMainPosX,
                        workMainPosY + inventoryType.MainSlotHeight - y - 1,
                        null, null, $"Slot [{workMainStart + i}] (Work Main)");
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
                var slotPos = inventoryType.GetInventorySlotPos(i);
                
                CreateSlot(workPanel, i, slotPos.x, slotPos.y, inventoryType.GetInventorySlotPreviewItem(i),
                    inventoryType.GetInventorySlotPlaceholderSpriteTypeId(i),
                    $"Slot [{i}] (Work Append) [{inventoryType.GetInventorySlotType(i).TypeId}]");
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
        }

        private InventoryItemSlot CreateSlot(RectTransform parent, int slotId, float x, float y, ItemStack previewItem,
                ResourceLocation? placeholderSpriteTypeId, string slotName)
        {
            var slotObj = Instantiate(inventorySlotPrefab, parent);
            var slot = slotObj.GetComponent<InventoryItemSlot>();
            
            slotObj.GetComponent<RectTransform>().anchoredPosition =
                new Vector2(x * inventorySlotSize, y * inventorySlotSize);
            slotObj.name = slotName;

            if (placeholderSpriteTypeId.HasValue) // Set placeholder sprite
            {
                var placeholderSprite = SpriteTypePalette.INSTANCE.GetById(
                    placeholderSpriteTypeId.Value).Sprite;
                slot.SetPlaceholderSprite(placeholderSprite);
            }

            if (previewItem is not null)
            {
                slot.UpdateItemStack(previewItem);
            }

            SetupSlot(slotId, slot);

            return slot;
        }

        private void SetupSlot(int slotId, InventoryItemSlot slot)
        {
            currentSlots[slotId] = slot;
            var slotType = activeInventoryData.Type.GetInventorySlotType(slotId);

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
                        Debug.Log($"Adding {slotId}, Dragged slots: {string.Join(", ", draggedSlots)}");
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

            slot.SetCursorTextHandler(str =>
            {
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
                }
            });
        }

        private void CreateLayout(RectTransform parent, InventoryType.InventoryLayoutInfo layoutInfo, bool createSlots)
        {
            // Populate layout sprites
            foreach (var spriteInfo in layoutInfo.SpriteInfo)
            {
                var spriteType = SpriteTypePalette.INSTANCE.GetById(spriteInfo.TypeId);
                var sprite = CreateSprite(parent, spriteInfo.PosX, spriteInfo.PosY, spriteInfo.Width, spriteInfo.Height,
                    spriteType, spriteInfo.CurFillProperty, spriteInfo.MaxFillProperty);
                RegisterPropertyDependent(spriteInfo, sprite);
            }

            // Populate layout labels
            foreach (var labelInfo in layoutInfo.LabelInfo)
            {
                var label = CreateLabel(parent, labelInfo.PosX, labelInfo.PosY, labelInfo.Width,
                    labelInfo.Alignment, labelInfo.TextTranslationKey);
                RegisterPropertyDependent(labelInfo, label);
            }

            // Populate layout inputs
            foreach (var (inputId, inputInfo) in layoutInfo.InputInfo)
            {
                var input = CreateInput(parent, inputId, inputInfo.PosX, inputInfo.PosY,
                    inputInfo.Width, inputInfo.PlaceholderTranslationKey, $"Input [{inputId}]");
                RegisterPropertyDependent(inputInfo, input);
            }

            // Populate layout buttons
            foreach (var (buttonId, buttonInfo) in layoutInfo.ButtonInfo)
            {
                var button = CreateButton(parent, buttonId, buttonInfo.PosX, buttonInfo.PosY,
                    buttonInfo.Width, buttonInfo.Height, buttonInfo.LayoutInfo, $"Button [{buttonId}]");
                RegisterPropertyDependent(buttonInfo, button);
            }

            if (createSlots)
            {
                // Populate layout slots
                foreach (var (slotId, slotInfo) in layoutInfo.SlotInfo)
                {
                    var slot = CreateSlot(parent, slotId, slotInfo.PosX, slotInfo.PosY, slotInfo.PreviewItemStack,
                        slotInfo.PlaceholderTypeId, $"Slot [{slotId}] (Nested)");
                    RegisterPropertyDependent(slotInfo, slot);
                }
            }
        }

        private TMP_Text CreateLabel(RectTransform parent, float x, float y, float w, InventoryType.LabelAlignment alignment, string translationKey)
        {
            var labelObj = Instantiate(inventoryLabelPrefab, parent);
            var labelText = labelObj.GetComponent<TMP_Text>();
            var rectTransform = labelObj.GetComponent<RectTransform>();

            rectTransform.anchoredPosition =
                new Vector2(x * inventorySlotSize, y * inventorySlotSize);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w * inventorySlotSize);

            labelText.text = Translations.Get(translationKey);
            labelText.horizontalAlignment = alignment switch
            {
                InventoryType.LabelAlignment.Left => HorizontalAlignmentOptions.Left,
                InventoryType.LabelAlignment.Center => HorizontalAlignmentOptions.Center,
                InventoryType.LabelAlignment.Right => HorizontalAlignmentOptions.Right,
                _ => throw new InvalidDataException($"Label alignment {alignment} is not defined!"),
            };

            return labelText;
        }

        private InventoryInput CreateInput(RectTransform parent, int inputId, float x, float y, float w,
                string translationKey, string inputName)
        {
            var inputObj = Instantiate(inventoryInputPrefab, parent);
            var input = inputObj.GetComponent<InventoryInput>();
            var rectTransform = inputObj.GetComponent<RectTransform>();

            inputObj.name = inputName;
            rectTransform.anchoredPosition =
                new Vector2(x * inventorySlotSize, y * inventorySlotSize);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w * inventorySlotSize);

            input.SetPlaceholderText(Translations.Get(translationKey));

            currentInputs[inputId] = input;

            return input;
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

            currentButtons[buttonId] = button;

            return button;
        }

        private Image CreateSprite(RectTransform parent, float x, float y, float w, float h, SpriteType spriteType, string curFillProp, string maxFillProp)
        {
            var spriteObj = Instantiate(inventorySpritePrefab, parent);
            var spriteImage = spriteObj.GetComponent<Image>();
            var rectTransform = spriteObj.GetComponent<RectTransform>();

            SpriteType.SetupSpriteImage(spriteType, spriteImage);
            
            rectTransform.anchoredPosition =
                new Vector2(x * inventorySlotSize, y * inventorySlotSize);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w * inventorySlotSize);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h * inventorySlotSize);
            
            spriteObj.name = $"Sprite [{spriteType.TypeId}]";

            if (spriteType.ImageType == SpriteType.SpriteImageType.Filled)
            {
                if (curFillProp is null || maxFillProp is null)
                {
                    Debug.LogWarning($"Filled sprite properties for sprite {spriteType.TypeId} is not specified!");
                }
                // Add it to the list
                currentFilledSprites.Add((curFillProp, maxFillProp, spriteType, spriteImage));
            }
            
            if (spriteType.FlipbookSprites is null)
            {
                Debug.LogWarning($"Flipbook sprite for {spriteType.TypeId} is null!");
            }
            else if (spriteType.FlipbookSprites.Length > 0)
            {
                // Add it to the list
                currentFlipbookSprites.Add((new(), spriteType, spriteImage));
            }

            return spriteImage;
        }

        private void RegisterPropertyDependent(InventoryType.InventoryFragmentInfo fragmentInfo, MonoBehaviour inventoryFragment)
        {
            foreach (var (predicateType, predicate) in fragmentInfo.Predicates)
            {
                propertyDependents.Add((predicateType, predicate, inventoryFragment));
            }
        }

        private void UpdatePredicateDependents()
        {
            foreach (var (predicateType, predicate, inventoryFragment) in propertyDependents)
            {
                var predicateResult = predicate.Check(propertyTable);
                
                Debug.Log($"Set {predicateType} status of {inventoryFragment.gameObject.name} to {predicateResult}");

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
        }

        public override bool IsActive
        {
            set {
                isActive = value;
                screenAnimator.SetBool(SHOW_HASH, isActive);
            }

            get => isActive;
        }
        
        private void OnDestroy()
        {
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
            }
            
            client.ScreenControl.TryPopScreen();
            
            // Clear all item slots
            foreach (var slot in currentSlots.Values)
            {
                slot.UpdateItemStack(null);
                slot.SetCursorTextHandler(null);
                slot.SetHoverHandler(null);
                slot.SetPointerDownHandler(null);
                slot.SetPointerUpHandler(null);
            }
            
            // Destroy all sprites and slots under work panel
            foreach (Transform t in workPanel)
            {
                Destroy(t.gameObject);
            }
            
            currentSlots.Clear();
            currentFilledSprites.Clear();
            currentFlipbookSprites.Clear();
            propertyDependents.Clear();
            propertyTable.Clear();

            if (propertyPreviewText)
            {
                propertyPreviewText.text = string.Empty;
            }

            activeInventoryId = -1;
            activeInventoryData = null;
        }

        protected override void Initialize()
        {
            // Initialize controls and add listeners
            closeButton.onClick.AddListener(CloseInventory);
            
            slotUpdateCallback = e =>
            {
                if (!IsActive) return;
                if (dragging && !e.FromClient) return; // We'll handle this just fine
                
                if (e.InventoryId == activeInventoryId)
                {
                    if (currentSlots.TryGetValue(e.Slot, out var slot))
                    {
                        slot.UpdateItemStack(e.ItemStack);
                    }
                    else
                    {
                        Debug.LogWarning($"Slot {e.Slot} is not available!");
                    }
                }
                else if (e.InventoryId == 0)
                {
                    if (e.Slot == -1) // Update cursor slot
                    {
                        currentSlots[-1].UpdateItemStack(e.ItemStack);
                    }
                    else
                    {
                        Debug.LogWarning($"Unhandled update Inventory [0]/[{e.Slot}], Item {e.ItemStack}");
                    }
                }
                else // Not current inventory, and not player inventory either
                {
                    Debug.LogWarning($"Invalid Inventory [{e.InventoryId}]/[{e.Slot}], Item {e.ItemStack}");
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
                            slot.UpdateItemStack(itemStack);
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
                    var propertyName = activeInventoryData.Type.PropertyNames.GetValueOrDefault(e.Property, "unnamed");
                    
                    Debug.Log($"Setting property [{activeInventoryId}]/[{e.Property}] {propertyName} to {e.Value}");

                    var propertyNames = activeInventoryData.Type.PropertyNames;
                    propertyTable[propertyNames[e.Property]] = e.Value;

                    // Update pseudo properties
                    if (activeInventoryData.Type.TypeId == InventoryType.BEACON_ID)
                    {
                        ResourceLocation SPEED_ID = new("speed");
                        ResourceLocation HASTE_ID = new("haste");
                        ResourceLocation RESISTANCE_ID = new("resistance");
                        ResourceLocation JUMP_BOOST_ID = new("jump_boost");
                        ResourceLocation STRENGTH_ID = new("strength");
                        ResourceLocation REGENERATION_ID = new("regeneration");

                        if (propertyName == "first_potion_effect")
                        {
                            var firstEffect = MobEffectPalette.INSTANCE.GetByNumId(e.Value);
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
                            var secondEffect = MobEffectPalette.INSTANCE.GetByNumId(e.Value);
                            //Debug.Log($"Second effect: {secondEffect.GetDescription()}");
                            short secondIndex = -1;
                            if (secondEffect.MobEffectId == REGENERATION_ID) secondIndex = 0;
                            else if (secondEffect.MobEffectId == SPEED_ID || secondEffect.MobEffectId == HASTE_ID ||
                                secondEffect.MobEffectId == RESISTANCE_ID || secondEffect.MobEffectId == JUMP_BOOST_ID ||
                                secondEffect.MobEffectId == STRENGTH_ID) secondIndex = 1;

                            SetPseudoProperty("second_potion_effect_index", secondIndex);
                        }
                    }

                    // Update property-dependent fragments
                    UpdatePredicateDependents();
                    
                    // Update property preview text (if present)
                    if (propertyPreviewText)
                    {
                        propertyPreviewText.text = string.Join('\n', propertyTable.Select(x => $"{x.Key}: {x.Value, 3}"));
                    }
                    
                    // Update filled sprites
                    foreach (var (curPropName, maxPropName, spriteType, spriteImage) in currentFilledSprites)
                    {
                        if (curPropName == propertyName || maxPropName == propertyName) // This one needs to be updated
                        {
                            if (activeInventoryData.Type.PropertySlots.TryGetValue(curPropName, out var curProp) &&
                                activeInventoryData.Type.PropertySlots.TryGetValue(maxPropName, out var maxProp))
                            {
                                if (activeInventoryData.Properties.TryGetValue(curProp, out var curPropValue) &&
                                    activeInventoryData.Properties.TryGetValue(maxProp, out var maxPropValue))
                                {
                                    SpriteType.UpdateFilledSpriteImage(spriteType, spriteImage, curPropValue, maxPropValue);
                                }
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

                return;

                void SetPseudoProperty(string propertyName, short propertyValue)
                {
                    propertyTable[propertyName] = propertyValue;
                    Debug.Log($"Setting property [{activeInventoryId}]/[pseudo] {propertyName} to {propertyValue}");
                }
            };
            
            EventManager.Instance.Register(slotUpdateCallback);
            EventManager.Instance.Register(itemsUpdateCallback);
            EventManager.Instance.Register(propertyUpdateCallback);

            // Clear property preview text
            if (propertyPreviewText)
            {
                propertyPreviewText.text = string.Empty;
            }
        }

        public override void UpdateScreen()
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CloseInventory();
            }

            var game = CornApp.CurrentClient;
            if (!game) return;

            // Update cursor slot position
            var cursorRect = cursorSlot.GetComponent<RectTransform>();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                transform as RectTransform, Mouse.current.position.value,
                game.UICamera, out Vector2 newPos);
            
            newPos = transform.TransformPoint(newPos);

            // Don't modify z coordinate
            cursorRect.position = new Vector3(newPos.x, newPos.y, cursorRect.position.z);

            if (currentFlipbookSprites.Count > 0) // Update flipbook sprites
            {
                foreach (var tuple in currentFlipbookSprites)
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