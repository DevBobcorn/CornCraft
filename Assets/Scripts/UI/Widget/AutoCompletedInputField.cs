using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace CraftSharp.UI
{
    [AddComponentMenu("UI/Auto Completed Input Field", 11)]
    public class AutoCompletedInputField : TMP_InputField
    {
        // UI controls and objects
        [SerializeField] private CanvasGroup autoCompleteCanvasGroup;
        [SerializeField] private TMP_Text autoCompleteOptionsText;
        [SerializeField] private TMP_Text inputFieldTextGhost;
        [SerializeField] private int maxDisplayedCompleteOptionCount = 20;

        // Control data
        private bool completionOptionsShown = false;
        private string[] completionOptions = { };
        private string confirmedPart = string.Empty;
        private int completionSelectedIndex = 0;
        private int completionStart = 0;
        private int completionLength = 0;
        private int optionCountBeforeFirstDisplayed = 0;

        [Serializable]
        public class ArrowKeyEvent : UnityEvent { }

        [Serializable]
        public class CaretArrowKeyEvent : UnityEvent { }

        /// <summary>
        /// Event delegates triggered when up arrow key is not consumed by completion selection.
        /// </summary>
        [SerializeField]
        public ArrowKeyEvent m_OnUpArrowKeyNotConsumedByCompletionSelection = new();

        /// <summary>
        /// Event delegates triggered when down arrow key is not consumed by completion selection.
        /// </summary>
        [SerializeField]
        public ArrowKeyEvent m_OnDownArrowKeyNotConsumedByCompletionSelection = new();

        public void ClearCompletionOptions()
        {
            SetCompletionOptions(Array.Empty<string>(), 0, 0, 0);
        }

        public void SetCompletionOptions(string[] options, int selectedIndex, int start, int length)
        {
            completionOptions = options;

            completionSelectedIndex = selectedIndex;
            completionStart = start;
            completionLength = length;

            optionCountBeforeFirstDisplayed = 0; // Reset this

            if (completionStart > 0)
            {
                confirmedPart = completionStart <= text.Length ? text[..completionStart] : text.PadRight(completionStart, ' ');

                // Update ghost text content for calculating width...
                inputFieldTextGhost.text = confirmedPart;
            }

            // Calculate content and position for auto completion hints
            RefreshCompletionOptions();

            if (options.Length <= 0)
            {
                HideCompletions();
            }
            else if (!completionOptionsShown)
            {
                ShowCompletions();
            }
        }

        public void PerformCompletion(Action<string> completedCallback)
        {
            if (completionOptionsShown)
            {
                if (completionSelectedIndex >= 0 && completionSelectedIndex < completionOptions.Length && completionStart > 0)
                {   // Apply selected auto completion
                    string original = text;
                    string completion = completionOptions[completionSelectedIndex];

                    //Debug.Log($"Completing \"{original}\" at {completionStart} with \"{completion}\". Length: {completionLength}");

                    var basePart = completionStart <= original.Length ?
                        original[..completionStart] : original.PadRight(completionStart, ' ');

                    var textBehindCursor =
                        caretPosition < original.Length ? original[caretPosition..original.Length] : string.Empty;

                    // Set input field text
                    text = basePart + completion + textBehindCursor;
                    caretPosition = (basePart + completion).Length;

                    completedCallback(text);
                }
            }
        }

        private void ShowCompletions()
        {
            autoCompleteCanvasGroup.alpha = 1F;
            autoCompleteCanvasGroup.interactable = true;
            autoCompleteCanvasGroup.blocksRaycasts = true;

            completionOptionsShown = true;
        }

        private void HideCompletions()
        {
            autoCompleteCanvasGroup.alpha = 0F;
            autoCompleteCanvasGroup.interactable = false;
            autoCompleteCanvasGroup.blocksRaycasts = false;

            completionOptionsShown = false;
            // Reset selected index
            completionSelectedIndex = 0;
        }

        private void OnChatInputChange(string message)
        {
            if (completionOptionsShown)
            {
                if (completionSelectedIndex >= 0 && completionSelectedIndex < completionOptions.Length)
                {
                    if (!(confirmedPart + completionOptions[completionSelectedIndex]).Contains(message))
                    {
                        // User is typing something which is not in the completion list, so hide it...
                        HideCompletions();
                    }
                }
            }
        }

        private void RefreshCompletionOptions()
        {
            StringBuilder str = new();

            int displayedCount = Math.Min(maxDisplayedCompleteOptionCount, completionOptions.Length);

            for (int i = 0; i < displayedCount; i++)
            {
                int optionIndex = i + optionCountBeforeFirstDisplayed;
                var optionText = completionOptions[optionIndex];

                str.AppendLine(optionIndex == completionSelectedIndex ?
                    $"<color=yellow>{optionText}</color>" : optionText);
            }

            autoCompleteOptionsText.text = str.ToString();

            float caretPosX = inputFieldTextGhost.preferredWidth;
            // Offset a bit to make sure that the completion part is perfectly aligned with user input
            if (inputFieldTextGhost.text.EndsWith(' '))
            {
                caretPosX += inputFieldTextGhost.fontSize;
            }

            var rectTransform = autoCompleteCanvasGroup.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = new(caretPosX, rectTransform.anchoredPosition.y);
        }

        protected override void Start()
        {
            base.Start();

            onValueChanged.AddListener(OnChatInputChange);

            autoCompleteCanvasGroup.alpha = 0F;
            autoCompleteCanvasGroup.interactable = false;
            autoCompleteCanvasGroup.blocksRaycasts = false;
            autoCompleteOptionsText.text = string.Empty; // Clear up at start
        }

        protected override void LateUpdate()
        {
            base.LateUpdate();

            if (isActiveAndEnabled)
            {
                if (Keyboard.current.leftArrowKey.wasPressedThisFrame ||
                        Keyboard.current.rightArrowKey.wasPressedThisFrame)
                {
                    // Refresh completions after moving cursor position
                    onValueChanged.Invoke(text);
                }

                if (Keyboard.current.upArrowKey.wasPressedThisFrame)
                {
                    if (completionOptionsShown && completionOptions.Length > 1) // More than one completion option is available
                    {
                        // Update displayed options list
                        if (completionOptions.Length > maxDisplayedCompleteOptionCount)
                        {
                            if (completionSelectedIndex == 0) // Previously selected first option, wrap to last option in list
                            {
                                optionCountBeforeFirstDisplayed = completionOptions.Length - maxDisplayedCompleteOptionCount;
                            }
                            else // Still some options before this one
                            {
                                var displayedIndex = completionSelectedIndex - optionCountBeforeFirstDisplayed;

                                if (displayedIndex == 0) // Scroll up by one option
                                {
                                    optionCountBeforeFirstDisplayed -= 1;
                                }
                            }
                        }

                        // Select previous completion
                        completionSelectedIndex = (completionSelectedIndex + completionOptions.Length - 1) % completionOptions.Length;

                        RefreshCompletionOptions();

                        // Set cursor position to line end
                        caretPosition = text.Length;
                    }
                    else // No or only one completion option is available
                    {
                        optionCountBeforeFirstDisplayed = 0;
                        completionSelectedIndex = 0;

                        // Pass the event to other handlers
                        m_OnUpArrowKeyNotConsumedByCompletionSelection.Invoke();
                    }

                    // Activate self, might be unnecessary though
                    ActivateInputField();
                }

                if (Keyboard.current.downArrowKey.wasPressedThisFrame)
                {
                    if (completionOptionsShown && completionOptions.Length > 1) // More than one completion option is available
                    {
                        // Update displayed options list
                        if (completionOptions.Length > maxDisplayedCompleteOptionCount)
                        {
                            if (completionSelectedIndex == completionOptions.Length - 1) // Previously selected last option, wrap to first option in list
                            {
                                optionCountBeforeFirstDisplayed = 0;
                            }
                            else // Still some options after this one
                            {
                                var displayedIndex = completionSelectedIndex - optionCountBeforeFirstDisplayed;

                                if (displayedIndex == maxDisplayedCompleteOptionCount - 1) // Scroll down by one option
                                {
                                    optionCountBeforeFirstDisplayed += 1;
                                }
                            }
                        }

                        // Select next completion
                        completionSelectedIndex = (completionSelectedIndex + 1) % completionOptions.Length;

                        RefreshCompletionOptions();

                        // Set cursor position to line end
                        caretPosition = text.Length;
                    }
                    else // No or only one completion option is available
                    {
                        optionCountBeforeFirstDisplayed = 0;
                        completionSelectedIndex = 0;

                        // Pass the event to other handlers
                        m_OnDownArrowKeyNotConsumedByCompletionSelection.Invoke();
                    }

                    // Activate self, might be unnecessary though
                    ActivateInputField();
                }
            }
        }
    }
}