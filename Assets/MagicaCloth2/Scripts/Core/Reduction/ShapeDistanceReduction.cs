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
    /// 形状に合わせた頂点リダクション
    /// このリダクションは頂点の接続状態に沿ってリダクションを行う
    /// </summary>
    public class ShapeDistanceReduction : StepReductionBase
    {
        //=========================================================================================
        public ShapeDistanceReduction(
            string name,
            VirtualMesh mesh,
            ReductionWorkData workingData,
            float startMergeLength,
            float endMergeLength,
            int maxStep,
            bool dontMakeLine,
            float joinPositionAdjustment
            )
            : base($"ShapeReduction [{name}]", mesh, workingData, startMergeLength, endMergeLength, maxStep, dontMakeLine, joinPositionAdjustment)
        {
        }

        public override void Dispose()
        {
            base.Dispose();
        }

        protected override void StepInitialize()
        {
            base.StepInitialize();
        }

        protected override void CustomReductionStep()
        {
            // 近傍ペアを検索、結合エッジリストに登録
            var searchJob = new SearchJoinEdgeJob()
            {
                vcnt = vmesh.VertexCount,
                radius = nowMergeLength,
                dontMakeLine = dontMakeLine,
                localPositions = vmesh.localPositions.GetNativeArray(),
                joinIndices = workData.vertexJoinIndices,
                //vertexToVertexArray = workData.vertexToVertexArray,
                vertexToVertexMap = workData.vertexToVertexMap,
                joinEdgeList = joinEdgeList,
            };
            searchJob.Run();
        }

        [BurstCompile]
        struct SearchJoinEdgeJob : IJob
        {
            public int vcnt;
            public float radius;
            public bool dontMakeLine;

            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> joinIndices;
            [Unity.Collections.ReadOnly]
            public NativeParallelMultiHashMap<ushort, ushort> vertexToVertexMap;
            //public NativeArray<ExFixedSet128Bytes<ushort>> vertexToVertexArray;

            public NativeList<JoinEdge> joinEdgeList;

            public void Execute()
            {
                for (int vindex = 0; vindex < vcnt; vindex++)
                {
                    if (joinIndices[vindex] >= 0)
                        continue; // isDelete

                    // 接続頂点リスト
                    int vcnt = vertexToVertexMap.CountValuesForKey((ushort)vindex);
                    if (vcnt == 0)
                        continue; // 接続なし
                    //var vlist = vertexToVertexArray[vindex];
                    //if (vlist.Count == 0)
                    //    continue; // 接続なし

                    float3 pos = localPositions[vindex];

                    // 自身の接続頂点数
                    //float linkCount = math.max(vlist.Count - 1, 1);
                    float linkCount = math.max(vcnt - 1, 1);

                    // 範囲内で最もコストが低い接続ペアを登録する
                    float minCost = float.MaxValue;
                    int minVindex = -1;
                    //for (int i = 0; i < vlist.Count; i++)
                    foreach (ushort tvindex in vertexToVertexMap.GetValuesForKey((ushort)vindex))
                    {
                        //int tvindex = vlist.Get(i);

                        // 距離判定
                        float3 tpos = localPositions[tvindex];
                        float dist = math.distance(pos, tpos);
                        if (dist > radius)
                            continue;

                        // 相手の接続頂点数
                        float tlinkCount = math.max(vertexToVertexMap.CountValuesForKey(tvindex) - 1, 1);
                        //var tvlist = vertexToVertexArray[tvindex];
                        //float tlinkCount = math.max(tvlist.Count - 1, 1);

                        // この頂点を結合して問題がないか調べる
                        //if (CheckJoin(vertexToVertexArray, vindex, tvindex, vlist, tvlist, dontMakeLine) == false)
                        if (CheckJoin2(vertexToVertexMap, vindex, tvindex, dontMakeLine) == false)
                            continue;

                        // コスト計算
                        float cost = dist * (1.0f + (linkCount + tlinkCount) / 2.0f); // とりあえず

                        // 最小コスト判定
                        if (cost < minCost)
                        {
                            minCost = cost;
                            minVindex = tvindex;
                        }
                    }
                    if (minVindex >= 0)
                    {
                        // 登録
                        var pair = new JoinEdge()
                        {
                            vertexPair = new int2(vindex, minVindex),
                            cost = minCost,
                        };
                        joinEdgeList.Add(pair);
                    }
                }
            }
        }
    }
}
