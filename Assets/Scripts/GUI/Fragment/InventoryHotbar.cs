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

        private Action<HotbarUpdateEvent>? hotbarUpdateCallback;

        public void Start()
        {
            hotbarUpdateCallback = (e) => {
                itemSlots[e.HotbarSlot].UpdateItemStack(e.ItemStack);
            };

            EventManager.Instance.Register(hotbarUpdateCallback);
        }
    }
}