using UnityEngine;

namespace CraftSharp.UI
{
    public class FloatingValueBar : BaseValueBar
    {
        [SerializeField] private Color normalColor   = Color.white;
        [SerializeField] private Color reduceColor   = new(1F, 0.521F, 0.596F); // 255 133 152 #FF8598
        [SerializeField] private Color increaseColor = new(0.815F, 1F, 0.372F); // 208 255 095 #D0FF5F
        [SerializeField] private Color warningColor  = Color.yellow;
        [SerializeField] private Color dangerColor   = Color.red;

        [SerializeField] [Range(0.1F, 1F)] private float warningThreshold = 0.3F;
        [SerializeField] [Range(0.1F, 1F)] private float dangerThreshold  = 0.1F;

        [SerializeField] private SpriteRenderer displayFillRenderer, deltaFillRenderer;
        private Transform displayTransform, deltaTransform;

        private void Start()
        {
            displayTransform = displayFillRenderer!.transform;
            deltaTransform = deltaFillRenderer!.transform;

            UpdateValue();
        }

        protected override void UpdateValue()
        {
            if (displayValue > curValue) // Reduce visual fill
            {
                // Calculate new display value
                displayValue = Mathf.Max(displayValue - maxValue * Time.deltaTime, curValue);
                // Then update visuals
                var deltaValue = displayValue - curValue;

                displayTransform!.localScale = new(curValue / maxValue, 1F, 1F);
                displayTransform!.localPosition = new(curValue / 2 / maxValue - 0.5F, 0F, 0F);

                deltaTransform!.localScale = new(deltaValue / maxValue, 1F, 1F);
                deltaTransform!.localPosition = new((curValue + deltaValue / 2F) / maxValue - 0.5F, 0F, 0F);

                deltaFillRenderer!.color = reduceColor;
            }
            else // Increase visual fill
            {
                // Calculate new display value
                displayValue = Mathf.Min(displayValue + maxValue * Time.deltaTime, curValue);
                // Then update visuals
                var deltaValue = curValue - displayValue;

                displayTransform!.localScale = new(displayValue / maxValue, 1F, 1F);
                displayTransform!.localPosition = new(displayValue / 2F / maxValue - 0.5F, 0F, 0F);

                deltaTransform!.localScale = new(deltaValue / maxValue, 1F, 1F);
                deltaTransform!.localPosition = new((displayValue + deltaValue / 2F) / maxValue - 0.5F, 0F, 0F);
                
                deltaFillRenderer!.color = increaseColor;
            }

            float displayFrac = displayValue / maxValue;

            if (displayFrac < dangerThreshold)
                displayFillRenderer!.color = dangerColor;
            else if (displayFrac < warningThreshold)
                displayFillRenderer!.color = warningColor;
            else
                displayFillRenderer!.color = normalColor;
        }
    }
}
