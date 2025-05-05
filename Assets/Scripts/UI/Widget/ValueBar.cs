using UnityEngine;
using UnityEngine.UI;

namespace CraftSharp.UI
{
    public class ValueBar : BaseValueBar
    {
        private static readonly int BORDER_COLOR = Shader.PropertyToID("_BorderColor");
        private static readonly int VALUE_COLOR  = Shader.PropertyToID("_ValueColor");
        private static readonly int DELTA_COLOR  = Shader.PropertyToID("_DeltaColor");
        private static readonly int FILL_AMOUNT  = Shader.PropertyToID("_FillAmount");
        private static readonly int DELTA_AMOUNT = Shader.PropertyToID("_DeltaAmount");
        
        [SerializeField] private Color normalColor   = Color.white;
        [SerializeField] private Color reduceColor   = new(1F, 0.521F, 0.596F); // 255 133 152 #FF8598
        [SerializeField] private Color increaseColor = new(0.815F, 1F, 0.372F); // 208 255 095 #D0FF5F
        [SerializeField] private Color warningColor  = Color.yellow;
        [SerializeField] private Color dangerColor   = Color.red;

        [SerializeField] [Range(0.1F, 1F)] private float warningThreshold = 0.3F;
        [SerializeField] [Range(0.1F, 1F)] private float dangerThreshold  = 0.1F;

        [SerializeField] private Image barImage;
        private Material barMaterial;

        private void Start()
        {
            // Create a material instance for each bar
            barMaterial = new Material(barImage.material);
            barImage.material = barMaterial;

            UpdateValue();
        }

        protected override void UpdateValue()
        {
            if (displayValue > curValue) // Reducing fill
            {
                displayValue = Mathf.Max(displayValue - maxValue * Time.deltaTime, curValue);
                barMaterial.SetColor(DELTA_COLOR, reduceColor);
            }
            else // Increasing fill
            {
                displayValue = Mathf.Min(displayValue + maxValue * Time.deltaTime, curValue);
                barMaterial.SetColor(DELTA_COLOR, increaseColor);
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

            barMaterial.SetColor(VALUE_COLOR, currentColor);
            barMaterial.SetColor(BORDER_COLOR, barMaterial.GetColor(BORDER_COLOR));
        }
    }
}