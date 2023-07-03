// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth2
{
    public class DataUtility
    {
        /// <summary>
        /// ２つのintをint2にパックする
        /// データは昇順にソートされる
        /// </summary>
        /// <param name="d0"></param>
        /// <param name="d1"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int2 PackInt2(int d0, int d1)
        {
            return d0 < d1 ? new int2(d0, d1) : new int2(d1, d0);
        }

        /// <summary>
        /// int2をデータの昇順にソートして格納し直す
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int2 PackInt2(in int2 d) => PackInt2(d.x, d.y);

        /// <summary>
        /// ３つのintをint3にパックする
        /// データは昇順にソートされる
        /// </summary>
        /// <param name="d0"></param>
        /// <param name="d1"></param>
        /// <param name="d2"></param>
        /// <returns></returns>
        public static int3 PackInt3(int d0, int d1, int d2)
        {
            if (d0 < d1 && d0 < d2)
            {
                // d0,x,x
                if (d1 < d2)
                    return new int3(d0, d1, d2);
                else
                    return new int3(d0, d2, d1);
            }
            if (d1 < d2)
            {
                // d1,x,x
                if (d0 < d2)
                    return new int3(d1, d0, d2);
                else
                    return new int3(d1, d2, d0);
            }
            else
            {
                // d2,x,x
                if (d0 < d1)
                    return new int3(d2, d0, d1);
                else
                    return new int3(d2, d1, d0);
            }
        }

        public static int3 PackInt3(in int3 d) => PackInt3(d.x, d.y, d.z);

        /// <summary>
        /// ４つのintをint4にパックする
        /// データは昇順にソートされる
        /// </summary>
        /// <param name="d0"></param>
        /// <param name="d1"></param>
        /// <param name="d2"></param>
        /// <param name="d3"></param>
        /// <returns></returns>
        public static int4 PackInt4(int d0, int d1, int d2, int d3)
        {
            int w;

            // step1
            if (d0 > d3)
            {
                w = d0;
                d0 = d3;
                d3 = w;
            }
            if (d1 > d2)
            {
                w = d1;
                d1 = d2;
                d2 = w;
            }

            // step2
            if (d0 > d1)
            {
                w = d0;
                d0 = d1;
                d1 = w;
            }
            if (d2 > d3)
            {
                w = d2;
                d2 = d3;
                d3 = w;
            }

            // step3
            if (d1 > d2)
            {
                w = d1;
                d1 = d2;
                d2 = w;
            }

            return new int4(d0, d1, d2, d3);
        }

        public static int4 PackInt4(int4 d) => PackInt4(d.x, d.y, d.z, d.w);


        /// <summary>
        /// ２つのintをushortに変換し１つのuintにパッキングする
        /// </summary>
        /// <param name="hi"></param>
        /// <param name="low"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Pack32(int hi, int low)
        {
            return (uint)hi << 16 | (uint)low & 0xffff;
        }

        /// <summary>
        /// ２つのintをushortに変換し１つのuintにパッキングする
        /// データの小さいほうが上位に格納されるようにソートされる
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Pack32Sort(int a, int b)
        {
            if (a > b)
            {
                return (uint)b << 16 | (uint)a & 0xffff;
            }
            else
            {
                return (uint)a << 16 | (uint)b & 0xffff;
            }
        }

        /// <summary>
        /// uintパックデータから上位16bitをintにして返す
        /// </summary>
        /// <param name="pack"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Unpack32Hi(uint pack)
        {
            return (int)((pack >> 16) & 0xffff);
        }

        /// <summary>
        /// uintパックデータから下位16bitをintにして返す
        /// </summary>
        /// <param name="pack"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Unpack32Low(uint pack)
        {
            return (int)(pack & 0xffff);
        }

#if false
        /// <summary>
        /// ２つのintをhi(10bit)とlow(22bit)に切り詰めて１つのuintにパッキングする
        /// </summary>
        /// <param name="hi"></param>
        /// <param name="low"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Pack10_22(int hi, int low)
        {
            return (uint)hi << 22 | (uint)low & 0x3fffff;
        }

        /// <summary>
        /// uint10-22パックデータから上位10bitデータをintにして返す
        /// </summary>
        /// <param name="pack"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Unpack10_22Hi(uint pack)
        {
            return (int)((pack >> 22) & 0x3ff);
        }

        /// <summary>
        /// uint10-22パックデータから下位22bitデータをintにして返す
        /// </summary>
        /// <param name="pack"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Unpack10_22Low(uint pack)
        {
            return (int)(pack & 0x3fffff);
        }

        /// <summary>
        /// uint10-22パックデータを分解して２つのintとして返す
        /// </summary>
        /// <param name="pack"></param>
        /// <param name="hi"></param>
        /// <param name="low"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Unpack10_22(uint pack, out int hi, out int low)
        {
            hi = (int)((pack >> 22) & 0x3ff);
            low = (int)(pack & 0x3fffff);
        }
#endif

        /// <summary>
        /// ２つのintをhi(12bit)とlow(20bit)に切り詰めて１つのuintにパッキングする
        /// </summary>
        /// <param name="hi"></param>
        /// <param name="low"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Pack12_20(int hi, int low)
        {
            return (uint)hi << 20 | (uint)low & 0xfffff;
        }

        /// <summary>
        /// uint12-20パックデータから上位12bitデータをintにして返す
        /// </summary>
        /// <param name="pack"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Unpack12_20Hi(uint pack)
        {
            return (int)((pack >> 20) & 0xfff);
        }

        /// <summary>
        /// uint12-20パックデータから下位20bitデータをintにして返す
        /// </summary>
        /// <param name="pack"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Unpack12_20Low(uint pack)
        {
            return (int)(pack & 0xfffff);
        }

        /// <summary>
        /// uint12-20パックデータを分解して２つのintとして返す
        /// </summary>
        /// <param name="pack"></param>
        /// <param name="hi"></param>
        /// <param name="low"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Unpack12_20(uint pack, out int hi, out int low)
        {
            hi = (int)((pack >> 20) & 0xfff);
            low = (int)(pack & 0xfffff);
        }

        /// <summary>
        /// ４つのintをushortに変換し１つのulongにパッキングする
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="w"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong Pack64(int x, int y, int z, int w)
        {
            return (((ulong)x) & 0xffff) << 48 | (((ulong)y) & 0xffff) << 32 | (((ulong)z) & 0xffff) << 16 | ((ulong)w) & 0xffff;
        }

        /// <summary>
        /// int4をushortに変換し１つのulongにパッキングする
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong Pack64(in int4 a)
        {
            return Pack64(a.x, a.y, a.z, a.w);
        }

        /// <summary>
        /// ulongパックデータからint4に展開して返す
        /// </summary>
        /// <param name="pack"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int4 Unpack64(in ulong pack)
        {
            return new int4(
                (int)((pack >> 48) & 0xffff),
                (int)((pack >> 32) & 0xffff),
                (int)((pack >> 16) & 0xffff),
                (int)(pack & 0xffff)
                );
        }

        /// <summary>
        /// ulongパックデータからx値を取り出す
        /// </summary>
        /// <param name="pack"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Unpack64X(in ulong pack)
        {
            return (int)((pack >> 48) & 0xffff);
        }

        /// <summary>
        /// ulongパックデータからy値を取り出す
        /// </summary>
        /// <param name="pack"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Unpack64Y(in ulong pack)
        {
            return (int)((pack >> 32) & 0xffff);
        }

        /// <summary>
        /// ulongパックデータからz値を取り出す
        /// </summary>
        /// <param name="pack"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Unpack64Z(in ulong pack)
        {
            return (int)((pack >> 16) & 0xffff);
        }

        /// <summary>
        /// ulongパックデータからw値を取り出す
        /// </summary>
        /// <param name="pack"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Unpack64W(in ulong pack)
        {
            return (int)(pack & 0xffff);
        }

        /// <summary>
        /// ４つのintをbyteに変換し１つのuintにパッキングする
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="w"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Pack32(int x, int y, int z, int w)
        {
            return (((uint)x) & 0xff) << 24 | (((uint)y) & 0xff) << 16 | (((uint)z) & 0xff) << 8 | ((uint)w) & 0xff;
        }

        /// <summary>
        /// int4をbyteに変換し１つのuintにパッキングする
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong Pack32(in int4 a)
        {
            return Pack64(a.x, a.y, a.z, a.w);
        }

        /// <summary>
        /// uintパックデータからint4に展開して返す
        /// </summary>
        /// <param name="pack"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int4 Unpack32(in uint pack)
        {
            return new int4(
                (int)((pack >> 24) & 0xff),
                (int)((pack >> 16) & 0xff),
                (int)((pack >> 8) & 0xff),
                (int)(pack & 0xff)
                );
        }

        /// <summary>
        /// int3のうちuse(int2)で使われていない残りの１つのデータを返す
        /// </summary>
        /// <param name="data"></param>
        /// <param name="use"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int RemainingData(in int3 data, in int2 use)
        {
            if (data.x != use.x && data.x != use.y)
                return data.x;
            if (data.y != use.x && data.y != use.y)
                return data.y;
            return data.z;
        }

        //=========================================================================================
        /// <summary>
        /// AnimationCurveを16個のfloatのリスト(float4x4)に変換する
        /// </summary>
        /// <param name="curve"></param>
        /// <returns></returns>
        public static float4x4 ConvertAnimationCurve(AnimationCurve curve)
        {
            float4x4 m = 0;
            for (int i = 0; i < 16; i++)
            {
                float time = i / 15.0f;
                float val = curve.Evaluate(time);
                m.SetValue(i, val);
            }

            return m;
        }

        /// <summary>
        /// AnimationCurveが格納されたfloat4x4からデータを取得する
        /// </summary>
        /// <param name="curve"></param>
        /// <param name="time"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float EvaluateCurve(in float4x4 curve, float time)
        {
            const float interval = 1.0f / 15.0f;
            int index = (int)(math.saturate(time) * 15);
            time -= index * interval;
            float t = time / interval;
            return math.lerp(curve.GetValue(index), curve.GetValue(index + 1), t);
        }

        //=========================================================================================
        /// <summary>
        /// 8bitフラグからコライダータイプを取得する
        /// </summary>
        /// <param name="flag"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ColliderManager.ColliderType GetColliderType(in ExBitFlag8 flag)
        {
            return (ColliderManager.ColliderType)(flag.Value & 0x0f);
        }

        /// <summary>
        /// 8bitフラグにコライダータイプを設定する
        /// </summary>
        /// <param name="flag"></param>
        /// <param name="ctype"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ExBitFlag8 SetColliderType(ExBitFlag8 flag, ColliderManager.ColliderType ctype)
        {
            flag.Value = (byte)(flag.Value & 0xf0 | (byte)ctype);
            return flag;
        }
    }
}
