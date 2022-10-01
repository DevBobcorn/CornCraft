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
                screenGroup!.alpha = value ? 1F : 0F;
                screenGroup!.blocksRaycasts = value;
                screenGroup!.interactable   = value;
            }

            get {
                return isActive;
            }
        }

        private CornClient? game;
        private CanvasGroup? screenGroup;
        private FirstPersonGUI? firstPersonPanel = null;

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
            screenGroup = GetComponent<CanvasGroup>();
            game = CornClient.Instance;

            // Create and initialize panel if not present
            if (firstPersonPanel is null)
            {
                var playerCon = game!.GetPlayerController();
                var cameraCon = game!.GetCameraController();

                if (playerCon is not null)
                {
                    var firstPersonPanelPrefab = Resources.Load<GameObject>("Prefabs/GUI/First Person GUI");
                    var firstPersonPanelObj = GameObject.Instantiate(firstPersonPanelPrefab);
                    firstPersonPanelObj.transform.SetParent(playerCon.transform, false);
                    firstPersonPanelObj.transform.localPosition = new(0F, 1.45F, 0F);

                    firstPersonPanel = firstPersonPanelObj.GetComponent<FirstPersonGUI>();
                    firstPersonPanel.EnsureInitialized();
                    firstPersonPanel.SetCameraCon(cameraCon);

                    return true;
                }
            }
            
            return false;
        }

        void Update()
        {
            if (!IsActive)
                return;
            
            if (Input.GetKeyDown(KeyCode.Escape))
                CornClient.Instance.ScreenControl?.TryPopScreen();

        }

    }
}
