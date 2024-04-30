// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
#if MAGICACLOTH2_REDUCTION_DEBUG
using UnityEngine;
#endif

namespace MagicaCloth2
{
    /// <summary>
    /// 距離内のすべての頂点を一度に結合させる
    /// </summary>
    public class SameDistanceReduction : IDisposable
    {
        string name = string.Empty;
        VirtualMesh vmesh;
        ReductionWorkData workData;
        ResultCode result;
        float mergeLength;

        //=========================================================================================
        GridMap<int> gridMap;
        //NativeParallelHashSet<int2> joinPairSet;
        NativeParallelMultiHashMap<ushort, ushort> joinPairMap;
        NativeReference<int> resultRef;

        //=========================================================================================
        public SameDistanceReduction() { }

        public SameDistanceReduction(
            string name,
            VirtualMesh mesh,
            ReductionWorkData workingData,
            float mergeLength
            )
        {
            this.name = name;
            this.vmesh = mesh;
            this.workData = workingData;
            this.result = ResultCode.None;
            this.mergeLength = math.max(mergeLength, 1e-09f);
        }

        public virtual void Dispose()
        {
            //if (joinPairSet.IsCreated)
            //    joinPairSet.Dispose();
            if (joinPairMap.IsCreated)
                joinPairMap.Dispose();
            if (resultRef.IsCreated)
                resultRef.Dispose();
            gridMap?.Dispose();
        }

        public ResultCode Result => result;

        //=========================================================================================
        /// <summary>
        /// リダクション実行（スレッド可）
        /// </summary>
        /// <returns></returns>
        public ResultCode Reduction()
        {
            //bool success = false;
            result.Clear();

            try
            {
                // グリッドマップ
                gridMap = new GridMap<int>(vmesh.VertexCount);

                // 最適なグリッドサイズを割り出す(mergeLengthは>0が保証されている)
                float gridSize = mergeLength * 2.0f;

                // 頂点ごとの接続マップ
                // インデックスが若い方が記録する
                //joinPairSet = new NativeParallelHashSet<int2>(vmesh.VertexCount / 4, Allocator.Persistent);
                joinPairMap = new NativeParallelMultiHashMap<ushort, ushort>(vmesh.VertexCount, Allocator.Persistent);
                resultRef = new NativeReference<int>(Allocator.Persistent);

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

                // 近傍頂点を検索、結合マップに登録する
                var searchJoinJob = new SearchJoinJob()
                {
                    vcnt = vmesh.VertexCount,
                    gridSize = gridSize,
                    radius = mergeLength,
                    localPositions = vmesh.localPositions.GetNativeArray(),
                    joinIndices = workData.vertexJoinIndices,
                    gridMap = gridMap.GetMultiHashMap(),
                    //joinPairSet = joinPairSet,
                    joinPairMap = joinPairMap,
                };
                searchJoinJob.Run();

                // 結合する
#if false
                var joinJob = new JoinJob()
                {
                    vertexCount = vmesh.VertexCount,
                    //joinPairSet = joinPairSet,
                    joinPairMap = joinPairMap,
                    joinIndices = workData.vertexJoinIndices,
                    vertexToVertexMap = workData.vertexToVertexMap,
                    boneWeights = vmesh.boneWeights.GetNativeArray(),
                    attributes = vmesh.attributes.GetNativeArray(),
                    result = resultRef,
                };
                joinJob.Run();
#endif
#if true
                // 高速化および詳細メッシュがある場合に１つの頂点に大量に結合しオーバーフローが発生する問題を回避したもの
                // 以前はA->B->C結合時にA->C間が接続距離以上でもA->Cが結合されてしまったが、この改良版ではそれがおこならない
                // そのため以前とは結果がことなることに注意！
                using var tempList = new NativeList<ushort>(2048, Allocator.Persistent);
                var joinJob2 = new JoinJob2()
                {
                    vertexCount = vmesh.VertexCount,
                    joinPairMap = joinPairMap,
                    joinIndices = workData.vertexJoinIndices,
                    vertexToVertexMap = workData.vertexToVertexMap,
                    boneWeights = vmesh.boneWeights.GetNativeArray(),
                    attributes = vmesh.attributes.GetNativeArray(),
                    result = resultRef,
                    tempList = tempList,
                };
                joinJob2.Run();
#endif

                // 頂点の接続状態を最新に更新する。すべて最新の生存ポイントを指すように変更する
                UpdateJoinAndLink();

                // 頂点情報を整理する
                // 法線単位化
                // ボーンウエイトを１に平均化
                UpdateReductionResultJob();

                // 削除頂点数集計
                int removeVertexCount = resultRef.Value;
                workData.removeVertexCount += removeVertexCount;
                //Debug.Log($"[SameDistanceReduction [{name}]] mergeLength:{mergeLength} RemoveVertex:{removeVertexCount}");

                // 完了
                //success = true;
                result.SetSuccess();
            }
            catch (Exception exception)
            {
                Debug.LogError(exception);
                result.SetError(Define.Result.Reduction_SameDistanceException);
            }
            finally
            {
                // 作業バッファを解放する（重要）
                // ★仮にタスクが例外やキャンセルされたとしてもこれで作成したバッファは正しくDispose()される
                //MagicaManager.Discard.Add();

                // 登録したジョブを解除する（重要）
                // ★仮にタスクが例外やキャンセルされたとしてもこれで発行したJobは正しくComplete()される
                //MagicaManager.Thread.DisposeJob(int);
            }

            return result;
        }

        //=========================================================================================
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
        struct SearchJoinJob : IJob
        {
            public int vcnt;
            public float gridSize;
            public float radius;

            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> joinIndices;
            [Unity.Collections.ReadOnly]
            public NativeParallelMultiHashMap<int3, int> gridMap;

            //public NativeParallelHashSet<int2> joinPairSet;
            public NativeParallelMultiHashMap<ushort, ushort> joinPairMap;

            public void Execute()
            {
                for (int vindex = 0; vindex < vcnt; vindex++)
                {
                    if (joinIndices[vindex] >= 0)
                        continue; // isDelete

                    float3 pos = localPositions[vindex];

                    // 範囲グリッド走査
                    foreach (int3 grid in GridMap<int>.GetArea(pos, radius, gridMap, gridSize))
                    {
                        if (gridMap.ContainsKey(grid) == false)
                            continue;

                        // このグリッドを検索する
                        foreach (int tvindex in gridMap.GetValuesForKey(grid))
                        {
                            // 自身は弾く
                            if (tvindex == vindex)
                                continue;

                            // 距離判定
                            float3 tpos = localPositions[tvindex];
                            float dist = math.distance(pos, tpos);
                            if (dist > radius)
                                continue;

                            // 結合登録
                            //int2 ehash = DataUtility.PackInt2(vindex, tvindex);
                            //joinPairSet.Add(ehash);
                            joinPairMap.Add((ushort)vindex, (ushort)tvindex);
                        }
                    }
                }
            }
        }

        [BurstCompile]
        struct JoinJob2 : IJob
        {
            public int vertexCount;

            [Unity.Collections.ReadOnly]
            public NativeParallelMultiHashMap<ushort, ushort> joinPairMap;

            public NativeArray<int> joinIndices;
            public NativeParallelMultiHashMap<ushort, ushort> vertexToVertexMap;
            public NativeArray<VirtualMeshBoneWeight> boneWeights;
            public NativeArray<VertexAttribute> attributes;
            public NativeReference<int> result;

            public NativeList<ushort> tempList;

            public void Execute()
            {
                int cnt = 0;

                for (ushort vindexLive = 0; vindexLive < vertexCount; vindexLive++)
                {
                    // すでに結合済みならスキップ
                    if (joinIndices[vindexLive] >= 0)
                        continue;

                    // 対象ループ
                    foreach (ushort vindexDead in joinPairMap.GetValuesForKey(vindexLive))
                    {
                        // 対象がすでに結合ずみならスキップ
                        if (joinIndices[vindexDead] >= 0)
                            continue;

                        // 結合(vertexDead -> vertexLive)
                        joinIndices[vindexDead] = vindexLive;
                        cnt++;

                        vertexToVertexMap.MC2RemoveValue(vindexLive, vindexDead);

                        tempList.Clear();
                        foreach (ushort i in vertexToVertexMap.GetValuesForKey(vindexDead))
                        {
                            tempList.Add(i);
                        }
                        foreach (ushort i in tempList)
                        {
                            if (joinIndices[i] >= 0)
                                continue;
                            if (i == vindexLive || i == vindexDead)
                                continue;

                            vertexToVertexMap.MC2RemoveValue(i, vindexDead);

                            vertexToVertexMap.MC2UniqueAdd(vindexLive, i);
                            vertexToVertexMap.MC2UniqueAdd(i, vindexLive);

                            // p2にBoneWeightを結合
                            var bw = boneWeights[vindexLive];
                            bw.AddWeight(boneWeights[vindexDead]);
                            boneWeights[vindexLive] = bw;

                            // 属性
                            var attr1 = attributes[vindexDead];
                            var attr2 = attributes[vindexLive];
                            attributes[vindexLive] = VertexAttribute.JoinAttribute(attr1, attr2);
                            attributes[vindexDead] = VertexAttribute.Invalid; // 削除頂点は無効にする
                        }
                    }
                }

                // 削除頂点数記録
                result.Value = cnt;
            }
        }

#if false // old
        [BurstCompile]
        struct JoinJob : IJob
        {
            [Unity.Collections.ReadOnly]
            public NativeParallelHashSet<int2> joinPairSet;

            public NativeArray<int> joinIndices;
            public NativeParallelMultiHashMap<ushort, ushort> vertexToVertexMap;
            public NativeArray<VirtualMeshBoneWeight> boneWeights;
            public NativeArray<VertexAttribute> attributes;
            public NativeReference<int> result;

            public void Execute()
            {
                var workSet = new FixedList512Bytes<ushort>();
                int cnt = 0;

                foreach (var ehash in joinPairSet)
                {
                    int vindexLive = ehash[0]; // 生存側
                    int vindexDead = ehash[1]; // 削除側

                    while (joinIndices[vindexDead] >= 0)
                    {
                        vindexDead = joinIndices[vindexDead];
                    }
                    while (joinIndices[vindexLive] >= 0)
                    {
                        vindexLive = joinIndices[vindexLive];
                    }
                    if (vindexDead == vindexLive)
                        continue;

                    // 結合(vertex1 -> vertex2)
                    joinIndices[vindexDead] = vindexLive;
                    cnt++;

                    // 接続数を結合する（重複は弾かれる）
                    workSet.Clear();
                    foreach (ushort i in vertexToVertexMap.GetValuesForKey((ushort)vindexDead))
                    {
                        int index = i;
                        // 生存インデックス
                        while (joinIndices[index] >= 0)
                        {
                            index = joinIndices[index];
                        }
                        if (index != vindexDead && index != vindexLive)
                            workSet.MC2Set((ushort)index);
                    }
                    foreach (ushort i in vertexToVertexMap.GetValuesForKey((ushort)vindexLive))
                    {
                        int index = i;
                        // 生存インデックス
                        while (joinIndices[index] >= 0)
                        {
                            index = joinIndices[index];
                        }
                        if (index != vindexDead && index != vindexLive)
                            workSet.MC2Set((ushort)index);
                    }
                    vertexToVertexMap.Remove((ushort)vindexLive);
                    for (int i = 0; i < workSet.Length; i++)
                    {
                        vertexToVertexMap.Add((ushort)vindexLive, workSet[i]);
                    }
                    //Debug.Assert(workSet.Length > 0);

                    // p2にBoneWeightを結合
                    var bw = boneWeights[vindexLive];
                    bw.AddWeight(boneWeights[vindexDead]);
                    boneWeights[vindexLive] = bw;

                    // 属性
                    var attr1 = attributes[vindexDead];
                    var attr2 = attributes[vindexLive];
                    attributes[vindexLive] = VertexAttribute.JoinAttribute(attr1, attr2);
                    attributes[vindexDead] = VertexAttribute.Invalid; // 削除頂点は無効にする
                }

                // 削除頂点数記録
                result.Value = cnt;
            }
        }
#endif

        //=========================================================================================
        /// <summary>
        /// 接続状態を最新に更新するジョブを発行する
        /// </summary>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        void UpdateJoinAndLink()
        {
            // JoinIndexの状態を更新する。現在の最新の生存ポイントを指すように変更する
            var updateJoinIndexJob = new UpdateJoinIndexJob()
            {
                joinIndices = workData.vertexJoinIndices,
            };
            updateJoinIndexJob.Run(vmesh.VertexCount);

            // 頂点の接続頂点リストを最新に更新する。すべて最新の生存ポイントを指すように変更する
            var updateLinkIndexJob = new UpdateLinkIndexJob()
            {
                joinIndices = workData.vertexJoinIndices,
                vertexToVertexMap = workData.vertexToVertexMap,
            };
            updateLinkIndexJob.Run(vmesh.VertexCount);
        }

        [BurstCompile]
        struct UpdateJoinIndexJob : IJobParallelFor
        {
            [NativeDisableParallelForRestriction]
            public NativeArray<int> joinIndices;

            public void Execute(int vindex)
            {
                int join = joinIndices[vindex];
                if (join >= 0)
                {
                    // 削除されている
                    // 最終的な生存ポイントに連結させる
                    while (joinIndices[join] >= 0)
                    {
                        join = joinIndices[join];
                    }
                    joinIndices[vindex] = join;
                }
            }
        }

        [BurstCompile]
        struct UpdateLinkIndexJob : IJobParallelFor
        {
            [NativeDisableParallelForRestriction]
            public NativeArray<int> joinIndices;
            public NativeParallelMultiHashMap<ushort, ushort> vertexToVertexMap;

            public void Execute(int vindex)
            {
                int join = joinIndices[vindex];

                // 自身が削除されている場合は無視
                if (join >= 0)
                    return;

                // 自身が生存している
                // 現在の接続インデックスから削除されたものを生存インデックスに入れ替える
                var newLinkSet = new FixedList512Bytes<ushort>();
                foreach (ushort i in vertexToVertexMap.GetValuesForKey((ushort)vindex))
                {
                    int tvindex = i;
                    int tjoin = joinIndices[tvindex];
                    if (tjoin >= 0)
                    {
                        // 削除されている
                        tvindex = tjoin;
                        Debug.Assert(joinIndices[tvindex] < 0);
                    }

                    // 自身は弾く
                    if (tvindex == vindex)
                        continue;

                    newLinkSet.MC2Set((ushort)tvindex);
                }
                // 生存のみの新しいセットに入れ替え
                vertexToVertexMap.Remove((ushort)vindex);
                for (int i = 0; i < newLinkSet.Length; i++)
                {
                    vertexToVertexMap.Add((ushort)vindex, newLinkSet[i]);
                }
                //Debug.Assert(newLinkSet.Length > 0);
            }
        }

        //=========================================================================================
        /// <summary>
        /// リダクション後のデータを整える
        /// </summary>
        void UpdateReductionResultJob()
        {
            // 頂点法線の単位化、およびボーンウエイトを１に整える
            var finalVertexJob = new FinalMergeVertexJob()
            {
                joinIndices = workData.vertexJoinIndices,
                localNormals = vmesh.localNormals.GetNativeArray(),
                boneWeights = vmesh.boneWeights.GetNativeArray(),
            };
            finalVertexJob.Run(vmesh.VertexCount);
        }

        [BurstCompile]
        struct FinalMergeVertexJob : IJobParallelFor
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<int> joinIndices;

            public NativeArray<float3> localNormals;
            public NativeArray<VirtualMeshBoneWeight> boneWeights;

            // 頂点ごと
            public void Execute(int vindex)
            {
                int join = joinIndices[vindex];
                if (join >= 0)
                {
                    // 削除されている
                    return;
                }

                // 法線単位化
                localNormals[vindex] = math.normalize(localNormals[vindex]);

                // ボーンウエイトを平均化
                var bw = boneWeights[vindex];
                bw.AdjustWeight();
                boneWeights[vindex] = bw;
            }
        }
    }
}
