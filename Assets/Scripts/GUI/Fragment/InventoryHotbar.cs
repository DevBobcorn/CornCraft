#nullable enable
using System;
using UnityEngine;

using MinecraftClient.Event;

namespace MinecraftClient.UI
{
    public class InventoryHotbar : MonoBehaviour
    {
        public const int HOTBAR_LENGTH = 9;

        [SerializeField] private InventoryItemSlot[] itemSlots = { };
        [SerializeField] private RectTransform? slotSelection;

        private Action<HotbarUpdateEvent>? hotbarUpdateCallback;
        private Action<HeldItemChangeEvent>? heldItemChangeCallback;

        private void SelectSlot(int slot)
        {
            slotSelection!.SetParent(itemSlots[slot].transform, false);
            slotSelection.SetAsLastSibling();
        }

        public void Start()
        {
            hotbarUpdateCallback = (e) => {
                itemSlots[e.HotbarSlot].UpdateItemStack(e.ItemStack);
            };

            heldItemChangeCallback = (e) => {
                SelectSlot(e.HotbarSlot);
            };

            EventManager.Instance.Register(hotbarUpdateCallback);
            EventManager.Instance.Register(heldItemChangeCallback);
        }

        void OnDestroy()
        {
            if (hotbarUpdateCallback is not null)
                EventManager.Instance.Unregister(hotbarUpdateCallback);
            
            if (heldItemChangeCallback is not null)
                EventManager.Instance.Unregister(heldItemChangeCallback);
        }
    }
}