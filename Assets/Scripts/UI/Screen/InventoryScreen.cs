using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

using CraftSharp.Inventory;
using TMPro;

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

        private bool isActive = false;

        public InventoryData ActiveInventoryData = null; // -1 for none

        public void SetActiveInventory(InventoryData inventoryData)
        {
            EnsureInitialized();

            ActiveInventoryData = inventoryData;

            inventoryTitleText.text = inventoryData.Title;
        }

        public override bool IsActive
        {
            set {
                isActive = value;
                screenAnimator.SetBool(SHOW_HASH, isActive);
            }

            get => isActive;
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
            if (ActiveInventoryData is null || client == null) return;

            client.CloseInventory(ActiveInventoryData.Id);
            client.ScreenControl.TryPopScreen();

            ActiveInventoryData = null;
        }

        protected override void Initialize()
        {
            // Initialize controls and add listeners
            closeButton.onClick.AddListener(CloseInventory);
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