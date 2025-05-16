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
        
        private Action clickHandler;
        
        public void SetClickHandler(Action handler)
        {
            clickHandler = handler;
        }
        
        public void ButtonClick()
        {
            clickHandler?.Invoke();
        }
    }
}