// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using System.Collections.Generic;
using UnityEngine;

namespace MagicaReductionMesh
{
    /// <summary>
    /// ポリゴンの接続に沿ったリダクション
    /// </summary>
    public class PolygonLinkReduction
    {
        protected MeshData meshData;

        private float reductionLength;

        /// <summary>
        /// 頂点データ
        /// </summary>
        public class Point
        {
            public MeshData.ShareVertex shareVertex;

            /// <summary>
            /// 現在の最近点のポイント(null=なし)
            /// </summary>
            public Point nearPoint;

            /// <summary>
            /// 現在の最近点ポイントまでの距離
            /// </summary>
            public float nearDist;

        }

        /// <summary>
        /// 頂点データリスト
        /// </summary>
        List<Point> pointList = new List<Point>();

        /// <summary>
        /// 共有頂点からの逆引き辞書
        /// </summary>
        Dictionary<MeshData.ShareVertex, Point> pointDict = new Dictionary<MeshData.ShareVertex, Point>();

        //=========================================================================================
        public PolygonLinkReduction(float length)
        {
            reductionLength = length;
        }

        public int PointCount
        {
            get
            {
                return pointList.Count;
            }
        }

        //=========================================================================================
        /// <summary>
        /// リダクションデータをメッシュデータから構築する
        /// </summary>
        /// <param name="meshData"></param>
        public void Create(MeshData meshData)
        {
            this.meshData = meshData;

            foreach (var sv in meshData.shareVertexList)
            {
                AddPoint(sv);
            }

            // すべてのの最近点を求める
            SearchNearPointAll();
        }

        /// <summary>
        /// リダクション実行
        /// </summary>
        public void Reduction()
        {
            Point p0 = null;
            var nlist = new List<Point>();
            while ((p0 = GetNearPointPair()) != null)
            {
                // p0にp1をマージする
                var p1 = p0.nearPoint;
                Debug.Assert(p1 != null);

                var sv0 = p0.shareVertex;
                var sv1 = p1.shareVertex;

                // この２つの頂点を最近点として参照しているリスト
                nlist.Clear();
                foreach (var sv in sv0.linkShareVertexSet)
                    nlist.Add(pointDict[sv]);
                foreach (var sv in sv1.linkShareVertexSet)
                    nlist.Add(pointDict[sv]);
                nlist.Add(p0); // p0も追加する

                // 最近点の参照を切る
                foreach (var np in nlist)
                {
                    np.nearPoint = null;
                    np.nearDist = 100000;
                }

                // p1を削除
                Remove(p1);
                p1 = null;

                // sv1にsv2をマージ
                meshData.CombineVertex(sv0, sv1);

                // p0/p1を指していたポイントに対して最近点を再計算する
                foreach (var np in nlist)
                {
                    SearchNearPoint(np);
                }
            }
        }

        //=========================================================================================
        void AddPoint(MeshData.ShareVertex sv)
        {
            var p = new Point();
            p.shareVertex = sv;
            pointList.Add(p);
            pointDict.Add(sv, p);
        }

        Point GetPoint(MeshData.ShareVertex sv)
        {
            if (pointDict.ContainsKey(sv))
                return pointDict[sv];
            return null;
        }

        /// <summary>
        /// 頂点をグリッドから削除する
        /// </summary>
        /// <param name="p"></param>
        void Remove(Point p)
        {
            // データ削除
            pointDict.Remove(p.shareVertex);
            pointList.Remove(p);
        }

        //=========================================================================================
        /// <summary>
        /// すべての共有頂点の最近接続頂点を調べる
        /// </summary>
        void SearchNearPointAll()
        {
            foreach (var p in pointList)
            {
                SearchNearPoint(p);
            }
        }

        /// <summary>
        /// 指定頂点の最近接続頂点を調べる
        /// </summary>
        /// <param name="p"></param>
        void SearchNearPoint(Point p)
        {
            p.nearPoint = null;
            p.nearDist = 100000;

            var wpos = p.shareVertex.wpos;

            foreach (var sv in p.shareVertex.linkShareVertexSet)
            {
                var dist = Vector3.Distance(wpos, sv.wpos);
                if (dist < p.nearDist && dist <= reductionLength)
                {
                    p.nearDist = dist;
                    p.nearPoint = pointDict[sv];
                }
            }
        }

        /// <summary>
        /// 現時点で最も距離が近いポイントペアを返す
        /// </summary>
        /// <returns></returns>
        Point GetNearPointPair()
        {
            float nearDist = 10000;
            Point nearPoint = null;

            // ※全検索
            foreach (var p in pointList)
            {
                if (p.nearPoint != null && p.nearDist < nearDist)
                {
                    nearDist = p.nearDist;
                    nearPoint = p;
                }
            }

            return nearPoint;
        }
    }
}
