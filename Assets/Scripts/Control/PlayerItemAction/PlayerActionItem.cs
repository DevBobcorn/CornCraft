#nullable enable
using UnityEngine;

namespace CraftSharp.Control
{
    public enum ItemActionType
    {
        None,
        MeleeWeaponSword,
        RangedWeaponBow,
    }
    
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
                if (item.ItemType.ItemId.Path.EndsWith("sword"))
                {
                    return ItemActionType.MeleeWeaponSword;
                }
                else
                {
                    return ItemActionType.None;
                }
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