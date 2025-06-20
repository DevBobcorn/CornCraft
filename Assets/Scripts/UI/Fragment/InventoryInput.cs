using System;
using UnityEngine;
using TMPro;

namespace CraftSharp.UI
{
    [RequireComponent(typeof (TMP_InputField))]
    public class InventoryInput : InventoryInteractable
    {
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private TMP_Text placeholderText;

        protected override void UpdateCursorText()
        {
            cursorTextDirty = false;

            if (HintStringOverride is not null)
            {
                cursorText = HintStringOverride;
            }
            else if (HintTranslationKey is not null)
            {
                cursorText = Translations.Get(HintTranslationKey);
            }
        }

        public void SetPlaceholderText(string text)
        {
            placeholderText.text = text;
        }

        #nullable enable
        
        private Action<string>? valueChangeHandler;
        
        #nullable disable

        public void SetValueChangeHandler(Action<string> handler)
        {
            valueChangeHandler = handler;
        }

        public void InputValueChanged(string text)
        {
            valueChangeHandler?.Invoke(text);
        }

        public void InputPointerEnter()
        {
            if (cursorTextDirty)
            {
                // Update only when needed
                UpdateCursorText();
            }
            
            cursorTextHandler?.Invoke(cursorText);
            hoverHandler?.Invoke();
        }

        public void InputPointerExit()
        {
            cursorTextHandler?.Invoke(string.Empty);
        }
    }
}