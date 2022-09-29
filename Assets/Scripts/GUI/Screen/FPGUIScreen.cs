#nullable enable
using UnityEngine;

namespace MinecraftClient.UI
{
    [RequireComponent(typeof (CanvasGroup))]
    public class FPGUIScreen : BaseScreen
    {
        private bool isActive = false;

        public override bool IsActive
        {
            set {
                if (value)
                    firstPersonPanel?.ShowPanel();
                else
                    firstPersonPanel?.HidePanel();

                isActive = value;
            }

            get {
                return isActive;
            }
        }

        private CornClient? game;
        private FirstPersonGUI? firstPersonPanel = null;
        private Canvas? firstPersonCanvas = null;

        public override bool ReleaseCursor()
        {
            return true;
        }

        public override bool ShouldPause()
        {
            return false;
        }

        protected override bool Initialize()
        {
            // Initialize owner
            game = CornClient.Instance;

            // Create and initialize panel if not present
            if (firstPersonPanel is null)
            {
                var player = game!.GetPlayerController();
                var camera = game!.GetCameraController();

                if (player is not null && camera is not null)
                {
                    var firstPersonPanelPrefab = Resources.Load<GameObject>("Prefabs/First Person GUI");
                    var firstPersonPanelObj = GameObject.Instantiate(firstPersonPanelPrefab);
                    firstPersonPanelObj.transform.SetParent(player.transform, false);
                    firstPersonPanelObj.transform.localPosition = new(0F, 1.45F, 0F);

                    firstPersonPanel = firstPersonPanelObj.GetComponent<FirstPersonGUI>();
                    firstPersonPanel.EnsureInitialized();

                    firstPersonCanvas = firstPersonPanelObj.GetComponentInChildren<Canvas>();
                    firstPersonCanvas.worldCamera = camera.ActiveCamera;

                    return true;
                }
            }
            
            return false;
        }

        void Update()
        {
            if (!IsActive)
                return;
            
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.I))
                CornClient.Instance.ScreenControl?.TryPopScreen();
            
            

        }

    }
}
