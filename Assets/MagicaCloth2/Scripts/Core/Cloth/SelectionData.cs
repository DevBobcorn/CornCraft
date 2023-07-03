// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace MagicaCloth2
{
    /// <summary>
    /// 頂点の属性情報(移動/固定/無効)データ
    /// このデータはシリアライズされる
    /// 座標はクロスコンポーネントのローカル空間で格納される
    /// Vertex attribute information (move/fix/disable) data.
    /// This data is serialized.
    /// Coordinates are stored in the cloth component's local space.
    /// </summary>
    [System.Serializable]
    public class SelectionData : IValid
    {
        /// <summary>
        /// 属性のローカル座標
        /// これはクロスコンポーネント空間
        /// Attribute local coordinates.
        /// This is the cloth component space.
        /// </summary>
        public float3[] positions;

        /// <summary>
        /// 上記の属性値
        /// サイズはpositionsと同じでなくてはならない
        /// Attribute value above.
        /// size must be the same as positions.
        /// </summary>
        public VertexAttribute[] attributes;

        /// <summary>
        /// セレクションデータ構築時のVirtualMeshの最大頂点接続距離
        /// Maximum vertex connection distance of VirtualMesh when constructing selection data.
        /// </summary>
        public float maxConnectionDistance;

        /// <summary>
        /// ユーザーが編集したデータかどうか
        /// Is the data edited by the user?
        /// </summary>
        public bool userEdit = false;

        //=========================================================================================
        public SelectionData() { }

        public SelectionData(int cnt)
        {
            positions = new float3[cnt];
            attributes = new VertexAttribute[cnt];
        }

        public SelectionData(VirtualMesh vmesh, float4x4 transformMatrix)
        {
            if (vmesh != null && vmesh.VertexCount > 0)
            {
                // vmesh座標をコンポーネント空間に変換して格納する
                using var posArray = new NativeArray<float3>(vmesh.localPositions.GetNativeArray(), Allocator.TempJob);
                var job = new TransformPositionJob()
                {
                    transformMatrix = transformMatrix,
                    localPositions = posArray,
                };
                job.Run(vmesh.VertexCount);

                positions = posArray.ToArray();
                attributes = vmesh.attributes.ToArray();
                maxConnectionDistance = vmesh.maxVertexDistance.Value;
            }
        }

        [BurstCompile]
        struct TransformPositionJob : IJobParallelFor
        {
            public float4x4 transformMatrix;
            public NativeArray<float3> localPositions;

            public void Execute(int index)
            {
                var pos = localPositions[index];
                pos = math.transform(transformMatrix, pos);
                localPositions[index] = pos;
            }
        }

        public int Count
        {
            get
            {
                return positions?.Length ?? 0;
            }
        }

        public bool IsValid()
        {
            if (positions == null || positions.Length == 0)
                return false;
            if (attributes == null || attributes.Length == 0)
                return false;
            if (positions.Length != attributes.Length)
                return false;

            return true;
        }

        public bool IsUserEdit() => userEdit;

        public SelectionData Clone()
        {
            var sdata = new SelectionData();
            sdata.positions = positions?.Clone() as float3[];
            sdata.attributes = attributes?.Clone() as VertexAttribute[];
            sdata.maxConnectionDistance = maxConnectionDistance;
            sdata.userEdit = userEdit;

            return sdata;
        }

        /// <summary>
        /// ハッシュ（このセレクションデータの識別に利用される）
        /// </summary>
        /// <returns></returns>
        //public override int GetHashCode()
        //{
        //    if (IsValid() == false)
        //        return 0;

        //    // 頂点座標のハッシュをいくつかサンプリングする
        //    uint hash = 0;
        //    int len = positions.Length;
        //    int step = math.max(len / 4, 1);
        //    for (int i = 0; i < len; i += step)
        //    {
        //        hash += math.hash(positions[i]);
        //        hash += (uint)attributes[i].Value;
        //    }
        //    hash += math.hash(positions[len - 1]);
        //    hash += (uint)attributes[len - 1].Value;

        //    return (int)hash;
        //}

        /// <summary>
        /// ２つのセレクションデータが等しいか判定する
        /// （座標の詳細は見ない。属性の詳細は見る）
        /// </summary>
        /// <param name="sdata"></param>
        /// <returns></returns>
        public bool Compare(SelectionData sdata)
        {
            //return GetHashCode() == sdata.GetHashCode();

            if (positions?.Length != sdata.positions?.Length)
                return false;

            if (attributes?.Length != sdata.attributes?.Length)
                return false;

            if (userEdit != sdata.userEdit)
                return false;

            // 座標と属性は正確に見る
            // todo:この処理が重いようならBurstに切り替える
            int cnt = attributes.Length;
            for (int i = 0; i < cnt; i++)
            {
                if (attributes[i] != sdata.attributes[i])
                    return false;
                if (positions[i].Equals(sdata.positions[i]) == false)
                    return false;
            }

            return true;
        }

        public void AddRange(float3[] addPositions, VertexAttribute[] addAttributes = null)
        {
            if (Count == 0)
            {
                positions = addPositions;
                attributes = addAttributes != null ? addAttributes : new VertexAttribute[addPositions.Length];
            }
            else
            {
                // 拡張
                int cnt = Count;
                int addCnt = addPositions.Length;
                float3[] newPositions = new float3[cnt + addCnt];
                VertexAttribute[] newAttribues = new VertexAttribute[cnt + addCnt];

                Array.Copy(positions, 0, newPositions, 0, cnt);
                Array.Copy(addPositions, 0, newPositions, cnt, addCnt);

                Array.Copy(attributes, 0, newAttribues, 0, cnt);
                if (addAttributes != null)
                {
                    Array.Copy(addAttributes, 0, newAttribues, cnt, addCnt);
                }

                positions = newPositions;
                attributes = newAttribues;
            }
        }

        public void Fill(VertexAttribute attr)
        {
#if UNITY_2020
            int cnt = Count;
            for (int i = 0; i < cnt; i++)
                attributes[i] = attr;
#else
            Array.Fill(attributes, attr);
#endif
        }

        //=========================================================================================
        public NativeArray<float3> GetPositionNativeArray()
        {
            return new NativeArray<float3>(positions, Allocator.Persistent);
        }

        public NativeArray<float3> GetPositionNativeArray(float4x4 transformMatrix)
        {
            var posArray = GetPositionNativeArray();
            var job = new TransformPositionJob()
            {
                transformMatrix = transformMatrix,
                localPositions = posArray,
            };
            job.Run(Count);

            return posArray;
        }

        public NativeArray<VertexAttribute> GetAttributeNativeArray()
        {
            return new NativeArray<VertexAttribute>(attributes, Allocator.Persistent);
        }

        //=========================================================================================
        /// <summary>
        /// 属性座標をグリッドマップに登録して返す
        /// </summary>
        /// <param name="gridSize"></param>
        /// <param name="positions"></param>
        /// <param name="attributes"></param>
        /// <param name="move">移動属性を含めるかどうか</param>
        /// <param name="fix">固定属性を含めるかどうか</param>
        /// <param name="invalid">無効属性を含めるかどうか</param>
        /// <returns></returns>
        public static GridMap<int> CreateGridMapRun(
            float gridSize,
            in NativeArray<float3> positions,
            in NativeArray<VertexAttribute> attributes,
            bool move = true, bool fix = true, bool ignore = true, bool invalid = true
            )
        {
            var gridMap = new GridMap<int>(positions.Length);

            var job = new CreateGridMapJob()
            {
                move = move,
                fix = fix,
                ignore = ignore,
                invalid = invalid,
                gridMap = gridMap.GetMultiHashMap(),
                gridSize = gridSize,
                positions = positions,
                attribute = attributes,
            };
            job.Run();

            return gridMap;
        }

        [BurstCompile]
        struct CreateGridMapJob : IJob
        {
            public bool move;
            public bool fix;
            public bool ignore;
            public bool invalid;

            public NativeParallelMultiHashMap<int3, int> gridMap;
            public float gridSize;

            [Unity.Collections.ReadOnly]
            public NativeArray<float3> positions;
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> attribute;

            public void Execute()
            {
                int cnt = positions.Length;
                for (int i = 0; i < cnt; i++)
                {
                    var attr = attribute[i];
                    if (move == false && attr.IsMove())
                        continue;
                    if (fix == false && attr.IsFixed())
                        continue;
                    //if (ignore == false && attr.IsIgnore())
                    //    continue;
                    if (invalid == false && attr.IsInvalid())
                        continue;

                    var pos = positions[i];

                    GridMap<int>.AddGrid(pos, i, gridMap, gridSize);
                }
            }
        }

        //=========================================================================================
        /// <summary>
        /// セレクションデータを結合する
        /// </summary>
        /// <param name="from"></param>
        public void Merge(SelectionData from)
        {
            if (from.Count == 0)
                return;

            var cnt = Count + from.Count;
            var newPositions = new List<float3>(cnt);
            var newAttributes = new List<VertexAttribute>(cnt);
            if (positions != null)
                newPositions.AddRange(positions);
            if (attributes != null)
                newAttributes.AddRange(attributes);
            newPositions.AddRange(from.positions);
            newAttributes.AddRange(from.attributes);

            positions = newPositions.ToArray();
            attributes = newAttributes.ToArray();
            maxConnectionDistance = math.max(maxConnectionDistance, from.maxConnectionDistance);
            userEdit = userEdit || from.userEdit;
        }

        /// <summary>
        /// 頂点数の異なるセレクションデータを移植する
        /// </summary>
        /// <param name="from">移動元セレクションデータ</param>
        public void ConvertFrom(SelectionData from)
        {
            if (from.Count == 0)
                return;
            if (Count == 0)
                return;

            // 移動先データ
            using var toPositions = GetPositionNativeArray();
            using var toAttributes = GetAttributeNativeArray();

            // 移動元データ
            using var fromPositions = from.GetPositionNativeArray();
            using var fromAttributes = from.GetAttributeNativeArray();

            // 移動先のAABB
            using var aabb = new NativeReference<AABB>(Allocator.TempJob);
            JobUtility.CalcAABBRun(toPositions, Count, aabb);
            float serachRadius = aabb.Value.MaxSideLength * 0.2f; // 20%
            serachRadius = math.max(serachRadius, Define.System.MinimumGridSize);

            // 移動元データをグリッドに登録する
            float gridSize = serachRadius * 0.5f;
            using var gridMap = CreateGridMapRun(gridSize, fromPositions, fromAttributes);

            // 移動先座標を範囲検索し見つかった移動元属性を付与する
            var job = new ConvertSelectionJob()
            {
                gridSize = gridSize,
                radius = serachRadius,
                toPositions = toPositions,
                toAttributes = toAttributes,
                gridMap = gridMap.GetMultiHashMap(),
                fromPositions = fromPositions,
                fromAttributes = fromAttributes,
            };
            job.Run(Count);

            // 結果の書き戻し
            positions = toPositions.ToArray();
            attributes = toAttributes.ToArray();
        }

        [BurstCompile]
        struct ConvertSelectionJob : IJobParallelFor
        {
            public float gridSize;
            public float radius;

            // to
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> toPositions;
            [Unity.Collections.WriteOnly]
            public NativeArray<VertexAttribute> toAttributes;

            // from
            [Unity.Collections.ReadOnly]
            public NativeParallelMultiHashMap<int3, int> gridMap;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> fromPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> fromAttributes;

            public void Execute(int vindex)
            {
                float3 pos = toPositions[vindex];

                // 見つからない場合はInvalid
                VertexAttribute attr = VertexAttribute.Invalid;

                // 範囲グリッド走査
                float minDist = float.MaxValue;
                foreach (int3 grid in GridMap<int>.GetArea(pos, radius, gridMap, gridSize))
                {
                    if (gridMap.ContainsKey(grid) == false)
                        continue;

                    // このグリッドを検索する
                    foreach (int tindex in gridMap.GetValuesForKey(grid))
                    {
                        // 距離判定
                        float3 tpos = fromPositions[tindex];
                        float dist = math.distance(pos, tpos);
                        if (dist > radius)
                            continue;
                        if (dist > minDist)
                            continue;

                        // 近傍設定
                        minDist = dist;
                        attr = fromAttributes[tindex];
                    }
                }

                // 属性反映
                toAttributes[vindex] = attr;
            }
        }
    }
}
