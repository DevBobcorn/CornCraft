using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

using CraftSharp.Event;

namespace CraftSharp.UI
{
    [RequireComponent(typeof (CanvasGroup))]
    public class SignEditorScreen : BaseScreen
    {
        // UI controls and objects
        [SerializeField] private Button submitButton;
        private CanvasGroup screenGroup;

        [SerializeField] private TMP_InputField text1Input;
        [SerializeField] private TMP_InputField text2Input;
        [SerializeField] private TMP_InputField text3Input;
        [SerializeField] private TMP_InputField text4Input;
        
        private BlockLoc? signLoc = null; // null for none
        private bool front = true;

        public void SetSignInfo(BlockLoc blockLoc, bool isFrontText)
        {
            EnsureInitialized();

            signLoc = blockLoc;
            front = isFrontText;
            
            
        }

        private bool isActive = false;

        public override bool IsActive
        {
            set {
                isActive = value;
                screenGroup.alpha = value ? 1F : 0F;
                screenGroup.blocksRaycasts = value;
                screenGroup.interactable   = value;
                
                // Select this text input
                text1Input.Select();
            }

            get => isActive;
        }

        public override bool ReleaseCursor()
        {
            return true;
        }

        public override bool ShouldPauseControllerInput()
        {
            return true;
        }

        private void Submit()
        {
            var client = CornApp.CurrentClient;
            if (!client || !signLoc.HasValue) return;

            client.UpdateSign(signLoc.Value, front, text1Input.text,
                text2Input.text, text3Input.text, text4Input.text);
            
            // Clear all texts
            text1Input.text = string.Empty;
            text2Input.text = string.Empty;
            text3Input.text = string.Empty;
            text4Input.text = string.Empty;
            
            client.ScreenControl.TryPopScreen();
        }

        protected override void Initialize()
        {
            // Initialize controls and add listeners
            screenGroup = GetComponent<CanvasGroup>();

            submitButton.onClick.AddListener(Submit);
            
            // Clear all texts
            text1Input.text = string.Empty;
            text2Input.text = string.Empty;
            text3Input.text = string.Empty;
            text4Input.text = string.Empty;
        }

        public override void UpdateScreen()
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Submit();
            }
        }
    }
}
