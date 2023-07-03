// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace MagicaCloth2
{
    public static class MathExtensions
    {
        /// <summary>
        /// float4x4を16の配列として扱うための拡張
        /// </summary>
        /// <param name="m"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetValue(in this float4x4 m, int index)
        {
            index = math.clamp(index, 0, 15);
            return m[index / 4][index % 4];
        }

        /// <summary>
        /// float4x4を16の配列として扱うための拡張
        /// </summary>
        /// <param name="m"></param>
        /// <param name="index"></param>
        /// <param name="value"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetValue(ref this float4x4 m, int index, float value)
        {
            index = math.clamp(index, 0, 15);
            m[index / 4][index % 4] = value;
        }

        /// <summary>
        /// AnimationCurveが格納されたfloat4x4からデータを取得する(0.0 ~ 1.0でクランプ)
        /// </summary>
        /// <param name="m"></param>
        /// <param name="time">0.0 ~ 1.0</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float EvaluateCurveClamp01(in this float4x4 m, float time)
        {
            return math.saturate(DataUtility.EvaluateCurve(m, time));
        }

        /// <summary>
        /// AnimationCurveが格納されたfloat4x4からデータを取得する
        /// </summary>
        /// <param name="m"></param>
        /// <param name="time">0.0 ~ 1.0</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float EvaluateCurve(in this float4x4 m, float time)
        {
            return DataUtility.EvaluateCurve(m, time);
        }
    }
}
