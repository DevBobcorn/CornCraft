using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

using CraftSharp.Protocol.Message;

namespace CraftSharp.UI
{
    public class ChatMessageInteractable : MonoBehaviour, IPointerClickHandler, IPointerMoveHandler, IPointerExitHandler
    {
        private TMP_Text messageText;
        private (string, string, string, string)[] messageActions;
        private Camera uiCamera;
        
        #nullable enable
        
        protected Action<string, string>? clickActionHandler;
        protected Action<string>? cursorTextHandler;
        
        #nullable disable

        public void SetupInteractable(TMP_Text text, (string, string, string, string)[] actions, Action<string, string> clickHandler, Action<string> hoverHandler, Camera uiCam)
        {
            messageText = text;
            messageActions = actions;
            
            clickActionHandler = clickHandler;
            cursorTextHandler = hoverHandler;
            uiCamera = uiCam;
        }

        private int GetPointerActionIndex(Vector2 pointerPosition)
        {
            int linkIndex = TMP_TextUtilities.FindIntersectingLink(messageText, pointerPosition, uiCamera);
            if (linkIndex != -1)
            {
                TMP_LinkInfo linkInfo = messageText.textInfo.linkInfo[linkIndex];
                if (int.TryParse(linkInfo.GetLinkID(), out int actionIndex))
                {
                    return actionIndex;
                }
                Debug.LogWarning($"{linkInfo.GetLinkID()} is not a valid action id!");
            }
            return -1;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            int pointerActionIndex = GetPointerActionIndex(eventData.position);

            if (pointerActionIndex >= 0 && pointerActionIndex < messageActions.Length)
            {
                var (clickAction, clickValue, _, _) = messageActions[pointerActionIndex];
                if (clickAction != string.Empty && clickValue != string.Empty)
                {
                    clickActionHandler?.Invoke(clickAction, clickValue);
                }
            }
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            int pointerActionIndex = GetPointerActionIndex(eventData.position);

            if (pointerActionIndex >= 0 && pointerActionIndex < messageActions.Length)
            {
                var (_, _, hoverAction, hoverContents) = messageActions[pointerActionIndex];
                if (hoverAction != string.Empty || hoverContents != string.Empty)
                {
                    var displayText = hoverAction switch
                    {
                        "show_text" => TMPConverter.MC2TMP(ChatParser.ParseText(hoverContents)),
                        "show_item" => InventoryItemSlot.GetItemDisplayText(ItemStack.FromJson(Json.ParseJson(hoverContents))),
                        
                        _ => $"{hoverAction}: {hoverContents}"
                    };
                    
                    cursorTextHandler?.Invoke(displayText);
                }
                else
                {
                    cursorTextHandler?.Invoke(string.Empty);
                }
            }
            else
            {
                cursorTextHandler?.Invoke(string.Empty);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            cursorTextHandler?.Invoke(string.Empty);
        }
    }
}