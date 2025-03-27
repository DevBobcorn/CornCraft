using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CraftSharp.UI
{
    [RequireComponent(typeof (CanvasGroup))]
    public class PauseScreen : BaseScreen
    {
        // UI controls and objects
        [SerializeField] private Button resumeButton, quitButton;
        [SerializeField] private Animator screenAnimator;

        private bool isActive = false;

        public override bool IsActive
        {
            set {
                isActive = value;
                screenAnimator.SetBool(SHOW_HASH, isActive);
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

        private void CloseScreen()
        {
            var client = CornApp.CurrentClient;
            if (client == null) return;

            client.ScreenControl.TryPopScreen();
        }

        private void QuitToLogin()
        {
            var client = CornApp.CurrentClient;
            if (client == null) return;

            client.Disconnect();
        }

        protected override void Initialize()
        {
            // Initialize controls and add listeners
            resumeButton.onClick.AddListener(CloseScreen);
            quitButton.onClick.AddListener(QuitToLogin);
        }

        public override void UpdateScreen()
        {
            // Escape key cannot be used here, otherwise it will push pause screen back after poping it
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CloseScreen();
            }
        }
    }
}
