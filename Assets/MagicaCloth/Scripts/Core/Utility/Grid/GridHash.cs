// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// ３次グリッドハッシュの基礎クラス
    /// このクラスを派生させて独自のグリッド判定処理などを記述してください。
    /// </summary>
    public class GridHash
    {
        /// <summary>
        /// 頂点データ
        /// </summary>
        public class Point
        {
            public int id;
            public float3 pos;
        }

        /// <summary>
        /// ３次元グリッドマップ
        /// </summary>
        protected Dictionary<uint, List<Point>> gridMap;

        /// <summary>
        /// グリッドサイズ
        /// </summary>
        protected float gridSize = 0.1f;

        //=========================================================================================
        /// <summary>
        /// 初期化
        /// </summary>
        /// <param name="gridSize"></param>
        public virtual void Create(float gridSize = 0.1f)
        {
            gridMap = new Dictionary<uint, List<Point>>();
            this.gridSize = gridSize;
        }

        //=========================================================================================
        /// <summary>
        /// 頂点をグリッドに追加する
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="id"></param>
        public virtual void AddPoint(float3 pos, int id)
        {
            var p = new Point()
            {
                id = id,
                pos = pos
            };
            var grid = GetGridHash(pos, gridSize);
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
        /// 頂点をグリッドから削除する
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="id"></param>
        public virtual void Remove(float3 pos, int id)
        {
            var grid = GetGridHash(pos, gridSize);
            if (gridMap.ContainsKey(grid))
            {
                var plist = gridMap[grid];
                for (int i = 0; i < plist.Count; i++)
                {
                    if (plist[i].id == id)
                    {
                        plist.RemoveAt(i);
                        break;
                    }
                }
            }
            else
                Debug.LogError("remove faild!");
        }

        /// <summary>
        /// クリア
        /// </summary>
        public void Clear()
        {
            gridMap.Clear();
        }

        //=========================================================================================
        /// <summary>
        /// 座標から３次元グリッド座標を割り出す
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="gridSize"></param>
        /// <returns></returns>
        public static int3 GetGridPos(float3 pos, float gridSize)
        {
            return math.int3(math.floor(pos / gridSize));
        }

        /// <summary>
        /// ３次元グリッド座標を10ビット刻みのuint型にグリッドハッシュに変換する
        /// 10ビットの範囲は+511～-512となり、グリッド座標がオーバー／アンダーする場合はループする
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public static uint GetGridHash(int3 pos)
        {
            uint hash = (uint)(pos.x & 0x3ff) | (uint)(pos.y & 0x3ff) << 10 | (uint)(pos.z & 0x3ff) << 20;
            return hash;
        }

        /// <summary>
        /// 座標からグリッドハッシュに変換して返す
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="gridSize"></param>
        /// <returns></returns>
        public static uint GetGridHash(float3 pos, float gridSize)
        {
            int3 xyz = GetGridPos(pos, gridSize);
            return GetGridHash(xyz);
        }
    }
}
