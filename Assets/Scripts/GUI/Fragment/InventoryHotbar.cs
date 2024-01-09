#nullable enable
using System;
using UnityEngine;

using CraftSharp.Event;

namespace CraftSharp.UI
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

        public void ShowItems()
        {
            for (int i = 0; i < itemSlots.Length; i++)
            {
                itemSlots[i].ShowItemStack();
            }
        }

        public void HideItems()
        {
            for (int i = 0; i < itemSlots.Length; i++)
            {
                itemSlots[i].HideItemStack();
            }
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