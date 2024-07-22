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

            get {
                return isActive;
            }
        }

        public override bool ReleaseCursor()
        {
            return true;
        }

        public override bool ShouldPause()
        {
            return true;
        }

        public void Back2Game()
        {
            var client = CornApp.CurrentClient;
            if (client == null) return;

            client.ScreenControl.TryPopScreen();
        }

        public void QuitGame()
        {
            var client = CornApp.CurrentClient;
            if (client == null) return;

            client.Disconnect();
        }

        protected override void Initialize()
        {
            // Initialize controls and add listeners
            resumeButton.onClick.AddListener(this.Back2Game);
            quitButton.onClick.AddListener(this.QuitGame);
        }

        public override void UpdateScreen()
        {
            // Escape key cannot be used here, otherwise it will push pause screen back after poping it
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Back2Game();
            }
        }
    }
}
