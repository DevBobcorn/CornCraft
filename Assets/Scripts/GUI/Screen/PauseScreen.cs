using UnityEngine;
using UnityEngine.UI;

namespace MinecraftClient.UI
{
    [RequireComponent(typeof (CanvasGroup))]
    public class PauseScreen : BaseScreen
    {
        private bool isActive = false;

        private Button resumeButton, quitButton;

        public override bool IsActive
        {
            set {
                isActive = value;
                screenGroup.alpha = value ? 1F : 0F;
                screenGroup.blocksRaycasts = value;
                screenGroup.interactable   = value;
            }

            get {
                return isActive;
            }
        }

        // UI controls
        private CanvasGroup screenGroup;

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
            CornClient.Instance.ScreenControl?.TryPopScreen();
        }

        public void QuitGame()
        {
            CornClient.StopClient();
        }

        protected override bool Initialize()
        {
            // Initialize controls and add listeners
            screenGroup = GetComponent<CanvasGroup>();

            resumeButton = transform.Find("Resume Button").GetComponent<Button>();
            quitButton   = transform.Find("Quit Button").GetComponent<Button>();

            resumeButton.onClick.AddListener(this.Back2Game);
            quitButton.onClick.AddListener(this.QuitGame);
            
            return true;
        }

        void Update()
        {
            if (!IsActive)
                return;
            
            if (Input.GetKeyDown(KeyCode.Escape))
                Back2Game();

        }

    }
}
