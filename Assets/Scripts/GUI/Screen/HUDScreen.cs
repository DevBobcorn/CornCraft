#nullable enable
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

using CraftSharp.Event;

namespace CraftSharp.UI
{
    [RequireComponent(typeof (CanvasGroup))]
    public class HUDScreen : BaseScreen
    {
        private static readonly string[] modeIdentifiers = { "survival", "creative", "adventure", "spectator" };
        private const float HEALTH_MULTIPLIER = 10F;

        // UI controls and objects
        [SerializeField] private TMP_Text? latencyText, debugText, modeText;
        [SerializeField] private Animator? modePanelAnimator, crosshairAnimator, statusPanelAnimator;
        [SerializeField] private Button[] modeButtons = new Button[4];
        [SerializeField] private ValueBar? healthBar;
        [SerializeField] private RingValueBar? staminaBar;
        [SerializeField] private InteractionPanel? interactionPanel;
        [SerializeField] protected InputActionReference? scrollInput;
        [SerializeField] private Camera? UICamera;
        [SerializeField] private Animator? screenAnimator;

        private Animator? staminaBarAnimator;
        private ChatScreen? chatScreen;
        private PauseScreen? pauseScreen;

        private bool isActive = false, debugInfo = true;

        private bool modePanelShown  = false;
        private int  selectedMode    = 0;
        private int displayedLatency = 0;

        [SerializeField] [Range(0.1F, 1F)] private float transitionTime = 0.1F;
        private float transitionCooldown = 0F;

        public override bool IsActive
        {
            set {
                isActive = value;
                screenAnimator!.SetBool(SHOW, isActive);

                if (isActive)
                {
                    transitionCooldown = transitionTime;
                }
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

        protected override void Initialize()
        {
            // Initialize controls...
            staminaBarAnimator = staminaBar!.GetComponent<Animator>();

            crosshairCallback = (e) => crosshairAnimator!.SetBool(SHOW, e.Show);

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

            EventManager.Instance.Register(crosshairCallback);
            EventManager.Instance.Register(gameModeCallback);
            EventManager.Instance.Register(healthCallback);
            EventManager.Instance.Register(staminaCallback);

            // Initialize screens
            chatScreen  = GameObject.FindObjectOfType<ChatScreen>(true);
            pauseScreen = GameObject.FindObjectOfType<PauseScreen>(true);

            // Initialize controls
            var game = CornApp.CurrentClient;
            if (game != null)
            {
                interactionPanel!.OnItemCountChange += newCount =>
                {
                    // Absorb mouse scroll input if there're more than 1 interaction options
                    game.ZoomEnabled = newCount > 1;
                };

                if (game.CameraController.GetPerspective() == Perspective.FirstPerson)
                {
                    crosshairAnimator!.SetBool(SHOW, true);
                }
                else
                {
                    crosshairAnimator!.SetBool(SHOW, false);
                }
            }
        }

        private Action<CrosshairEvent>?         crosshairCallback;
        private Action<GameModeUpdateEvent>?    gameModeCallback;
        private Action<HealthUpdateEvent>?      healthCallback;
        private Action<StaminaUpdateEvent>?     staminaCallback;

        void OnDestroy()
        {
            if (crosshairCallback is not null)
                EventManager.Instance.Unregister(crosshairCallback);
            
            if (gameModeCallback is not null)
                EventManager.Instance.Unregister(gameModeCallback);
            
            if (healthCallback is not null)
                EventManager.Instance.Unregister(healthCallback);
            
            if (staminaCallback is not null)
                EventManager.Instance.Unregister(staminaCallback);
        }

        void Update()
        {
            if (!IsActive)
                return;
            
            if (transitionCooldown > 0F)
            {
                transitionCooldown -= Time.unscaledDeltaTime;
                return;
            }
            
            var game = CornApp.CurrentClient;
            if (game == null) return;

            if (Keyboard.current.f3Key.isPressed)
            {
                if (Keyboard.current.f4Key.wasPressedThisFrame)
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

            if (Keyboard.current.f3Key.wasReleasedThisFrame)
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
                    if (game.CameraController.GetPerspective() == Perspective.FirstPerson)
                    {
                        crosshairAnimator!.SetBool(SHOW, true);
                    }
                }
                else // Toggle debug info
                {
                    debugInfo = !debugInfo;
                }
            }

            if (Keyboard.current.fKey.wasPressedThisFrame) // Execute interactions
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

            if (Keyboard.current.f5Key.wasPressedThisFrame)
            {
                game.CameraController.SwitchPerspective();
            }

            // Hotbar slot switching
            if (Keyboard.current.digit1Key.wasPressedThisFrame)
                game.ChangeSlot(0);
            if (Keyboard.current.digit2Key.wasPressedThisFrame)
                game.ChangeSlot(1);
            if (Keyboard.current.digit3Key.wasPressedThisFrame)
                game.ChangeSlot(2);
            if (Keyboard.current.digit4Key.wasPressedThisFrame)
                game.ChangeSlot(3);
            if (Keyboard.current.digit5Key.wasPressedThisFrame)
                game.ChangeSlot(4);
            if (Keyboard.current.digit6Key.wasPressedThisFrame)
                game.ChangeSlot(5);
            if (Keyboard.current.digit7Key.wasPressedThisFrame)
                game.ChangeSlot(6);
            if (Keyboard.current.digit8Key.wasPressedThisFrame)
                game.ChangeSlot(7);
            if (Keyboard.current.digit9Key.wasPressedThisFrame)
                game.ChangeSlot(8);

            if (Keyboard.current.slashKey.wasPressedThisFrame)
            {   // Open chat screen and input a slash
                game.ScreenControl.PushScreen(chatScreen);
                chatScreen?.SetChatMessage("/", 1);
            }
            else if (Keyboard.current.tKey.wasPressedThisFrame) // Just open chat screen
                game.ScreenControl.PushScreen(chatScreen);

            if (Keyboard.current.escapeKey.wasPressedThisFrame)
                game.ScreenControl.PushScreen(pauseScreen);
            
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
                    game!.CameraController.GetTargetViewportPos());

            staminaBar!.transform.position = Vector3.Lerp(
                    staminaBar.transform.position, targetPosition, Time.deltaTime * 10F);
        }
    }
}
