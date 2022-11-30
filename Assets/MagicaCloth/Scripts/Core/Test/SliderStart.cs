// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using UnityEngine;
using UnityEngine.UI;

namespace MagicaCloth
{
    public class SliderStart : MonoBehaviour
    {
        [SerializeField]
        private Text text = null;

        [SerializeField]
        private string lable = "";

        [SerializeField]
        private string format = "0.00";

        private string formatString;

        void Start()
        {
            formatString = "{0} ({1:" + format + "})";

            var slider = GetComponent<Slider>();
            if (slider)
            {
                slider.onValueChanged.AddListener(OnChangeValue);

                var val = slider.value;
                slider.value = 0.001f;
                slider.value = val;
            }

        }

        private void OnChangeValue(float value)
        {
            if (text)
            {
                //text.text = string.Format("{0} ({1:0.00})", lable, value);
                text.text = string.Format(formatString, lable, value);
            }
        }
    }
}
