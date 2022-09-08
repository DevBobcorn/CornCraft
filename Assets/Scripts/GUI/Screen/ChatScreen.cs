using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

using MinecraftClient.Event;

namespace MinecraftClient.UI
{
    [RequireComponent(typeof (CanvasGroup))]
    public class ChatScreen : BaseScreen
    {
        private bool isActive = false;

        public override bool IsActive
        {
            set {
                EnsureInitialized();

                isActive = value;
                screenGroup.alpha = value ? 1F : 0F;
                screenGroup.blocksRaycasts = value;
                screenGroup.interactable   = value;
                // Focus chat input on enter chat screen
                if (value)
                {
                    chatInput.text = string.Empty;
                    chatInput.ActivateInputField();
                }
            }

            get {
                return isActive;
            }
        }

        // UI controls and objects
        private CanvasGroup screenGroup;
        private RectTransform chatScrollRect;
        private TMP_InputField chatInput;
        private TMP_Text       chatTexts;

        // Chat message data
        private List<string> chatHistory = new List<string>();
        private int chatIndex = 0;
        private string chatBuffer = string.Empty;

        public override string ScreenName()
        {
            return "Chat Screen";
        }

        public override bool ReleaseCursor()
        {
            return true;
        }

        public override bool ShouldPause()
        {
            return true;
        }

        public void SetChatMessage(string message)
        {
            chatInput.text = message;
            chatInput.caretPosition = message.Length;
        }

        public void OnChatInputChange(string message)
        {
            // TODO
            
        }

        public void SendChatMessage()
        {
            if (chatInput.text.Trim() == string.Empty)
                return;
            
            string chat = chatInput.text;
            // Send if client exists...
            CornClient.Instance?.CommandPrompt(chat);
            chatInput.text = string.Empty;

            // Remove the chat text from previous history if present
            if (chatHistory.Contains(chat))
                chatHistory.Remove(chat);

            chatHistory.Add(chat);
            chatIndex = chatHistory.Count;
        }

        public void AutoCompleteChatMessage(string message)
        {
            CornClient.Instance?.AutoComplete(message);
        }

        public void PrevChatMessage()
        {
            if (chatHistory.Count > 0 && chatIndex - 1 >= 0)
            {
                if (chatIndex == chatHistory.Count)
                {   // Store to buffer...
                    chatBuffer = chatInput.text;
                }
                chatIndex--;
                chatInput.text = chatHistory[chatIndex];
                chatInput.caretPosition = chatHistory[chatIndex].Length;
            }
        }

        public void NextChatMessage()
        {
            if (chatHistory.Count > 0 && chatIndex < chatHistory.Count)
            {
                chatIndex++;
                if (chatIndex == chatHistory.Count)
                {
                    // Restore buffer...
                    chatInput.text = chatBuffer;
                    chatInput.caretPosition = chatBuffer.Length;
                }
                else
                {
                    chatInput.text = chatHistory[chatIndex];
                    chatInput.caretPosition = chatHistory[chatIndex].Length;
                }
            }
        }

        private Action<ChatMessageEvent> chatCallback;

        protected override void Initialize()
        {
            // Initialize controls and add listeners
            screenGroup = GetComponent<CanvasGroup>();
            chatScrollRect = transform.Find("Chat Scroll").GetComponent<RectTransform>();

            chatInput = transform.Find("Chat Input").GetComponent<TMP_InputField>();
            chatInput.onValueChanged.AddListener(this.OnChatInputChange);

            chatTexts = FindHelper.FindChildRecursively(chatScrollRect, "Chat Texts").GetComponent<TMP_Text>();
            chatTexts.text = string.Empty;

            // Register callbacks
            chatCallback = (e) => {
                chatTexts.text += StringConvert.MC2TMP(e.message) + '\n';
            };

            EventManager.Instance.Register<ChatMessageEvent>(chatCallback);
        }

        void OnDestroy()
        {
            if (chatCallback != null)
            {
                EventManager.Instance.Unregister<ChatMessageEvent>(chatCallback);
            }
        }

        void Update()
        {
            if (!IsActive)
                return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CornClient.Instance.ScreenControl?.TryPopScreen();
                return;
            }

            if (chatInput.IsActive())
            {
                if (Input.GetKeyDown(KeyCode.Return))
                {
                    SendChatMessage();
                    chatInput.ActivateInputField();
                }
                if (Input.GetKeyDown(KeyCode.UpArrow))
                {
                    PrevChatMessage();
                    chatInput.ActivateInputField();
                }
                if (Input.GetKeyDown(KeyCode.DownArrow))
                {
                    NextChatMessage();
                    chatInput.ActivateInputField();
                }
                if (Input.GetKeyDown(KeyCode.Tab))
                {
                    AutoCompleteChatMessage(chatInput.text);
                }

            }

        }

    }
}
