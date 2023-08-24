#nullable enable
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

using CraftSharp.Event;
using CraftSharp.Control;

namespace CraftSharp.UI
{
    [RequireComponent(typeof (CanvasGroup))]
    public class HUDScreen : BaseScreen
    {
        private static readonly int SHOW = Animator.StringToHash("Show");

        private static readonly string[] modeIdentifiers = { "survival", "creative", "adventure", "spectator" };
        private const float HEALTH_MULTIPLIER = 10F;

        // UI controls and objects
        [SerializeField] private TMP_Text? latencyText, debugText, modeText;
        [SerializeField] private Animator? modePanelAnimator, crosshairAnimator, statusPanelAnimator;
        [SerializeField] private Button[] modeButtons = new Button[4];
        [SerializeField] private ValueBar? healthBar;
        [SerializeField] private RingValueBar? staminaBar;
        [SerializeField] private InteractionPanel? interactionPanel;
        [SerializeField] private Camera? UICamera;
        private Animator? staminaBarAnimator;
        private ChatScreen? chatScreen;
        private PauseScreen? pauseScreen;
        private CanvasGroup? screenGroup;

        private bool isActive = false, debugInfo = true;

        private bool modePanelShown  = false;
        private int  selectedMode    = 0;
        private int displayedLatency = 0;

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
            return false;
        }

        public override bool ShouldPause()
        {
            return false;
        }

        protected override bool Initialize()
        {
            // Initialize controls...
            staminaBarAnimator = staminaBar!.GetComponent<Animator>();

            perspectiveCallback = (e) => {
                switch (e.Perspective)
                {
                    case Perspective.FirstPerson:
                        crosshairAnimator!.SetBool(SHOW, true);
                        break;
                    case Perspective.ThirdPerson:
                        crosshairAnimator!.SetBool(SHOW, false);
                        break;
                }
            };

            gameModeCallback = (e) => {
                var showStatus = e.GameMode switch {
                    GameMode.Survival   => true,
                    GameMode.Creative   => false,
                    GameMode.Adventure  => true,
                    GameMode.Spectator  => false,

                    _                   => false
                };

                statusPanelAnimator!.SetBool(SHOW, showStatus);
            };

            healthCallback = (e) => {
                if (e.UpdateMaxHealth)
                    healthBar!.MaxValue = e.Health * HEALTH_MULTIPLIER;

                healthBar!.CurValue = e.Health * HEALTH_MULTIPLIER;
            };

            staminaCallback = (e) => {
                staminaBar.CurValue = e.Stamina;

                if (e.IsStaminaFull)
                {
                    staminaBar.MaxValue = e.Stamina;
                    staminaBarAnimator.SetBool(SHOW, false);
                }
                else
                    staminaBarAnimator.SetBool(SHOW, true);
            };

            EventManager.Instance.Register(perspectiveCallback);
            EventManager.Instance.Register(gameModeCallback);
            EventManager.Instance.Register(healthCallback);
            EventManager.Instance.Register(staminaCallback);

            // Initialize screens
            chatScreen  = GameObject.FindObjectOfType<ChatScreen>(true);
            pauseScreen = GameObject.FindObjectOfType<PauseScreen>(true);

            // Initialize controls
            screenGroup = GetComponent<CanvasGroup>();

            if (interactionPanel != null)
            {
                interactionPanel.OnItemCountChange += newCount =>
                {
                    // Absorb mouse scroll input if there're more than 1 interaction options
                    PlayerUserInputData.Current.MouseScrollAbsorbed = newCount > 1;
                };
            }

            var game = CornApp.CurrentClient;
            if (game != null)
            {
                if (game.CameraController?.GetPerspective() == Perspective.FirstPerson)
                {
                    crosshairAnimator!.SetBool(SHOW, true);
                }
                else
                {
                    crosshairAnimator!.SetBool(SHOW, false);
                }
            }
            
            return true;
        }

        private Action<PerspectiveUpdateEvent>? perspectiveCallback;
        private Action<GameModeUpdateEvent>?    gameModeCallback;
        private Action<HealthUpdateEvent>?      healthCallback;
        private Action<StaminaUpdateEvent>?     staminaCallback;

        void OnDestroy()
        {
            if (perspectiveCallback is not null)
                EventManager.Instance.Unregister(perspectiveCallback);
            
            if (gameModeCallback is not null)
                EventManager.Instance.Unregister(gameModeCallback);
            
            if (healthCallback is not null)
                EventManager.Instance.Unregister(healthCallback);
            
            if (staminaCallback is not null)
                EventManager.Instance.Unregister(staminaCallback);
        }

        void Update()
        {
            if (!initialized || !IsActive)
                return;
            
            var game = CornApp.CurrentClient;
            if (game == null) return;

            if (Input.GetKey(KeyCode.F3))
            {
                if (Input.GetKeyDown(KeyCode.F4))
                {
                    int buttonCount = modeButtons.Length;
                    if (modePanelShown) // Select next gamemode
                    {
                        selectedMode = (selectedMode + 1) % buttonCount;
                        modeText!.text = ((GameMode)selectedMode).ToString();
                        modeButtons[selectedMode].Select();
                    }
                    else // Show gamemode switch
                    {
                        selectedMode = (int) game.GameMode;
                        if (selectedMode >= 0 && selectedMode < modeButtons.Length)
                        {
                            modeText!.text = ((GameMode)selectedMode).ToString();
                            modePanelAnimator!.SetBool(SHOW, true);
                            modePanelShown = true;
                            modeButtons[selectedMode].Select();
                            // Hide crosshair (if shown)
                            crosshairAnimator!.SetBool(SHOW, false);
                        }
                    }
                }
            }

            if (Input.GetKeyUp(KeyCode.F3))
            {
                if (modePanelShown) // Hide gamemode switch
                {
                    modePanelAnimator!.SetBool(SHOW, false);
                    modePanelShown = false;

                    if (selectedMode != (int) game.GameMode) // Commit switch request
                    {
                        game.TrySendChat($"/gamemode {modeIdentifiers[selectedMode]}");
                    }
                    
                    // Restore crosshair if necessary
                    if (game.CameraController?.GetPerspective() == Perspective.FirstPerson)
                    {
                        crosshairAnimator!.SetBool(SHOW, true);
                    }
                }
                else // Toggle debug info
                {
                    debugInfo = !debugInfo;
                }
            }

            if (Input.GetKeyDown(KeyCode.F)) // Execute interactions
                interactionPanel!.RunInteractionOption();

            if (interactionPanel!.ShouldAbsordMouseScroll)
            {
                var scroll = Input.GetAxis("Mouse ScrollWheel");
                if (scroll != 0F && interactionPanel is not null)
                {
                    if (scroll < 0F)
                        interactionPanel.SelectNextOption();
                    else
                        interactionPanel.SelectPrevOption();
                }
            }

            if (Input.GetKeyDown(KeyCode.F5))
            {
                game.CameraController?.SwitchPerspective();
            }

            // Hotbar slot switching
            if (Input.GetKeyDown(KeyCode.Alpha1))
                game.ChangeSlot(0);
            if (Input.GetKeyDown(KeyCode.Alpha2))
                game.ChangeSlot(1);
            if (Input.GetKeyDown(KeyCode.Alpha3))
                game.ChangeSlot(2);
            if (Input.GetKeyDown(KeyCode.Alpha4))
                game.ChangeSlot(3);
            if (Input.GetKeyDown(KeyCode.Alpha5))
                game.ChangeSlot(4);
            if (Input.GetKeyDown(KeyCode.Alpha6))
                game.ChangeSlot(5);
            if (Input.GetKeyDown(KeyCode.Alpha7))
                game.ChangeSlot(6);
            if (Input.GetKeyDown(KeyCode.Alpha8))
                game.ChangeSlot(7);
            if (Input.GetKeyDown(KeyCode.Alpha9))
                game.ChangeSlot(8);

            if (Input.GetKeyDown(KeyCode.Slash))
            {   // Open chat screen and input a slash
                game.ScreenControl?.PushScreen(chatScreen);
                chatScreen?.SetChatMessage("/", 1);
            }
            else if (Input.GetKeyDown(KeyCode.T)) // Just open chat screen
                game.ScreenControl?.PushScreen(chatScreen);

            if (Input.GetKeyDown(KeyCode.Escape))
                game.ScreenControl?.PushScreen(pauseScreen);
            
            debugText!.text = game.GetInfoString(debugInfo);

            var realLatency = game.GetOwnLatency();
            if (displayedLatency != realLatency)
            {
                if (realLatency > displayedLatency)
                    displayedLatency++;
                else
                    displayedLatency--;
                
                if (displayedLatency >= 500)
                    latencyText!.text =  $"<color=red>{displayedLatency} ms</color>";
                else if (displayedLatency >= 100)
                    latencyText!.text =  $"<color=orange>{displayedLatency} ms</color>";
                else
                    latencyText!.text =  $"{displayedLatency} ms";
            }

            // Update stamina bar position
            var targetPosition = UICamera!.ViewportToWorldPoint(
                    game!.CameraController!.GetTargetViewportPos());

            staminaBar!.transform.position = Vector3.Lerp(
                    staminaBar.transform.position, targetPosition, Time.deltaTime * 10F);
        }

    }
}
