using UnityEngine;
using TMPro;

namespace CraftSharp.UI
{
    public abstract class BaseValueBar : MonoBehaviour
    {
        protected float maxValue = 100F, curValue = 100F, displayValue = 100F;
        [SerializeField] protected TMP_Text barText;
        [SerializeField] private string textFormat = "{0:0}/{1:0}";

        public float MaxValue
        {
            get => maxValue;

            set { // Preserve old visual fill percentage
                float oldFraction = displayValue / maxValue; // old max value
                maxValue = value;
                displayValue = oldFraction * maxValue; // new max value

                if (barText)
                {
                    barText.text = string.Format(textFormat, displayValue, maxValue);
                }
            }
        }

        public float CurValue
        {
            get => curValue;

            set {
                if (value < 0F)
                    curValue = 0F;
                else if (value > maxValue)
                    curValue = maxValue;
                else
                    curValue = value;
                
                if (barText)
                {
                    barText.text = string.Format(textFormat, displayValue, maxValue);
                }
            }
        }
    
        protected abstract void UpdateValue();

        protected virtual void Update()
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (displayValue != curValue)
            {
                // Update bar value
                UpdateValue();

                // Update bar text
                if (barText)
                {
                    barText.text = string.Format(textFormat, displayValue, maxValue);
                }
            }
        }
    }
}
