// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp

using Unity.Mathematics;

namespace MagicaCloth
{
    /// <summary>
    /// ２次ベジェ曲線データ
    /// </summary>
    public struct CurveParam
    {
        public float sval;
        public float eval;
        public float cval;
        public int useCurve;

        public CurveParam(float value)
        {
            // 線形
            useCurve = 0;
            sval = value;
            eval = value;
            cval = 0;
        }

        public CurveParam(float svalue, float evalue)
        {
            // 線形
            useCurve = 0;
            sval = svalue;
            eval = evalue;
            cval = 0;
        }

        public CurveParam(BezierParam bezier)
        {
            useCurve = 0;
            sval = 0;
            eval = 0;
            cval = 0;
            Setup(bezier);
        }

        /// <summary>
        /// ベジェ曲線のデータを格納
        /// </summary>
        /// <param name="bezier"></param>
        public void Setup(BezierParam bezier)
        {
            useCurve = bezier.UseCurve ? 1 : 0;
            sval = bezier.StartValue;
            eval = bezier.EndValue;

            // 制御点を事前計算しておく
            cval = math.lerp(eval, sval, math.saturate(bezier.CurveValue * 0.5f + 0.5f));
        }

        /// <summary>
        /// データを取得する
        /// </summary>
        /// <param name="t">横軸(0.0-1.0)</param>
        /// <returns></returns>
        public float Evaluate(float t)
        {
            t = math.saturate(t);
            if (useCurve == 1)
            {
                // ２次ベジェ曲線
                float w = 1.0f - t;
                return (w * w * sval + 2 * w * t * cval + t * t * eval);
            }
            else
            {
                // 線形
                return math.lerp(sval, eval, t);
            }
        }
    }
}
