using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;
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
        private CanvasGroup screenGroup, autoCompleteGroup;
        private RectTransform chatScrollRect;
        private TMP_InputField chatInput, chatInputGhost;
        private TMP_Text chatTexts, autoCompleteOptions;

        // Chat message data
        private List<string> chatHistory = new List<string>();
        private int chatIndex = 0, completionIndex = -1, completionStart = 0, completionLength = 0;
        private string chatBuffer = string.Empty;
        private bool completionsShown = false;
        private string[] completionOptions = { };
        private string confirmedPart = string.Empty;

        public override bool ReleaseCursor()
        {
            return true;
        }

        public override bool ShouldPause()
        {
            return true;
        }

        public void SetChatMessage(string message, int caretPos)
        {
            chatInput.text = message;
            chatInput.caretPosition = caretPos;
        }

        private void ShowCompletions()
        {
            autoCompleteGroup.alpha = 1F;
            autoCompleteGroup.interactable = true;
            autoCompleteGroup.blocksRaycasts = true;
            completionsShown = true;
        }

        private void HideCompletions()
        {
            autoCompleteGroup.alpha = 0F;
            autoCompleteGroup.interactable = false;
            autoCompleteGroup.blocksRaycasts = false;
            completionsShown = false;
            // Restart marker value...
            completionIndex = -1;
        }

        public void OnChatInputChange(string message)
        {
            if (message.StartsWith("/") && message.Length > 1)
            {
                string requestText;
                if (chatInput.caretPosition > 0 && chatInput.caretPosition < message.Length)
                    requestText = message[0..chatInput.caretPosition];
                else
                    requestText = message;
                
                RequestAutoCompleteChat(requestText);
            }
            else
                HideCompletions();
            
            if (completionsShown)
            {
                if (completionIndex >= 0 && completionIndex < completionOptions.Length)
                {
                    if (!(confirmedPart + completionOptions[completionIndex]).Contains(message))
                    {   // User is typing something which is not in the completion list, so hide it...
                        HideCompletions();
                    }
                }
            }

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

        public void RequestAutoCompleteChat(string message)
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

        public void PrevCompletionOption()
        {
            if (completionIndex == -1)
                completionIndex = 0; // Select first option
            else
                completionIndex = (completionIndex + completionOptions.Length - 1) % completionOptions.Length;
            
            RefreshCompletionOptions();
        }

        public void NextCompletionOption()
        {
            if (completionIndex == -1)
                completionIndex = 0; // Select first option
            else
                completionIndex = (completionIndex + 1) % completionOptions.Length;
            
            RefreshCompletionOptions();
        }

        private void RefreshCompletionOptions()
        {
            StringBuilder str = new();
            for (int i = 0;i < completionOptions.Length;i++)
            {
                str.Append(
                    i == completionIndex ? $"<color=yellow>{completionOptions[i]}</color>" : completionOptions[i]
                ).Append('\n');
            }
            autoCompleteOptions.text = str.ToString();
        }

        private Action<ChatMessageEvent> chatCallback;
        private Action<AutoCompletionEvent> autoCompleteCallback;

        protected override bool Initialize()
        {
            // Initialize controls and add listeners
            screenGroup = GetComponent<CanvasGroup>();
            chatScrollRect = transform.Find("Chat Scroll").GetComponent<RectTransform>();

            chatInput = transform.Find("Chat Input").GetComponent<TMP_InputField>();
            chatInput.onValueChanged.AddListener(this.OnChatInputChange);

            var chatInputGhostObj = GameObject.Instantiate(chatInput.gameObject, Vector3.zero, Quaternion.identity);
            chatInputGhostObj.name = "Chat Input Ghost";
            chatInputGhostObj.SetActive(false);

            chatInputGhost = chatInputGhostObj.GetComponent<TMP_InputField>();

            chatTexts = FindHelper.FindChildRecursively(chatScrollRect, "Chat Texts").GetComponent<TMP_Text>();
            chatTexts.text = string.Empty;

            autoCompleteGroup = transform.Find("Auto Complete Panel").GetComponent<CanvasGroup>();
            autoCompleteGroup.alpha = 0F;
            autoCompleteGroup.interactable = false;
            autoCompleteGroup.blocksRaycasts = false;

            autoCompleteOptions = autoCompleteGroup.transform.Find("Auto Complete Options").GetComponent<TMP_Text>();
            autoCompleteOptions.text = string.Empty; // Clear up at start

            // Register callbacks
            chatCallback = (e) => {
                chatTexts.text += StringConvert.MC2TMP(e.message) + '\n';
            };

            autoCompleteCallback = (e) => {
                if (e.options.Length > 0)
                {   // Show at most 20 options
                    completionOptions = e.options.Length > 20 ? e.options[..20] : e.options;
                    completionStart = e.start;
                    completionLength = e.length;
                    completionIndex = 0; // Select first option
                    RefreshCompletionOptions();
                    if (!completionsShown)
                        ShowCompletions();

                    if (completionStart > 0)
                    {   // Apply selected auto completion
                        string original   = chatInput.text;
                        string completion = completionOptions[completionIndex];
                        confirmedPart     = completionStart <= original.Length ? original[..completionStart] : original.PadRight(completionStart, ' ');

                        // Update auto completion panel position...
                        chatInputGhost.text = confirmedPart;
                    }
                    
                    float caretPosX = chatInputGhost.textComponent.preferredWidth;
                    // Offset a bit to make sure that the completion part is perfectly aligned with user input
                    if (chatInputGhost.text.EndsWith(' '))
                        caretPosX += chatInputGhost.textComponent.fontSize * 0.5F;
                    
                    autoCompleteGroup.GetComponent<RectTransform>().anchoredPosition = new(caretPosX, 0F);
                }
                else // No option available
                {
                    if (completionsShown)
                        HideCompletions();
                }
            };

            EventManager.Instance.Register(chatCallback);
            EventManager.Instance.Register(autoCompleteCallback);

            return true;
        }

        void OnDestroy()
        {
            if (chatCallback is not null)
                EventManager.Instance.Unregister(chatCallback);
            
            if (autoCompleteCallback is not null)
                EventManager.Instance.Unregister(autoCompleteCallback);

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
                    if (completionsShown)
                    {
                        PrevCompletionOption();
                        chatInput.caretPosition = chatInput.text.Length;
                    }
                    else
                    {
                        PrevChatMessage();
                        chatInput.MoveTextEnd(false);
                    }
                    chatInput.ActivateInputField();
                }
                if (Input.GetKeyDown(KeyCode.DownArrow))
                {
                    if (completionsShown)
                    {
                        NextCompletionOption();
                        chatInput.caretPosition = chatInput.text.Length;
                    }
                    else
                    {
                        NextChatMessage();
                        chatInput.MoveTextEnd(false);
                    }
                    chatInput.ActivateInputField();
                }
                if (Input.GetKeyUp(KeyCode.LeftArrow) || Input.GetKeyUp(KeyCode.RightArrow))
                {
                    // Refresh complete after moving cursor position
                    chatInput.onValueChanged.Invoke(chatInput.text);
                }
                if (Input.GetKeyDown(KeyCode.Tab))
                {
                    if (completionsShown)
                    {
                        if (completionIndex >= 0 && completionIndex < completionOptions.Length && completionStart > 0)
                        {   // Apply selected auto completion
                            string original   = chatInput.text;
                            string completion = completionOptions[completionIndex];
                            //Debug.Log($"Completing \"{original}\" at {completionStart} with \"{completion}\". Length: {completionLength}");

                            string basePart = completionStart <= original.Length ? original[..completionStart] : original.PadRight(completionStart, ' ');

                            string textBehindCursor;

                            if (chatInput.caretPosition < original.Length)
                                textBehindCursor = original[chatInput.caretPosition..original.Length];
                            else
                                textBehindCursor = string.Empty;

                            SetChatMessage(basePart + completion + textBehindCursor, (basePart + completion).Length);
                            HideCompletions();
                        }
                    }

                }

            }

        }

    }
}
