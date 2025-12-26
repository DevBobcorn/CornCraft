using System;
using UnityEngine;
using UnityEngine.UI;

using CraftSharp.Event;

namespace CraftSharp.UI
{
    [RequireComponent(typeof (CanvasGroup))]
    public class DeathScreen : BaseScreen
    {
        // UI controls and objects
        [SerializeField] private Button respawnButton, quitButton;
        private CanvasGroup screenGroup;

        private bool isActive = false;

        public override bool IsActive
        {
            set {
                isActive = value;
                screenGroup.alpha = value ? 1F : 0F;
                screenGroup.blocksRaycasts = value;
                screenGroup.interactable   = value;
            }

            get => isActive;
        }

        public override bool ReleaseCursor()
        {
            return true;
        }

        public override bool ShouldPauseControllerInput()
        {
            return true;
        }

        private static void Respawn()
        {
            var client = CornApp.CurrentClient;
            if (!client) return;

            client.SendRespawnPacket();
        }

        private static void QuitGame()
        {
            var client = CornApp.CurrentClient;
            if (!client) return;

            client.Disconnect();
        }

        #nullable enable

        private Action<HealthUpdateEvent>? healthCallback;

        #nullable disable

        protected override void Initialize()
        {
            // Initialize controls and add listeners
            screenGroup = GetComponent<CanvasGroup>();

            respawnButton.onClick.AddListener(Respawn);
            quitButton.onClick.AddListener(QuitGame);

            healthCallback = e => {
                var client = CornApp.CurrentClient;
                if (!client) return;

                if (e.Health <= 0F && !isActive)
                {
                    client.ScreenControl.PushScreen<DeathScreen>();
                }
                else if (e.Health > 0F && isActive) // Hide death screen
                {
                    client.ScreenControl.TryPopScreen();
                }
            };

            EventManager.Instance.Register(healthCallback);
        }

        protected override void OnDestroy()
        {
            // Make sure base actions are disabled
            base.OnDestroy();
            
            if (healthCallback is not null)
            {
                EventManager.Instance.Unregister(healthCallback);
            }
        }

        public override void UpdateScreen()
        {
            
        }
    }
}
