#nullable enable
using UnityEngine;
using MinecraftClient.Control;

namespace MinecraftClient.UI
{
    public class FirstPersonPanel : MonoBehaviour
    {
        public float followSpeed = 75F;
        public float maxDeltaAngle = 25F;

        private PlayerController? player;
        private CornClient? game;
        private Animator? panel;

        private bool panelShown = false;
        public bool PanelShown
        {
            get {
                return panelShown;
            }
        }

        private void ShowPanel()
        {
            // First calculate and set rotation
            var cameraRot = Camera.main.transform.eulerAngles.y;
            transform.eulerAngles = new(0F, cameraRot, 0F);

            // Then play show animation
            panel!.SetBool("Show", true);
            panelShown = true;
        }

        private void HidePanel()
        {
            // Play hide animation
            panel!.SetBool("Show", false);
            panelShown = false;
        }

        void Start()
        {
            // Find game instance
            game = CornClient.Instance;

            // Initialize panel animator
            panel = GetComponent<Animator>();
            panel.SetBool("Show", false);

            panelShown = false;

        }

        void Update()
        {
            if (game!.IsPaused())
                return;

            if (Input.GetKeyDown(KeyCode.I))
            {
                if (panelShown)
                    HidePanel();
                else
                    ShowPanel();
            }

            if (Camera.main is not null)
            {
                var cameraRot = Camera.main.transform.eulerAngles.y;
                var ownRot = transform.eulerAngles.y;

                var deltaRot = Mathf.DeltaAngle(ownRot, cameraRot);

                if (Mathf.Abs(deltaRot) > maxDeltaAngle)
                {
                    if (deltaRot > 0F)
                        transform.eulerAngles = new(0F, ownRot + followSpeed * Time.deltaTime, 0F);
                    else
                        transform.eulerAngles = new(0F, ownRot - followSpeed * Time.deltaTime, 0F);

                }
                
            }

        }

    }
}
