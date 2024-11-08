#nullable enable
using UnityEngine;

namespace CraftSharp.Control
{
    public static class PlayerActionHelper
    {
        public static ItemActionType GetItemActionType(ItemStack? item)
        {
            if (item == null)
            {
                return ItemActionType.None;
            }
            else
            {
                return item.ItemType.ActionType;
            }
        }
    }

    public abstract class PlayerActionItem : MonoBehaviour
    {
        [SerializeField] public Vector3 slotPosition = Vector3.zero;
        [SerializeField] public Vector3 slotEularAngles = Vector3.zero;

        public abstract void StartAction();
        public abstract void EndAction();
    }
}