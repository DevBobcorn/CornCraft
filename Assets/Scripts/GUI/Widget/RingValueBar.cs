#nullable enable
using UnityEngine;
using UnityEngine.UI;

namespace MinecraftClient.UI
{
    public class RingValueBar : MonoBehaviour
    {
        [SerializeField] private Color normalColor   = Color.white;
        [SerializeField] private float smoothSpeed   = 1000F;

        [SerializeField] private Color warningColor = Color.yellow;
        [SerializeField] private Color dangerColor  = Color.red;

        [SerializeField] private float warningThreshold = 0.3F;
        [SerializeField] private float dangerThreshold  = 0.1F;

        [SerializeField] private float fullBarDegree = 40F;

        private Image? backgroundImage, fillImage;

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

        void Start()
        {
            backgroundImage = FindHelper.FindChildRecursively(transform, "Bar Background").GetComponent<Image>();
            fillImage = FindHelper.FindChildRecursively(transform, "Bar Fill").GetComponent<Image>();

            backgroundImage.transform.rotation = Quaternion.AngleAxis(fullBarDegree / 2F, Vector3.back);
            fillImage.transform.rotation = Quaternion.AngleAxis(fullBarDegree / 2F, Vector3.back);

            float displayFrac = displayValue / maxValue;

            backgroundImage.fillAmount = fullBarDegree / 360F;
            fillImage.fillAmount = displayFrac * fullBarDegree / 360F;

            if (displayFrac < dangerThreshold)
                fillImage.color = dangerColor;
            else if (displayFrac < warningThreshold)
                fillImage.color = warningColor;
            else
                fillImage.color = normalColor;

        }

        void Update()
        {
            if (displayValue != curValue)
            {
                // Calculate new display value
                displayValue = Mathf.MoveTowards(displayValue, curValue, smoothSpeed * Time.deltaTime);
                
                // Then update visuals
                float displayFrac = displayValue / maxValue;
                float displayDegree = displayFrac * fullBarDegree / 360F;
                
                fillImage!.fillAmount = displayDegree;

                if (displayFrac < dangerThreshold)
                    fillImage!.color = dangerColor;
                else if (displayFrac < warningThreshold)
                    fillImage!.color = warningColor;
                else
                    fillImage!.color = normalColor;

            }
            
        }
    }
}
