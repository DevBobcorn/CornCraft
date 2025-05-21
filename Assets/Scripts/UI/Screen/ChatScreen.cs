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
        private const int MAX_CHAT_MESSAGES = 100;
        private const string COMMAND_PREFIX = "/";
        private bool isActive;

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

            get => isActive;
        }

        // UI controls and objects
        [SerializeField] private AutoCompletedInputField chatInput;
        [SerializeField] private RectTransform chatContentPanel;
        [SerializeField] private GameObject chatMessagePrefab;
        [SerializeField] private RectTransform cursorTextPanel;
        [SerializeField] private TMP_Text cursorText;
        private CanvasGroup screenGroup;
        private readonly Queue<TMP_Text> chatMessages = new();

        // Chat message data
        private readonly List<string> sentChatHistory = new();
        private int chatIndex;
        private string chatBuffer = string.Empty;

        public override bool ReleaseCursor()
        {
            return true;
        }

        public override bool ShouldPauseControllerInput()
        {
            return true;
        }

        public void InputCommandPrefix()
        {
            chatInput.text = COMMAND_PREFIX;
            chatInput.caretPosition = COMMAND_PREFIX.Length;
        }

        private void RefreshCompletions(string chatInputText)
        {
            if (chatInputText.StartsWith(COMMAND_PREFIX) && chatInputText.Length > COMMAND_PREFIX.Length)
            {
                string requestText;
                if (chatInput.caretPosition > 0 && chatInput.caretPosition < chatInputText.Length)
                    requestText = chatInputText[..chatInput.caretPosition];
                else
                    requestText = chatInputText;

                // Request command auto complete
                var client = CornApp.CurrentClient;
                if (!client) return;

                client.SendAutoCompleteRequest(requestText);

                //Debug.Log($"Requesting auto completion: [{requestText}]");
            }
            else
            {
                chatInput.ClearCompletionOptions();
            }
        }

        private void SendChatMessage()
        {
            if (chatInput.text.Trim() == string.Empty)
                return;
            
            string chat = chatInput.text;
            // Send if client exists...
            var client = CornApp.CurrentClient;
            if (client)
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

        private void PrevChatMessage()
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

        private void NextChatMessage()
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

        private Action<ChatMessageEvent>? chatMessageCallback;
        private Action<AutoCompletionEvent>? autoCompleteCallback;

        #nullable disable
        
        private void UpdateCursorText(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                cursorTextPanel.gameObject.SetActive(false);
            }
            else
            {
                cursorText.text = str;
                cursorTextPanel.gameObject.SetActive(true);
            }
        }

        protected override void Initialize()
        {
            // Initialize controls and add listeners
            screenGroup = GetComponent<CanvasGroup>();

            if (chatMessages.Count > 0)
            {
                foreach (var chatMessage in chatMessages)
                {
                    Destroy(chatMessage.gameObject);
                }
                chatMessages.Clear();
            }

            chatInput.onValueChanged.AddListener(RefreshCompletions);
            
            // Hide cursor text
            cursorTextPanel.gameObject.SetActive(false);

            // Register callbacks
            chatMessageCallback = e =>
            {
                var styledMessage = TMPConverter.MC2TMP(e.Message);
                var chatMessageObj = Instantiate(chatMessagePrefab, chatContentPanel);
                
                var chatMessage = chatMessageObj.GetComponent<TMP_Text>();
                chatMessage.text = styledMessage;

                if (e.Actions is not null && e.Actions.Length > 0)
                {
                    var game = CornApp.CurrentClient;
                    if (!game) return;
                    
                    var chatMessageInteractable = chatMessageObj.AddComponent<ChatMessageInteractable>();
                    chatMessageInteractable.SetupInteractable(chatMessage, e.Actions, UpdateCursorText, game.UICamera);
                }
                
                chatMessages.Enqueue(chatMessage);

                while (chatMessages.Count > MAX_CHAT_MESSAGES)
                {
                    // Dequeue and destroy
                    Destroy(chatMessages.Dequeue().gameObject);
                }
            };

            autoCompleteCallback = e =>
            {
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

            EventManager.Instance.Register(chatMessageCallback);
            EventManager.Instance.Register(autoCompleteCallback);
        }

        private void OnDestroy()
        {
            if (chatMessageCallback is not null)
                EventManager.Instance.Unregister(chatMessageCallback);
            
            if (autoCompleteCallback is not null)
                EventManager.Instance.Unregister(autoCompleteCallback);

        }

        public override void UpdateScreen()
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                var client = CornApp.CurrentClient;
                if (client)
                {
                    client.ScreenControl.TryPopScreen();
                }
                return;
            }
            
            var game = CornApp.CurrentClient;
            if (!game) return;

            // Update cursor text position
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                transform as RectTransform, Mouse.current.position.value,
                game.UICamera, out Vector2 newPos);
            
            newPos = transform.TransformPoint(newPos);

            // Don't modify z coordinate
            cursorTextPanel.position = new Vector3(newPos.x, newPos.y, cursorTextPanel.position.z);

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
