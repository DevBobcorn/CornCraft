using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

using CraftSharp.Event;

namespace CraftSharp.UI
{
    [RequireComponent(typeof (CanvasGroup))]
    public class ChatScreen : BaseScreen
    {
        private static readonly string COMMAND_PREFIX = "/";
        private bool isActive = false;

        public override bool IsActive
        {
            set {
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
        [SerializeField] private RectTransform chatScrollRectTransform;
        [SerializeField] private AutoCompletedInputField chatInput;
        [SerializeField] private TMP_Text chatContent;
        private CanvasGroup screenGroup;

        // Chat message data
        private readonly List<string> sentChatHistory = new();
        private int chatIndex = 0;
        private string chatBuffer = string.Empty;

        public override bool ReleaseCursor()
        {
            return true;
        }

        public override bool ShouldPauseInput()
        {
            return true;
        }

        public void InputCommandPrefix()
        {
            chatInput.text = COMMAND_PREFIX;
            chatInput.caretPosition = COMMAND_PREFIX.Length;
        }

        public void RefreshCompletions(string chatInputText)
        {
            if (chatInputText.StartsWith(COMMAND_PREFIX) && chatInputText.Length > COMMAND_PREFIX.Length)
            {
                string requestText;
                if (chatInput.caretPosition > 0 && chatInput.caretPosition < chatInputText.Length)
                    requestText = chatInputText[0..chatInput.caretPosition];
                else
                    requestText = chatInputText;

                // Request command auto complete
                var client = CornApp.CurrentClient;
                if (client == null) return;

                client.SendAutoCompleteRequest(requestText);

                //Debug.Log($"Requesting auto completion: [{requestText}]");
            }
            else
            {
                chatInput.ClearCompletionOptions();
            }
        }

        public void SendChatMessage()
        {
            if (chatInput.text.Trim() == string.Empty)
                return;
            
            string chat = chatInput.text;
            // Send if client exists...
            var client = CornApp.CurrentClient;
            if (client != null)
            {
                client.TrySendChat(chat);
            }
            
            chatInput.text = string.Empty;

            // Remove the chat text from previous history if present
            if (sentChatHistory.Contains(chat))
                sentChatHistory.Remove(chat);

            sentChatHistory.Add(chat);
            chatIndex = sentChatHistory.Count;
        }

        public void PrevChatMessage()
        {
            if (sentChatHistory.Count > 0 && chatIndex - 1 >= 0)
            {
                if (chatIndex == sentChatHistory.Count)
                {   // Store to buffer...
                    chatBuffer = chatInput.text;
                }
                chatIndex--;

                // Don't notify before we set the caret position
                chatInput.SetTextWithoutNotify(sentChatHistory[chatIndex]);
                chatInput.caretPosition = sentChatHistory[chatIndex].Length;
                chatInput.ClearCompletionOptions();

                RefreshCompletions(sentChatHistory[chatIndex]);
            }
        }

        public void NextChatMessage()
        {
            if (sentChatHistory.Count > 0 && chatIndex < sentChatHistory.Count)
            {
                chatIndex++;
                if (chatIndex == sentChatHistory.Count)
                {
                    // Restore buffer... Don't notify before we set the caret position
                    chatInput.SetTextWithoutNotify(chatBuffer);
                    chatInput.caretPosition = chatBuffer.Length;
                    chatInput.ClearCompletionOptions();

                    RefreshCompletions(chatBuffer);
                }
                else
                {
                    // Don't notify before we set the caret position
                    chatInput.SetTextWithoutNotify(sentChatHistory[chatIndex]);
                    chatInput.caretPosition = sentChatHistory[chatIndex].Length;
                    chatInput.ClearCompletionOptions();

                    RefreshCompletions(sentChatHistory[chatIndex]);
                }
            }
        }

        #nullable enable

        private Action<ChatMessageEvent>? chatCallback;
        private Action<AutoCompletionEvent>? autoCompleteCallback;

        #nullable disable

        protected override void Initialize()
        {
            // Initialize controls and add listeners
            screenGroup = GetComponent<CanvasGroup>();

            chatInput.onValueChanged.AddListener(this.RefreshCompletions);
            chatContent.text = string.Empty;

            // Register callbacks
            chatCallback = (e) => {
                var styledMessage = TMPConverter.MC2TMP(e.Message);

                chatContent.text += styledMessage + '\n';
            };

            autoCompleteCallback = (e) => {
                if (e.Options.Length > 0)
                {   // Show at most 20 options
                    var completionOptions = e.Options;
                    var completionStart = e.Start;
                    var completionLength = e.Length;
                    var completionSelectedIndex = 0; // Select first option

                    //Debug.Log($"Received completions: s{completionStart} l{completionLength} [{string.Join(", ", completionOptions)}]");

                    chatInput.SetCompletionOptions(completionOptions, completionSelectedIndex, completionStart, completionLength);
                }
                else // No option available
                {
                    chatInput.ClearCompletionOptions();
                }
            };

            chatInput.m_OnUpArrowKeyNotConsumedByCompletionSelection.AddListener(PrevChatMessage);
            chatInput.m_OnDownArrowKeyNotConsumedByCompletionSelection.AddListener(NextChatMessage);

            EventManager.Instance.Register(chatCallback);
            EventManager.Instance.Register(autoCompleteCallback);
        }

        void OnDestroy()
        {
            if (chatCallback is not null)
                EventManager.Instance.Unregister(chatCallback);
            
            if (autoCompleteCallback is not null)
                EventManager.Instance.Unregister(autoCompleteCallback);

        }

        public override void UpdateScreen()
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                var client = CornApp.CurrentClient;
                if (client != null)
                {
                    client.ScreenControl.TryPopScreen();
                }
                return;
            }

            if (chatInput.IsActive())
            {
                if (Keyboard.current.enterKey.wasPressedThisFrame)
                {
                    SendChatMessage();
                    chatInput.ActivateInputField();
                }

                if (Keyboard.current.tabKey.wasPressedThisFrame)
                {
                    chatInput.PerformCompletion(RefreshCompletions);
                }
            }
        }
    }
}
