using UnityEngine;
using UnityEngine.UI;

namespace CraftSharp.UI
{
    [RequireComponent(typeof (Button))]
    public class InventoryButton : InventoryInteractable
    {
        [SerializeField] private Button button;
    }
}