// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using System.Collections.Generic;
using UnityEngine;

namespace MagicaReductionMesh
{
    /// <summary>
    /// ３次グリッドハッシュを利用した最近点リダクション
    /// </summary>
    public class NearPointReduction
    {
        protected MeshData meshData;

        /// <summary>
        /// 頂点データ
        /// </summary>
        public class Point
        {
            public MeshData.ShareVertex shareVertex;
            public Vector3 pos;
            public Vector3Int grid;

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
        /// ３次元グリッドマップ
        /// </summary>
        protected Dictionary<Vector3Int, List<Point>> gridMap = new Dictionary<Vector3Int, List<Point>>();

        /// <summary>
        /// グリッドサイズ
        /// </summary>
        protected float gridSize = 0.05f;

        /// <summary>
        /// 検索範囲
        /// </summary>
        float searchRadius;

        /// <summary>
        /// 最近点ペアの逆引き辞書（キー：最近点ポイント、データ：それを指すポイントのリスト）
        /// </summary>
        Dictionary<Point, List<Point>> nearPointDict = new Dictionary<Point, List<Point>>();

        //=========================================================================================
        public NearPointReduction(float radius = 0.05f)
        {
            searchRadius = radius;
            gridSize = radius * 2;
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
                AddPoint(sv, sv.wpos);
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
                if (nearPointDict.ContainsKey(p0))
                {
                    nlist.AddRange(nearPointDict[p0]);
                    nearPointDict.Remove(p0);
                }
                if (nearPointDict.ContainsKey(p1))
                {
                    nlist.AddRange(nearPointDict[p1]);
                    nearPointDict.Remove(p1);
                }
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

                // p0を新しいグリッド位置に移動
                Move(p0, sv0.wpos);

                // p0/p1を指していたポイントに対して最近点を再計算する
                foreach (var np in nlist)
                {
                    SearchNearPoint(np, searchRadius, null);
                }
            }
        }

        //=========================================================================================
        /// <summary>
        /// 頂点をグリッドに追加する
        /// </summary>
        /// <param name="pos"></param>
        Point AddPoint(MeshData.ShareVertex sv, Vector3 pos)
        {
            var p = new Point()
            {
                shareVertex = sv,
                pos = pos
            };
            pointList.Add(p);

            AddGrid(p);

            return p;
        }

        /// <summary>
        /// グリッドに追加
        /// </summary>
        /// <param name="p"></param>
        void AddGrid(Point p)
        {
            var grid = GetGridPos(p.pos);
            p.grid = grid;
            if (gridMap.ContainsKey(grid))
                gridMap[grid].Add(p);
            else
            {
                var plist = new List<Point>();
                plist.Add(p);
                gridMap.Add(grid, plist);
            }
        }

        /// <summary>
        /// グリッドから削除
        /// </summary>
        /// <param name="p"></param>
        void RemoveGrid(Point p)
        {
            var grid = p.grid;
            if (gridMap.ContainsKey(grid))
            {
                var plist = gridMap[grid];
                plist.Remove(p);

                if (plist.Count == 0)
                    gridMap.Remove(grid);
            }
            else
                Debug.LogError("remove faild!");
            p.grid = Vector3Int.zero;
        }

        void Move(Point p, Vector3 newpos)
        {
            // グリッドから削除
            RemoveGrid(p);

            // 座標更新
            p.pos = newpos;

            // グリッド追加
            AddGrid(p);
        }

        /// <summary>
        /// 頂点をグリッドから削除する
        /// </summary>
        /// <param name="p"></param>
        void Remove(Point p)
        {
            // データ削除
            RemoveGrid(p);
            pointList.Remove(p);
        }

        //=========================================================================================
        /// <summary>
        /// 座標から３次元グリッド座標を割り出す
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="gridSize"></param>
        /// <returns></returns>
        protected Vector3Int GetGridPos(Vector3 pos)
        {
            var v = pos / gridSize;
            return new Vector3Int((int)Mathf.Floor(v.x), (int)Mathf.Floor(v.y), (int)Mathf.Floor(v.z));
        }

        //=========================================================================================
        /// <summary>
        /// すべてのポイントの近接インデックスを算出しバッファに格納する
        /// </summary>
        void SearchNearPointAll()
        {
            nearPointDict.Clear();

            foreach (var plist in gridMap.Values)
            {
                foreach (var p in plist)
                {
                    SearchNearPoint(p, searchRadius, null);
                }
            }
        }

        /// <summary>
        /// 指定インデックス１つの近接インデックスを算出しバッファに格納する
        /// </summary>
        /// <param name="id"></param>
        /// <param name="pos"></param>
        void SearchNearPoint(Point p, float radius, Point ignorePoint)
        {
            Point nearPoint = null;
            float nearDist = 100000;

            // 現在Pが登録している逆引き最近点辞書があるなら削除する
            if (p.nearPoint != null)
            {
                if (nearPointDict.ContainsKey(p.nearPoint))
                {
                    nearPointDict[p.nearPoint].Remove(p);
                }
            }

            // 範囲内のグリッドを走査してもっとも近いポイントを算出する
            var center = p.grid;
            int size = (int)(radius / gridSize) + 1;
            var s = new Vector3Int(size, size, size);
            var sgrid = center - s;
            var egrid = center + s;

            Vector3Int grid = Vector3Int.zero;
            for (int x = sgrid.x; x <= egrid.x; x++)
            {
                grid.x = x;
                for (int y = sgrid.y; y <= egrid.y; y++)
                {
                    grid.y = y;
                    for (int z = sgrid.z; z <= egrid.z; z++)
                    {
                        grid.z = z;

                        // このグリッドを検索する
                        if (gridMap.ContainsKey(grid))
                        {
                            var plist = gridMap[grid];
                            foreach (var wp in plist)
                            {
                                // 自身は弾く
                                if (wp == p)
                                    continue;

                                // 計算除外ポイントは弾く
                                if (wp == ignorePoint)
                                    continue;

                                // 距離判定
                                float dist = Vector3.Distance(wp.pos, p.pos);
                                if (dist < nearDist && dist <= radius)
                                {
                                    nearPoint = wp;
                                    nearDist = dist;
                                }
                            }
                        }
                    }
                }
            }

            // 結果格納
            if (nearPoint != null)
            {
                p.nearPoint = nearPoint;
                p.nearDist = nearDist;

                // 逆引き辞書に登録
                if (nearPointDict.ContainsKey(nearPoint) == false)
                    nearPointDict.Add(nearPoint, new List<Point>());
                nearPointDict[nearPoint].Add(p);
            }
            else
            {
                p.nearPoint = null;
                p.nearDist = 100000;
            }
        }

        /// <summary>
        /// 現時点で最も距離が近いポイントペアを返す
        /// </summary>
        /// <returns></returns>
        Point GetNearPointPair()
        {
#if true
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
#else
            if (pointList.Count == 0)
                return null;

            // 距離ソート
            pointList.Sort((a, b) => a.nearDist < b.nearDist ? -1 : 1);
            return pointList[0];
#endif
        }
    }
}
