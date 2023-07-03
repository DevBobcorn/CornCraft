// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth2
{
    [System.Serializable]
    public class CurveSerializeData
    {
        /// <summary>
        /// Basic value.
        /// </summary>
        public float value;

        /// <summary>
        /// Use of curves.
        /// </summary>
        public bool useCurve;

        /// <summary>
        /// Animation curve.
        /// </summary>
        public AnimationCurve curve = AnimationCurve.Linear(0.0f, 1.0f, 1.0f, 1.0f);

        public CurveSerializeData()
        {
        }

        public CurveSerializeData(float value)
        {
            this.value = value;
            useCurve = false;
            curve = AnimationCurve.Linear(0.0f, 1.0f, 1.0f, 1.0f);
        }

        public CurveSerializeData(float value, float curveStart, float curveEnd, bool useCurve = true)
        {
            this.value = value;
            this.useCurve = useCurve;
            curve = AnimationCurve.Linear(0.0f, Mathf.Clamp01(curveStart), 1.0f, Mathf.Clamp01(curveEnd));
        }

        public CurveSerializeData(float value, AnimationCurve curve)
        {
            this.value = value;
            useCurve = true;
            this.curve = curve;
        }

        public void SetValue(float value)
        {
            this.value = value;
            useCurve = false;
        }

        public void SetValue(float value, float curveStart, float curveEnd, bool useCurve = true)
        {
            this.value = value;
            this.useCurve = useCurve;
            curve = AnimationCurve.Linear(0.0f, Mathf.Clamp01(curveStart), 1.0f, Mathf.Clamp01(curveEnd));
        }

        public void SetValue(float value, AnimationCurve curve)
        {
            this.value = value;
            useCurve = true;
            this.curve = curve;
        }

        public void DataValidate(float min, float max)
        {
            value = Mathf.Clamp(value, min, max);
        }

        /// <summary>
        /// Get the current value of Time(0.0 ~ 1.0).
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public float Evaluate(float time)
        {
            if (useCurve)
                return curve.Evaluate(time) * value;
            else
                return value;
        }

        /// <summary>
        /// カーブ情報をジョブで利用するための16個のfloat配列(float4x4)に変換して返す
        /// Convert the curve information into a 16 float array (float4x4) for use in the job and return it.
        /// </summary>
        /// <returns></returns>
        public float4x4 ConvertFloatArray()
        {
            if (useCurve)
            {
                return DataUtility.ConvertAnimationCurve(curve) * value;
            }
            else
            {
                return value;
            }
        }

        public CurveSerializeData Clone()
        {
            var cdata = new CurveSerializeData()
            {
                value = value,
                useCurve = useCurve,
            };
            var c = new AnimationCurve(curve.keys);
            c.preWrapMode = curve.preWrapMode;
            c.postWrapMode = curve.postWrapMode;
            cdata.curve = c;

            return cdata;
        }
    }
}
