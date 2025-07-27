using UnityEngine;
using TMPro;

/// <summary>
/// Limits the input of a TMP_InputField based on the visual width of the text.
/// Attach this script to the same GameObject that has the TMP_InputField component.
/// </summary>
[RequireComponent(typeof (TMP_InputField))]
public class InputWidthLimiter : MonoBehaviour
{
    private TMP_InputField _inputField;
    private RectTransform _textRectTransform;
    private string _lastValidText = "";

    private void Awake()
    {
        // Get the required components
        _inputField = GetComponent<TMP_InputField>();
        _textRectTransform = _inputField.textComponent.rectTransform;
    }

    private void Start()
    {
        // Set the initial text as the last known valid state
        _lastValidText = _inputField.text;

        // Subscribe our validation method to the input field's onValueChanged event
        _inputField.onValueChanged.AddListener(ValidateInputWidth);
    }

    private void OnDestroy()
    {
        // Unsubscribe from the event when the object is destroyed to prevent memory leaks
        if (_inputField)
        {
            _inputField.onValueChanged.RemoveListener(ValidateInputWidth);
        }
    }

    /// <summary>
    /// Checks if the new text fits within the RectTransform's width.
    /// </summary>
    /// <param name="newText">The text currently in the input field after the change.</param>
    private void ValidateInputWidth(string newText)
    {
        // Get the max width from the text area's RectTransform
        float maxWidth = _textRectTransform.rect.width;

        // Calculate the preferred rendered width of the new text using the text component's settings (font, size, etc.)
        float preferredWidth = _inputField.textComponent.GetPreferredValues(newText).x;

        if (preferredWidth > maxWidth)
        {
            // If the new text is too wide, revert to the last known valid text
            _inputField.text = _lastValidText;
        }
        else
        {
            // Otherwise, the new text is valid, so we update our "last valid" tracker
            _lastValidText = newText;
        }
    }
}