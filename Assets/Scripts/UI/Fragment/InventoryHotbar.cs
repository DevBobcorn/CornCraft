using System;
using UnityEngine;

using CraftSharp.Event;

namespace CraftSharp.UI
{
    public class InventoryHotbar : MonoBehaviour, IAlphaListener
    {
        private static readonly int SHOW_HASH = Animator.StringToHash("Show");
        public const int HOTBAR_LENGTH = 9;

        [SerializeField] private InventoryItemSlot[] itemSlots = { };
        [SerializeField] private InventoryItemSlot offhandItemSlot;

        [SerializeField] private Animator hotbarAnimator;

        private InventoryItemSlot selectedSlot;

        #nullable enable

        private Action<HotbarUpdateEvent>? hotbarUpdateCallback;
        private Action<HeldItemChangeEvent>? heldItemChangeCallback;

        #nullable disable

        private CanvasGroup[] parentCanvasGroups = { };
        private float selfAlpha = 1F;

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
            parentCanvasGroups = GetComponentsInParent<CanvasGroup>(true);
            
            for (int i = 0; i < itemSlots.Length; i++)
            {
                itemSlots[i].SetKeyHint((i + 1).ToString());
            }

            offhandItemSlot.SetKeyHint("F");

            hotbarUpdateCallback = (e) => {
                if (e.HotbarSlot == HOTBAR_LENGTH) // Update offhand slot
                {
                    offhandItemSlot.UpdateItemStack(e.ItemStack);
                    var offhandIsEmpty = e.ItemStack?.IsEmpty ?? true;

                    hotbarAnimator.SetBool(SHOW_HASH, !offhandIsEmpty);
                }
                else if (e.HotbarSlot >= 0 && e.HotbarSlot < HOTBAR_LENGTH) // Update hotbar slot
                {
                    itemSlots[e.HotbarSlot].UpdateItemStack(e.ItemStack);
                }
                else
                {
                    Debug.LogWarning($"Trying to set hotbar at invalid index: {e.HotbarSlot}");
                }
            };

            heldItemChangeCallback = (e) => {
                SelectSlot(e.HotbarSlot);
            };

            EventManager.Instance.Register(hotbarUpdateCallback);
            EventManager.Instance.Register(heldItemChangeCallback);
        }

        public void UpdateAlpha(float alpha)
        {
            for (int i = 0; i < HOTBAR_LENGTH; i++)
            {
                itemSlots[i].SetSlotItemScale(alpha);
            }

            offhandItemSlot.SetSlotItemScale(alpha);

            selfAlpha = alpha;
        }

        void Update()
        {
            if (parentCanvasGroups.Length > 0)
            {
                float updatedAlpha = 1F;

                for (int i = 0; i < parentCanvasGroups.Length; i++)
                {
                    if ((!parentCanvasGroups[i].gameObject.activeSelf) || parentCanvasGroups[i].alpha == 0F)
                    {
                        updatedAlpha = 0F;
                        break;
                    }

                    updatedAlpha *= parentCanvasGroups[i].alpha;
                }

                if (selfAlpha != updatedAlpha)
                {
                    UpdateAlpha(updatedAlpha);
                }
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