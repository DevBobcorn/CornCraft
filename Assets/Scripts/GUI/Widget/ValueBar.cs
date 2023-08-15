#nullable enable
using UnityEngine;
using UnityEngine.UI;

namespace CraftSharp.UI
{
    public class ValueBar : BaseValueBar
    {
        [SerializeField] private Color normalColor   = Color.white;
        [SerializeField] private Color reduceColor   = new(1F, 0.521F, 0.596F); // 255 133 152 #FF8598
        [SerializeField] private Color increaseColor = new(0.815F, 1F, 0.372F); // 208 255 095 #D0FF5F
        [SerializeField] private Color warningColor  = Color.yellow;
        [SerializeField] private Color dangerColor   = Color.red;

        [SerializeField] [Range(0.1F, 1F)] private float warningThreshold = 0.3F;
        [SerializeField] [Range(0.1F, 1F)] private float dangerThreshold  = 0.1F;

        [SerializeField] private RectTransform.Axis barAxis = RectTransform.Axis.Horizontal;

        [SerializeField] private RectTransform? barFillTransform, deltaFillTransform, displayFillTransform;
        private Image? displayFillImage, deltaFillImage;

        private float fullBarLength;

        void Start()
        {
            deltaFillImage   = deltaFillTransform!.GetComponent<Image>();
            displayFillImage = displayFillTransform!.GetComponent<Image>();

            if (barAxis == RectTransform.Axis.Horizontal)
                fullBarLength = barFillTransform!.rect.width;
            else
                fullBarLength = barFillTransform!.rect.height;

            UpdateValue();

        }

        protected override void UpdateValue()
        {
            if (displayValue > curValue) // Reduce visual fill
            {
                // Calculate new display value
                displayValue = Mathf.Max(displayValue - maxValue * Time.deltaTime, curValue);
                // Then update visuals
                float curValuePos = (curValue / maxValue) * fullBarLength;
                displayFillTransform!.SetSizeWithCurrentAnchors(barAxis, curValuePos);
                deltaFillImage!.color = reduceColor;
                deltaFillTransform!.anchoredPosition = new(curValuePos, 0F);
                deltaFillTransform.SetSizeWithCurrentAnchors(barAxis, (displayValue - curValue) / maxValue * fullBarLength);
            }
            else // Increase visual fill
            {
                // Calculate new display value
                displayValue = Mathf.Min(displayValue + maxValue * Time.deltaTime, curValue);
                // Then update visuals
                float displayValuePos = (displayValue / maxValue) * fullBarLength;
                displayFillTransform!.SetSizeWithCurrentAnchors(barAxis, displayValuePos);
                deltaFillImage!.color = increaseColor;
                deltaFillTransform!.anchoredPosition = new(displayValuePos, 0F);
                deltaFillTransform.SetSizeWithCurrentAnchors(barAxis, (curValue - displayValue) / maxValue * fullBarLength);
            }

            float displayFrac = displayValue / maxValue;

            if (displayFrac < dangerThreshold)
                displayFillImage!.color = dangerColor;
            else if (displayFrac < warningThreshold)
                displayFillImage!.color = warningColor;
            else
                displayFillImage!.color = normalColor;
        }

    }
}
