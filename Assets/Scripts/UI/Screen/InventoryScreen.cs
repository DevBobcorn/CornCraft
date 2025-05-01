using System;
using System.Collections.Generic;
using CraftSharp.Event;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

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
        [SerializeField] private GameObject inventorySpritePrefab;
        [SerializeField] private RectTransform listPanel, workPanel, backpackPanel, hotbarPanel;
        [SerializeField] private InventoryItemSlot cursorSlot;
        [SerializeField] private RectTransform cursorTextPanel;
        [SerializeField] private TMP_Text cursorText;
        [SerializeField] private InventoryItemSlot[] backpackSlots;
        [SerializeField] private InventoryItemSlot[] hotbarSlots;
        [SerializeField] private float inventorySlotSize = 90F;

        private readonly Dictionary<int, InventoryItemSlot> currentSlots = new();

        private bool isActive = false;

        private int activeInventoryId = -1; // -1 for none
        private InventoryData activeInventoryData = null;
        
#nullable enable

        private Action<SlotUpdateEvent>? slotUpdateCallback;

#nullable disable

        public void SetActiveInventory(InventoryData inventoryData)
        {
            EnsureInitialized();

            var game = CornApp.CurrentClient;

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
            
            // Populate work panel sprites
            foreach (var sprite in inventoryType.spriteInfo)
            {
                createSprite(sprite.PosX, sprite.PosY,
                    sprite.Width, sprite.Height, sprite.TypeId);
            }

            // Populate work panel slots
            for (int i = 0; i < inventoryType.PrependSlotCount; i++)
            {
                var slotPos = inventoryType.GetInventorySlotPos(i);
                
                var newSlot = createSlot(slotPos.x, slotPos.y,
                    inventoryType.GetInventorySlotPreviewItem(i),
                    inventoryType.GetInventorySlotPlaceholderSpriteTypeId(i),
                    $"Slot [{i}] (Work Prepend) [{inventoryType.GetInventorySlotType(i)}]");
                
                setupSlot(i, newSlot);
            }
            
            var workMainStart = inventoryType.PrependSlotCount;
            var workMainPosX = inventoryType.MainPosX;
            var workMainPosY = inventoryType.MainPosY;
            for (int y = 0, i = 0; y < inventoryType.MainSlotHeight; y++)
                for (int x = 0; x < inventoryType.MainSlotWidth; x++, i++)
                {
                    var newSlot = createSlot(x + workMainPosX,
                        workMainPosY + inventoryType.MainSlotHeight - y - 1,
                        null, null,
                        $"Slot [{workMainStart + i}] (Work Main)");
                    
                    setupSlot(workMainStart + i, newSlot);
                }

            if (inventoryType.HasBackpackSlots)
            {
                var backpackStart = inventoryData.GetFirstBackpackSlot();
                
                // Initialize backpack slots
                for (int i = 0; i < backpackSlots.Length; i++)
                {
                    backpackSlots[i].UpdateItemStack(inventoryData.Items.GetValueOrDefault(backpackStart + i));
                    backpackSlots[i].gameObject.name = $"Slot [{backpackStart + i}] (Backpack)";
                    
                    setupSlot(backpackStart + i, backpackSlots[i]);
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

                    setupSlot(hotbarStart + i, hotbarSlots[i]);
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
                
                var newSlot = createSlot(slotPos.x, slotPos.y,
                    inventoryType.GetInventorySlotPreviewItem(i),
                    inventoryType.GetInventorySlotPlaceholderSpriteTypeId(i),
                    $"Slot [{i}] (Work Append) [{inventoryType.GetInventorySlotType(i)}]");

                setupSlot(i, newSlot);
            }

            // Initialize cursor slot
            currentSlots[-1] = cursorSlot;
            setupSlot(-1, cursorSlot);
            
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

            cursorTextPanel.gameObject.SetActive(false);

            return;
            
            void createSprite(float x, float y, int w, int h, ResourceLocation spriteTypeId)
            {
                var spriteObj = Instantiate(inventorySpritePrefab, workPanel);//new GameObject($"Sprite {spriteTypeId}");
                var spriteImage = spriteObj.GetComponent<Image>();
                var rectTransform = spriteObj.GetComponent<RectTransform>();

                spriteImage.type = Image.Type.Simple;
                spriteImage.overrideSprite = SpriteTypePalette.INSTANCE.GetById(spriteTypeId).Sprite;
                
                rectTransform.anchoredPosition =
                    new Vector2(x * inventorySlotSize, y * inventorySlotSize);
                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w * inventorySlotSize);
                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h * inventorySlotSize);
                
                spriteObj.name = $"Sprite [{spriteTypeId}]";
            }

            InventoryItemSlot createSlot(float x, float y, ItemStack previewItem,
                ResourceLocation? placeholderSpriteTypeId, string slotName)
            {
                var slotObj = Instantiate(inventorySlotPrefab, workPanel);
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

                return slot;
            }

            void setupSlot(int slotId, InventoryItemSlot slot)
            {
                currentSlots[slotId] = slot;

                slot.SetClickHandler(action =>
                {
                    if (!game) return;
                    game.DoInventoryAction(activeInventoryId, slotId, action);
                });

                slot.SetCursorTextHandler(str =>
                {
                    if (string.IsNullOrEmpty(str))
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
                slot.SetClickHandler(null);
            }
            
            // Destroy all sprites and slots under work panel
            foreach (Transform t in workPanel)
            {
                Destroy(t.gameObject);
            }
            
            currentSlots.Clear();

            activeInventoryId = -1;
            activeInventoryData = null;
        }

        protected override void Initialize()
        {
            // Initialize controls and add listeners
            closeButton.onClick.AddListener(CloseInventory);
            
            slotUpdateCallback = e =>
            {
                if (e.InventoryId == activeInventoryId)
                {
                    if (currentSlots.TryGetValue(e.SlotId, out var slot))
                    {
                        slot.UpdateItemStack(e.ItemStack);
                    }
                    else
                    {
                        Debug.LogWarning($"Slot {e.SlotId} is not available!");
                    }
                }
                else if (e.InventoryId != 0)
                {
                    Debug.Log($"Invalid inventory id: {e.InventoryId}, slot {e.SlotId}");
                }
            };
            
            EventManager.Instance.Register(slotUpdateCallback);
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
        }
    }
}