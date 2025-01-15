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
        public abstract void StartAction();
        public abstract void EndAction();
    }
}