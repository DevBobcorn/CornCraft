// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using System.Collections.Generic;
using Unity.Mathematics;


namespace MagicaCloth
{
    /// <summary>
    /// ポイントリストからその中で最も距離が近いペアを検索する
    /// ※これはエディタ用でランタイムでの用途には設計されていません。
    /// </summary>
    public class NearPointSearch : GridHash
    {
        float radius;
        Dictionary<int, int> nearDict = new Dictionary<int, int>();
        Dictionary<int, float> distDict = new Dictionary<int, float>();
        HashSet<uint> lockPairSet = new HashSet<uint>();

        /// <summary>
        /// 初期化
        /// </summary>
        /// <param name="positionList">対象となるポイントリスト</param>
        /// <param name="radius">各点の検索半径</param>
        public void Create(float3[] positionList, float radius)
        {
            base.Create(radius);

            this.radius = radius;

            // グリッドにポイントを追加
            for (int i = 0; i < positionList.Length; i++)
            {
                AddPoint(positionList[i], i);
            }
        }

        /// <summary>
        /// すべてのポイントの近接インデックスを算出しバッファに格納する
        /// </summary>
        public void SearchNearPointAll()
        {
            foreach (var plist in gridMap.Values)
            {
                foreach (var p in plist)
                {
                    SearchNearPoint(p.id, p.pos);
                }
            }
        }

        /// <summary>
        /// 指定インデックス１つの近接インデックスを算出しバッファに格納する
        /// </summary>
        /// <param name="id"></param>
        /// <param name="pos"></param>
        public void SearchNearPoint(int id, float3 pos)
        {
            int nearId = -1;
            float nearDist = 100000.0f;

            // 範囲内のグリッドを走査してもっとも近いポイントを算出する
            int3 sgrid = GetGridPos(pos - radius, gridSize);
            int3 egrid = GetGridPos(pos + radius, gridSize);

            for (int x = sgrid.x; x <= egrid.x; x++)
            {
                for (int y = sgrid.y; y <= egrid.y; y++)
                {
                    for (int z = sgrid.z; z <= egrid.z; z++)
                    {
                        uint hash = GetGridHash(new int3(x, y, z));

                        // このグリッドを検索する
                        if (gridMap.ContainsKey(hash))
                        {
                            var plist = gridMap[hash];
                            foreach (var p in plist)
                            {
                                // 自身は弾く
                                if (p.id == id)
                                    continue;

                                // 距離判定
                                float dist = math.length(pos - p.pos);
                                if (dist < nearDist)
                                {
                                    nearId = p.id;
                                    nearDist = dist;
                                }
                            }
                        }
                    }
                }
            }

            // 結果格納
            if (nearId >= 0)
            {
                nearDict[id] = nearId;
                distDict[id] = nearDist;
            }
            else
            {
                if (nearDict.ContainsKey(id))
                {
                    nearDict.Remove(id);
                    distDict.Remove(id);
                }
            }
        }

        /// <summary>
        /// 指定範囲の近接頂点を再計算する
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="r"></param>
        public void SearchNearPoint(float3 pos, float r)
        {
            int3 sgrid = GetGridPos(pos - r, gridSize);
            int3 egrid = GetGridPos(pos + r, gridSize);

            for (int x = sgrid.x; x <= egrid.x; x++)
            {
                for (int y = sgrid.y; y <= egrid.y; y++)
                {
                    for (int z = sgrid.z; z <= egrid.z; z++)
                    {
                        uint hash = GetGridHash(new int3(x, y, z));
                        if (gridMap.ContainsKey(hash))
                        {
                            var plist = gridMap[hash];
                            foreach (var p in plist)
                            {
                                SearchNearPoint(p.id, p.pos);
                            }
                        }
                    }
                }
            }
        }

        public override void AddPoint(float3 pos, int id)
        {
            base.AddPoint(pos, id);
        }

        public override void Remove(float3 pos, int id)
        {
            base.Remove(pos, id);

            if (nearDict.ContainsKey(id))
            {
                nearDict.Remove(id);
                distDict.Remove(id);
            }
        }

        public void AddLockPair(int id1, int id2)
        {
            uint pair = DataUtility.PackPair(id1, id2);
            lockPairSet.Add(pair);
        }

        /// <summary>
        /// バッファ内の最も近接にあるペアを返す
        /// </summary>
        /// <param name="id1"></param>
        /// <param name="id2"></param>
        /// <returns></returns>
        public bool GetNearPointPair(out int id1, out int id2)
        {
            int index = -1;
            int nearIndex = -1;
            float nearDist = 100000.0f;

            foreach (var keyval in nearDict)
            {
                int id = keyval.Key;
                int nearId = keyval.Value;
                if (nearId == -1)
                    continue;

                // ロックペアならスルー
                uint pair = DataUtility.PackPair(id, nearId);
                if (lockPairSet.Contains(pair))
                    continue;

                float dist = distDict[id];
                if (dist > radius)
                    continue;

                if (dist < nearDist)
                {
                    index = id;
                    nearIndex = nearId;
                    nearDist = dist;
                }
            }

            if (index >= 0 && nearIndex >= 0)
            {
                id1 = index;
                id2 = nearIndex;
                return true;
            }
            else
            {
                id1 = -1;
                id2 = -1;
                return false;
            }
        }

        public override string ToString()
        {
            string str = "";

            foreach (var keyval in nearDict)
            {
                str += string.Format("[{0}] -> {1} {2}\n", keyval.Key, keyval.Value, distDict[keyval.Key]);
            }

            return str;
        }
    }
}
