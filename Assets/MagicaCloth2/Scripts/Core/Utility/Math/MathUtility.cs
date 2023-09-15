// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth2
{
    public static class MathUtility
    {
        /// <summary>
        /// 数値を(-1.0f～1.0f)にクランプする
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Clamp1(float a)
        {
            return math.clamp(a, -1.0f, 1.0f);
        }

        /// <summary>
        /// 投影ベクトルを求める
        /// </summary>
        /// <param name="v"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 Project(in float3 v, in float3 n)
        {
            return math.dot(v, n) * n;
        }

        /// <summary>
        /// ベクトルを平面に投影する
        /// </summary>
        /// <param name="v"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 ProjectOnPlane(in float3 v, in float3 n)
        {
            return v - math.dot(v, n) * n;
        }

        /// <summary>
        /// ２つのベクトルのなす角を返す（ラジアン）
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns>ラジアン</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Angle(in float3 v1, in float3 v2)
        {
            float len1 = math.length(v1);
            float len2 = math.length(v2);

            Develop.Assert(len1 * len2 > 0.0f);
            float cos_sita = math.dot(v1, v2) / (len1 * len2);

            float sita = math.acos(Clamp1(cos_sita));

            //return degrees(sita);
            return sita;
        }

        /// <summary>
        /// ベクトルの長さをクランプする
        /// </summary>
        /// <param name="v"></param>
        /// <param name="minlength"></param>
        /// <param name="maxlength"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 ClampVector(float3 v, float minlength, float maxlength)
        {
            float len = math.length(v);
            if (len > 1e-09f)
            {
                if (len > maxlength)
                {
                    v *= (maxlength / len);
                }
                else if (len < minlength)
                {
                    v *= (minlength / len);
                }
            }

            return v;
        }

        /// <summary>
        /// ベクトルの長さをクランプする
        /// </summary>
        /// <param name="v"></param>
        /// <param name="minlength"></param>
        /// <param name="maxlength"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 ClampVector(float3 v, float maxlength)
        {
            float len = math.length(v);
            if (len > 1e-09f && len > maxlength)
            {
                v *= (maxlength / len);
            }

            return v;
        }

        /// <summary>
        /// frotmからtoへの移動を最大移動距離でクランプする
        /// </summary>
        /// <param name="from">基準座標</param>
        /// <param name="to">目標座標</param>
        /// <param name="maxlength">最大移動距離</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 ClampDistance(float3 from, float3 to, float maxlength)
        {
            float len = math.distance(from, to);
            if (len <= maxlength)
                return to;

            Develop.Assert(len > 0.0f);
            float t = maxlength / len;
            return math.lerp(from, to, t);
        }

        /// <summary>
        /// ベクトル(dir)を最大角度にクランプする
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="basedir"></param>
        /// <param name="maxAngle">最大角度（ラジアン）</param>
        /// <returns></returns>
        public static bool ClampAngle(in float3 dir, in float3 basedir, float maxAngle, out float3 outdir)
        {
            float3 v1 = math.normalize(dir);
            float3 v2 = math.normalize(basedir);

            float c = Clamp1(math.dot(v1, v2));
            float angle = math.acos(c);

            //if (c > 0.9995f || angle <= maxAngle)
            if (angle <= maxAngle)
            {
                // クランプの必要なし
                outdir = dir;
                return false;
            }

            // 戻す割合
            Develop.Assert(angle != 0.0f);
            float t = (angle - maxAngle) / angle;

            // dirをmaxAngleにクランプするクォータニオンを求める
            float3 axis = math.cross(v1, v2);
            if (math.abs(1.0f + c) < 1e-06f)
            {
                angle = (float)math.PI;

                if (v1.x > v1.y && v1.x > v1.z)
                {
                    axis = math.cross(v1, new float3(0, 1, 0));
                }
                else
                {
                    axis = math.cross(v1, new float3(1, 0, 0));
                }
            }
            else if (math.abs(1.0f - c) < 1e-06f)
            {
                //angle = 0.0f;
                //axis = new float3(1, 0, 0);
                outdir = dir;
                return false;
            }
            var q = quaternion.AxisAngle(math.normalize(axis), angle * t);

            outdir = math.mul(q, dir);
            return true;
        }

        /// <summary>
        /// fromからtoへ回転させるクォータニオンを返します
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="t">補間率(0.0-1.0)</param>
        /// <returns></returns>
        public static quaternion FromToRotation(in float3 from, in float3 to, float t = 1.0f)
        {
            float3 v1 = math.normalize(from);
            float3 v2 = math.normalize(to);

            float c = Clamp1(math.dot(v1, v2));
            float angle = math.acos(c);
            float3 axis = math.cross(v1, v2);

            if (math.abs(1.0f + c) < 1e-06f)
            {
                angle = (float)math.PI;

                if (v1.x > v1.y && v1.x > v1.z)
                {
                    axis = math.cross(v1, new float3(0, 1, 0));
                }
                else
                {
                    axis = math.cross(v1, new float3(1, 0, 0));
                }
            }
            else if (math.abs(1.0f - c) < 1e-06f)
            {
                //angle = 0.0f;
                //axis = new float3(1, 0, 0);
                return quaternion.identity;
            }

            return quaternion.AxisAngle(math.normalize(axis), angle * t);
        }

        /// <summary>
        /// fromからtoへ回転させるクォータニオンを返します
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion FromToRotation(in quaternion from, in quaternion to)
        {
            return math.mul(to, math.inverse(from));
        }

        /// <summary>
        /// ２つのクォータニオンの角度を返します（ラジアン）
        /// 不正なクォータニオンでは結果が不定になるので注意！例:(0,0,0,0)など
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Angle(in quaternion a, in quaternion b)
        {
            const float PI2 = math.PI * 2.0f;

            float dot = math.dot(a, b);
            if (math.abs(dot) < 0.9999f)
            {
                var ang = math.acos(Clamp1(dot)) * 2.0f; // x2.0が必要
                return ang > math.PI ? PI2 - ang : ang;
            }
            else
                return 0;
        }

        /// <summary>
        /// クォータニオンを最大角度にクランプします
        /// </summary>
        /// <param name="from">基準回転</param>
        /// <param name="to">目標回転</param>
        /// <param name="maxAngle">最大角度（ラジアン）</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion ClampAngle(quaternion from, quaternion to, float maxAngle)
        {
            var ang = Angle(from, to);
            if (ang <= maxAngle)
                return to;

            Develop.Assert(ang != 0.0f);
            float t = maxAngle / ang;

            return math.slerp(from, to, t);
        }

        /// <summary>
        /// 法線と接線から回転姿勢を求める
        /// </summary>
        /// <param name="nor"></param>
        /// <param name="tan"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion ToRotation(in float3 nor, in float3 tan)
        {
#if MC2_DEBUG
            float ln = math.length(nor);
            float lt = math.length(tan);
            Develop.Assert(ln > 0.0f);
            Develop.Assert(lt > 0.0f);
            float dot = math.dot(nor / ln, tan / lt);
            Develop.Assert(dot != 1.0f && dot != -1.0f);
#endif
            return quaternion.LookRotation(tan, nor);
        }

        /// <summary>
        /// 回転姿勢を法線と接線に分解して返す
        /// </summary>
        /// <param name="rot"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ToNormalTangent(in quaternion rot, out float3 nor, out float3 tan)
        {
            nor = math.mul(rot, math.up());
            tan = math.mul(rot, math.forward());
        }

        /// <summary>
        /// 回転姿勢から法線を取り出す
        /// </summary>
        /// <param name="rot"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 ToNormal(in quaternion rot)
        {
            return math.mul(rot, math.up());
        }

        /// <summary>
        /// 回転姿勢から接線を取り出す
        /// </summary>
        /// <param name="rot"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 ToTangent(in quaternion rot)
        {
            return math.mul(rot, math.forward());
        }

        /// <summary>
        /// 回転姿勢から従法線を取り出す
        /// </summary>
        /// <param name="rot"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 ToBinormal(in quaternion rot)
        {
            return math.mul(rot, math.right());
        }

        /// <summary>
        /// 法線／接線から従法線を求めて返す
        /// </summary>
        /// <param name="nor"></param>
        /// <param name="tan"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 Binormal(in float3 nor, in float3 tan)
        {
            return math.cross(nor, tan);
        }

        /// <summary>
        /// 方向ベクトルをXY回転角度(ラジアン)に分離する、Z角度は常に０である
        /// </summary>
        /// <param name="axis"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 AxisToEuler(in float3 axis)
        {
            float angy = math.atan2(axis.x, axis.z);
            float angx = math.atan2(-axis.y, math.length(axis - new float3(0, axis.y, 0)));
            return new float3(angx, angy, 0);
        }

        /// <summary>
        /// 方向ベクトルからクォータニオンを作成して返す
        /// ベクトルは一旦オイラー角に分解されてからクォータニオンへ組み立て直される
        /// XYの回転軸を安定させるため
        /// </summary>
        /// <param name="dir"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion AxisQuaternion(float3 dir)
        {
            return quaternion.Euler(AxisToEuler(dir));
        }

        /// <summary>
        /// クォータニオンから回転軸と回転角度(rad)を取得する
        /// この結果はUnity.Quaternion.ToAngleAxisと同じである（僅かに誤差あり）
        /// ただ回転角度が360度を越えると軸が逆転するので注意！（これはUnity.ToAngleAxis()でも同じ）
        /// 回転がほぼ０の場合は回転軸として(0, 0, 0)を返す（Unity.ToAngleAxisでは(1, 0, 0))
        /// </summary>
        /// <param name="q"></param>
        /// <param name="angle">回転角度(rad)</param>
        /// <param name="axis">回転軸</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ToAngleAxis(in quaternion q, out float angle, out float3 axis)
        {
            float a = math.abs(q.value.w) < 0.9999f ? math.acos(q.value.w) : 0;
            angle = 2.0f * a;

            float s = math.sin(a);
            if (math.abs(s) < 1e-06f)
                axis = 0; // Unity.Quaternion.ToAngleAxisでは(1, 0, 0)
            else
                axis = q.value.xyz / s;
        }

        /// <summary>
        /// 与えられた線分abおよび点cに対して、ab上の最近接点t(0.0-1.0)を計算して返す
        /// </summary>
        /// <param name="c"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ClosestPtPointSegmentRatio(in float3 c, in float3 a, in float3 b)
        {
            float3 ab = b - a;
            // パラメータ化されている位置d(t) = a + t * (b - a) の計算によりabにcを射影
            float dot = math.dot(ab, ab);
            // abが同じ座標を考慮
            if (dot == 0.0f)
                return 0.0f;
            //Develop.Assert(dot != 0.0f);
            float t = math.dot(c - a, ab) / dot;
            // 線分の外側にある場合、t(従ってd)を最近接点までクランプ
            t = math.saturate(t);
            return t;
        }

        /// <summary>
        /// 与えられた線分abおよび点cに対して、ab上の最近接点tを計算して返す。tはクランプされない
        /// </summary>
        /// <param name="c"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ClosestPtPointSegmentRatioNoClamp(float3 c, float3 a, float3 b)
        {
            float3 ab = b - a;
            // パラメータ化されている位置d(t) = a + t * (b - a) の計算によりabにcを射影
            float dot = math.dot(ab, ab);
            Develop.Assert(dot != 0.0f);
            float t = math.dot(c - a, ab) / dot;
            return t;
        }

        /// <summary>
        /// 与えられた線分abおよび点cに対して、ab上の最近接点座標dを計算して返す
        /// </summary>
        /// <param name="c"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 ClosestPtPointSegment(float3 c, float3 a, float3 b)
        {
            float3 ab = b - a;
            // パラメータ化されている位置d(t) = a + t * (b - a) の計算によりabにcを射影
            float dot = math.dot(ab, ab);
            Develop.Assert(dot != 0.0f);
            float t = math.dot(c - a, ab) / dot;
            // 線分の外側にある場合、t(従ってd)を最近接点までクランプ
            t = math.saturate(t);
            // クランプされているtからの射影されている位置を計算
            return a + t * ab;
        }

        /// <summary>
        /// 与えられた線分abおよび点cに対して、ab上の最近接点座標dを計算して返す。dはクランプされない
        /// </summary>
        /// <param name="c"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 ClosestPtPointSegmentNoClamp(float3 c, float3 a, float3 b)
        {
            float3 ab = b - a;
            // パラメータ化されている位置d(t) = a + t * (b - a) の計算によりabにcを射影
            float dot = math.dot(ab, ab);
            Develop.Assert(dot != 0.0f);
            float t = math.dot(c - a, ab) / dot;
            // クランプされているtからの射影されている位置を計算
            return a + t * ab;
        }

        /// <summary>
        /// ２つの線分(p1-q1)(p2-q2)の最近接点(c1, c2)を計算する
        /// 戻り値として最近接点の距離の平方を返す
        /// </summary>
        /// <param name="p1">線分１の始点</param>
        /// <param name="q1">線分１の終点</param>
        /// <param name="p2">線分２の始点</param>
        /// <param name="q2">線分２の終点</param>
        /// <param name="c1">最近接点１</param>
        /// <param name="c2">最近接点２</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ClosestPtSegmentSegment(in float3 p1, in float3 q1, in float3 p2, in float3 q2, out float s, out float t, out float3 c1, out float3 c2)
        {
            //s = 0.0f;
            //t = 0.0f;
            float3 d1 = q1 - p1; // 線分s1の方向ベクトル
            float3 d2 = q2 - p2; // 線分s2の方向ベクトル
            float3 r = p1 - p2;
            float a = math.dot(d1, d1); // 線分s1の距離の平方、常に正
            float e = math.dot(d2, d2); // 線分s2の距離の平方、常に正
            float f = math.dot(d2, r);
            // 片方あるいは両方の線分が点に縮退しているかどうかチェック
            if (a <= 1e-8f && e <= 1e-8f)
            {
                // 両方の線分が点に縮退
                s = t = 0.0f;
                c1 = p1;
                c2 = p2;
                return math.dot(c1 - c2, c1 - c2);
            }
            if (a <= 1e-8f)
            {
                // 最初の線分が点に縮退
                s = 0.0f;
                t = math.saturate(f / e);
            }
            else
            {
                float c = math.dot(d1, r);
                if (e <= 1e-8f)
                {
                    // 2番目の線分が点に縮退
                    t = 0.0f;
                    s = math.saturate(-c / a);
                }
                else
                {
                    // ここから一般的な縮退の場合を開始
                    float b = math.dot(d1, d2);
                    float denom = a * e - b * b; // 常に正
                    // 線分が平行でない場合、L1上のL2に対する最近接点を計算、そして
                    // 線分s1に対してクランプ。そうでない場合は任意s(ここでは0)を選択
                    if (denom != 0.0f)
                    {
                        s = math.saturate((b * f - c * e) / denom);
                    }
                    else
                    {
                        s = 0.0f;
                    }
                    // L2上のs1(s)に対する最近接点を以下を用いて計算
                    // t = dot((p1 + d1 * s) - p2, d2) / dot(d2, d2) = (b * s + f) / e
                    t = (b * s + f) / e;
                    // tが[0,1]の中にあれば終了。
                    // そうでなければtをクランプ、sをtの新しい値に対して以下を用いて再計算
                    // s = dot((p2 + d2 * t) - p1, d1) / dot(d1, d1) = (t * b - c) / a
                    // そしてsを[0,1]にクランプ
                    if (t < 0.0f)
                    {
                        t = 0.0f;
                        s = math.saturate(-c / a);
                    }
                    else if (t > 1.0f)
                    {
                        t = 1.0f;
                        s = math.saturate((b - c) / a);
                    }
                }
            }

            c1 = p1 + d1 * s;
            c2 = p2 + d2 * t;

            return math.dot(c1 - c2, c1 - c2);
        }

        /// <summary>
        /// 三角形(abc)から点(p)への最近接点とその重心座標uvwを返す
        /// </summary>
        /// <param name="p"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <param name="uvw"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 ClosestPtPointTriangle(in float3 p, in float3 a, in float3 b, in float3 c, out float3 uvw)
        {
            uvw = 0;
            float v = 0, w = 0;

            // PがAの外側の頂点領域の中にあるかどうかチェック
            var ab = b - a;
            var ac = c - a;
            var ap = p - a;
            float d1 = math.dot(ab, ap);
            float d2 = math.dot(ac, ap);
            if (d1 <= 0.0f && d2 <= 0.0f)
            {
                uvw.x = 1; // 重心座標(1,0,0)
                return a;
            }

            // PがBの外側の頂点領域の中にあるかどうかチェック
            var bp = p - b;
            float d3 = math.dot(ab, bp);
            float d4 = math.dot(ac, bp);
            if (d3 >= 0.0f && d4 <= d3)
            {
                uvw.y = 1; // 重心座標(0,1,0)
                return b;
            }

            // PがABの辺領域の中にあるかどうかチェックし、あればPのAB上に対する射影を返す
            float vc = d1 * d4 - d3 * d2;
            if (vc <= 0.0f && d1 >= 0.0f && d3 <= 0.0f)
            {
                Develop.Assert((d1 - d3) != 0.0f);
                v = d1 / (d1 - d3);
                uvw = new float3(1 - v, v, 0); // 重心座標(1-v,v,0)
                return a + v * ab;
            }

            // PがCの外側の頂点領域の中にあるかどうかチェック
            var cp = p - c;
            float d5 = math.dot(ab, cp);
            float d6 = math.dot(ac, cp);
            if (d6 >= 0.0f && d5 <= d6)
            {
                uvw.z = 1; // 重心座標(0,0,1)
                return c;
            }

            // PがACの辺領域の中にあるかどうかチェックし、あればPのAC上に対する射影を返す
            float vb = d5 * d2 - d1 * d6;
            if (vb <= 0.0f && d2 >= 0.0f && d6 <= 0.0f)
            {
                Develop.Assert((d2 - d6) != 0.0f);
                w = d2 / (d2 - d6);
                uvw = new float3(1 - w, 0, w); // 重心座標(1-w,0,w)
                return a + w * ac;
            }

            // PがBCの辺領域の中にあるかどうかチェックし、あればPのBC上に対する射影を返す
            float va = d3 * d6 - d5 * d4;
            if (va <= 0.0f && (d4 - d3) >= 0.0f && (d5 - d6) >= 0.0f)
            {
                float g = (d4 - d3) + (d5 - d6);
                Develop.Assert(g != 0.0f);
                w = (d4 - d3) / g;
                uvw = new float3(0, 1 - w, w); // 重心座標(0,1-w,w)
                return b + w * (c - b);
            }

            // Pは面領域の中にある。Qをその重心座標(u,v,w)を用いて計算
            float h = va + vb + vc;
            Develop.Assert(h != 0.0f);
            float denom = 1.0f / h;
            v = vb * denom;
            w = vc * denom;
            uvw = new float3(1 - v - w, v, w); // 重心座標
            return a + ab * v + ac * w;
        }

        /// <summary>
        /// 三角形と点の最近接点重心(uvw)から点が三角形の内部にあるか判定する
        /// </summary>
        /// <param name="uvw"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool PointInTriangleUVW(float3 uvw)
        {
            return math.all(uvw);
        }


        /// <summary>
        /// トライアングルの重心を返す
        /// </summary>
        /// <param name="p0"></param>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 TriangleCenter(in float3 p0, in float3 p1, in float3 p2)
        {
            return (p0 + p1 + p2) / 3.0f;
        }

        /// <summary>
        /// トライアングルの法線を計算して返す
        /// </summary>
        /// <param name="p0"></param>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 TriangleNormal(in float3 p0, in float3 p1, in float3 p2)
        {
            var c = math.cross(p1 - p0, p2 - p0);
#if MC2_DEBUG
            Develop.Assert(math.length(c) > 0.0f);
#endif
            return math.normalize(c);
        }

        /// <summary>
        /// トライアングルの面積を求めて返す
        /// </summary>
        /// <param name="p0"></param>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float TriangleArea(in float3 p0, in float3 p1, in float3 p2)
        {
            var c = math.cross(p1 - p0, p2 - p0);
            return math.length(c);
        }

        /// <summary>
        /// 安全なトライアングルか判定する
        /// 面積が極端に小さいトライアングルは不正とする
        /// </summary>
        /// <param name="p0"></param>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsSafeTriangle(in float3 p0, in float3 p1, in float3 p2)
        {
            return TriangleArea(p0, p1, p2) > 1e-06f;
        }

        /// <summary>
        /// トライアングルの接線を計算して返す。
        /// 接線は単位化される。ただし、状況により長さ０となるケースがありその場合はベクトル０を返す。
        /// </summary>
        /// <param name="p0"></param>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="uv0"></param>
        /// <param name="uv1"></param>
        /// <param name="uv2"></param>
        /// <returns></returns>
        public static float3 TriangleTangent(in float3 p0, in float3 p1, in float3 p2, in float2 uv0, in float2 uv1, in float2 uv2)
        {
            // 接線(頂点座標とUVから接線を求める一般的なアルゴリズム)
            float3 distBA = p1 - p0;
            float3 distCA = p2 - p0;
            float2 tdistBA = uv1 - uv0;
            float2 tdistCA = uv2 - uv0;
            float area = tdistBA.x * tdistCA.y - tdistBA.y * tdistCA.x;
            float3 tan = 0;
            if (area == 0.0f)
            {
#if MC2_DEBUG
                Debug.LogWarning($"Calc tangent area = 0!\np0:{p0},p1:{p1},p2:{p2}\nuv0:{uv0},uv1:{uv1},uv2:{uv2}");
#endif

                // どうしてもまれに発生するので一旦定数で処理を流してみる
                area = 1;
            }

            //else
            {
                float delta = 1.0f / area;
                tan = new float3(
                    (distBA.x * tdistCA.y) + (distCA.x * -tdistBA.y),
                    (distBA.y * tdistCA.y) + (distCA.y * -tdistBA.y),
                    (distBA.z * tdistCA.y) + (distCA.z * -tdistBA.y)
                    ) * delta;
                // 左手座標系に合わせる
                tan = -tan;
            }

            // 長さ０はベクトル０となる
            tan = math.normalizesafe(tan, 0);
            //#if MC2_DEBUG
            //            Debug.Assert(math.length(tan) > Define.System.Epsilon);
            //#endif
            //            tan = math.normalize(tan);

            return tan;
        }

        /// <summary>
        /// トライアングルの回転姿勢を返す
        /// 法線と(重心-p0)の軸からなるクォータニオン
        /// </summary>
        /// <param name="p0"></param>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion TriangleRotation(float3 p0, float3 p1, float3 p2)
        {
            var n = TriangleNormal(p0, p1, p2);
            var cen = TriangleCenter(p0, p1, p2);
            var tan = math.normalize(p0 - cen);
            return quaternion.LookRotation(tan, n);
        }

        /// <summary>
        /// 隣接する２つのトライアングルの回転姿勢を返す
        /// 法線の平均と共通エッジからなるクォータニオン
        /// </summary>
        /// <param name="p0"></param>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="p3"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion TriangleCenterRotation(float3 p0, float3 p1, float3 p2, float3 p3)
        {
            var n0 = TriangleNormal(p0, p2, p3);
            var n1 = TriangleNormal(p1, p3, p2);
            var n = (n0 + n1) * 0.5f;
            var tan = math.normalize(p3 - p2);
            return quaternion.LookRotation(tan, n);
        }

        /// <summary>
        /// トライアングルペアのなす角を返す（ラジアン）
        ///   v2 +
        ///     / \
        /// v0 +---+ v1
        ///     \ /
        ///   v3 +
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns>ラジアン、水平時は0となる</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float TriangleAngle(in float3 v0, in float3 v1, in float3 v2, in float3 v3)
        {
            var ev = v1 - v0;
            var va = v2 - v0;
            var vb = v3 - v0;
            var na = math.cross(va, ev);
            var nb = math.cross(ev, vb);
            float ang = Angle(na, nb);

            return ang;
        }

        /// <summary>
        /// トライアングル重心からの距離を返す
        /// </summary>
        /// <param name="p"></param>
        /// <param name="p0"></param>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float DistanceTriangleCenter(float3 p, float3 p0, float3 p1, float3 p2)
        {
            var cen = (p0 + p1 + p2) / 3.0f;
            return math.distance(p, cen);
        }

        /// <summary>
        /// 点ｐがトライアングルの正負どちらの向きにあるか返す(-1/0/+1)
        /// </summary>
        /// <param name="p"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float DirectionPointTriangle(float3 p, float3 a, float3 b, float3 c)
        {
            var ab = b - a;
            var ac = c - a;
            var ap = p - a;

            float3 n = math.cross(ab, ac);

            float d = math.dot(ap, n);
            return math.sign(d);
        }

        /// <summary>
        /// ２つのトライアングルと共通するエッジから残りの２つ頂点（対角点）を返す
        /// </summary>
        /// <param name="tri1"></param>
        /// <param name="tri2"></param>
        /// <param name="edge"></param>
        /// <returns></returns>
        public static int2 GetRestTriangleVertex(int3 tri1, int3 tri2, int2 edge)
        {
            int2 r = -1;

            for (int i = 0; i < 3; i++)
            {
                var val = tri1[i];
                if (val != edge.x && val != edge.x && val != edge.y && val != edge.y)
                {
                    r[0] = val;
                    break;
                }
            }
            for (int i = 0; i < 3; i++)
            {
                var val = tri2[i];
                if (val != edge.x && val != edge.x && val != edge.y && val != edge.y)
                {
                    r[1] = val;
                    break;
                }
            }

            return r;
        }

        /// <summary>
        /// ２つのトライアングルから共通する辺のインデックスを返す
        /// </summary>
        /// <param name="tri1"></param>
        /// <param name="tri2"></param>
        /// <returns>見つからない場合は(0,0)</returns>
        public static int2 GetCommonEdgeFromTrianglePair(int3 tri1, int3 tri2)
        {
            int2 e = 0;
            int eindex = 0;

            for (int i = 0; i < 3; i++)
            {
                if (tri1[i] == tri2.x || tri1[i] == tri2.y || tri1[i] == tri2.z)
                {
                    e[eindex] = tri1[i];
                    eindex++;
                }
            }
            if (eindex != 2)
            {
                Debug.LogError("Common edge nothing!");
                return 0;
            }

            if (e.x > e.y)
            {
                int w = e.x;
                e.x = e.y;
                e.y = w;
            }

            return e;
        }

        /// <summary>
        /// 共通する辺をもつ２つのトライアングルから四角を形成する４つの頂点インデックスを返す
        /// 頂点インデックスは[2][3]が共通する辺を示し、[0][1]は各トライアングルの残りのインデックス
        ///   v2 +
        ///     /|\
        /// v0 + | + v1
        ///     \|/
        ///   v3 +
        /// </summary>
        /// <param name="tri1"></param>
        /// <param name="tri2"></param>
        /// <returns></returns>
        public static int4 GetTrianglePairIndices(int3 tri1, int3 tri2)
        {
            // 共通の辺
            int2 e = GetCommonEdgeFromTrianglePair(tri1, tri2);
            Debug.Assert((e.x > 0 || e.y > 0) && e.x != e.y);

            int4 r = new int4(0, 0, e.x, e.y);
            for (int i = 0; i < 3; i++)
            {
                if (tri1[i] != e.x && tri1[i] != e.y)
                    r[0] = tri1[i];
                if (tri2[i] != e.x && tri2[i] != e.y)
                    r[1] = tri2[i];
            }

            return r;
        }

        /// <summary>
        /// トライアングルについて指定エッジ以外の頂点インデックスを返す
        /// </summary>
        /// <param name="tri"></param>
        /// <param name="edge"></param>
        /// <returns></returns>
        public static int GetUnuseTriangleIndex(int3 tri, int2 edge)
        {
            if (tri.x != edge.x && tri.x != edge.y)
                return tri.x;
            if (tri.y != edge.x && tri.y != edge.y)
                return tri.y;
            if (tri.z != edge.x && tri.z != edge.y)
                return tri.z;

            return -1;
        }

        /// <summary>
        /// 共通するエッジをもつ２つのトライアングルのなす角を求める（ラジアン）
        ///   v2 +
        ///     /|\
        /// v0 + | + v1
        ///     \|/
        ///   v3 +
        /// </summary>
        /// <param name="pos0"></param>
        /// <param name="pos1"></param>
        /// <param name="pos2"></param>
        /// <param name="pos3"></param>
        /// <returns>ラジアン</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetTrianglePairAngle(float3 pos0, float3 pos1, float3 pos2, float3 pos3)
        {
            var va = pos3 - pos2;
            var vb = pos0 - pos2;
            var vc = pos1 - pos2;

            var n0 = math.cross(va, vb);
            var n1 = math.cross(vc, va);

            float ang = Angle(n0, n1);
            return ang;
        }

        /// <summary>
        /// トライアングルを反転させる
        /// </summary>
        /// <param name="tri"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int3 FlipTriangle(in int3 tri)
        {
            return tri.xzy;
        }

        /// <summary>
        /// トライアングルを包む球の中心と半径を求める
        /// これは球からトライアングルがはみ出る事はないが完全に正確ではないので注意！
        /// あくまで衝突判定のブロードフェーズなどで使用することが目的のもの
        /// 正確性よりも速度を重視した実装となっている
        /// </summary>
        /// <param name="pos0"></param>
        /// <param name="pos1"></param>
        /// <param name="pos2"></param>
        /// <param name="sc"></param>
        /// <param name="sr"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetTriangleSphere(float3 pos0, float3 pos1, float3 pos2, out float3 sc, out float sr)
        {
            float3 max = math.max(math.max(pos0, pos1), pos2);
            float3 min = math.min(math.min(pos0, pos1), pos2);
            sc = (min + max) * 0.5f;
            sr = math.distance(min, max) * 0.5f;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4x4 LocalToWorldMatrix(in float3 wpos, in quaternion wrot, in float3 wscl)
        {
            return Matrix4x4.TRS(wpos, wrot, wscl);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4x4 WorldToLocalMatrix(in float3 wpos, in quaternion wrot, in float3 wscl)
        {
            return math.inverse(Matrix4x4.TRS(wpos, wrot, wscl));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 TransformPoint(in float3 pos, in float4x4 localToWorldMatrix)
        {
            return math.transform(localToWorldMatrix, pos);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 TransformVector(in float3 vec, in float4x4 localToWorldMatrix)
        {
            return math.mul(localToWorldMatrix, new float4(vec, 0)).xyz;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 TransformDirection(in float3 dir, in float4x4 localToWorldMatrix)
        {
            float len = math.length(dir);
            if (len > 0.0f)
                return math.normalize(TransformVector(dir, localToWorldMatrix)) * len;
            else
                return dir;
        }

        /// <summary>
        /// 距離を空間変換する
        /// 不均等スケールを考慮して各軸の平均値を返す
        /// </summary>
        /// <param name="dist"></param>
        /// <param name="localToWorldMatrix"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float TransformDistance(in float dist, in float4x4 localToWorldMatrix)
        {
            var dist3 = math.mul(localToWorldMatrix, new float4(dist, dist, dist, 0)).xyz;
            return math.csum(dist3) / 3; // 平均化
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void TransformPositionNormalTangent(in float3 tpos, in quaternion trot, in float3 tscl, ref float3 pos, ref float3 nor, ref float3 tan)
        {
            // position
            pos *= tscl;
            pos = math.mul(trot, pos);
            pos += tpos;

            // normal
            nor = math.mul(trot, nor);

            // tangent
            tan = math.mul(trot, tan);
        }

        /// <summary>
        /// 長さをマトリックス空間に変換する
        /// </summary>
        /// <param name="length"></param>
        /// <param name="matrix"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float TransformLength(float length, in float4x4 matrix)
        {
            return math.length(math.mul(matrix, new float4(length, length, length, 0)).xyz) / 1.73205f;
        }

        // ！！これはスケールが入るとうまく行かない
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static quaternion TransformRotation(in quaternion rot, in float4x4 localToWorldMatrix)
        //{
        //    return math.mul(rot, new quaternion(localToWorldMatrix));
        //}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 InverseTransformPoint(in float3 pos, in float4x4 worldToLocalMatrix)
        {
            return math.transform(worldToLocalMatrix, pos);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 InverseTransformVector(in float3 vec, in float4x4 worldToLocalMatrix)
        {
            return math.mul(worldToLocalMatrix, new float4(vec, 0)).xyz;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 InverseTransformVector(in float3 vec, in quaternion rot)
        {
            return math.mul(math.inverse(rot), vec);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 InverseTransformDirection(in float3 dir, in float4x4 worldToLocalMatrix)
        {
            float len = math.length(dir);
            if (len > 0.0f)
                return math.normalize(InverseTransformVector(dir, worldToLocalMatrix)) * len;
            else
                return dir;
        }

        // ！！これはスケールが入るとうまく行かない
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static quaternion InverseTransformRotation(in quaternion rot, in float4x4 worldToLocalMatrix)
        //{
        //    return math.mul(new quaternion(worldToLocalMatrix), rot);
        //}

        /// <summary>
        /// fromのローカル座標をtoのローカル座標に変換するmatrixを返す
        /// </summary>
        /// <param name="to"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4x4 Transform(in float4x4 fromLocalToWorldMatrix, in float4x4 toWorldToLocalMatrix)
        {
            return math.mul(toWorldToLocalMatrix, fromLocalToWorldMatrix);
        }

        /// <summary>
        /// ２つのマトリックスが等しいか判定する
        /// </summary>
        /// <param name="m1"></param>
        /// <param name="m2"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CompareMatrix(in float4x4 m1, in float4x4 m2)
        {
#if true
            var b4 = m1 == m2;
            return math.all(b4.c0) && math.all(b4.c1) && math.all(b4.c2) && math.all(b4.c3);
#else
            // ハッシュの方が早いかな
            // ★ハッシュではすぐに衝突を起こすのでだめ！
            return math.hash(m1) == math.hash(m2);
#endif
        }

        /// <summary>
        /// ２つの座標系が等しいか判定する
        /// </summary>
        /// <param name="pos1"></param>
        /// <param name="rot1"></param>
        /// <param name="scl1"></param>
        /// <param name="pos2"></param>
        /// <param name="rot2"></param>
        /// <param name="scl2"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CompareTransform(in float3 pos1, in quaternion rot1, in float3 scl1, in float3 pos2, in quaternion rot2, in float3 scl2)
        {
            return pos1.Equals(pos2) && rot1.Equals(rot2) && scl1.Equals(scl2);
        }

        /// <summary>
        /// 線分pqおよび三角形abcに対して、線分が三角形と交差しているかどうかを返す
        /// 交差している場合は、交差点の重心(u,v,w)と線分の位置tを返す
        /// </summary>
        /// <param name="p"></param>
        /// <param name="q"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <param name="doubleSide">両面判定はtrue</param>
        /// <param name="u"></param>
        /// <param name="v"></param>
        /// <param name="w"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static bool IntersectSegmentTriangle(in float3 p, in float3 q, float3 a, float3 b, float3 c, bool doubleSide, out float u, out float v, out float w, out float t)
        {
            t = 0.0f;
            u = 0.0f;
            v = 0.0f;
            w = 0.0f;

            var ab = b - a;
            var ac = c - a;
            var qp = p - q;

            float3 n = math.cross(ab, ac);

            float d = math.dot(qp, n);

            // 水平は無効
            if (math.abs(d) < 1e-09f)
                return false;

            if (doubleSide == false)
            {
                // 法線方向からの侵入のみ（オリジナル）
                if (d <= 0.0f)
                    return false;
            }
            else if (d < 0.0f)
            {
                // 三角形の両面からの衝突を許可する
                n = -n;
                float3 x = b;
                b = c;
                c = x;
                ab = b - a;
                ac = c - a;
                d = math.dot(qp, n);
            }

            var ap = p - a;
            t = math.dot(ap, n);
            if (t < 0.0f)
                return false;
            if (t > d)
                return false;

            float3 e = math.cross(qp, ap);
            v = math.dot(ac, e);
            if (v < 0.0f || v > d)
                return false;
            w = -math.dot(ab, e);
            if (w < 0.0f || (v + w) > d)
                return false;

            float ood = 1.0f / d;
            t *= ood;
            v *= ood;
            w *= ood;
            u = 1.0f - v - w;

            return true;
        }

        /// <summary>
        /// 線分pqおよび三角形abcに対して、線分が三角形と交差しているかどうかを返す
        /// 交差している場合は、交差点の重心(u,v,w)と線分の位置tを返す
        /// </summary>
        /// <param name="p"></param>
        /// <param name="q"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        public static bool IntersectSegmentTriangle(in float3 p, in float3 q, float3 a, float3 b, float3 c)
        {
            var ab = b - a;
            var ac = c - a;
            var qp = p - q;

            float3 n = math.cross(ab, ac);

            float d = math.dot(qp, n);

            // 水平は無効
            if (math.abs(d) < 1e-09f)
                return false;

            if (d < 0.0f)
            {
                // 三角形の両面からの衝突を許可する
                n = -n;
                float3 x = b;
                b = c;
                c = x;
                ab = b - a;
                ac = c - a;
                d = math.dot(qp, n);
            }

            var ap = p - a;
            var t = math.dot(ap, n);
            if (t < 0.0f)
                return false;
            if (t > d)
                return false;

            float3 e = math.cross(qp, ap);
            var v = math.dot(ac, e);
            if (v < 0.0f || v > d)
                return false;
            var w = -math.dot(ab, e);
            if (w < 0.0f || (v + w) > d)
                return false;

            return true;
        }

        /// <summary>
        /// 点と面の衝突判定
        /// 衝突した場合にその押し出し位置を計算して返す
        /// </summary>
        /// <param name="planePos"></param>
        /// <param name="planeDir"></param>
        /// <param name="pos"></param>
        /// <param name="outPos"></param>
        /// <returns>平面までの距離。押し出された（衝突の）場合は0.0以下(マイナス)</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float IntersectPointPlaneDist(in float3 planePos, in float3 planeDir, in float3 pos, out float3 outPos)
        {
            float3 v = pos - planePos;

            // 押出しベクトル
            float3 gv = Project(v, planeDir);
            var len = math.length(gv);

            if (math.dot(planeDir, v) < 0.0f)
            {
                // 押出し座標
                outPos = pos - gv;

                // 面までの距離をマイナスで返す
                return -len;
                //return 0.0f;
            }
            else
            {
                outPos = pos;

                // 面までの距離を返す
                return len;
            }
        }


        /// <summary>
        /// 光線と球が交差しているか判定する
        /// 交差している場合は交差しているtの値および交差点dを返す
        /// </summary>
        /// <param name="p">光線の始点</param>
        /// <param name="d">光線の方向|d|=1</param>
        /// <param name="sc">球の位置</param>
        /// <param name="sr">球の半径</param>
        /// <param name="t"></param>
        /// <param name="q"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IntersectRaySphere(in float3 p, in float3 d, in float3 sc, in float sr, ref float t, ref float3 q)
        {
            float3 m = p - sc;
            float b = math.dot(m, d);
            float c = math.dot(m, m) - sr * sr;
            // rの原点がsの外側にあり(c > 0),rがsから離れていく方向を指している場合(b > 0)に終了
            if (c > 0.0f && b > 0.0f)
                return false;
            float discr = b * b - c;
            // 負の判定式は光線が球を外れていることに一致
            if (discr < 0.0f)
                return false;
            // これで光線は球と交差していることがわかり交差する最小のtを計算
            t = -b - math.sqrt(discr);
            // tが負である場合、光線は球の内部から開始しているのでtをゼロにクランプ
            if (t < 0.0f)
                t = 0.0f;
            q = p + t * d;
            return true;
        }

        /// <summary>
        /// 点Cと線分abの間の距離の平方を返す
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        public static float SqDistPointSegment(Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 ab = b - a;
            Vector3 ac = c - a;
            Vector3 bc = c - b;
            float e = Vector3.Dot(ac, ab);

            // Cがabの外側に射影される場合を扱う
            if (e <= 0)
                return Vector3.Dot(ac, ac);
            float f = Vector3.Dot(ab, ab);
            if (e >= f)
                return Vector3.Dot(bc, bc);

            // Cがab上に射影される場合を扱う
            Develop.Assert(f != 0.0f);
            return Vector3.Dot(ac, ac) - e * e / f;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNaN(float3 v)
        {
            return math.any(math.isnan(v));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNaN(float4 v)
        {
            return math.any(math.isnan(v));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNaN(quaternion q)
        {
            return math.any(math.isnan(q.value));
        }

        /// <summary>
        /// 座標をPivotのローカル姿勢を保ちながらシフトさせる
        /// 主に慣性シフト用
        /// </summary>
        /// <param name="oldPos">移動前座標</param>
        /// <param name="oldPivotPosition">移動前のシフト中心座標</param>
        /// <param name="shiftVector">シフト移動量</param>
        /// <param name="shiftRotation">シフト回転量</param>
        /// <returns></returns>
        public static float3 ShiftPosition(in float3 oldPos, in float3 oldPivotPosition, in float3 shiftVector, in quaternion shiftRotation)
        {
            float3 lpos = oldPos - oldPivotPosition;
            lpos = math.mul(shiftRotation, lpos);
            lpos += shiftVector;
            return oldPivotPosition + lpos;
        }

        //=========================================================================================
        /// <summary>
        /// 深さから重量を計算して返す
        /// </summary>
        /// <param name="depth"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float CalcMass(float depth)
        {
            var a = (1.0f - depth);
            return 1.0f + a * a * Define.System.DepthMass;
        }

        /// <summary>
        /// 摩擦係数から逆重量を計算して返す
        /// </summary>
        /// <param name="friction"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float CalcInverseMass(float friction)
        {
            // 摩擦(0.0 ~ 1.0)により重量が増加する
            float mass = 1.0f + friction * Define.System.FrictionMass;

            Develop.Assert(mass > 0.0f);
            return 1.0f / mass;
        }

        /// <summary>
        /// 逆重量を計算して返す
        /// </summary>
        /// <param name="friction"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float CalcInverseMass(float friction, float depth)
        {
            float mass = 1.0f;

            // 摩擦(0.0 ~ 1.0)により重量を増加させる
            mass += friction * Define.System.FrictionMass;

            // 深さにより重量を増加させる
            //mass += (1.0f - depth) * Define.System.DepthMass;
            var a = (1.0f - depth);
            mass += a * a * Define.System.DepthMass;

            Develop.Assert(mass > 0.0f);
            return 1.0f / mass;
        }

        /// <summary>
        /// 逆重量を計算して返す
        /// </summary>
        /// <param name="friction"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float CalcInverseMass(float friction, float depth, bool fix)
        {
            return fix ? (1.0f / 100.0f) : CalcInverseMass(friction, depth);
        }

        /// <summary>
        /// セルフコリジョン用の逆重量を計算して返す
        /// 固定パーティクルはほとんど動かなくする
        /// </summary>
        /// <param name="friction"></param>
        /// <param name="fix"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float CalcSelfCollisionInverseMass(float friction, bool fix, float clothMass)
        {
            float mass = fix ? Define.System.SelfCollisionFixedMass : 1.0f + friction * Define.System.SelfCollisionFrictionMass;
            mass += clothMass * Define.System.SelfCollisionClothMass;
            Develop.Assert(mass > 0.0f);
            return 1.0f / mass;
        }
    }
}
