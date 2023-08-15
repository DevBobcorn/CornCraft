#nullable enable
using UnityEngine;
using TMPro;

namespace CraftSharp.UI
{
    public abstract class BaseValueBar : MonoBehaviour
    {
        protected float maxValue = 100F, curValue = 100F, displayValue = 100F;
        [SerializeField] private TMP_Text? barText;
        [SerializeField] private string textFormat = "{0:0}/{1:0}";

        public virtual float MaxValue
        {
            get => maxValue;

            set { // Preserve old visual fill percentage
                float oldFract = displayValue / maxValue; // old max value
                maxValue = value;
                displayValue = oldFract * maxValue; // new max value

                if (barText is not null)
                    barText.text = string.Format(textFormat, displayValue, maxValue);
            }
        }

        public virtual float CurValue
        {
            get => curValue;

            set {
                if (value < 0F)
                    curValue = 0F;
                else if (value > maxValue)
                    curValue = maxValue;
                else
                    curValue = value;
                
                if (barText is not null)
                    barText.text = string.Format(textFormat, displayValue, maxValue);
            }
        }
    
        protected abstract void UpdateValue();

        protected void Update()
        {
            if (displayValue != curValue)
            {
                // Update bar value
                UpdateValue();

                // Update bar text
                
                if (barText is not null)
                    barText.text = $"{(int)displayValue}/{(int)maxValue}";
            }
        }
    }
}
