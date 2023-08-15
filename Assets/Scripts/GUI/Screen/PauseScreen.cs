#nullable enable
using UnityEngine;
using UnityEngine.UI;

namespace CraftSharp.UI
{
    [RequireComponent(typeof (CanvasGroup))]
    public class PauseScreen : BaseScreen
    {
        private bool isActive = false;

        // UI controls and objects
        [SerializeField] private Button? resumeButton, quitButton;
        private CanvasGroup? screenGroup;

        public override bool IsActive
        {
            set {
                isActive = value;
                screenGroup!.alpha = value ? 1F : 0F;
                screenGroup.blocksRaycasts = value;
                screenGroup.interactable   = value;
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
            CornApp.CurrentClient!.ScreenControl!.TryPopScreen();
        }

        public void QuitGame()
        {
            CornApp.CurrentClient!.Disconnect();
        }

        protected override bool Initialize()
        {
            // Initialize controls and add listeners
            screenGroup = GetComponent<CanvasGroup>();

            resumeButton!.onClick.AddListener(this.Back2Game);
            quitButton!.onClick.AddListener(this.QuitGame);
            
            return true;
        }

        void Update()
        {
            if (!IsActive)
                return;
            
            // Escape key cannot be used here, otherwise it will push pause screen back after poping it
            if (Input.GetKeyDown(KeyCode.Return))
                Back2Game();

        }

    }
}
