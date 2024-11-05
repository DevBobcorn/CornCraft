using UnityEngine;
using UnityEngine.UI;

namespace CraftSharp.UI
{
    public class RawValueBar : BaseValueBar, IAlphaListener
    {
        [SerializeField] private Color normalColor   = Color.white;
        [SerializeField] private Color reduceColor   = new(1F, 0.521F, 0.596F); // 255 133 152 #FF8598
        [SerializeField] private Color increaseColor = new(0.815F, 1F, 0.372F); // 208 255 095 #D0FF5F
        [SerializeField] private Color warningColor  = Color.yellow;
        [SerializeField] private Color dangerColor   = Color.red;

        [SerializeField] [Range(0.1F, 1F)] private float warningThreshold = 0.3F;
        [SerializeField] [Range(0.1F, 1F)] private float dangerThreshold  = 0.1F;

        [SerializeField] private Image valueBarImage;

        private Material valueBarMaterial;

        private CanvasGroup[] parentCanvasGroups = { };
        private float selfAlpha = 1F;

        private float currentFillAmount = 1f;
        private float currentDeltaAmount = 1f;

        void Start()
        {
            valueBarImage = GetComponentInChildren<Image>();
            valueBarMaterial = valueBarImage.material;
            parentCanvasGroups = GetComponentsInParent<CanvasGroup>(true);

            UpdateValue();
        }

        public void UpdateAlpha(float alpha)
        {
            if (valueBarMaterial != null)
            {
                Color fillColor = valueBarMaterial.GetColor("_ValueColor");
                valueBarMaterial.SetColor("_ValueColor", IAlphaListener.GetColorWithAlpha(fillColor, selfAlpha));
                selfAlpha = alpha;
            }
        }

        protected override void UpdateValue()
        {
            if (displayValue > curValue) // Reducing fill
            {
                displayValue = Mathf.Max(displayValue - maxValue * Time.deltaTime, curValue);
                valueBarMaterial.SetColor("_DeltaColor", IAlphaListener.GetColorWithAlpha(reduceColor, selfAlpha));
            }
            else // Increasing fill
            {
                displayValue = Mathf.Min(displayValue + maxValue * Time.deltaTime, curValue);
                valueBarMaterial.SetColor("_DeltaColor", IAlphaListener.GetColorWithAlpha(increaseColor, selfAlpha));
            }

            currentFillAmount = curValue / maxValue;
            valueBarMaterial.SetFloat("_FillAmount", currentFillAmount);

            currentDeltaAmount = displayValue / maxValue;
            valueBarMaterial.SetFloat("_DeltaAmount", currentDeltaAmount);

            float displayFraction = displayValue / maxValue;
            Color currentColor = normalColor;

            if (displayFraction < dangerThreshold)
                currentColor = dangerColor;
            else if (displayFraction < warningThreshold)
                currentColor = warningColor;

            valueBarMaterial.SetColor("_ValueColor", IAlphaListener.GetColorWithAlpha(currentColor, selfAlpha));
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
