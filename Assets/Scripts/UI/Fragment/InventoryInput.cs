using UnityEngine;
using TMPro;

namespace CraftSharp.UI
{
    [RequireComponent(typeof (TMP_InputField))]
    public class InventoryInput : InventoryInteractable
    {
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private TMP_Text placeholderText;

        public void SetPlaceholderText(string text)
        {
            placeholderText.text = text;
        }
    }
}