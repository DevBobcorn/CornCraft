// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace MagicaCloth
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
        /// 数値を(0.0f～1.0f)にクランプする
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Clamp01(float a)
        {
            return math.clamp(a, 0.0f, 1.0f);
        }

        /// <summary>
        /// 投影ベクトルを求める
        /// </summary>
        /// <param name="v"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 Project(float3 v, float3 n)
        {
            return math.dot(v, n) * n;
        }

        /// <summary>
        /// ２つのベクトルのなす角を返す（ラジアン）
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns>ラジアン</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Angle(float3 v1, float3 v2)
        {
            float len1 = math.length(v1);
            float len2 = math.length(v2);

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
            if (len > 1e-06f)
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
            if (len > 1e-06f && len > maxlength)
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
        public static bool ClampAngle(float3 dir, float3 basedir, float maxAngle, out float3 outdir)
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
        public static quaternion FromToRotation(float3 from, float3 to, float t = 1.0f)
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
        public static quaternion FromToRotation(quaternion from, quaternion to)
        {
            return math.mul(to, math.inverse(from));
        }

        /// <summary>
        /// クォータニオンの回転角度を返します（ラジアン）
        /// </summary>
        /// <param name="q"></param>
        /// <returns></returns>
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static float Angle(quaternion q)
        //{
        //    //float3 v1 = new float3(0, 0, 1);
        //    float3 v2 = math.forward(q);
        //    //float c = math.dot(v1, v2);
        //    float c = v2.z;
        //    float angle = math.acos(Clamp01(c));
        //    return angle;
        //}

        /// <summary>
        /// ２つのクォータニオンの角度を返します（ラジアン）
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Angle(quaternion a, quaternion b)
        {
            const float PI2 = math.PI * 2.0f;
            var ang = math.acos(Clamp1(math.dot(a, b))) * 2.0f; // x2.0が必要
            return ang > math.PI ? PI2 - ang : ang;
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

            float t = maxAngle / ang;

            return math.slerp(from, to, t);
        }

        /// <summary>
        /// 方向ベクトルをXY回転角度(ラジアン)に分離する、Z角度は常に０である
        /// </summary>
        /// <param name="axis"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 AxisToEuler(float3 axis)
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
        /// 与えられた線分abおよび点cに対して、ab上の最近接点t(0.0-1.0)を計算して返す
        /// </summary>
        /// <param name="c"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ClosestPtPointSegmentRatio(float3 c, float3 a, float3 b)
        {
            float3 ab = b - a;
            // パラメータ化されている位置d(t) = a + t * (b - a) の計算によりabにcを射影
            float t = math.dot(c - a, ab) / math.dot(ab, ab);
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
            float t = math.dot(c - a, ab) / math.dot(ab, ab);
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
            float t = math.dot(c - a, ab) / math.dot(ab, ab);
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
            float t = math.dot(c - a, ab) / math.dot(ab, ab);
            // クランプされているtからの射影されている位置を計算
            return a + t * ab;
        }

        /// <summary>
        /// 点と面の衝突判定
        /// 衝突した場合にその押し出し位置を計算して返す
        /// </summary>
        /// <param name="planePos"></param>
        /// <param name="planeDir"></param>
        /// <param name="pos"></param>
        /// <param name="outpos"></param>
        /// <returns>衝突があった場合はtrue</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IntersectPointPlane(float3 planePos, float3 planeDir, float3 pos, out float3 outpos)
        {
            float3 v = pos - planePos;
            if (math.dot(planeDir, v) < 0.0f)
            {
                // 押出しベクトル
                float3 gv = Project(v, planeDir);

                // 押出し座標
                outpos = pos - gv;

                return true;
            }
            else
            {
                outpos = pos;

                return false;
            }
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
        public static float IntersectPointPlaneDist(float3 planePos, float3 planeDir, float3 pos, out float3 outPos)
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
        /// 線分(ab)と面(p, pn)の衝突判定
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="p"></param>
        /// <param name="pn"></param>
        /// <param name="opos"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IntersectSegmentPlane(float3 a, float3 b, float3 p, float3 pn, out float3 opos)
        {
            // 方向のある直線abに対して平面と交差するtの値を計算
            var ab = b - a;
            float pd = math.dot(pn, p);
            float t = (pd - math.dot(pn, a)) / math.dot(pn, ab);
            // tが[0..1]の場合は交差
            if (t >= 0.0f && t <= 1.0f)
            {
                opos = a + t * ab;
                return true;
            }

            opos = 0;
            return false;
        }

        /// <summary>
        /// 球と点の衝突判定
        /// 衝突した場合は点を押し出す
        /// </summary>
        /// <param name="sc"></param>
        /// <param name="sr"></param>
        /// <param name="pos"></param>
        /// <param name="outPos"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IntersectPointSphere(float3 sc, float sr, float3 pos, out float3 outPos)
        {
            var v = pos - sc;
            var len = math.length(v);
            if (len < sr && len > 0.00001f)
            {
                outPos = pos + math.normalize(v) * (sr - len);
                return true;
            }
            else
            {
                outPos = pos;
                return false;
            }
        }

        /// <summary>
        /// 球と点の衝突判定
        /// </summary>
        /// <param name="p"></param>
        /// <param name="sc"></param>
        /// <param name="sr"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IntersectPointSphere(float3 p, float3 sc, float sr)
        {
            var v = p - sc;
            var slen = math.lengthsq(v);
            return slen <= sr * sr;
            //return math.distance(p, sc) <= sr;
        }

        /// <summary>
        /// 球と光線の衝突判定
        /// 光線 r = p + td,|d|=1 が球 s に対して交差しているかどうか。
        /// 交差している場合は交差点 q を返す
        /// dは単位ベクトルの必要があるので注意！
        /// </summary>
        /// <param name="p"></param>
        /// <param name="d"></param>
        /// <param name="sc"></param>
        /// <param name="sr"></param>
        /// <param name="q"></param>
        /// <returns></returns>
        public static bool IntersectRaySphere(float3 p, float3 d, float3 sc, float sr, out float3 q, out float t)
        {
            q = 0;
            t = 0;
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
            // これで光線は球と交差していることがわかり、交差する最小の値tを計算
            t = -b - math.sqrt(discr);
            // tが負である場合、光線は球の内側から開始しているのでtをゼロにクランプ
            if (t < 0)
                t = 0;
            q = p + t * d;
            return true;
        }

        /// <summary>
        /// 球と線分の衝突判定
        /// 線分(a->b)が球(s)に対して交差しているか判定する
        /// 交差している場合は交差点 q を返す
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="sc"></param>
        /// <param name="sr"></param>
        /// <param name="q"></param>
        /// <returns></returns>
        public static bool IntersectLineSphare(float3 a, float3 b, float3 sc, float sr, out float3 q)
        {
            var v = b - a;
            float vlen = math.length(v);

            // 線分ではなく点の場合は、点と球判定を行う
            if (vlen == 0)
            {
                q = a;
                return IntersectPointSphere(a, sc, sr);
            }
            float3 d = math.normalize(v);

            // 光線と球判定から計算
            float t;
            if (IntersectRaySphere(a, d, sc, sr, out q, out t))
            {
                float len = math.distance(a, q);
                if (len < vlen)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 円錐とレイの衝突判定
        /// </summary>
        /// <param name="o">レイの開始位置</param>
        /// <param name="d">レイの方向</param>
        /// <param name="c">円錐の先端位置</param>
        /// <param name="v">円錐の方向</param>
        /// <param name="cost">円錐のコサイン角度</param>
        /// <param name="t">交差点t</param>
        /// <param name="p">交差点座標</param>
        /// <returns></returns>
        public static bool IntersectRayCone(float3 o, float3 d, float3 c, float3 v, float cost, out float t, out float3 p)
        {
            p = 0;
            t = 0;

            //float cost = math.cos(ang);
            float cos2 = cost * cost;

            // a
            float dot_d_v = math.dot(d, v);
            float _a = dot_d_v * dot_d_v - cos2;

            // b
            float3 co = c - o;
            float dot_co_v = math.dot(co, v);
            float _b = 2.0f * (dot_d_v * dot_co_v - math.dot(d, co * cos2));

            // c
            float _c = (dot_co_v * dot_co_v) - math.dot(co, co * cos2);

            // delta
            float delta = _b * _b - 4.0f * _a * _c;

            if (delta < 0.0f)
            {
                // 交差しない
                return false;
            }
            else if (delta == 0.0f)
            {
                // １箇所で交差する
                t = -_b / (2.0f * _a);
                // どうも逆らしい
                t = -t;

                // 交差点と円錐からの内積
                p = o + d * t;
                float dot1 = math.dot(v, p - c); // ゴースト判定

                if (t < 0.0f || dot1 < 0.0f)
                    return false;
            }
            else
            {
                // ２箇所で交差する
                float sq = math.sqrt(delta);
                float t1 = (-_b - sq) / (2.0f * _a);
                float t2 = (-_b + sq) / (2.0f * _a);
                // どうも逆らしい
                t1 = -t1;
                t2 = -t2;

                // 交差点と円錐からの内積
                float3 p1 = o + d * t1;
                float3 p2 = o + d * t2;
                float dot1 = math.dot(v, p1 - c);
                float dot2 = math.dot(v, p2 - c);

                bool valid1 = t1 >= 0.0f && dot1 >= 0.0f; // ゴースト判定
                bool valid2 = t2 >= 0.0f && dot2 >= 0.0f; // ゴースト判定

                if (valid1 && valid2)
                {
                    // 近い方を選択
                    if (t1 < t2)
                    {
                        t = t1;
                        p = p1;
                    }
                    else
                    {
                        t = t2;
                        p = p2;
                    }
                }
                else if (valid1)
                {
                    t = t1;
                    p = p1;
                }
                else if (valid2)
                {
                    t = t2;
                    p = p2;
                }
                else
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 線分と円錐型シリンダ表面の衝突判定（底面は無視する）
        /// この判定は内部から外部への線分方向時に表面で衝突します
        /// </summary>
        /// <param name="a">線分始点</param>
        /// <param name="b">線分終点</param>
        /// <param name="d">線分方向（単位ベクトル）</param>
        /// <param name="dlen">線分の長さ</param>
        /// <param name="c">円錐の先端位置</param>
        /// <param name="v">円錐の方向（単位ベクトル）</param>
        /// <param name="cost">円錐のコサイン角度</param>
        /// <param name="c1">円錐の始点</param>
        /// <param name="c2">円錐の終点</param>
        /// <param name="p">衝突座標</param>
        /// <returns></returns>
        public static bool IntersectLineConeSurface(float3 a, float3 b, float3 d, float dlen, float3 c, float3 v, float cost, float3 c1, float3 c2, out float3 p)
        {
            p = 0;

            // 円錐とレイの交差判定を行う
            float t;
            if (IntersectRayCone(a, d, c, v, cost, out t, out p) == false)
            {
                // 交差しない
                return false;
            }

            // 交差点が線分に含まれるか判定する
            if (t > dlen)
            {
                // 交差しない
                return false;
            }

            // 交差点が円錐の始点と終点に含まれるか判定する
            // ClosestPtPointSegmentRatio()参照
            float3 cv = c2 - c1;
            float ct = math.dot(p - c1, cv) / math.dot(cv, cv);
            if (ct < 0.0f || ct > 1.0f)
            {
                // 交差しない
                return false;
            }

            return true;
        }

        /// <summary>
        /// 線分と円柱シリンダ表面の衝突判定（底面は無視する）
        /// この判定は内部から外部への線分方向時には衝突しません
        /// </summary>
        /// <param name="sa">線分始点</param>
        /// <param name="sb">線分終点</param>
        /// <param name="p">シリンダ始点</param>
        /// <param name="q">シリンダ終点</param>
        /// <param name="r">シリンダ半径</param>
        /// <param name="t">衝突点t</param>
        /// <returns></returns>
        public static bool IntersectLineCylinderSurface(float3 sa, float3 sb, float3 p, float3 q, float r, out float t)
        {
            t = 0;

            float3 d = q - p;
            float3 m = sa - p;
            float3 n = sb - sa;
            float md = math.dot(m, d);
            float nd = math.dot(n, d);
            float dd = math.dot(d, d);

            // 線分どちらかの円柱の底面に対して完全に外側にあるかどうかを判定
            if (md < 0.0f && md + nd < 0.0f)
            {
                // 線分が円柱pの側の外側にある
                return false;
            }
            if (md > dd && md + nd > dd)
            {
                // 線分が円柱qの側の外側にある
                return false;
            }

            float nn = math.dot(n, n);
            float mn = math.dot(m, n);
            float a = dd * nn - nd * nd;
            float k = math.dot(m, m) - r * r;
            float c = dd * k - md * md;

            if (math.abs(a) < 1e-6f)
            {
                // 線分が円柱の軸に対して平行に走っている
                return false;
            }

            float b = dd * mn - nd * md;
            float discr = b * b - a * c;
            if (discr < 0.0f)
            {
                // 実数解がないので交差はない
                return false;
            }
            t = (-b - math.sqrt(discr)) / a;
            if (t < 0.0f || t > 1.0f)
            {
                // 交差が線分の外側にある
                return false;
            }
            if (md + t * nd < 0.0f)
            {
                // 円柱のpの側の外側で交差
                if (nd <= 0.0f)
                {
                    // 線分は底面から離れる方向を指している
                    return false;
                }
                t = -md / nd;
                // Dot(S(t) - p, S(t) - p) <= r ^2 の場合、交差している
                return k + 2 * t * (mn + t * nn) <= 0.0f;
            }
            else if (md + t * nd > dd)
            {
                // 円柱のqの側の外側で交差
                if (nd >= 0.0f)
                {
                    // 線分は底面から離れる方向を指している
                    return false;
                }
                t = (dd - md) / nd;
                // Dot(S(t) - q, S(t) - q) <= r ^ 2 の場合、交差している
                return k + dd - 2 * md + t * (2 * (mn - nd) + t * nn) <= 0.0f;
            }

            return true;
        }

        /// <summary>
        /// 線分とシリンダ表面の交差判定
        /// </summary>
        /// <param name="a">線分の始点</param>
        /// <param name="b">線分の終点</param>
        /// <param name="d">線分の方向</param>
        /// <param name="c1">シリンダ始点</param>
        /// <param name="c2">シリンダ終点</param>
        /// <param name="r1">シリンダ始点半径</param>
        /// <param name="r2">シリンダ終点半径</param>
        /// <param name="p">衝突座標</param>
        public static bool IntersectLineCylinderSurface(float3 a, float3 b, float3 c1, float3 c2, float r1, float r2, out float3 p)
        {
            p = 0;

            float sa = math.abs(1.0f - r1 / r2); // 半径の割合で円錐判定
            if (sa > 0.001f)
            {
                // 円錐型

                // 線分の長さと方向
                float3 d = b - a;
                float dlen = math.length(d);
                d /= dlen;

                // 円錐の先端位置と方向、および円錐の角度を求める
                // ★これは予めデータ化可能
                float3 c;
                float3 v;
                float vlen = math.distance(c1, c2);
                float f = 0;
                float g = 0;
                float cost = 0;
                float len1, len2;
                if (r1 < r2)
                {
                    v = c2 - c1;
                    //v = math.normalize(v);
                    v /= vlen;

                    f = r2 - r1;
                    g = r1 / (f / vlen);
                    c = c1 - v * g; // 先端

                    len1 = vlen + g;
                    len2 = r2;

                }
                else
                {
                    v = c1 - c2;
                    //v = math.normalize(v);
                    v /= vlen;

                    f = r1 - r2;
                    g = r2 / (f / vlen);
                    c = c2 - v * g; // 先端

                    len1 = vlen + g;
                    len2 = r1;
                }

                // コサイン角度を求める
                float len3 = math.sqrt(len1 * len1 + len2 * len2);
                cost = len1 / len3;

                // c = 円錐の先端
                // v = 円錐の方向
                // cost = 円錐のコサイン角

                return IntersectLineConeSurface(a, b, d, dlen, c, v, cost, c1, c2, out p);
            }
            else
            {
                // 円柱型
                float t;
                bool ret = IntersectLineCylinderSurface(a, b, c1, c2, r1, out t);
                if (ret)
                {
                    p = math.lerp(a, b, t);
                    return true;
                }
            }

            return false;
        }


        /// <summary>
        /// 線分とカプセルの衝突判定
        /// </summary>
        /// <param name="a">線分始点</param>
        /// <param name="b">線分終点</param>
        /// <param name="c1">カプセル始点</param>
        /// <param name="c2">カプセル終点</param>
        /// <param name="r1">カプセル始点半径</param>
        /// <param name="r2">カプセル終点半径</param>
        /// <param name="p">衝突座標</param>
        /// <returns></returns>
        public static bool IntersectLineCapsule(float3 a, float3 b, float3 c1, float3 c2, float r1, float r2, out float3 p)
        {
            p = a;

            // すでに開始点がカプセルの内部ならば衝突として返す
            float t = ClosestPtPointSegmentRatio(a, c1, c2);
            float dist = math.distance(a, math.lerp(c1, c2, t));
            float r = math.lerp(r1, r2, t);
            if (dist <= r)
            {
                return true;
            }

            // カプセル左右の半球と交差判定
            float3 v = c2 - c1;
            if (IntersectLineSphare(a, b, c1, r1, out p))
            {
                // 半球判定
                if (math.dot(v, p - c1) <= 0.0f)
                {
                    return true;
                }
            }
            if (IntersectLineSphare(a, b, c2, r2, out p))
            {
                // 半球判定
                if (math.dot(v, p - c2) >= 0.0f)
                {
                    return true;
                }
            }

            // カプセル表面（円柱型／円錐型）と交差判定
            return IntersectLineCylinderSurface(a, b, c1, c2, r1, r2, out p);
        }

        /// <summary>
        /// 点とトライアングルの最近接点を調べ、その距離がrestDist以下場合は、双方を非接触距離まで引き離す
        /// これは球（座標:p, 半径:restDist)とトライアングルの接触判定と意味は同じである
        /// compressionStiffnessとstretchStiffnessは接触時にその引き離す割合を指定する
        /// 1.0ならば１回で非接触距離まで離れる、0.0ならば動かない
        /// </summary>
        /// <param name="p">点の座標</param>
        /// <param name="p0">トライアングル座標0</param>
        /// <param name="p1">トライアングル座標1</param>
        /// <param name="p2">トライアングル座標2</param>
        /// <param name="restDist">点とトライアングルを引き離す距離</param>
        /// <param name="compressionStiffness">基本1.0(0.0 - 1.0)</param>
        /// <param name="stretchStiffness">基本1.0(0.0 - 1.0)</param>
        /// <param name="corr">pの押し出しベクトル</param>
        /// <param name="corr0">p0の押し出しベクトル</param>
        /// <param name="corr1">p1の押し出しベクトル</param>
        /// <param name="corr2">p2の押し出しベクトル</param>
        /// <returns></returns>
        public static bool IntersectTrianglePointDistance(
            float3 p, float3 p0, float3 p1, float3 p2,
            float restDist, float compressionStiffness, float stretchStiffness,
            out float3 corr, out float3 corr0, out float3 corr1, out float3 corr2
            )
        {
            corr = 0;
            corr0 = 0;
            corr1 = 0;
            corr2 = 0;

            // find barycentric coordinates of closest point on triangle

            float b0 = 1.0f / 3.0f;        // for singular case
            float b1 = b0;
            float b2 = b0;

            float3 d1 = p1 - p0;
            float3 d2 = p2 - p0;
            float3 pp0 = p - p0;
            float a = math.dot(d1, d1);
            float b = math.dot(d2, d1);
            float c = math.dot(pp0, d1);
            float d = b;
            float e = math.dot(d2, d2);
            float f = math.dot(pp0, d2);
            float det = a * e - b * d;

            //Debug.Log("det->" + det);

            if (det != 0.0f)
            {
                float s2 = (c * e - b * f) / det;
                float t = (a * f - c * d) / det;
                b0 = 1.0f - s2 - t;       // inside triangle
                b1 = s2;
                b2 = t;
                if (b0 < 0.0f)
                {
                    // on edge 1-2
                    float3 dv = p2 - p1;
                    float dv2 = math.dot(dv, dv);
                    t = (dv2 == 0.0f) ? 0.5f : math.dot(dv, p - p1) / dv2;
                    if (t < 0.0f) t = 0.0f;   // on point 1
                    if (t > 1.0f) t = 1.0f;   // on point 2
                    b0 = 0.0f;
                    b1 = (1.0f - t);
                    b2 = t;
                }
                else if (b1 < 0.0f)
                {
                    // on edge 2-0
                    float3 dv = p0 - p2;
                    float dv2 = math.dot(dv, dv);
                    t = (dv2 == 0.0f) ? 0.5f : math.dot(dv, p - p2) / dv2;
                    if (t < 0.0f) t = 0.0f;   // on point 2
                    if (t > 1.0f) t = 1.0f; // on point 0
                    b1 = 0.0f;
                    b2 = (1.0f - t);
                    b0 = t;
                }
                else if (b2 < 0.0f)
                {
                    // on edge 0-1
                    float3 dv = p1 - p0;
                    float dv2 = math.dot(dv, dv);
                    t = (dv2 == 0.0f) ? 0.5f : math.dot(dv, p - p0) / dv2;
                    if (t < 0.0f) t = 0.0f;   // on point 0
                    if (t > 1.0f) t = 1.0f;   // on point 1
                    b2 = 0.0f;
                    b0 = (1.0f - t);
                    b1 = t;
                }
            }
            float3 q = p0 * b0 + p1 * b1 + p2 * b2;
            float3 n = p - q;
            float dist = math.length(n);
            //Debug.Log("dist->" + dist);

            // ここはオリジナル
            // distが点とトライアングルの最近接点距離を表すので、触れていななら終了させる
            if (dist > restDist)
                return false;

            n = math.normalize(n);
            float C = dist - restDist;
            float3 grad = n;
            float3 grad0 = -n * b0;
            float3 grad1 = -n * b1;
            float3 grad2 = -n * b2;

            float s = 1 + b0 * b0 + b1 * b1 + b2 * b2;
            if (s == 0.0f)
                return false;

            s = C / s;
            if (C < 0.0f)

                s *= compressionStiffness;
            else
                s *= stretchStiffness;

            if (s == 0.0f)
                return false;

            corr = -s * grad;
            corr0 = -s * grad0;
            corr1 = -s * grad1;
            corr2 = -s * grad2;

            return true;
        }

        /// <summary>
        /// 点とトライアングルの最近接点を調べ、その距離がrestDist以下場合は、双方を非接触距離まで引き離す
        /// これは球（座標:p, 半径:restDist)とトライアングルの接触判定と意味は同じである
        /// compressionStiffnessとstretchStiffnessは接触時にその引き離す割合を指定する
        /// 1.0ならば１回で非接触距離まで離れる、0.0ならば動かない
        /// このバージョンはトライアングル法線の一定方向に押し出す
        /// 押し出すトライアングル法線方向は side で指定する
        /// </summary>
        /// <param name="p">点の座標</param>
        /// <param name="p0">トライアングル座標0</param>
        /// <param name="p1">トライアングル座標1</param>
        /// <param name="p2">トライアングル座標2</param>
        /// <param name="restDist">点とトライアングルを引き離す距離</param>
        /// <param name="compressionStiffness">基本1.0(0.0 - 1.0)</param>
        /// <param name="stretchStiffness">基本1.0(0.0 - 1.0)</param>
        /// <param name="side">押し出すトライアングル法線方向(1.0 / -1.0)</param>
        /// <param name="corr">pの押し出しベクトル</param>
        /// <param name="corr0">p0の押し出しベクトル</param>
        /// <param name="corr1">p1の押し出しベクトル</param>
        /// <param name="corr2">p2の押し出しベクトル</param>
        /// <returns></returns>
        public static bool IntersectTrianglePointDistanceSide(
            float3 p, float3 p0, float3 p1, float3 p2,
            float restDist, float compressionStiffness, float stretchStiffness, float side,
            out float3 corr, out float3 corr0, out float3 corr1, out float3 corr2
            )
        {
            corr = 0;
            corr0 = 0;
            corr1 = 0;
            corr2 = 0;

            // find barycentric coordinates of closest point on triangle

            float b0 = 1.0f / 3.0f;        // for singular case
            float b1 = b0;
            float b2 = b0;

            float3 d1 = p1 - p0;
            float3 d2 = p2 - p0;
            float3 pp0 = p - p0;
            float a = math.dot(d1, d1);
            float b = math.dot(d2, d1);
            float c = math.dot(pp0, d1);
            float d = b;
            float e = math.dot(d2, d2);
            float f = math.dot(pp0, d2);
            float det = a * e - b * d;

            //Debug.Log("det->" + det);

            if (det != 0.0f)
            {
                float s2 = (c * e - b * f) / det;
                float t = (a * f - c * d) / det;
                b0 = 1.0f - s2 - t;       // inside triangle
                b1 = s2;
                b2 = t;
                if (b0 < 0.0f)
                {
                    // on edge 1-2
                    float3 dv = p2 - p1;
                    float dv2 = math.dot(dv, dv);
                    t = (dv2 == 0.0f) ? 0.5f : math.dot(dv, p - p1) / dv2;
                    if (t < 0.0f) t = 0.0f;   // on point 1
                    if (t > 1.0f) t = 1.0f;   // on point 2
                    b0 = 0.0f;
                    b1 = (1.0f - t);
                    b2 = t;
                }
                else if (b1 < 0.0f)
                {
                    // on edge 2-0
                    float3 dv = p0 - p2;
                    float dv2 = math.dot(dv, dv);
                    t = (dv2 == 0.0f) ? 0.5f : math.dot(dv, p - p2) / dv2;
                    if (t < 0.0f) t = 0.0f;   // on point 2
                    if (t > 1.0f) t = 1.0f; // on point 0
                    b1 = 0.0f;
                    b2 = (1.0f - t);
                    b0 = t;
                }
                else if (b2 < 0.0f)
                {
                    // on edge 0-1
                    float3 dv = p1 - p0;
                    float dv2 = math.dot(dv, dv);
                    t = (dv2 == 0.0f) ? 0.5f : math.dot(dv, p - p0) / dv2;
                    if (t < 0.0f) t = 0.0f;   // on point 0
                    if (t > 1.0f) t = 1.0f;   // on point 1
                    b2 = 0.0f;
                    b0 = (1.0f - t);
                    b1 = t;
                }
            }
            float3 q = p0 * b0 + p1 * b1 + p2 * b2;
            float3 n = p - q;
            float dist = math.length(n);
            //Debug.Log("dist->" + dist);

            // ここはオリジナル
            // distが点とトライアングルの最近接点距離を表すので、触れていななら終了させる
            if (dist > restDist)
                return false;

            // 押し出し方向を常に side で指定されたトライアングル方向に固定する
            float3 tnor = math.cross(d1, d2) * side;
            float C = dist - restDist;
            if (math.dot(tnor, n) < 0.0f)
            {
                n = -n;
                //C = -(restDist + dist);
            }

            n = math.normalize(n);
            float3 grad = n;
            float3 grad0 = -n * b0;
            float3 grad1 = -n * b1;
            float3 grad2 = -n * b2;

            float s = 1 + b0 * b0 + b1 * b1 + b2 * b2;
            if (s == 0.0f)
                return false;

            s = C / s;
            if (C < 0.0f)

                s *= compressionStiffness;
            else
                s *= stretchStiffness;

            if (s == 0.0f)
                return false;

            corr = -s * grad;
            corr0 = -s * grad0;
            corr1 = -s * grad1;
            corr2 = -s * grad2;

            return true;
        }

        public static bool IntersectTrianglePointDistanceSide2(
            float3 p, float3 p0, float3 p1, float3 p2,
            float radius, float restDist, float compressionStiffness, float stretchStiffness, float side,
            out float3 corr, out float3 corr0, out float3 corr1, out float3 corr2
            )
        {
            corr = 0;
            corr0 = 0;
            corr1 = 0;
            corr2 = 0;

            // find barycentric coordinates of closest point on triangle

            float b0 = 1.0f / 3.0f;        // for singular case
            float b1 = b0;
            float b2 = b0;

            float3 d1 = p1 - p0;
            float3 d2 = p2 - p0;
            float3 pp0 = p - p0;
            float a = math.dot(d1, d1);
            float b = math.dot(d2, d1);
            float c = math.dot(pp0, d1);
            float d = b;
            float e = math.dot(d2, d2);
            float f = math.dot(pp0, d2);
            float det = a * e - b * d;

            //Debug.Log("det->" + det);

            if (det != 0.0f)
            {
                float s2 = (c * e - b * f) / det;
                float t = (a * f - c * d) / det;
                b0 = 1.0f - s2 - t;       // inside triangle
                b1 = s2;
                b2 = t;
                if (b0 < 0.0f)
                {
                    // on edge 1-2
                    float3 dv = p2 - p1;
                    float dv2 = math.dot(dv, dv);
                    t = (dv2 == 0.0f) ? 0.5f : math.dot(dv, p - p1) / dv2;
                    if (t < 0.0f) t = 0.0f;   // on point 1
                    if (t > 1.0f) t = 1.0f;   // on point 2
                    b0 = 0.0f;
                    b1 = (1.0f - t);
                    b2 = t;
                }
                else if (b1 < 0.0f)
                {
                    // on edge 2-0
                    float3 dv = p0 - p2;
                    float dv2 = math.dot(dv, dv);
                    t = (dv2 == 0.0f) ? 0.5f : math.dot(dv, p - p2) / dv2;
                    if (t < 0.0f) t = 0.0f;   // on point 2
                    if (t > 1.0f) t = 1.0f; // on point 0
                    b1 = 0.0f;
                    b2 = (1.0f - t);
                    b0 = t;
                }
                else if (b2 < 0.0f)
                {
                    // on edge 0-1
                    float3 dv = p1 - p0;
                    float dv2 = math.dot(dv, dv);
                    t = (dv2 == 0.0f) ? 0.5f : math.dot(dv, p - p0) / dv2;
                    if (t < 0.0f) t = 0.0f;   // on point 0
                    if (t > 1.0f) t = 1.0f;   // on point 1
                    b2 = 0.0f;
                    b0 = (1.0f - t);
                    b1 = t;
                }
            }
            float3 q = p0 * b0 + p1 * b1 + p2 * b2;
            float3 n = p - q;
            float dist = math.length(n);
            //Debug.Log("dist->" + dist);

            // ここはオリジナル
            // distが点とトライアングルの最近接点距離を表すので、触れていななら終了させる
            if (dist > restDist)
                return false;

            // 押し出し方向を常に side で指定されたトライアングル方向に固定する
            float3 tnor = math.cross(d1, d2) * side;
            //float C = dist - radius;
            float C = dist - restDist;
            //if (math.dot(tnor, n) < 0.0f)
            //{
            //    //n = -n;
            //    //C = -(radius + dist);
            //}
            //else if (dist > radius)
            //{
            //    return false;
            //}

            n = math.normalize(n);
            float3 grad = n;
            float3 grad0 = -n * b0;
            float3 grad1 = -n * b1;
            float3 grad2 = -n * b2;

            float s = 1 + b0 * b0 + b1 * b1 + b2 * b2;
            if (s == 0.0f)
                return false;

            s = C / s;
            if (C < 0.0f)

                s *= compressionStiffness;
            else
                s *= stretchStiffness;

            if (s == 0.0f)
                return false;

            //if (math.dot(tnor, n) < 0.0f)
            //{
            //    grad = -grad;
            //    grad0 = -grad0;
            //    grad1 = -grad1;
            //    grad2 = -grad2;
            //}


            corr = -s * grad;
            corr0 = -s * grad0;
            corr1 = -s * grad1;
            corr2 = -s * grad2;

            return true;
        }

        //public static float IntersectTrianglePoint(float3 p, float3 p0, float3 p1, float3 p2, float radius)
        public static float DistanceTrianglePoint(float3 p, float3 p0, float3 p1, float3 p2)
        {
            // find barycentric coordinates of closest point on triangle

            float b0 = 1.0f / 3.0f;        // for singular case
            float b1 = b0;
            float b2 = b0;

            float3 d1 = p1 - p0;
            float3 d2 = p2 - p0;
            float3 pp0 = p - p0;
            float a = math.dot(d1, d1);
            float b = math.dot(d2, d1);
            float c = math.dot(pp0, d1);
            float d = b;
            float e = math.dot(d2, d2);
            float f = math.dot(pp0, d2);
            float det = a * e - b * d;

            if (det != 0.0f)
            {
                float s2 = (c * e - b * f) / det;
                float t = (a * f - c * d) / det;
                b0 = 1.0f - s2 - t;       // inside triangle
                b1 = s2;
                b2 = t;
                if (b0 < 0.0f)
                {
                    // on edge 1-2
                    float3 dv = p2 - p1;
                    float dv2 = math.dot(dv, dv);
                    t = (dv2 == 0.0f) ? 0.5f : math.dot(dv, p - p1) / dv2;
                    if (t < 0.0f) t = 0.0f;   // on point 1
                    if (t > 1.0f) t = 1.0f;   // on point 2
                    b0 = 0.0f;
                    b1 = (1.0f - t);
                    b2 = t;
                }
                else if (b1 < 0.0f)
                {
                    // on edge 2-0
                    float3 dv = p0 - p2;
                    float dv2 = math.dot(dv, dv);
                    t = (dv2 == 0.0f) ? 0.5f : math.dot(dv, p - p2) / dv2;
                    if (t < 0.0f) t = 0.0f;   // on point 2
                    if (t > 1.0f) t = 1.0f; // on point 0
                    b1 = 0.0f;
                    b2 = (1.0f - t);
                    b0 = t;
                }
                else if (b2 < 0.0f)
                {
                    // on edge 0-1
                    float3 dv = p1 - p0;
                    float dv2 = math.dot(dv, dv);
                    t = (dv2 == 0.0f) ? 0.5f : math.dot(dv, p - p0) / dv2;
                    if (t < 0.0f) t = 0.0f;   // on point 0
                    if (t > 1.0f) t = 1.0f;   // on point 1
                    b2 = 0.0f;
                    b0 = (1.0f - t);
                    b1 = t;
                }
            }
            float3 q = p0 * b0 + p1 * b1 + p2 * b2;
            float3 n = p - q;
            float dist = math.length(n);

            // ここはオリジナル
            // distが点とトライアングルの最近接点距離を表すので、触れていななら終了させる
            //return dist <= radius;
            return dist;
        }

        /// <summary>
        /// トライアングルの重心を返す
        /// </summary>
        /// <param name="p0"></param>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 TriangleCenter(float3 p0, float3 p1, float3 p2)
        {
            return (p0 + p1 + p2) / 3.0f;
        }

        /// <summary>
        /// トライアングルの法線を返す
        /// </summary>
        /// <param name="p0"></param>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 TriangleNormal(float3 p0, float3 p1, float3 p2)
        {
            return math.normalize(math.cross(p1 - p0, p2 - p0));
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
        /// 与えられた線分pqおよび三角形abcに対して、線分が三角形と交差するか判定する
        /// 交差している場合は交差点hitposとtを返す
        /// ・三角形の法線方向からの侵入のみ判定
        /// ・線分の長さが0の場合は判定しない
        /// </summary>
        /// <param name="p"></param>
        /// <param name="q"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <param name="hitpos"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static bool IntersectLineTriangle(float3 p, float3 q, float3 a, float3 b, float3 c, out float3 hitpos, out float t, out float3 n)
        {
            hitpos = 0;
            t = 0.0f;

            var ab = b - a;
            var ac = c - a;
            var qp = p - q;

            //float3 n = math.cross(ab, ac);
            n = math.cross(ab, ac);

            float d = math.dot(qp, n);
            if (d <= 0.0f)
                return false;

            var ap = p - a;
            t = math.dot(ap, n);
            if (t < 0.0f)
                return false;
            if (t > d)
                return false;

            float3 e = math.cross(qp, ap);
            float v = math.dot(ac, e);
            if (v < 0.0f || v > d)
                return false;
            float w = -math.dot(ab, e);
            if (w < 0.0f || (v + w) > d)
                return false;

            float ood = 1.0f / d;
            t *= ood;
            v *= ood;
            w *= ood;
            float u = 1.0f - v - w;
            //uvw = new float3(u, v, w);
            hitpos = a * u + b * v + c * w;

            return true;
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
        public static float ClosestPtSegmentSegment(float3 p1, float3 q1, float3 p2, float3 q2, out float s, out float t, out float3 c1, out float3 c2)
        {
            s = 0.0f;
            t = 0.0f;

            float3 d1 = q1 - p1; // 線分s1の方向ベクトル
            float3 d2 = q2 - p2; // 線分s2の方向ベクトル
            float3 r = p1 - p2;
            float a = math.dot(d1, d1); // 線分s1の距離の平方、常に正
            float e = math.dot(d2, d2); // 線分s2の距離の平方、常に正
            float f = math.dot(d2, r);
            // 片方あるいは両方の線分が点に縮退しているかどうかチェック
            if (a <= 1e-6f && e <= 1e-6f)
            {
                // 両方の線分が点に縮退
                s = t = 0.0f;
                c1 = p1;
                c2 = p2;
                return math.dot(c1 - c2, c1 - c2);
            }
            if (a <= 1e-6f)
            {
                // 最初の線分が点に縮退
                s = 0.0f;
                t = math.saturate(f / e);
            }
            else
            {
                float c = math.dot(d1, r);
                if (e <= 1e-6f)
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

        //=========================================================================================
        /// <summary>
        /// ベジェ曲線から横軸位置(t=0.0～1.0)の縦軸値を取得する
        /// </summary>
        /// <param name="bparam">曲線パラメータ</param>
        /// <param name="t">横軸位置(0.0～1.0)</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetBezierValue(BezierParam bparam, float t)
        {
            return GetBezierValue(bparam.StartValue, bparam.EndValue, bparam.CurveValue, t);
        }

        /// <summary>
        /// ２次ベジェ曲線から横軸位置(t=0.0～1.0)の縦軸値を取得する
        /// </summary>
        /// <param name="sval">開始値</param>
        /// <param name="eval">終了値</param>
        /// <param name="curve">カーブ率(-1.0～+1.0), 0.0ならば直線</param>
        /// <param name="posx">横軸位置(0.0～1.0)</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetBezierValue(float sval, float eval, float curve, float t)
        {
            if (curve == 0.0f)
            {
                // 線形補間
                return math.lerp(sval, eval, t);
            }
            else
            {
                // ２次ベジェ曲線
                // 制御点
                float cval = math.lerp(eval, sval, curve * 0.5f + 0.5f);

                //float x = (1.0f - t) * (1.0f - t) * p[0].x + 2 * (1.0f - t) * t * p[1].x + t * t * p[2].x;
                //float y = (1.0f - t) * (1.0f - t) * p[0].y + 2 * (1.0f - t) * t * p[1].y + t * t * p[2].y;

                float w = 1.0f - t;
                float y = w * w * sval + 2 * w * t * cval + t * t * eval;

                return y;
            }
        }
    }
}
