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
        NativeParallelHashSet<int2> joinPairSet;
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
            if (joinPairSet.IsCreated)
                joinPairSet.Dispose();
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
                joinPairSet = new NativeParallelHashSet<int2>(vmesh.VertexCount / 4, Allocator.Persistent);
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
                    joinPairSet = joinPairSet,
                };
                searchJoinJob.Run();

                // 結合する
                var joinJob = new JoinJob()
                {
                    joinPairSet = joinPairSet,
                    joinIndices = workData.vertexJoinIndices,
                    vertexToVertexMap = workData.vertexToVertexMap,
                    boneWeights = vmesh.boneWeights.GetNativeArray(),
                    attributes = vmesh.attributes.GetNativeArray(),
                    result = resultRef,
                };
                joinJob.Run();

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

            public NativeParallelHashSet<int2> joinPairSet;

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
                            int2 ehash = DataUtility.PackInt2(vindex, tvindex);
                            joinPairSet.Add(ehash);
                        }
                    }
                }
            }
        }

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
                    int vindex2 = ehash[0]; // 生存側
                    int vindex1 = ehash[1]; // 削除側

                    // 両方とも生存インデックスに変換する
                    while (joinIndices[vindex1] >= 0)
                    {
                        vindex1 = joinIndices[vindex1];
                    }
                    while (joinIndices[vindex2] >= 0)
                    {
                        vindex2 = joinIndices[vindex2];
                    }
                    if (vindex1 == vindex2)
                        continue;

                    // 結合(vertex1 -> vertex2)
                    joinIndices[vindex1] = vindex2;
                    cnt++;

                    // 接続数を結合する（重複は弾かれる）
                    workSet.Clear();
                    foreach (ushort i in vertexToVertexMap.GetValuesForKey((ushort)vindex1))
                    {
                        int index = i;
                        // 生存インデックス
                        while (joinIndices[index] >= 0)
                        {
                            index = joinIndices[index];
                        }
                        if (index != vindex1 && index != vindex2)
                            workSet.Set((ushort)index);
                    }
                    foreach (ushort i in vertexToVertexMap.GetValuesForKey((ushort)vindex2))
                    {
                        int index = i;
                        // 生存インデックス
                        while (joinIndices[index] >= 0)
                        {
                            index = joinIndices[index];
                        }
                        if (index != vindex1 && index != vindex2)
                            workSet.Set((ushort)index);
                    }
                    vertexToVertexMap.Remove((ushort)vindex2);
                    for (int i = 0; i < workSet.Length; i++)
                    {
                        vertexToVertexMap.Add((ushort)vindex2, workSet[i]);
                    }
                    //Debug.Assert(workSet.Length > 0);

                    // p2にBoneWeightを結合
                    var bw = boneWeights[vindex2];
                    bw.AddWeight(boneWeights[vindex1]);
                    boneWeights[vindex2] = bw;

                    // 属性
                    var attr1 = attributes[vindex1];
                    var attr2 = attributes[vindex2];
                    attributes[vindex2] = VertexAttribute.JoinAttribute(attr1, attr2);
                    attributes[vindex1] = VertexAttribute.Invalid; // 削除頂点は無効にする
                }

                // 削除頂点数記録
                result.Value = cnt;
            }
        }

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

                    newLinkSet.Set((ushort)tvindex);
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
