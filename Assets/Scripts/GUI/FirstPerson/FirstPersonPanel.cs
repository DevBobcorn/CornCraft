#nullable enable
using UnityEngine;

namespace MinecraftClient.UI
{
    [RequireComponent(typeof (Animator))]
    public class FirstPersonPanel : MonoBehaviour
    {
        private const float RESIZE_SPEED = 1000F;

        private Vector2 targetSize = Vector2.zero;
        private bool resized = false;

        private Animator? firstPersonPanelAnim, contentAnim;

        private RectTransform? panelRectTransform;

        public WidgetState State { get; set; } = WidgetState.Hidden;

        void Start()
        {
            // Initialize panel animators
            firstPersonPanelAnim = GetComponent<Animator>();
            firstPersonPanelAnim.SetBool("Show", false);
            State = WidgetState.Hidden;

            var contentObj = transform.Find("Content").gameObject;
            contentAnim = contentObj.GetComponent<Animator>();
            contentAnim.SetBool("Show", false);

            var panelObj = transform.Find("Panel").gameObject;
            panelRectTransform = panelObj.GetComponent<RectTransform>();

        }

        public void Show(float width, float height)
        {
            targetSize = new(width, height);

            firstPersonPanelAnim!.SetBool("Show", true);
            contentAnim!.SetBool("Show", false);

            resized = false;
        }

        public void Hide()
        {
            targetSize = Vector2.zero;

            firstPersonPanelAnim!.SetBool("Show", false);
            contentAnim!.SetBool("Show", false);

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
                {
                    resized = true;
                    contentAnim!.SetBool("Show", true);
                }
            }

        }

    }
}
