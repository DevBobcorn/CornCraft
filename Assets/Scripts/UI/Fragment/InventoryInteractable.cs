using System;
using UnityEngine;

namespace CraftSharp.UI
{
    public class InventoryInteractable : MonoBehaviour
    {
        public bool Enabled { get; set; }
        public bool Selected { get; set; }
        
        protected Action<string> cursorTextHandler;
        
        protected string cursorText = string.Empty;
        protected bool cursorTextDirty = false;

        public void SetCursorTextHandler(Action<string> handler)
        {
            cursorTextHandler = handler;
        }

        protected virtual void UpdateCursorText()
        {
            cursorTextDirty = false;
        }
    }
}