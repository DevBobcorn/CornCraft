using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MinecraftClient.UI
{
    public class ValueBar : MonoBehaviour
    {
        private const RectTransform.Axis HOR = RectTransform.Axis.Horizontal;

        private RectTransform barFillTransform, deltaFillTransform, displayFillTransform;
        private Image displayFillImage, deltaFillImage;
        private TMP_Text barText;

        private float fullBarWidth;

        private float maxValue = 100F, curValue = 100F, displayValue = 100F;

        public float MaxValue 
        {
            get {
                return maxValue;
            }

            set { // Preserve old visual fill percentage
                float oldFract = displayValue / maxValue; // old max value
                maxValue = value;
                displayValue = oldFract * maxValue; // new max value
            }
        }

        public float CurValue
        {
            get {
                return curValue;
            }

            set {
                if (value < 0F)
                    curValue = 0F;
                else if (value > maxValue)
                    curValue = maxValue;
                else
                    curValue = value;
            }
        }

        public Color normalColor   = Color.white;
        public Color reduceColor   = new(1F, 0.521F, 0.596F); // 255 133 152 #FF8598
        public Color increaseColor = new(0.815F, 1F, 0.372F); // 208 255 095 #D0FF5F

        public float smoothSpeed   = 1000F;

        public Color warningColor = Color.yellow;
        public Color dangerColor  = Color.red;

        public float warningThreshold = 0.3F;
        public float dangerThreshold  = 0.1F;

        void Start()
        {
            barFillTransform = transform.Find("Bar Fill Mask").GetComponent<RectTransform>();
            barText = transform.Find("Bar Text").GetComponent<TMP_Text>();

            deltaFillTransform   = barFillTransform.Find("Delta Fill").GetComponent<RectTransform>();
            displayFillTransform = barFillTransform.Find("Current Fill").GetComponent<RectTransform>();

            deltaFillImage   = deltaFillTransform.GetComponent<Image>();
            displayFillImage = displayFillTransform.GetComponent<Image>();

            fullBarWidth   = barFillTransform.rect.width;

            deltaFillTransform.SetSizeWithCurrentAnchors(HOR, 0F);
            displayFillTransform.SetSizeWithCurrentAnchors(HOR, fullBarWidth);

            barText.text = $"{(int)displayValue}/{(int)maxValue}";
            
            float displayFrac = displayValue / maxValue;

            if (displayFrac < dangerThreshold)
                displayFillImage.color = dangerColor;
            else if (displayFrac < warningThreshold)
                displayFillImage.color = warningColor;
            else
                displayFillImage.color = normalColor;

        }

        void Update()
        {
            if (displayValue != curValue)
            {
                if (displayValue > curValue) // Reduce visual fill
                {
                    // Calculate new display value
                    displayValue = Mathf.Max(displayValue - smoothSpeed * Time.deltaTime, curValue);
                    // Then update visuals
                    float curValuePos = (curValue / maxValue) * fullBarWidth;
                    displayFillTransform.SetSizeWithCurrentAnchors(HOR, curValuePos);
                    deltaFillImage.color = reduceColor;
                    deltaFillTransform.anchoredPosition = new(curValuePos, 0F);
                    deltaFillTransform.SetSizeWithCurrentAnchors(HOR, (displayValue - curValue) / maxValue * fullBarWidth);
                    barText.text = $"{(int)displayValue}/{(int)maxValue}";
                }
                else // Increase visual fill
                {
                    // Calculate new display value
                    displayValue = Mathf.Min(displayValue + smoothSpeed * Time.deltaTime, curValue);
                    // Then update visuals
                    float displayValuePos = (displayValue / maxValue) * fullBarWidth;
                    displayFillTransform.SetSizeWithCurrentAnchors(HOR, displayValuePos);
                    deltaFillImage.color = increaseColor;
                    deltaFillTransform.anchoredPosition = new(displayValuePos, 0F);
                    deltaFillTransform.SetSizeWithCurrentAnchors(HOR, (curValue - displayValue) / maxValue * fullBarWidth);
                    barText.text = $"{(int)displayValue}/{(int)maxValue}";
                }

                float displayFrac = displayValue / maxValue;

                if (displayFrac < dangerThreshold)
                    displayFillImage.color = dangerColor;
                else if (displayFrac < warningThreshold)
                    displayFillImage.color = warningColor;
                else
                    displayFillImage.color = normalColor;

            }
            
        }
    }
}
