using UnityEngine;
using UnityEngine.UI;

namespace CraftSharp.UI
{
    [RequireComponent(typeof (ScrollRect))]
    public class InventoryScrollView : MonoBehaviour
    {
        public GridLayoutGroup ItemGridLayoutGroup;

        public void ClearAllItems()
        {
            foreach (Transform t in ItemGridLayoutGroup.transform)
            {
                Destroy(t.gameObject);
            }
        }
    }
}