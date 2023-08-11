#nullable enable
using UnityEngine;

namespace MinecraftClient.UI
{
    public class InventoryHotbar : MonoBehaviour
    {
        public const int HOTBAR_LENGTH = 9;

        [SerializeField] private InventoryItemSlot[] itemSlots = { };

    }
}