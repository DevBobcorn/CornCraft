using System;
using UnityEngine;
using UnityEngine.UI;

namespace CraftSharp.UI
{
    [RequireComponent(typeof (Button))]
    public class InventoryButton : InventoryInteractable
    {
        [SerializeField] private Button button;
        [SerializeField] private Sprite selectedSprite;
        [SerializeField] private Sprite disabledSprite;

        public override bool Enabled
        {
            get => _enabled;
            set
            {
                _enabled = value;
                button.interactable = _enabled;
                button.image.overrideSprite = _enabled ? _selected ? selectedSprite : null : disabledSprite;
            }
        }
        
        public override bool Selected
        {
            get => _selected;
            set
            {
                _selected = value;
                button.image.overrideSprite = _enabled ? _selected ? selectedSprite : null : disabledSprite;
            }
        }

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

        #nullable enable
        
        private Action? clickHandler;
        
        #nullable disable
        
        public void SetClickHandler(Action handler)
        {
            clickHandler = handler;
        }
        
        public void ButtonClick()
        {
            clickHandler?.Invoke();
        }

        public void ButtonPointerEnter()
        {
            if (cursorTextDirty)
            {
                // Update only when needed
                UpdateCursorText();
            }
            
            cursorTextHandler?.Invoke(cursorText);
            hoverHandler?.Invoke();
        }

        public void ButtonPointerExit()
        {
            cursorTextHandler?.Invoke(string.Empty);
        }
    }
}