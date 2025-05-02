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

        private Action<HotbarSlotUpdateEvent>? hotbarUpdateCallback;
        private Action<HeldItemUpdateEvent>? heldItemChangeCallback;

        #nullable disable

        private CanvasGroup[] parentCanvasGroups = { };
        private float selfAlpha = 1F;

        private void SelectSlot(int slot)
        {
            if (selectedSlot)
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

            hotbarUpdateCallback = e =>
            {
                if (e.HotbarSlot == HOTBAR_LENGTH) // Update offhand slot
                {
                    offhandItemSlot.UpdateItemStack(e.ItemStack);
                    var offhandIsEmpty = e.ItemStack?.IsEmpty ?? true;

                    hotbarAnimator.SetBool(SHOW_HASH, !offhandIsEmpty);
                }
                else if (e.HotbarSlot is >= 0 and < HOTBAR_LENGTH) // Update hotbar slot
                {
                    itemSlots[e.HotbarSlot].UpdateItemStack(e.ItemStack);
                }
                else
                {
                    Debug.LogWarning($"Trying to set hotbar at invalid index: {e.HotbarSlot}");
                }
            };

            heldItemChangeCallback = e =>
            {
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

        private void Update()
        {
            if (parentCanvasGroups.Length > 0)
            {
                float updatedAlpha = 1F;

                foreach (var t in parentCanvasGroups)
                {
                    if ((!t.gameObject.activeSelf) || t.alpha == 0F)
                    {
                        updatedAlpha = 0F;
                        break;
                    }

                    updatedAlpha *= t.alpha;
                }

                if (!Mathf.Approximately(selfAlpha, updatedAlpha))
                {
                    UpdateAlpha(updatedAlpha);
                }
            }
        }

        private void OnDestroy()
        {
            if (hotbarUpdateCallback is not null)
                EventManager.Instance.Unregister(hotbarUpdateCallback);
            
            if (heldItemChangeCallback is not null)
                EventManager.Instance.Unregister(heldItemChangeCallback);
        }
    }
}