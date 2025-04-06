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
        [SerializeField] private RectTransform listPanel, workPanel, backpackPanel, hotbarPanel;
        [SerializeField] private InventoryItemSlot[] backpackSlots;
        [SerializeField] private InventoryItemSlot[] hotbarSlots;

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

            inventoryTitleText.text = inventoryData.Title;
            Debug.Log($"Set inventory: [{ActiveInventoryId}] {inventoryData.Title}");
            
            if (inventoryData.Type.TypeId == InventoryType.MERCHANT_ID)
            {
                // TODO: Initialize trade list
                Debug.Log("Initialize trade list");
                
                // Show list panel
                listPanel.gameObject.SetActive(true);
            }
            else
            {
                // Hide list panel
                listPanel.gameObject.SetActive(false);
            }

            if (inventoryData.Type.HasBackpackSlots)
            {
                var backpackStart = inventoryData.GetFirstBackpackSlot();
                
                // Initialize backpack slots
                for (int i = 0; i < backpackSlots.Length; i++)
                {
                    backpackSlots[i].UpdateItemStack(inventoryData.Items.GetValueOrDefault(backpackStart + i));
                    backpackSlots[i].gameObject.name = $"Slot [{backpackStart + i}] (Backpack)";
                    currentSlots[backpackStart + i] = backpackSlots[i];
                }
                backpackPanel.gameObject.SetActive(true);
            }
            else
            {
                backpackPanel.gameObject.SetActive(false);
            }
            
            if (inventoryData.Type.HasHotbarSlots)
            {
                var hotbarStart = inventoryData.GetFirstHotbarSlot();
                
                // Initialize hotbar slots
                for (int i = 0; i < hotbarSlots.Length; i++)
                {
                    hotbarSlots[i].UpdateItemStack(inventoryData.Items.GetValueOrDefault(hotbarStart + i));
                    hotbarSlots[i].gameObject.name = $"Slot [{hotbarStart + i}] (Hotbar)";
                    currentSlots[hotbarStart + i] = hotbarSlots[i];
                }
                hotbarPanel.gameObject.SetActive(true);
            }
            else
            {
                hotbarPanel.gameObject.SetActive(false);
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
            // Escape key cannot be used here, otherwise it will push pause screen back after poping it
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CloseInventory();
            }
        }
    }
}