using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

using CraftSharp.Event;
using CraftSharp.Protocol.Message;

namespace CraftSharp.UI
{
    [RequireComponent(typeof (CanvasGroup))]
    public class HUDScreen : BaseScreen
    {
        private const float HEALTH_MULTIPLIER = 1F;
        private static readonly Vector3 STAMINA_TARGET_OFFSET = new(0, -0.5F, 0F);

        // UI controls and objects
        [SerializeField] private TMP_Text latencyText, debugText, modeText;
        [SerializeField] private Animator modePanelAnimator, crosshairAnimator, statusPanelAnimator, playerPanelAnimator;
        [SerializeField] private Button[] modeButtons = new Button[4];
        [SerializeField] private IconSpritePanel mobEffectsPanel;
        [SerializeField] private ValueBar healthBar;
        [SerializeField] private Image experienceBarFill, hungerBarFill;
        [SerializeField] private TMP_Text experienceText, hungerText;
        [SerializeField] private RingValueBar staminaBar;
        [SerializeField] private InteractionPanel interactionPanel;
        [SerializeField] private InventoryHotbar inventoryHotbar;
        [SerializeField] private Animator screenAnimator;

        private Animator staminaBarAnimator;

        private bool isActive = false, debugInfo = false;

        private bool modePanelShown  = false;
        private int selectedMode     = 0;
        private int displayedLatency = 0;
        private readonly Dictionary<ResourceLocation, int> mobEffectsDurationTicks = new();
        private readonly Dictionary<ResourceLocation, int> mobEffectsCurrentTicks = new();

        [SerializeField] private RectTransform chatContentPanel;
        [SerializeField] private GameObject chatMessagePreviewPrefab;
        [SerializeField] [Range(0.1F, 1F)] private float transitionTime = 0.1F;
        private float transitionCooldown = 0F;
        
        // Input System Fields & Methods
        public HUDActions Actions { get; private set; }

        public override bool IsActive
        {
            set {
                isActive = value;
                
                screenAnimator.SetBool(SHOW_HASH, isActive);
                var gamemode = CornApp.CurrentClient?.GameMode ?? GameMode.Spectator;

                if (isActive)
                {
                    // Set transition cooldown to prevent HUD screen from being pushed and popped repeatedly
                    transitionCooldown = transitionTime;
                    // Update visibility of item icons on interaction panel
                    interactionPanel.UpdateItemIconsAndTargetHintVisibility(gamemode != GameMode.Spectator);
                    
                    // Enable actions
                    BaseActions?.Enable();
                    Actions?.Enable();
                }
                else
                {
                    // Update visibility of item icons on interaction panel
                    interactionPanel.UpdateItemIconsAndTargetHintVisibility(false);
                    
                    // Disable actions
                    BaseActions.Disable();
                    Actions?.Disable();
                }
            }

            get => isActive;
        }

        public override bool ReleaseCursor()
        {
            return false;
        }

        public override bool ShouldPauseControllerInput()
        {
            return false;
        }

        protected override void Initialize()
        {
            // Initialize actions...
            Actions = new HUDActions();
            
            // Initialize controls...
            staminaBarAnimator = staminaBar.GetComponent<Animator>();

            cameraAimCallback = e => crosshairAnimator.SetBool(SHOW_HASH, e.Aiming);

            gameModeCallback = e =>
            {
                var showStatusPanel = e.GameMode switch {
                    GameMode.Survival   => true,
                    GameMode.Adventure  => true,
                    _                   => false
                };
                var showPlayerPanel = e.GameMode != GameMode.Spectator;

                statusPanelAnimator.SetBool(SHOW_HASH, showStatusPanel);
                playerPanelAnimator.SetBool(SHOW_HASH, showPlayerPanel);
                interactionPanel.UpdateItemIconsAndTargetHintVisibility(showPlayerPanel);
            };

            mobEffectUpdateCallback = e =>
            {
                var effectId = e.Effect.EffectId;
                var spriteTypeId = new ResourceLocation(
                    CornApp.RESOURCE_LOCATION_NAMESPACE, $"gui_mob_effect_{effectId.Path}");

                if (e.Effect.ShowIcon)
                {
                    mobEffectsDurationTicks[effectId] = e.Effect.Duration;
                    mobEffectsCurrentTicks[effectId] = e.Effect.Duration;
                    mobEffectsPanel.AddIconSprite(effectId, spriteTypeId);
                    mobEffectsPanel.UpdateIconText(effectId, ChatParser.TranslateString($"potion.potency.{e.Effect.Amplifier}"));
                }
            };
            
            mobEffectRemovalCallback = e =>
            {
                var effectId = MobEffectPalette.INSTANCE.GetIdByNumId(e.EffectId);
                
                mobEffectsPanel.RemoveIconSprite(effectId);
                mobEffectsCurrentTicks.Remove(effectId);
                mobEffectsDurationTicks.Remove(effectId);
            };
            
            tickSyncCallback = e =>
            {
                if (mobEffectsCurrentTicks.Count > 0)
                {
                    foreach (var effectId in mobEffectsCurrentTicks.Keys.ToArray())
                    {
                        var updatedTicks = Mathf.Max(0, mobEffectsCurrentTicks[effectId] - e.PassedTicks);
                        mobEffectsCurrentTicks[effectId] = updatedTicks;
                        var fill = updatedTicks / (float) mobEffectsDurationTicks[effectId];
                        mobEffectsPanel.UpdateIconFill(effectId, Mathf.Clamp01(fill));
                        var blink = updatedTicks < 200; // Roughly 10 seconds at 20TPS
                        mobEffectsPanel.UpdateIconBlink(effectId, blink, blink ? 200F / Mathf.Max(40, updatedTicks) : 1F);
                    }
                }
            };

            healthCallback = e =>
            {
                if (e.UpdateMaxHealth)
                    healthBar.MaxValue = e.Health * HEALTH_MULTIPLIER;

                healthBar.CurValue = e.Health * HEALTH_MULTIPLIER;
            };
            
            hungerCallback = e =>
            {
                hungerText.text = $"[{e.Saturation:0.0}] {e.Hunger}/20";
                hungerBarFill.fillAmount = Mathf.Clamp01(e.Hunger / 20F);
            };
            
            experienceCallback = e =>
            {
                experienceText.text = $"Lv.{e.Level}";
                experienceBarFill.fillAmount = Mathf.Clamp01(e.LevelUpProgress);
            };

            staminaCallback = e =>
            {
                staminaBar.CurValue = e.Stamina;

                if (!Mathf.Approximately(staminaBar.MaxValue, e.MaxStamina))
                {
                    staminaBar.MaxValue = e.MaxStamina;
                }

                staminaBarAnimator.SetBool(SHOW_HASH, e.Stamina < e.MaxStamina); // Stamina is full
            };

            // Register callbacks
            chatMessageCallback = e =>
            {
                var styledMessage = TMPConverter.MC2TMP(e.Message);
                var chatMessageObj = Instantiate(chatMessagePreviewPrefab, chatContentPanel);
                
                var chatMessage = chatMessageObj.GetComponent<TMP_Text>();
                chatMessage.text = styledMessage;
            };

            EventManager.Instance.Register(cameraAimCallback);
            EventManager.Instance.Register(gameModeCallback);
            EventManager.Instance.Register(mobEffectUpdateCallback);
            EventManager.Instance.Register(mobEffectRemovalCallback);
            EventManager.Instance.Register(tickSyncCallback);
            EventManager.Instance.Register(healthCallback);
            EventManager.Instance.Register(hungerCallback);
            EventManager.Instance.Register(experienceCallback);
            EventManager.Instance.Register(staminaCallback);
            EventManager.Instance.Register(chatMessageCallback);

            // Initialize controls
            var game = CornApp.CurrentClient;
            if (game)
            {
                interactionPanel.OnItemCountChange += newCount =>
                {
                    // Disable camera zoom if there are more than 1 interaction options
                    game.SetCameraZoomEnabled(newCount <= 1);
                };

                crosshairAnimator.SetBool(SHOW_HASH, false);
            }
        }

        #nullable enable

        private Action<CameraAimingEvent>?      cameraAimCallback;
        private Action<GameModeUpdateEvent>?    gameModeCallback;
        private Action<MobEffectUpdateEvent>?   mobEffectUpdateCallback;
        private Action<MobEffectRemovalEvent>?  mobEffectRemovalCallback;
        private Action<TickSyncEvent>?          tickSyncCallback;
        private Action<HealthUpdateEvent>?      healthCallback;
        private Action<HungerUpdateEvent>?      hungerCallback;
        private Action<ExperienceUpdateEvent>?  experienceCallback;
        private Action<StaminaUpdateEvent>?     staminaCallback;
        private Action<ChatMessageEvent>?       chatMessageCallback;

        #nullable disable

        protected override void OnDestroy()
        {
            // Make sure base actions are disabled
            base.OnDestroy();
            
            if (cameraAimCallback is not null)
                EventManager.Instance.Unregister(cameraAimCallback);
            if (gameModeCallback is not null)
                EventManager.Instance.Unregister(gameModeCallback);
            if (mobEffectUpdateCallback is not null)
                EventManager.Instance.Unregister(mobEffectUpdateCallback);
            if (mobEffectRemovalCallback is not null)
                EventManager.Instance.Unregister(mobEffectRemovalCallback);
            if (tickSyncCallback is not null)
                EventManager.Instance.Unregister(tickSyncCallback);
            if (healthCallback is not null)
                EventManager.Instance.Unregister(healthCallback);
            if (hungerCallback is not null)
                EventManager.Instance.Unregister(hungerCallback);
            if (experienceCallback is not null)
                EventManager.Instance.Unregister(experienceCallback);
            if (staminaCallback is not null)
                EventManager.Instance.Unregister(staminaCallback);
            if (chatMessageCallback is not null)
                EventManager.Instance.Unregister(chatMessageCallback);
            
            // Make sure actions are disabled
            Actions?.Disable();
        }

        public override void UpdateScreen()
        {
            if (transitionCooldown > 0F)
            {
                transitionCooldown -= Time.unscaledDeltaTime;
                return;
            }

            var game = CornApp.CurrentClient;
            if (!game) return;

            if (Keyboard.current != null && Keyboard.current.f3Key.isPressed)
            {
                if (Keyboard.current.f4Key.wasPressedThisFrame)
                {
                    int buttonCount = modeButtons.Length;
                    if (modePanelShown) // Select next gamemode
                    {
                        selectedMode = (selectedMode + 1) % buttonCount;
                        modeText.text =
                            ChatParser.TranslateString($"gameMode.{((GameMode)selectedMode).GetIdentifier()}");
                        modeButtons[selectedMode].Select();
                    }
                    else // Show gamemode switch
                    {
                        selectedMode = (int)game.GameMode;
                        if (selectedMode >= 0 && selectedMode < modeButtons.Length)
                        {
                            modeText.text =
                                ChatParser.TranslateString($"gameMode.{((GameMode)selectedMode).GetIdentifier()}");
                            modePanelAnimator.SetBool(SHOW_HASH, true);
                            modePanelShown = true;
                            modeButtons[selectedMode].Select();
                            // Hide crosshair (if shown)
                            crosshairAnimator.SetBool(SHOW_HASH, false);
                        }
                    }
                }
            }

            if (Keyboard.current != null && Keyboard.current.f3Key.wasReleasedThisFrame)
            {
                if (modePanelShown) // Hide gamemode switch
                {
                    modePanelAnimator.SetBool(SHOW_HASH, false);
                    modePanelShown = false;
                    // Show crosshair (if should be shown)
                    if (game.CameraController && game.CameraController.IsAimingOrLocked)
                    {
                        crosshairAnimator.SetBool(SHOW_HASH, true);
                    }

                    if (selectedMode != (int)game.GameMode) // Commit switch request
                    {
                        game.TrySendChat($"/gamemode {((GameMode)selectedMode).GetIdentifier()}");
                    }
                }
                else // Toggle debug info
                {
                    debugInfo = !debugInfo;
                }
            }

            if (Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame) // Open packet screen
            {
                game.ScreenControl.PushScreen<PacketScreen>();
            }
            
            if (game.GameMode != GameMode.Spectator) // Check inventory actions
            {
                if (Actions.Interaction.ExecuteOption.WasPressedThisFrame()) // Execute interactions
                {
                    interactionPanel.RunInteractionOption();
                }

                if (Keyboard.current != null && Mouse.current != null)
                {
                    var mouseScroll = Mouse.current.scroll.value.y;
                    if (mouseScroll != 0F && !Keyboard.current.shiftKey.IsPressed())
                    {
                        if (interactionPanel && interactionPanel.ShouldConsumeMouseScroll &&
                            Keyboard.current.altKey.isPressed) // Interaction option selection
                        {
                            if (mouseScroll < 0F)
                                interactionPanel.SelectNextOption();
                            else
                                interactionPanel.SelectPrevOption();
                        }
                        else // Hotbar slot selection
                        {
                            if (mouseScroll < 0F)
                                game.ChangeHotbarSlotBy(1);
                            else
                                game.ChangeHotbarSlotBy(-1);
                        }
                    }

                    // Hotbar slot selection by key
                    if (Keyboard.current.digit1Key.wasPressedThisFrame)
                        game.ChangeHotbarSlot(0);
                    if (Keyboard.current.digit2Key.wasPressedThisFrame)
                        game.ChangeHotbarSlot(1);
                    if (Keyboard.current.digit3Key.wasPressedThisFrame)
                        game.ChangeHotbarSlot(2);
                    if (Keyboard.current.digit4Key.wasPressedThisFrame)
                        game.ChangeHotbarSlot(3);
                    if (Keyboard.current.digit5Key.wasPressedThisFrame)
                        game.ChangeHotbarSlot(4);
                    if (Keyboard.current.digit6Key.wasPressedThisFrame)
                        game.ChangeHotbarSlot(5);
                    if (Keyboard.current.digit7Key.wasPressedThisFrame)
                        game.ChangeHotbarSlot(6);
                    if (Keyboard.current.digit8Key.wasPressedThisFrame)
                        game.ChangeHotbarSlot(7);
                    if (Keyboard.current.digit9Key.wasPressedThisFrame)
                        game.ChangeHotbarSlot(8);
                }

                if (Actions.Interaction.DropItem.WasPressedThisFrame())
                {
                    var dropEntireStack = Keyboard.current != null && Keyboard.current.ctrlKey.isPressed;
                    
                    game.DropItem(dropEntireStack);
                }

                if (Actions.Interaction.SwapItems.WasPressedThisFrame())
                {
                    game.SwapItemOnHands();
                }

                if (Actions.Interaction.OpenInventory.WasPressedThisFrame())
                {
                    game.OpenPlayerInventory();
                }
            }

            if (Keyboard.current != null && Keyboard.current.slashKey.wasPressedThisFrame)
            {
                var chatScreen = game.ScreenControl.PushScreen<ChatScreen>();

                // Input command prefix '/'
                chatScreen.InputCommandPrefix();
            }
            
            if (Actions.Interaction.OpenChat.WasPressedThisFrame())
            {
                game.ScreenControl.PushScreen<ChatScreen>();
            }

            if (BaseActions.Interaction.CloseScreen.WasPressedThisFrame())
            {
                game.ScreenControl.PushScreen<PauseScreen>();
            }
            
            debugText.text = game.GetInfoString(debugInfo);

            var currentLatency = game.GetOwnLatency();

            if (displayedLatency != currentLatency)
            {
                displayedLatency = (int) Mathf.Lerp(displayedLatency, currentLatency, 0.55F);
                
                if (displayedLatency >= 500)
                    latencyText.text =  $"<color=red>{displayedLatency} ms</color>";
                else if (displayedLatency >= 100)
                    latencyText.text =  $"<color=orange>{displayedLatency} ms</color>";
                else
                    latencyText.text =  $"{displayedLatency} ms";
            }
        }

        private void LateUpdate()
        {
            var game = CornApp.CurrentClient;
            if (!game || !isActive) return;
            
            if (game.CameraController)
            {
                var originOffset = game.WorldOriginOffset;
                var uiCamera = game.UICamera;
                var camControl = game.CameraController;
                
                // Update stamina bar position
                var newPos = uiCamera.ViewportToWorldPoint(camControl.GetTargetViewportPos(STAMINA_TARGET_OFFSET));

                // Don't modify z coordinate
                staminaBar.transform.position = new Vector3(newPos.x, newPos.y, staminaBar.transform.position.z);

                // Update interaction target hint
                interactionPanel.UpdateInteractionTargetHint(originOffset, uiCamera, camControl);
            }
        }
    }
}
