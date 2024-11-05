using UnityEngine;
using UnityEngine.UI;

namespace CraftSharp.UI
{
    public partial class ValueBar : BaseValueBar, IAlphaListener
    {
        [SerializeField] private Color normalColor   = Color.white;
        [SerializeField] private Color reduceColor   = new(1F, 0.521F, 0.596F); // 255 133 152 #FF8598
        [SerializeField] private Color increaseColor = new(0.815F, 1F, 0.372F); // 208 255 095 #D0FF5F
        [SerializeField] private Color warningColor  = Color.yellow;
        [SerializeField] private Color dangerColor   = Color.red;

        [SerializeField] [Range(0.1F, 1F)] private float warningThreshold = 0.3F;
        [SerializeField] [Range(0.1F, 1F)] private float dangerThreshold  = 0.1F;

        [SerializeField] private bool horizontalBar = true;

        [SerializeField] private float fullBarLength;

        private CanvasGroup[] parentCanvasGroups = { };
        private float selfAlpha = 1F;

        partial void StartImplementation();
        partial void UpdateAlphaImplementation(float alpha);
        partial void UpdateValueImplementation();

        void Start()
        {
            StartImplementation();

            parentCanvasGroups = GetComponentsInParent<CanvasGroup>(true);

            UpdateValue();
        }

        public void UpdateAlpha(float alpha)
        {
            UpdateAlphaImplementation(alpha);

            if (barText != null)
            {
                barText.color = IAlphaListener.GetColorWithAlpha(barText.color, alpha);
            }

            selfAlpha = alpha;
        }

        protected override void UpdateValue()
        {
            UpdateValueImplementation();
        }

        protected override void Update()
        {
            base.Update();

            if (parentCanvasGroups.Length > 0)
            {
                float updatedAlpha = 1F;

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

#if SHAPES_URP || SHAPES_HDRP
    public partial class ValueBar
    {
        private Shapes.Rectangle selfRect;
        [SerializeField] private Shapes.Rectangle deltaFillRect, displayFillRect;

        partial void StartImplementation()
        {
            selfRect = GetComponent<Shapes.Rectangle>();
        }

        partial void UpdateAlphaImplementation(float alpha)
        {
            selfRect.Color = IAlphaListener.GetColorWithAlpha(selfRect.Color, alpha);
            deltaFillRect.Color = IAlphaListener.GetColorWithAlpha(deltaFillRect.Color, alpha);
            displayFillRect.Color = IAlphaListener.GetColorWithAlpha(displayFillRect.Color, alpha);
        }

        partial void UpdateValueImplementation()
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
    }
#else
    public partial class ValueBar
    {
        private static readonly int ValueColor = Shader.PropertyToID("_ValueColor");
        private static readonly int DeltaColor = Shader.PropertyToID("_DeltaColor");
        private static readonly int FillAmount = Shader.PropertyToID("_FillAmount");
        private static readonly int DeltaAmount = Shader.PropertyToID("_DeltaAmount");

        [SerializeField] private Image barImage;
        private Material barMaterial;

        partial void StartImplementation()
        {
            barImage = GetComponentInChildren<Image>();
            barMaterial = barImage.material;
        }

        partial void UpdateAlphaImplementation(float alpha)
        {
            Color fillColor = barMaterial.GetColor(ValueColor);
            barMaterial.SetColor(ValueColor, IAlphaListener.GetColorWithAlpha(fillColor, selfAlpha));
        }

        partial void UpdateValueImplementation()
        {
            if (displayValue > curValue) // Reducing fill
            {
                displayValue = Mathf.Max(displayValue - maxValue * Time.deltaTime, curValue);
                barMaterial.SetColor(DeltaColor, IAlphaListener.GetColorWithAlpha(reduceColor, selfAlpha));
            }
            else // Increasing fill
            {
                displayValue = Mathf.Min(displayValue + maxValue * Time.deltaTime, curValue);
                barMaterial.SetColor(DeltaColor, IAlphaListener.GetColorWithAlpha(increaseColor, selfAlpha));
            }

            var currentFillAmount = curValue / maxValue;
            barMaterial.SetFloat(FillAmount, currentFillAmount);

            var currentDeltaAmount = displayValue / maxValue;
            barMaterial.SetFloat(DeltaAmount, currentDeltaAmount);

            float displayFraction = displayValue / maxValue;
            Color currentColor = normalColor;

            if (displayFraction < dangerThreshold)
                currentColor = dangerColor;
            else if (displayFraction < warningThreshold)
                currentColor = warningColor;

            barMaterial.SetColor(ValueColor, IAlphaListener.GetColorWithAlpha(currentColor, selfAlpha));
        }
    }
#endif
}