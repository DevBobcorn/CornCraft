using UnityEngine;
using TMPro;

namespace CraftSharp.UI
{
    [RequireComponent(typeof (TMP_InputField))]
    public class InventoryInput : MonoBehaviour
    {
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private TMP_Text placeholderText;

        public void SetPlaceholderText(string text)
        {
            placeholderText.text = text;
        }
    }
}