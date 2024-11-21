using UnityEngine;
using UnityEngine.UI;

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

        private static readonly int ValueColor = Shader.PropertyToID("_ValueColor");
        private static readonly int DeltaColor = Shader.PropertyToID("_DeltaColor");
        private static readonly int FillAmount = Shader.PropertyToID("_FillAmount");
        private static readonly int DeltaAmount = Shader.PropertyToID("_DeltaAmount");

        [SerializeField] private Image barImage;
        private Material barMaterial;

        private CanvasGroup[] parentCanvasGroups = { };
        private float selfAlpha = 1F;

        void Start()
        {
            if (barImage == null)
            {
                barImage = GetComponentInChildren<Image>();
            }
            
            // Create a material instance for each bar
            barMaterial = new Material(barImage.material);
            barImage.material = barMaterial;

            var barImageRect = barImage.GetComponent<RectTransform>();

            barMaterial.SetVector("_BarSize", new(barImageRect.rect.width, barImageRect.rect.height, 0F, 0F));

            parentCanvasGroups = GetComponentsInParent<CanvasGroup>(true);

            UpdateValue();
        }

        public void UpdateAlpha(float alpha)
        {
            Color fillColor = barMaterial.GetColor(ValueColor);
            barMaterial.SetColor(ValueColor, IAlphaListener.GetColorWithAlpha(fillColor, selfAlpha));

            if (barText != null)
            {
                barText.color = IAlphaListener.GetColorWithAlpha(barText.color, alpha);
            }

            selfAlpha = alpha;
        }

        protected override void UpdateValue()
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
}