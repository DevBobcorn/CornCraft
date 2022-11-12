#nullable enable
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MinecraftClient.UI
{
    public class ValueBar : MonoBehaviour
    {
        [SerializeField] private Color normalColor   = Color.white;
        [SerializeField] private Color reduceColor   = new(1F, 0.521F, 0.596F); // 255 133 152 #FF8598
        [SerializeField] private Color increaseColor = new(0.815F, 1F, 0.372F); // 208 255 095 #D0FF5F

        [SerializeField] private float smoothSpeed   = 1000F;

        [SerializeField] private Color warningColor = Color.yellow;
        [SerializeField] private Color dangerColor  = Color.red;

        [SerializeField] private float warningThreshold = 0.3F;
        [SerializeField] private float dangerThreshold  = 0.1F;

        [SerializeField] private RectTransform.Axis barAxis = RectTransform.Axis.Horizontal;

        private RectTransform? barFillTransform, deltaFillTransform, displayFillTransform;
        private Image? displayFillImage, deltaFillImage;
        private TMP_Text? barText;

        private float fullBarLength;

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

                if (barText is not null)
                    barText.text = $"{(int)displayValue}/{(int)maxValue}";
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
                
                if (barText is not null)
                    barText.text = $"{(int)displayValue}/{(int)maxValue}";
            }
        }

        void Start()
        {
            barFillTransform = transform.Find("Bar Fill Mask").GetComponent<RectTransform>();
            barText = transform.Find("Bar Text").GetComponent<TMP_Text>();

            deltaFillTransform   = barFillTransform.Find("Delta Fill").GetComponent<RectTransform>();
            displayFillTransform = barFillTransform.Find("Current Fill").GetComponent<RectTransform>();

            deltaFillImage   = deltaFillTransform.GetComponent<Image>();
            displayFillImage = displayFillTransform.GetComponent<Image>();

            if (barAxis == RectTransform.Axis.Horizontal)
                fullBarLength = barFillTransform.rect.width;
            else
                fullBarLength = barFillTransform.rect.height;

            deltaFillTransform.SetSizeWithCurrentAnchors(barAxis, 0F);
            displayFillTransform.SetSizeWithCurrentAnchors(barAxis, fullBarLength);

            if (barText is not null)
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
                    float curValuePos = (curValue / maxValue) * fullBarLength;
                    displayFillTransform!.SetSizeWithCurrentAnchors(barAxis, curValuePos);
                    deltaFillImage!.color = reduceColor;
                    deltaFillTransform!.anchoredPosition = new(curValuePos, 0F);
                    deltaFillTransform.SetSizeWithCurrentAnchors(barAxis, (displayValue - curValue) / maxValue * fullBarLength);

                    if (barText is not null)
                        barText.text = $"{(int)displayValue}/{(int)maxValue}";
                }
                else // Increase visual fill
                {
                    // Calculate new display value
                    displayValue = Mathf.Min(displayValue + smoothSpeed * Time.deltaTime, curValue);
                    // Then update visuals
                    float displayValuePos = (displayValue / maxValue) * fullBarLength;
                    displayFillTransform!.SetSizeWithCurrentAnchors(barAxis, displayValuePos);
                    deltaFillImage!.color = increaseColor;
                    deltaFillTransform!.anchoredPosition = new(displayValuePos, 0F);
                    deltaFillTransform.SetSizeWithCurrentAnchors(barAxis, (curValue - displayValue) / maxValue * fullBarLength);

                    if (barText is not null)
                        barText.text = $"{(int)displayValue}/{(int)maxValue}";
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
}
