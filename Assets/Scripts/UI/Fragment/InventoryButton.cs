using UnityEngine;
using UnityEngine.UI;

namespace CraftSharp.UI
{
    [RequireComponent(typeof (Button))]
    public class InventoryButton : MonoBehaviour
    {
        [SerializeField] private Button button;
    }
}