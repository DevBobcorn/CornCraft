using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

using CraftSharp.Event;

namespace CraftSharp.UI
{
    [RequireComponent(typeof (CanvasGroup))]
    public class DeathScreen : BaseScreen
    {
        // UI controls and objects
        [SerializeField] private Animator screenAnimator;
        [SerializeField] private Button respawnButton, quitButton;

        private bool isActive = false;

        public override bool IsActive
        {
            set {
                isActive = value;
                screenAnimator.SetBool(SHOW_HASH, isActive);

                if (isActive)
                {
                    respawnButton.interactable = false;
                    
                    StartCoroutine(EnableRespawn());
                }
            }

            get => isActive;
        }

        private IEnumerator EnableRespawn()
        {
            yield return new WaitForSecondsRealtime(0.5F);
            
            respawnButton.interactable = true;
        }

        public override bool ReleaseCursor()
        {
            return true;
        }

        public override bool ShouldPauseControllerInput()
        {
            return true;
        }

        private void Respawn()
        {
            var client = CornApp.CurrentClient;
            if (!client) return;

            client.SendRespawnPacket();

            if (isActive)
            {
                client.ScreenControl.TryPopScreen();
            }
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
            respawnButton.onClick.AddListener(Respawn);
            quitButton.onClick.AddListener(QuitGame);

            healthCallback = e => {
                var client = CornApp.CurrentClient;
                if (!client) return;

                if (e.Health <= 0F && !isActive)
                {
                    client.ScreenControl.PushScreen<DeathScreen>();
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
