// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using UnityEngine;

namespace MagicaCloth
{
    public static class GizmoUtility
    {
        // ギズモカラー定義
        public static readonly Color ColorDynamic = new Color(1.0f, 1.0f, 1.0f);
        public static readonly Color ColorKinematic = new Color(1.0f, 1.0f, 0.0f);
        public static readonly Color ColorInvalid = new Color(0.5f, 0.5f, 0.5f);
        public static readonly Color ColorCollider = new Color(0.0f, 1.0f, 0.0f);
        public static readonly Color ColorNonSelectedCollider = new Color(0.5f, 0.3f, 0.0f);
        public static readonly Color ColorTriangle = new Color(1.0f, 0.0f, 1.0f);
        public static readonly Color ColorStructLine = new Color(0.0f, 1.0f, 1.0f);
        public static readonly Color ColorBendLine = new Color(0.0f, 0.5f, 1.0f);
        public static readonly Color ColorNearLine = new Color(0.55f, 0.5f, 0.7f);
        public static readonly Color ColorRotationLine = new Color(1.0f, 0.65f, 0.0f);
        public static readonly Color ColorAdjustLine = new Color(1.0f, 1.0f, 0.0f);
        public static readonly Color ColorAirLine = new Color(0.55f, 0.5f, 0.7f);
        public static readonly Color ColorBasePosition = new Color(1.0f, 0.0f, 0.0f);
        public static readonly Color ColorDirectionMoveLimit = new Color(0.0f, 1.0f, 1.0f);
        public static readonly Color ColorPenetration = new Color(1.0f, 0.3f, 0.0f);
        public static readonly Color ColorCollisionNormal = new Color(0.6f, 0.2f, 1.0f);
        public static readonly Color ColorVelocity = new Color(1.0f, 0.6f, 0.2f);
        public static readonly Color ColorSkinningBone = new Color(1.0f, 0.5f, 0.0f);

        public static readonly Color ColorDeformerPoint = new Color(1.0f, 1.0f, 1.0f);
        public static readonly Color ColorDeformerPointRange = new Color(0.5f, 0.2f, 0.0f);

        public static readonly Color ColorWind = new Color(0.55f, 0.592f, 0.796f);

        /// <summary>
        /// ワイヤーカプセルを描画する
        /// </summary>
        /// <param name="pos">基準座標</param>
        /// <param name="rot">基準回転</param>
        /// <param name="ldir">カプセルの方向</param>
        /// <param name="lup">カプセルの上方向</param>
        /// <param name="length">カプセルの長さ（片側）</param>
        /// <param name="startRadius">始点の半径</param>
        /// <param name="endRadius">終点の半径</param>
        public static void DrawWireCapsule(
            Vector3 pos, Quaternion rot, Vector3 scl,
            Vector3 ldir, Vector3 lup,
            float length, float startRadius, float endRadius,
            bool resetMatrix = true
            )
        {
            //Gizmos.matrix = Matrix4x4.TRS(pos, rot, Vector3.one);
            Gizmos.matrix = Matrix4x4.TRS(pos, rot, scl);
            var l = ldir * length;
            Gizmos.DrawWireSphere(-l, startRadius);
            Gizmos.DrawWireSphere(l, endRadius);

            for (int i = 0; i < 360; i += 45)
            {
                var q = Quaternion.AngleAxis(i, ldir);
                var up1 = q * (lup * startRadius);
                var up2 = q * (lup * endRadius);
                Gizmos.DrawLine(-l + up1, l + up2);
            }

            // 45度ずらしてもう１回球を描く
            Gizmos.matrix = Matrix4x4.TRS(pos, rot * Quaternion.AngleAxis(45, ldir), scl);
            Gizmos.DrawWireSphere(-l, startRadius);
            Gizmos.DrawWireSphere(l, endRadius);

            if (resetMatrix)
                Gizmos.matrix = Matrix4x4.identity;
        }

        /// <summary>
        /// ワイヤー球を描画する
        /// </summary>
        /// <param name="pos">基準座標</param>
        /// <param name="rot">基準回転</param>
        /// <param name="radius">半径</param>
        /// <param name="resetMatrix"></param>
        public static void DrawWireSphere(
            Vector3 pos, Quaternion rot, Vector3 scl, float radius,
            bool drawSphere, bool drawAxis,
            bool resetMatrix = true)
        {
            //Gizmos.matrix = Matrix4x4.TRS(pos, rot, Vector3.one);
            Gizmos.matrix = Matrix4x4.TRS(pos, rot, scl);

            // 球
            if (drawSphere)
                Gizmos.DrawWireSphere(Vector3.zero, radius);

            // 軸
            if (drawAxis)
            {
                const float axisRadius = 0.03f;
                Gizmos.color = Color.red;
                Gizmos.DrawLine(Vector3.zero, Vector3.right * axisRadius);
                Gizmos.color = Color.green;
                Gizmos.DrawLine(Vector3.zero, Vector3.up * axisRadius);
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(Vector3.zero, Vector3.forward * axisRadius);
            }

            // 45度ずらしてもう１回球を描く
            //Gizmos.matrix = Matrix4x4.TRS(pos, rot * Quaternion.AngleAxis(45, Vector3.up), Vector3.one);
            //Gizmos.DrawWireSphere(Vector3.zero, radius);

            if (resetMatrix)
                Gizmos.matrix = Matrix4x4.identity;
        }

        /// <summary>
        /// ワイヤーボックスを描画する
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="rot"></param>
        /// <param name="size"></param>
        /// <param name="resetMatrix"></param>
        public static void DrawWireCube(Vector3 pos, Quaternion rot, Vector3 size, bool resetMatrix = true)
        {
            Gizmos.matrix = Matrix4x4.TRS(pos, rot, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, size);
            if (resetMatrix)
                Gizmos.matrix = Matrix4x4.identity;
        }

        public static void DrawWireCone(Vector3 pos, Quaternion rot, float length, float radius, int div = 8)
        {
            Gizmos.matrix = Matrix4x4.TRS(pos, rot, Vector3.one);
            var epos = Vector3.forward * length;
            Vector3 oldpos = epos;
            for (int i = 0; i < div; i++)
            {
                float t = (float)i / (float)div;
                var q = Quaternion.AngleAxis(t * 360.0f, Vector3.forward);
                var x = q * Vector3.right * radius;
                Gizmos.DrawLine(Vector3.zero, epos + x);
                Gizmos.DrawLine(epos, epos + x);

                if (i > 0)
                    Gizmos.DrawLine(oldpos, epos + x);

                oldpos = epos + x;
            }

            Gizmos.DrawLine(oldpos, epos + Vector3.right * radius);


            Gizmos.matrix = Matrix4x4.identity;
        }

        /// <summary>
        /// ワイヤー矢印を描画する
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="rot"></param>
        /// <param name="size"></param>
        /// <param name="cross">十字描画</param>
        public static void DrawWireArrow(Vector3 pos, Quaternion rot, Vector3 size, bool cross = false)
        {
            Gizmos.matrix = Matrix4x4.TRS(pos, rot, size);

            Vector3[] points = new Vector3[]
            {
                new Vector3(0.0f, 0.0f, -1.0f),
                new Vector3(0.0f, 0.5f, -1.0f),
                new Vector3(0.0f, 0.5f, 0.0f),
                new Vector3(0.0f, 1.0f, 0.0f),
                new Vector3(0.0f, 0.0f, 1.0f),
            };

            float addAngle = cross ? 90.0f : 180.0f;
            int loop = cross ? 4 : 2;

            for (int j = 0; j < loop; j++)
            {
                for (int i = 0; i < points.Length - 1; i++)
                {
                    Gizmos.DrawLine(points[i], points[i + 1]);
                }

                rot = rot * Quaternion.AngleAxis(addAngle, Vector3.forward);
                Gizmos.matrix = Matrix4x4.TRS(pos, rot, size);
            }

            Gizmos.matrix = Matrix4x4.identity;
        }

        /// <summary>
        /// XYZ軸を描画する
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="rot"></param>
        /// <param name="size"></param>
        /// <param name="resetMatrix"></param>
        public static void DrawAxis(Vector3 pos, Quaternion rot, float size, bool resetMatrix = true)
        {
            Gizmos.matrix = Matrix4x4.TRS(pos, rot, Vector3.one);
            Gizmos.color = Color.red;
            Gizmos.DrawRay(Vector3.zero, Vector3.right * size);
            Gizmos.color = Color.green;
            Gizmos.DrawRay(Vector3.zero, Vector3.up * size);
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(Vector3.zero, Vector3.forward * size);
            if (resetMatrix)
                Gizmos.matrix = Matrix4x4.identity;
        }

        /// <summary>
        /// ボーン形状を描画する
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="tpos"></param>
        /// <param name="size"></param>
        public static void DrawBone(Vector3 pos, Vector3 tpos, float size)
        {
            var v = tpos - pos;
            var rot = Quaternion.FromToRotation(Vector3.forward, v);

            Gizmos.matrix = Matrix4x4.TRS(pos, rot, Vector3.one);
            Gizmos.color = ColorSkinningBone;

            Gizmos.DrawWireSphere(Vector3.zero, size);

            //Gizmos.DrawLine(Vector3.zero, Vector3.forward * v.magnitude);
            float bsize = size * 0.8f;
            float zoff = size;
            var gpos = Vector3.forward * v.magnitude;
            var p0 = new Vector3(bsize, bsize, zoff);
            var p1 = new Vector3(bsize, -bsize, zoff);
            var p2 = new Vector3(-bsize, -bsize, zoff);
            var p3 = new Vector3(-bsize, bsize, zoff);

            Gizmos.DrawLine(p0, gpos);
            Gizmos.DrawLine(p1, gpos);
            Gizmos.DrawLine(p2, gpos);
            Gizmos.DrawLine(p3, gpos);

            Gizmos.DrawLine(p0, p1);
            Gizmos.DrawLine(p1, p2);
            Gizmos.DrawLine(p2, p3);
            Gizmos.DrawLine(p3, p0);

            Gizmos.matrix = Matrix4x4.identity;
        }
    }
}
