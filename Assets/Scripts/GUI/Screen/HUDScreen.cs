using UnityEngine;
using UnityEngine.UI;
using TMPro;

using MinecraftClient.Control;
namespace MinecraftClient.UI
{
    [RequireComponent(typeof (CanvasGroup))]
    public class HUDScreen : BaseScreen
    {
        private static readonly string[] modeIdentifiers = { "survival", "creative", "adventure", "spectator" };

        private CornClient game;

        private TMP_Text    latencyText, debugText, modeText;
        private Animator    modePanel;
        private Button[]    modeButtons = new Button[4];

        private ChatScreen  chatScreen;
        private PauseScreen pauseScreen;

        private bool isActive = false, debugInfo = false;

        private bool modePanelShown = false;
        private int  selectedMode   = 0;

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

            // Initialize controls...
            debugText = transform.Find("Debug Text").GetComponent<TMP_Text>();
            debugText.text = "Initializing...";

            modePanel = transform.Find("Mode Panel").GetComponent<Animator>();
            modeText  = modePanel.transform.Find("Mode Text").GetComponent<TMP_Text>();

            modeButtons[0] = FindHelper.FindChildRecursively(transform, "Survival").GetComponent<Button>();
            modeButtons[1] = FindHelper.FindChildRecursively(transform, "Creative").GetComponent<Button>();
            modeButtons[2] = FindHelper.FindChildRecursively(transform, "Adventure").GetComponent<Button>();
            modeButtons[3] = FindHelper.FindChildRecursively(transform, "Spectator").GetComponent<Button>();

        }

        private int displayedLatency = 0;

        void Update()
        {
            if (!IsActive || game is null)
                return;
            
            if (game is null || !CornClient.Connected) return;

            if (Input.GetKey(KeyCode.F3))
            {
                if (Input.GetKeyDown(KeyCode.F4))
                {
                    int buttonCount = modeButtons.Length;
                    if (modePanelShown) // Select next gamemode
                    {
                        selectedMode = (selectedMode + 1) % buttonCount;
                        // Turn identifiers into names with first letter in upper case...
                        modeText.text = modeIdentifiers[selectedMode][0].ToString().ToUpper() + modeIdentifiers[selectedMode][1..];
                        modeButtons[selectedMode].Select();
                    }
                    else // Show gamemode switch
                    {
                        selectedMode = (int)game.GetGamemode();
                        if (selectedMode >= 0 && selectedMode < modeButtons.Length)
                        {
                            // Turn identifiers into names with first letter in upper case...
                            modeText.text = modeIdentifiers[selectedMode][0].ToString().ToUpper() + modeIdentifiers[selectedMode][1..];
                            modePanel.SetBool("Show", true);
                            modePanelShown = true;
                            modeButtons[selectedMode].Select();
                        }
                    }
                }
            }

            if (Input.GetKeyUp(KeyCode.F3))
            {
                if (modePanelShown) // Hide gamemode switch
                {
                    modePanel.SetBool("Show", false);
                    modePanelShown = false;

                    if (selectedMode != (int)game.GetGamemode()) // Commit switch request
                    {
                        game.SendText("/gamemode " + modeIdentifiers[selectedMode]);
                        CornClient.ShowNotification("Game mode switched to " + modeIdentifiers[selectedMode], 2F, Notification.Type.Notification);
                    }
                }
                else // Toggle debug info
                    debugInfo = !debugInfo;
            }

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

            if (debugInfo)
                debugText.text = $"FPS: {((int)(1F / Time.deltaTime)).ToString().PadLeft(4, ' ')}\n{game.GetPlayerController()?.GetDebugInfo()}\n{game.GetWorldRender()?.GetDebugInfo()}";
            else
                debugText.text = $"FPS: {((int)(1F / Time.deltaTime)).ToString().PadLeft(4, ' ')}";

            var realLatency = game.GetOwnLatency();
            if (displayedLatency != realLatency)
            {
                if (realLatency > displayedLatency)
                    displayedLatency++;
                else
                    displayedLatency--;
                
                if (displayedLatency >= 500)
                    latencyText.text =  $"<color=red>{displayedLatency} ms</color>";
                else if (displayedLatency >= 100)
                    latencyText.text =  $"<color=orange>{displayedLatency} ms</color>";
                else latencyText.text =  $"{displayedLatency} ms";
            }

        }

    }
}
