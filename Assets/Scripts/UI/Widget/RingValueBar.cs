using UnityEngine;
using UnityEngine.UI;

namespace CraftSharp.UI
{
    public class RingValueBar : BaseValueBar
    {
        [SerializeField] private Color normalColor  = Color.white;
        [SerializeField] private Color warningColor = Color.yellow;
        [SerializeField] private Color dangerColor  = Color.red;

        [SerializeField] [Range(0.1F, 1F)] private float warningThreshold = 0.3F;
        [SerializeField] [Range(0.1F, 1F)] private float dangerThreshold  = 0.1F;

        [SerializeField] [Range(30F, 360F)] private float fullBarDegree = 40F;

        [SerializeField] private Image backgroundImage, fillImage;

        private void Start()
        {
            backgroundImage!.transform.rotation = Quaternion.AngleAxis(fullBarDegree / 2F, Vector3.back);
            fillImage!.transform.rotation = Quaternion.AngleAxis(fullBarDegree / 2F, Vector3.back);

            backgroundImage.fillAmount = fullBarDegree / 360F;

            UpdateValue();
        }

        protected override void UpdateValue()
        {
            // Calculate new display value
            displayValue = Mathf.MoveTowards(displayValue, curValue, maxValue * Time.deltaTime);
            
            // Then update visuals
            float displayFraction = displayValue / maxValue;
            float displayDegree = displayFraction * fullBarDegree / 360F;
            
            fillImage!.fillAmount = displayDegree;

            if (displayFraction < dangerThreshold)
                fillImage!.color = dangerColor;
            else if (displayFraction < warningThreshold)
                fillImage!.color = warningColor;
            else
                fillImage!.color = normalColor;
        }
    }
}
