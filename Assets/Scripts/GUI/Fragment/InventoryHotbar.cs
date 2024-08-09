using System;
using UnityEngine;

using CraftSharp.Event;
using UnityEngine.UI;

namespace CraftSharp.UI
{
    public class InventoryHotbar : MonoBehaviour
    {
        public const int HOTBAR_LENGTH = 9;

        [SerializeField] private InventoryItemSlot[] itemSlots = { };

        private InventoryItemSlot selectedSlot;

        #nullable enable

        private Action<HotbarUpdateEvent>? hotbarUpdateCallback;
        private Action<HeldItemChangeEvent>? heldItemChangeCallback;

        #nullable disable

        private void SelectSlot(int slot)
        {
            if (selectedSlot != null)
            {
                selectedSlot.DeselectSlot();
            }

            selectedSlot = itemSlots[slot];
            selectedSlot.SelectSlot();
        }

        public void Start()
        {
            for (int i = 0; i < itemSlots.Length; i++)
            {
                itemSlots[i].SetKeyHint((i + 1).ToString());
            }

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