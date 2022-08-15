using System;
using UnityEngine;

using TMPro;

using MinecraftClient.Event;

namespace MinecraftClient.UI
{
    [RequireComponent(typeof (CanvasGroup))]
    public class HUDScreen : BaseScreen
    {
        private CornClient game;

        private TMP_Text    latencyText;

        private ChatScreen  chatScreen;
        private PauseScreen pauseScreen;

        private bool isActive = false;

        public override bool IsActive
        {
            set {
                EnsureInitialized();

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

        public override string ScreenName()
        {
            return "HUD Screen";
        }

        public override bool ReleaseCursor()
        {
            return false;
        }

        public override bool ShouldPause()
        {
            return false;
        }

        protected override void Initialize()
        {
            // Initialize screens
            chatScreen  = GameObject.FindObjectOfType<ChatScreen>(true);
            pauseScreen = GameObject.FindObjectOfType<PauseScreen>(true);

            // Initialize controls
            screenGroup = GetComponent<CanvasGroup>();
            latencyText = transform.Find("Latency Text").GetComponent<TMP_Text>();
            
        }

        protected override void Start()
        {
            // Ensure initialization
            base.Start();
            game = CornClient.Instance;
        }

        private int displayedLatency = 0;

        void Update()
        {
            if (!IsActive)
                return;

            if (Input.GetKeyDown(KeyCode.Slash))
            {
                // Open chat screen and input a slash
                CornClient.Instance.ScreenControl?.PushScreen(chatScreen);
                chatScreen?.SetChatMessage("/");
            }
            else if (Input.GetKeyDown(KeyCode.T))
            {
                // Just open chat screen
                CornClient.Instance.ScreenControl?.PushScreen(chatScreen);
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CornClient.Instance.ScreenControl?.PushScreen(pauseScreen);
            }

            var realLatency = game.GetOwnLatency();
            if (displayedLatency != realLatency)
            {
                if (realLatency > displayedLatency)
                    displayedLatency++;
                else
                    displayedLatency--;
                
                latencyText.text = displayedLatency.ToString() + " ms";
            }

        }

    }
}
