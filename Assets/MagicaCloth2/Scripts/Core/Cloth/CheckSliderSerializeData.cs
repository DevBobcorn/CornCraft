// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using UnityEngine;

namespace MagicaCloth2
{
    [System.Serializable]
    public class CheckSliderSerializeData
    {
        /// <summary>
        /// slider value.
        /// </summary>
        public float value;

        /// <summary>
        /// Use
        /// </summary>
        public bool use;

        public CheckSliderSerializeData()
        {
        }

        public CheckSliderSerializeData(bool use, float value)
        {
            this.use = use;
            this.value = value;
        }

        public float GetValue(float unusedValue)
        {
            return use ? value : unusedValue;
        }

        public void SetValue(bool use, float value)
        {
            this.use = use;
            this.value = value;
        }

        public void DataValidate(float min, float max)
        {
            value = Mathf.Clamp(value, min, max);
        }

        public CheckSliderSerializeData Clone()
        {
            var cdata = new CheckSliderSerializeData()
            {
                value = value,
                use = use,
            };
            return cdata;
        }
    }
}
