using System;
using UnityEngine;

namespace CraftSharp.UI
{
    public class InventoryInteractable : MonoBehaviour
    {
        protected bool _enabled = true;
        public virtual bool Enabled { get => _enabled; set => _enabled = value; }
        
        protected bool _selected;
        public virtual bool Selected { get => _selected; set => _selected = value; }

        #nullable enable
        
        protected Action<string>? cursorTextHandler;
        protected Action? hoverHandler;
        public string? HintTranslationKey { get; set; }
        
        #nullable disable
        
        protected string cursorText = string.Empty;
        protected bool cursorTextDirty = false;

        public void SetCursorTextHandler(Action<string> handler)
        {
            cursorTextHandler = handler;
        }

        public void SetHoverHandler(Action handler)
        {
            hoverHandler = handler;
        }

        public void MarkCursorTextDirty()
        {
            cursorTextDirty = true;
        }

        protected virtual void UpdateCursorText()
        {
            cursorTextDirty = false;
        }
    }
}