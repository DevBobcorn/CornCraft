// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace MagicaCloth2
{
    /// <summary>
    /// 単純な頂点間の距離によるリダクション
    /// このリダクションは頂点の接続状態を無視する
    /// </summary>
    public class SimpleDistanceReduction : StepReductionBase
    {
        /// <summary>
        /// グリッドマップ
        /// </summary>
        private GridMap<int> gridMap;

        //=========================================================================================
        public SimpleDistanceReduction(
            string name,
            VirtualMesh mesh,
            ReductionWorkData workingData,
            float startMergeLength,
            float endMergeLength,
            int maxStep,
            bool dontMakeLine,
            float joinPositionAdjustment
            )
            : base($"SimpleDistanceReduction [{name}]", mesh, workingData, startMergeLength, endMergeLength, maxStep, dontMakeLine, joinPositionAdjustment)
        {
        }

        public override void Dispose()
        {
            base.Dispose();
            gridMap.Dispose();
        }

        protected override void StepInitialize()
        {
            base.StepInitialize();
            gridMap = new GridMap<int>(vmesh.VertexCount);
        }

        protected override void CustomReductionStep()
        {
            // 最適なグリッドサイズを割り出す(nowMergeLengthは>0が保証されている)
            float gridSize = nowMergeLength * 2.0f; // 1.5?

            // 作業用バッファクリア
            gridMap.GetMultiHashMap().Clear();

            // ポイントをグリッドに登録
            var initGridJob = new InitGridJob()
            {
                vcnt = vmesh.VertexCount,
                gridSize = gridSize,
                localPositions = vmesh.localPositions.GetNativeArray(),
                joinIndices = workData.vertexJoinIndices,
                gridMap = gridMap.GetMultiHashMap(),
            };
            initGridJob.Run();

            // 近傍ポイントを検索、結合エッジリストに登録
            var searchJob = new SearchJoinEdgeJob()
            {
                vcnt = vmesh.VertexCount,
                gridSize = gridSize,
                radius = nowMergeLength,
                localPositions = vmesh.localPositions.GetNativeArray(),
                joinIndices = workData.vertexJoinIndices,
                vertexToVertexMap = workData.vertexToVertexMap, // cost用
                gridMap = gridMap.GetMultiHashMap(),
                joinEdgeList = joinEdgeList,
            };
            searchJob.Run();
        }

        [BurstCompile]
        struct InitGridJob : IJob
        {
            public int vcnt;
            public float gridSize;

            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> joinIndices;

            public NativeParallelMultiHashMap<int3, int> gridMap;

            // 頂点ごと
            public void Execute()
            {
                for (int vindex = 0; vindex < vcnt; vindex++)
                {
                    if (joinIndices[vindex] >= 0)
                        continue; // isDelete

                    GridMap<int>.AddGrid(localPositions[vindex], vindex, gridMap, gridSize);
                }
            }
        }

        [BurstCompile]
        struct SearchJoinEdgeJob : IJob
        {
            public int vcnt;
            public float gridSize;
            public float radius;

            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> joinIndices;

            [Unity.Collections.ReadOnly]
            public NativeParallelMultiHashMap<ushort, ushort> vertexToVertexMap;
            [Unity.Collections.ReadOnly]
            public NativeParallelMultiHashMap<int3, int> gridMap;

            [Unity.Collections.WriteOnly]
            public NativeList<JoinEdge> joinEdgeList;

            // 頂点ごと
            public void Execute()
            {
                for (int vindex = 0; vindex < vcnt; vindex++)
                {
                    if (joinIndices[vindex] >= 0)
                        continue; // isDelete

                    float3 pos = localPositions[vindex];

                    // 自身の接続頂点数
                    float linkCount = math.max(vertexToVertexMap.CountValuesForKey((ushort)vindex) - 1, 1);

                    // 範囲グリッド走査
                    foreach (int3 grid in GridMap<int>.GetArea(pos, radius, gridMap, gridSize))
                    {
                        if (gridMap.ContainsKey(grid) == false)
                            continue;

                        // このグリッドを検索する
                        foreach (int tindex in gridMap.GetValuesForKey(grid))
                        {
                            // 自身は弾く
                            if (tindex == vindex)
                                continue;

                            // 距離判定
                            float3 tpos = localPositions[tindex];
                            float dist = math.distance(pos, tpos);
                            if (dist > radius)
                                continue;

                            // 相手の接続頂点数
                            float tlinkCount = math.max(vertexToVertexMap.CountValuesForKey((ushort)tindex) - 1, 1);

                            // コスト計算
                            //float cost = dist / (linkCount + tlinkCount); // todo:とりあえずテスト
                            //float cost = dist * (1.0f / (linkCount + tlinkCount));
                            float cost = dist * (1.0f + (linkCount + tlinkCount) / 2.0f); // 12

                            // 全部登録
                            var pair = new JoinEdge()
                            {
                                vertexPair = new int2(vindex, tindex),
                                cost = cost,
                            };
                            joinEdgeList.Add(pair);
                        }
                    }
                }
            }
        }
    }
}
