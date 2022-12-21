#nullable enable
using UnityEngine;
using TMPro;

namespace MinecraftClient.UI
{
    [RequireComponent(typeof (Animator))]
    public class FirstPersonChatPanel : MonoBehaviour
    {
        private static readonly Vector2 PANEL_SIZE = new(300F, 390F);
        private const float RESIZE_SPEED = 1000F;

        private Vector2 targetSize = Vector2.zero;
        private bool resized = false;

        public bool Shown { get; set; } = false;

        private TMP_Text? contactName;
        private Animator? firstPersonChatAnim;

        private RectTransform? panelRectTransform;

        void Start()
        {
            // Initialize panel animators
            firstPersonChatAnim = GetComponent<Animator>();
            firstPersonChatAnim.SetBool("Show", false);

            var panelObj = transform.Find("Panel").gameObject;
            panelRectTransform = panelObj.GetComponent<RectTransform>();

            contactName = panelObj.transform.Find("Contact Name").GetComponent<TMP_Text>();

        }

        public void Show(string contact)
        {
            if (Shown) // Shown already, change contact
            {
                contactName!.text = contact;

                // TODO Implement real contact logic

            }
            else
            {
                targetSize = PANEL_SIZE;
                firstPersonChatAnim!.SetBool("Show", true);

                contactName!.text = contact;

                Shown = true;
            }
            
            resized = false;
        }

        public void Hide()
        {
            targetSize = Vector2.zero;
            firstPersonChatAnim!.SetBool("Show", false);

            Shown = false;
            resized = true;
        }

        void Update()
        {
            var widthDelta = targetSize.x - panelRectTransform!.rect.width;

            if (widthDelta != 0F)
            {
                if (widthDelta > 0F)
                {
                    panelRectTransform!.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
                            Mathf.Min(targetSize.x, panelRectTransform.rect.width + Time.deltaTime * RESIZE_SPEED));
                }
                else
                {
                    panelRectTransform!.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
                            Mathf.Max(targetSize.x, panelRectTransform.rect.width - Time.deltaTime * RESIZE_SPEED));
                }
            }

            var heightDelta = targetSize.y - panelRectTransform!.rect.height;

            if (heightDelta != 0F)
            {
                if (heightDelta > 0F)
                {
                    panelRectTransform!.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
                            Mathf.Min(targetSize.y, panelRectTransform.rect.height + Time.deltaTime * RESIZE_SPEED));
                }
                else
                {
                    panelRectTransform!.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
                            Mathf.Max(targetSize.y, panelRectTransform.rect.height - Time.deltaTime * RESIZE_SPEED));
                }
            }

            if (!resized)
            {
                if (widthDelta == 0F && heightDelta == 0F)
                    resized = true;
            }

        }

    }
}
