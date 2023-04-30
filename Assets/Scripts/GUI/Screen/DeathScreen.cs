#nullable enable
using System;
using UnityEngine;
using UnityEngine.UI;

using MinecraftClient.Event;

namespace MinecraftClient.UI
{
    [RequireComponent(typeof (CanvasGroup))]
    public class DeathScreen : BaseScreen
    {
        private bool isActive = false;

        private Button? respawnButton, quitButton;

        public override bool IsActive
        {
            set {
                isActive = value;
                screenGroup!.alpha = value ? 1F : 0F;
                screenGroup!.blocksRaycasts = value;
                screenGroup!.interactable   = value;
            }

            get {
                return isActive;
            }
        }

        // UI controls
        private CanvasGroup? screenGroup;

        public override bool ReleaseCursor()
        {
            return true;
        }

        public override bool ShouldPause()
        {
            return true;
        }

        public void Respawn()
        {
            CornApp.CurrentClient?.SendRespawnPacket();
        }

        public void QuitGame()
        {
            CornApp.CurrentClient?.Disconnect();
        }

        private Action<HealthUpdateEvent>? healthCallback;

        protected override bool Initialize()
        {
            // Initialize controls and add listeners
            screenGroup = GetComponent<CanvasGroup>();

            respawnButton = transform.Find("Respawn Button").GetComponent<Button>();
            quitButton   = transform.Find("Quit Button").GetComponent<Button>();

            respawnButton.onClick.AddListener(this.Respawn);
            quitButton.onClick.AddListener(this.QuitGame);

            healthCallback = (e) => {
                if (e.Health <= 0F && !this.isActive)
                    CornApp.CurrentClient?.ScreenControl?.PushScreen(this);
                else if (e.Health > 0F && this.isActive) // Hide death screen
                    CornApp.CurrentClient?.ScreenControl?.TryPopScreen();
            };

            EventManager.Instance.Register(healthCallback);
            
            return true;
        }

        void OnDestroy()
        {
            if (healthCallback is not null)
                EventManager.Instance.Unregister(healthCallback);

        }

        void Update()
        {
            if (!IsActive)
                return;

        }

    }
}
