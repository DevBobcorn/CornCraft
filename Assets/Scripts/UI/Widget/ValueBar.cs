using UnityEngine;
using UnityEngine.UI;

namespace CraftSharp.UI
{
    public class ValueBar : BaseValueBar, IAlphaListener
    {
        private static readonly int BORDER_COLOR = Shader.PropertyToID("_BorderColor");
        private static readonly int VALUE_COLOR  = Shader.PropertyToID("_ValueColor");
        private static readonly int DELTA_COLOR  = Shader.PropertyToID("_DeltaColor");
        private static readonly int FILL_AMOUNT  = Shader.PropertyToID("_FillAmount");
        private static readonly int DELTA_AMOUNT = Shader.PropertyToID("_DeltaAmount");
        private static readonly int BAR_SIZE     = Shader.PropertyToID("_BarSize");
        private static readonly int CORNER_RADII = Shader.PropertyToID("_CornerRadii");
        
        [SerializeField] private Color normalColor   = Color.white;
        [SerializeField] private Color reduceColor   = new(1F, 0.521F, 0.596F); // 255 133 152 #FF8598
        [SerializeField] private Color increaseColor = new(0.815F, 1F, 0.372F); // 208 255 095 #D0FF5F
        [SerializeField] private Color warningColor  = Color.yellow;
        [SerializeField] private Color dangerColor   = Color.red;

        [SerializeField] [Range(0.1F, 1F)] private float warningThreshold = 0.3F;
        [SerializeField] [Range(0.1F, 1F)] private float dangerThreshold  = 0.1F;

        [SerializeField] private Vector4 cornerRadius = Vector4.zero;

        [SerializeField] private Image barImage;
        private Material barMaterial;

        private CanvasGroup[] parentCanvasGroups = { };
        private float selfAlpha = 1F;

        private void Start()
        {
            if (barImage == null)
            {
                barImage = GetComponentInChildren<Image>();
            }
            
            // Create a material instance for each bar
            barMaterial = new Material(barImage.material);
            barImage.material = barMaterial;

            var barImageRect = barImage.GetComponent<RectTransform>();

            barMaterial.SetVector(BAR_SIZE, new(
                barImageRect.rect.width, barImageRect.rect.height, 0F, 0F));
            barMaterial.SetVector(CORNER_RADII, cornerRadius);

            parentCanvasGroups = GetComponentsInParent<CanvasGroup>(true);

            UpdateValue();
        }

        public void UpdateCurValueWithoutAnimation(float newValue)
        {
            displayValue = newValue;
            CurValue = newValue;

            UpdateValue();
        }

        public void UpdateAlpha(float alpha)
        {
            Color valueColor = barMaterial.GetColor(VALUE_COLOR);
            barMaterial.SetColor(VALUE_COLOR, IAlphaListener.GetColorWithAlpha(valueColor, selfAlpha));

            Color borderColor = barMaterial.GetColor(BORDER_COLOR);
            barMaterial.SetColor(BORDER_COLOR, IAlphaListener.GetColorWithAlpha(borderColor, selfAlpha));

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
                barMaterial.SetColor(DELTA_COLOR, IAlphaListener.GetColorWithAlpha(reduceColor, selfAlpha));
            }
            else // Increasing fill
            {
                displayValue = Mathf.Min(displayValue + maxValue * Time.deltaTime, curValue);
                barMaterial.SetColor(DELTA_COLOR, IAlphaListener.GetColorWithAlpha(increaseColor, selfAlpha));
            }

            var currentFillAmount = curValue / maxValue;
            barMaterial.SetFloat(FILL_AMOUNT, currentFillAmount);

            var currentDeltaAmount = displayValue / maxValue;
            barMaterial.SetFloat(DELTA_AMOUNT, currentDeltaAmount);

            float displayFraction = displayValue / maxValue;
            Color currentColor = normalColor;

            if (displayFraction < dangerThreshold)
                currentColor = dangerColor;
            else if (displayFraction < warningThreshold)
                currentColor = warningColor;

            barMaterial.SetColor(VALUE_COLOR, IAlphaListener.GetColorWithAlpha(currentColor, selfAlpha));

            Color borderColor = barMaterial.GetColor(BORDER_COLOR);

            barMaterial.SetColor(BORDER_COLOR, IAlphaListener.GetColorWithAlpha(borderColor, selfAlpha));
        }

        protected override void Update()
        {
            base.Update();

            if (parentCanvasGroups.Length > 0)
            {
                float updatedAlpha = 1F;

                foreach (var t in parentCanvasGroups)
                {
                    if (!t.gameObject.activeSelf || t.alpha == 0F)
                    {
                        updatedAlpha = 0F;
                        break;
                    }

                    updatedAlpha *= t.alpha;
                }

                if (!Mathf.Approximately(selfAlpha, updatedAlpha))
                {
                    UpdateAlpha(updatedAlpha);
                }
            }
        }
    }
}