using System;
using System.Collections.Generic;
using CraftSharp.Event;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

using CraftSharp.Inventory;
using CraftSharp.Protocol;

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

        public int ActiveInventoryId = -1; // -1 for none
        public InventoryData ActiveInventoryData = null;
        
#nullable enable

        private Action<SlotUpdateEvent>? slotUpdateCallback;

#nullable disable

        public void SetActiveInventory(InventoryData inventoryData)
        {
            EnsureInitialized();

            ActiveInventoryId = inventoryData.Id;
            ActiveInventoryData = inventoryData;
            
            var inventoryType = inventoryData.Type;

            inventoryTitleText.text = inventoryData.Title;
            Debug.Log($"Set inventory: [{ActiveInventoryId}] {inventoryData.Title}");
            
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
                    $"Slot [{i}] (Work Prepend) [{inventoryType.GetInventorySlotType(i)}]");
                
                setupSlot(i, newSlot);
            }
            
            var workMainStart = inventoryType.PrependSlotCount;
            var workMainPosX = inventoryType.MainPosX;
            var workMainPosY = inventoryType.MainPosY;
            for (int x = 0, i = 0; x < inventoryType.MainSlotWidth; x++)
                for (int y = 0; y < inventoryType.MainSlotHeight; y++, i++)
                {
                    var newSlot = createSlot(x + workMainPosX,
                        workMainPosY + inventoryType.MainSlotHeight - y - 1,
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
                    $"Slot [{i}] (Work Append) [{inventoryType.GetInventorySlotType(i)}]");

                setupSlot(i, newSlot);
            }

            // Initialize cursor slot
            currentSlots[-1] = cursorSlot;
            setupSlot(-1, cursorSlot);

            cursorTextPanel.gameObject.SetActive(false);

            return;
            
            void createSprite(int x, int y, int w, int h, ResourceLocation spriteTypeId)
            {
                var spriteObj = Instantiate(inventorySpritePrefab, workPanel);//new GameObject($"Sprite {spriteTypeId}");
                var spriteImage = spriteObj.GetComponent<Image>();
                var rectTransform = spriteObj.GetComponent<RectTransform>();

                spriteImage.overrideSprite = SpriteTypePalette.INSTANCE.GetById(spriteTypeId).Sprite;
                
                rectTransform.anchoredPosition =
                    new Vector2(x * inventorySlotSize, y * inventorySlotSize);
                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w * inventorySlotSize);
                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h * inventorySlotSize);
                
                spriteObj.name = $"Sprite [{spriteTypeId}]";
            }

            InventoryItemSlot createSlot(int x, int y, string slotName)
            {
                var slotObj = Instantiate(inventorySlotPrefab, workPanel);
                var slot = slotObj.GetComponent<InventoryItemSlot>();
                
                slotObj.GetComponent<RectTransform>().anchoredPosition =
                    new Vector2(x * inventorySlotSize, y * inventorySlotSize);
                slotObj.name = slotName;

                return slot;
            }

            void setupSlot(int slotId, InventoryItemSlot slot)
            {
                currentSlots[slotId] = slot;

                slot.SetClickHandler(() =>
                {
                    Debug.Log($"Mouse down: {slotId}");
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
            if (ActiveInventoryData is null || !client) return;

            client.CloseInventory(ActiveInventoryId);
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

            ActiveInventoryId = -1;
            ActiveInventoryData = null;
        }

        protected override void Initialize()
        {
            // Initialize controls and add listeners
            closeButton.onClick.AddListener(CloseInventory);
            
            slotUpdateCallback = e =>
            {
                if (e.InventoryId == ActiveInventoryId)
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