using UnityEngine;

namespace CraftSharp.UI
{
    public class ValueBar : BaseValueBar, IAlphaListener
    {
        [SerializeField] private Color normalColor   = Color.white;
        [SerializeField] private Color reduceColor   = new(1F, 0.521F, 0.596F); // 255 133 152 #FF8598
        [SerializeField] private Color increaseColor = new(0.815F, 1F, 0.372F); // 208 255 095 #D0FF5F
        [SerializeField] private Color warningColor  = Color.yellow;
        [SerializeField] private Color dangerColor   = Color.red;

        [SerializeField] [Range(0.1F, 1F)] private float warningThreshold = 0.3F;
        [SerializeField] [Range(0.1F, 1F)] private float dangerThreshold  = 0.1F;

        [SerializeField] private bool horizontalBar = true;

        private Shapes.Rectangle selfRect;
        [SerializeField] private Shapes.Rectangle deltaFillRect, displayFillRect;

        [SerializeField] private float fullBarLength;
        [SerializeField] private float fullAlpha = 0.75F;

        private CanvasGroup[] parentCanvasGroups = { };
        private float selfAlpha = 1F;

        void Start()
        {
            selfRect = GetComponent<Shapes.Rectangle>();
            parentCanvasGroups = GetComponentsInParent<CanvasGroup>(true);

            UpdateValue();
        }

        public void UpdateAlpha(float alpha)
        {
            selfRect.Color = IAlphaListener.GetColorWithAlpha(selfRect.Color, alpha);
            deltaFillRect.Color = IAlphaListener.GetColorWithAlpha(deltaFillRect.Color, alpha);
            displayFillRect.Color = IAlphaListener.GetColorWithAlpha(displayFillRect.Color, alpha);

            if (barText != null)
            {
                barText.color = IAlphaListener.GetColorWithAlpha(barText.color, alpha);
            }

            selfAlpha = alpha;
        }

        protected override void UpdateValue()
        {
            if (displayValue > curValue) // Reduce visual fill
            {
                // Calculate new display value
                displayValue = Mathf.Max(displayValue - maxValue * Time.deltaTime, curValue);

                // Then update visuals
                if (horizontalBar)
                    displayFillRect.Width = curValue / maxValue * fullBarLength;
                else
                    displayFillRect.Height = curValue / maxValue * fullBarLength;
                
                deltaFillRect.Color = IAlphaListener.GetColorWithAlpha(reduceColor, selfAlpha);

                if (horizontalBar)
                    deltaFillRect.Width = displayValue / maxValue * fullBarLength;
                else
                    deltaFillRect.Height = displayValue / maxValue * fullBarLength;
            }
            else // Increase visual fill
            {
                // Calculate new display value
                displayValue = Mathf.Min(displayValue + maxValue * Time.deltaTime, curValue);

                // Then update visuals
                if (horizontalBar)
                    displayFillRect.Width = displayValue / maxValue * fullBarLength;
                else
                    displayFillRect.Height = displayValue / maxValue * fullBarLength;
                
                deltaFillRect.Color = IAlphaListener.GetColorWithAlpha(increaseColor, selfAlpha);

                if (horizontalBar)
                    deltaFillRect.Width = curValue / maxValue * fullBarLength;
                else
                    deltaFillRect.Height = curValue / maxValue * fullBarLength;
            }

            float displayFrac = displayValue / maxValue;

            if (displayFrac < dangerThreshold)
                displayFillRect.Color = IAlphaListener.GetColorWithAlpha(dangerColor, selfAlpha);
            else if (displayFrac < warningThreshold)
                displayFillRect.Color = IAlphaListener.GetColorWithAlpha(warningColor, selfAlpha);
            else
                displayFillRect.Color = IAlphaListener.GetColorWithAlpha(normalColor, selfAlpha);
        }

        protected override void Update()
        {
            base.Update();

            if (parentCanvasGroups.Length > 0)
            {
                float updatedAlpha = fullAlpha;

                for (int i = 0; i < parentCanvasGroups.Length; i++)
                {
                    if ((!parentCanvasGroups[i].gameObject.activeSelf) || parentCanvasGroups[i].alpha == 0F)
                    {
                        updatedAlpha = 0F;
                        break;
                    }

                    updatedAlpha *= parentCanvasGroups[i].alpha;
                }

                if (selfAlpha != updatedAlpha)
                {
                    UpdateAlpha(updatedAlpha);
                }
            }
        }
    }
}
