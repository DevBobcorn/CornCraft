#nullable enable
using System;
using UnityEngine;
using UnityEngine.UI;

using CraftSharp.Event;

namespace CraftSharp.UI
{
    [RequireComponent(typeof (CanvasGroup))]
    public class DeathScreen : BaseScreen
    {
        private bool isActive = false;

        // UI controls and objects
        [SerializeField] private Button? respawnButton, quitButton;
        private CanvasGroup? screenGroup;

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

        protected override void Initialize()
        {
            // Initialize controls and add listeners
            screenGroup = GetComponent<CanvasGroup>();

            respawnButton!.onClick.AddListener(this.Respawn);
            quitButton!.onClick.AddListener(this.QuitGame);

            healthCallback = (e) => {
                if (e.Health <= 0F && !this.isActive)
                    CornApp.CurrentClient?.ScreenControl.PushScreen(this);
                else if (e.Health > 0F && this.isActive) // Hide death screen
                    CornApp.CurrentClient?.ScreenControl.TryPopScreen();
            };

            EventManager.Instance.Register(healthCallback);
        }

        void OnDestroy()
        {
            if (healthCallback is not null)
                EventManager.Instance.Unregister(healthCallback);
        }

        public override void UpdateScreen()
        {
            
        }
    }
}
