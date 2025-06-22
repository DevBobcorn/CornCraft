using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CraftSharp.UI
{
    public class InventoryBackground : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        #nullable enable
        
        private Action? enterHandler;
        private Action? exitHandler;
        private Action<PointerEventData.InputButton>? clickHandler;
        
        #nullable disable
        
        public void SetEnterHandler(Action handler)
        {
            enterHandler = handler;
        }
        
        public void SetExitHandler(Action handler)
        {
            exitHandler = handler;
        }
        
        public void SetClickHandler(Action<PointerEventData.InputButton> handler)
        {
            clickHandler = handler;
        }
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            enterHandler?.Invoke();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            exitHandler?.Invoke();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            clickHandler?.Invoke(eventData.button);
        }
    }
}