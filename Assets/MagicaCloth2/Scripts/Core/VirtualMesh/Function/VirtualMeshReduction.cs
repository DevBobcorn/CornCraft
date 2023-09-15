// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using System.Threading;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth2
{
    public partial class VirtualMesh
    {
        //static readonly ProfilerMarker reductionProfiler = new ProfilerMarker("Reduction");

        //=========================================================================================
        /// <summary>
        /// リダクションを実行する（スレッド可）
        /// 処理時間が長いためCancellationTokenを受け入れる
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="ct"></param>
        public void Reduction(ReductionSettings settings, CancellationToken ct)
        {
            //reductionProfiler.Begin();

            try
            {
                // リダクション作業データ
                using var workData = new ReductionWorkData(this);

                // 作業データの初期化
                InitReductionWorkData(workData);
                if (result.IsError())
                {
                    throw new MagicaClothProcessingException();
                }

                // リダクションのリニア距離をAABBの最大の辺の長さを基準に算出する
                float maxSideLength = boundingBox.Value.MaxSideLength;
                if (maxSideLength < 1e-08f)
                {
                    result.SetError(Define.Result.Reduction_MaxSideLengthZero);
                    throw new MagicaClothProcessingException(); // リダクション失敗
                }
                float sameDistance = maxSideLength * math.saturate(Define.System.ReductionSameDistance);
                float simpleDistance = maxSideLength * math.saturate(settings.simpleDistance);
                float shapeDistance = maxSideLength * math.saturate(settings.shapeDistance);
                //Develop.DebugLog($"ReductionDista. maxSideLength:{maxSideLength}, same:{sameDistance}, simple:{simpleDistance}, shape:{shapeDistance}");

                // 同一距離リダクション
                ct.ThrowIfCancellationRequested();
#if true
                using (var sameReduction = new SameDistanceReduction(this.name, this, workData, sameDistance))
                {
                    sameReduction.Reduction();
                    if (sameReduction.Result.IsError())
                    {
                        result = sameReduction.Result;
                        throw new MagicaClothProcessingException(); // リダクション失敗
                    }
                    //workData.DebugVerify();
                }
#endif

                // 距離リダクション
                ct.ThrowIfCancellationRequested();
                if (simpleDistance > sameDistance)
                {
                    float startDistance = math.min(sameDistance * 2.0f, simpleDistance);
                    using (var simpleReduction = new SimpleDistanceReduction(this.name, this, workData, startDistance, simpleDistance, Define.System.ReductionMaxStep, Define.System.ReductionDontMakeLine, Define.System.ReductionJoinPositionAdjustment))
                    {
                        simpleReduction.Reduction();
                        if (simpleReduction.Result.IsError())
                        {
                            result = simpleReduction.Result;
                            throw new MagicaClothProcessingException(); // リダクション失敗
                        }
                        //workData.DebugVerify();
                    }
                }

                // 接続リダクション
                ct.ThrowIfCancellationRequested();
                if (shapeDistance > 0.0f && shapeDistance > simpleDistance)
                {
                    float startDistance = math.min(math.max(sameDistance * 2.0f, simpleDistance), shapeDistance);
                    using (var shapeReduction = new ShapeDistanceReduction(this.name, this, workData, startDistance, shapeDistance, Define.System.ReductionMaxStep, Define.System.ReductionDontMakeLine, Define.System.ReductionJoinPositionAdjustment))
                    {
                        shapeReduction.Reduction();
                        if (shapeReduction.Result.IsError())
                        {
                            result = shapeReduction.Result;
                            throw new MagicaClothProcessingException(); // リダクション失敗
                        }
                        //workData.DebugVerify();
                    }
                }

                //workData.DebugInfo();

                // リダクション結果からメッシュを再構成する
                ct.ThrowIfCancellationRequested();
                Organization(settings, workData);
                if (result.IsError())
                {
                    throw new MagicaClothProcessingException();
                }

                //Debug.Log($"Vertex:{workData.newVertexCount}, Line:{workData.newLineList.Length}, Triangle:{workData.newTriangleList.Length}");

                // 結果をvmeshに書き込む
                ct.ThrowIfCancellationRequested();
                OrganizeStoreVirtualMesh(workData);
                if (result.IsError())
                {
                    throw new MagicaClothProcessingException();
                }

                // 平均頂点間距離を再計算する
                CalcAverageAndMaxVertexDistanceRun();

                // 成功
                ct.ThrowIfCancellationRequested();
            }
            catch (MagicaClothProcessingException)
            {
                if (result.IsError() == false)
                    result.SetError(Define.Result.Reduction_UnknownError);
                result.DebugLog();
            }
            catch (OperationCanceledException)
            {
                result.SetCancel();
            }
            catch (Exception exception)
            {
                Debug.LogError(exception);
                result.SetError(Define.Result.Reduction_Exception);
            }
            finally
            {
            }

            //reductionProfiler.End();
        }

        //=========================================================================================
        /// <summary>
        /// リダクション用作業データの初期化
        /// </summary>
        /// <param name="workData"></param>
        void InitReductionWorkData(ReductionWorkData workData)
        {
            try
            {
                int vertexCount = VertexCount;
                int triangleCount = TriangleCount;

                // 頂点結合先リストの作成
                workData.vertexJoinIndices = new NativeArray<int>(vertexCount, Allocator.Persistent);
                JobUtility.FillRun(workData.vertexJoinIndices, vertexCount, -1);

                // 頂点ごとの接続先頂点マップの構築
                workData.vertexToVertexMap = new NativeParallelMultiHashMap<ushort, ushort>(vertexCount, Allocator.Persistent);
                new Reduction_InitVertexToVertexJob2()
                {
                    triangleCount = triangleCount,
                    triangles = triangles.GetNativeArray(),
                    vertexToVertexMap = workData.vertexToVertexMap,
                }.Run();
            }
            catch (Exception)
            {
                result.SetError(Define.Result.Reduction_InitError);
            }
        }

        [BurstCompile]
        unsafe struct Reduction_InitVertexToVertexJob2 : IJob
        {
            public int triangleCount;

            [Unity.Collections.ReadOnly]
            public NativeArray<int3> triangles;

            public NativeParallelMultiHashMap<ushort, ushort> vertexToVertexMap;

            public void Execute()
            {
                for (int i = 0; i < triangleCount; i++)
                {
                    int3 tri = triangles[i];

                    ushort x = (ushort)tri.x;
                    ushort y = (ushort)tri.y;
                    ushort z = (ushort)tri.z;

                    vertexToVertexMap.Add(x, y);
                    vertexToVertexMap.Add(x, z);
                    vertexToVertexMap.Add(y, x);
                    vertexToVertexMap.Add(y, z);
                    vertexToVertexMap.Add(z, x);
                    vertexToVertexMap.Add(z, y);
                }
            }
        }

#if false
        [BurstCompile]
        unsafe struct Reduction_InitVertexToVertexJob : IJob
        {
            public int triangleCount;

            [Unity.Collections.ReadOnly]
            public NativeArray<int3> triangles;

            public NativeArray<FixedList128Bytes<ushort>> vertexToVertexArray;

            public void Execute()
            {
                var arrayPtr = (FixedList128Bytes<ushort>*)vertexToVertexArray.GetUnsafePtr();

                for (int i = 0; i < triangleCount; i++)
                {
                    int3 tri = triangles[i];

                    var ptrx = (arrayPtr + tri.x);
                    var ptry = (arrayPtr + tri.y);
                    var ptrz = (arrayPtr + tri.z);

                    ushort x = (ushort)tri.x;
                    ushort y = (ushort)tri.y;
                    ushort z = (ushort)tri.z;
                    ptrx->SetLimit(y);
                    ptrx->SetLimit(z);
                    ptry->SetLimit(x);
                    ptry->SetLimit(z);
                    ptrz->SetLimit(x);
                    ptrz->SetLimit(y);
                }
            }
        }
#endif

        //=========================================================================================
        /// <summary>
        /// リダクション結果からデータを再編成する
        /// </summary>
        /// <param name="setting"></param>
        /// <param name="workData"></param>
        void Organization(ReductionSettings setting, ReductionWorkData workData)
        {
            try
            {
                // 再編成に必要なすべての準備を整える
                OrganizationInit(setting, workData);

                // リマップデータの作成
                OrganizationCreateRemapData(workData);

                // 基本データ作成
                OrganizationCreateBasicData(workData);

                // ライン／トライアングル生成
                OrganizationCreateLineTriangle(workData);
            }
            catch (Exception)
            {
                result.SetError(Define.Result.Reduction_OrganizationError);
            }
        }

        //=========================================================================================
        /// <summary>
        /// 再編成に必要なすべての準備を整える
        /// </summary>
        /// <param name="setting"></param>
        /// <param name="workData"></param>
        void OrganizationInit(ReductionSettings setting, ReductionWorkData workData)
        {
            // 最終的な頂点数
            // 生存頂点のみのリスト
            // 旧頂点リストからの連動インデックス
            workData.oldVertexCount = VertexCount;
            workData.newVertexCount = workData.oldVertexCount - workData.removeVertexCount;

            int newVertexCount = workData.newVertexCount;

            // 削除された頂点やボーンを生存するデータへ接続する
            // OrganizationCreateRemapData用
            {
                // 頂点リマップデータ
                workData.vertexRemapIndices = new NativeArray<int>(workData.oldVertexCount, Allocator.Persistent);

                // スキンボーンリマップデータ作成
                workData.useSkinBoneMap = new NativeParallelHashMap<int, int>(SkinBoneCount, Allocator.Persistent);

                // 使用ボーンに新しいインデックスを割り振る
                // ついでに新しいスキンボーンリストとバインドポーズのリストを作成する
                workData.newSkinBoneCount = new NativeReference<int>(Allocator.Persistent);
                workData.newSkinBoneTransformIndices = new NativeList<int>(SkinBoneCount, Allocator.Persistent);
                workData.newSkinBoneBindPoseList = new NativeList<float4x4>(SkinBoneCount, Allocator.Persistent);
            }

            // 基本データの作成
            //OrganizationCreateBasicData用
            {
                workData.newAttributes = new ExSimpleNativeArray<VertexAttribute>(newVertexCount);
                workData.newLocalPositions = new ExSimpleNativeArray<float3>(newVertexCount);
                workData.newLocalNormals = new ExSimpleNativeArray<float3>(newVertexCount);
                workData.newLocalTangents = new ExSimpleNativeArray<float3>(newVertexCount);
                workData.newUv = new ExSimpleNativeArray<float2>(newVertexCount);
                workData.newBoneWeights = new ExSimpleNativeArray<VirtualMeshBoneWeight>(newVertexCount);

                // 新しい頂点の接続頂点リスト
                workData.newVertexToVertexMap = new NativeParallelMultiHashMap<ushort, ushort>(newVertexCount, Allocator.Persistent);
            }

            // ライン／トライアングル生成
            // OrganizationCreateLineTriangle用
            {
                workData.edgeSet = new NativeParallelHashSet<int2>(newVertexCount * 2, Allocator.Persistent);
                workData.triangleSet = new NativeParallelHashSet<int3>(newVertexCount, Allocator.Persistent);
                workData.newLineList = new NativeList<int2>(newVertexCount, Allocator.Persistent);
                workData.newTriangleList = new NativeList<int3>(newVertexCount, Allocator.Persistent);
            }
        }

        //=========================================================================================
        /// <summary>
        /// リマップデータの作成
        /// 削除された頂点やボーンを生存するデータへ接続する
        /// </summary>
        /// <param name="workData"></param>
        void OrganizationCreateRemapData(ReductionWorkData workData)
        {
            // 頂点リマップデータ作成
            // 生存頂点にインデックスを割り振る、削除頂点に新しい生存頂点インデックスを割り振る
            var vertexRemapJob = new Organize_RemapVertexJob()
            {
                oldVertexCount = workData.oldVertexCount,
                joinIndices = workData.vertexJoinIndices,
                vertexRemapIndices = workData.vertexRemapIndices,
            };
            vertexRemapJob.Run();

            // スキニングボーンのリマップデータ作成
            // 使用しているボーンインデックスを収集する
            //Debug.Log($"workData.useSkinBoneMap count:{workData.useSkinBoneMap.Count()}");
            using var useSkinBoneMapKeyList = new NativeList<int>(Allocator.Persistent); // Unity2023.1.5対応
            var collectUseSkinJob = new Organize_CollectUseSkinBoneJob()
            {
                oldVertexCount = workData.oldVertexCount,
                joinIndices = workData.vertexJoinIndices,
                oldBoneWeights = boneWeights.GetNativeArray(),
                oldBindPoses = skinBoneBindPoses.GetNativeArray(),
                useSkinBoneMap = workData.useSkinBoneMap,
                newSkinBoneTransformIndices = workData.newSkinBoneTransformIndices,
                newSkinBoneBindPoses = workData.newSkinBoneBindPoseList,
                newSkinBoneCount = workData.newSkinBoneCount,
                useSkinBoneMapKeyList = useSkinBoneMapKeyList, // Unity2023.1.5対応
            };
            collectUseSkinJob.Run();
        }

        /// <summary>
        /// 生存頂点にインデックスを割り振る
        /// </summary>
        [BurstCompile]
        struct Organize_RemapVertexJob : IJob
        {
            public int oldVertexCount;

            [Unity.Collections.ReadOnly]
            public NativeArray<int> joinIndices;
            public NativeArray<int> vertexRemapIndices;

            public void Execute()
            {
                // 生存頂点に新しいインデックスを割り振る
                int remapIndex = 0;
                for (int i = 0; i < oldVertexCount; i++)
                {
                    int join = joinIndices[i];
                    if (join < 0)
                    {
                        // live
                        vertexRemapIndices[i] = remapIndex;
                        remapIndex++;
                    }
                }

                // 削除頂点に新しい生存頂点インデックスを割り振る
                for (int i = 0; i < oldVertexCount; i++)
                {
                    int join = joinIndices[i];
                    if (join >= 0)
                    {
                        // delete
                        vertexRemapIndices[i] = vertexRemapIndices[join];
                    }
                }
            }
        }

        /// <summary>
        /// 使用しているスキニングボーンを収集する
        /// </summary>
        [BurstCompile]
        struct Organize_CollectUseSkinBoneJob : IJob
        {
            public int oldVertexCount;

            [Unity.Collections.ReadOnly]
            public NativeArray<int> joinIndices;
            [Unity.Collections.ReadOnly]
            public NativeArray<VirtualMeshBoneWeight> oldBoneWeights;
            [Unity.Collections.ReadOnly]
            public NativeArray<float4x4> oldBindPoses;

            public NativeParallelHashMap<int, int> useSkinBoneMap;

            public NativeList<int> newSkinBoneTransformIndices;
            public NativeList<float4x4> newSkinBoneBindPoses;
            public NativeReference<int> newSkinBoneCount;

            public NativeList<int> useSkinBoneMapKeyList; // Unity2023.1.5対応

            public void Execute()
            {
                // 生存頂点で使用されている旧ボーンインデックスを収集する
                for (int vindex = 0; vindex < oldVertexCount; vindex++)
                {
                    int join = joinIndices[vindex];
                    if (join >= 0)
                        continue; // isDelete

                    // 使用している旧ボーンインデックスを収集する
                    var bw = oldBoneWeights[vindex];
                    for (int i = 0; i < 4; i++)
                    {
                        if (bw.weights[i] > 0.0f)
                        {
                            //Debug.Log(bw.boneIndices[i]);

                            // まずは旧ボーンインデックスを収集
                            int index = bw.boneIndices[i];
                            useSkinBoneMap.TryAdd(bw.boneIndices[i], 0);
                        }
                    }
                }

                // 利用されるボーンに連番をふる
                // Unity2023.1.5対応
                //var oldBoneIndexArray = useSkinBoneMap.GetKeyArray(Allocator.Temp);
                useSkinBoneMapKeyList.Clear();
                foreach (var kv in useSkinBoneMap)
                {
                    useSkinBoneMapKeyList.Add(kv.Key);
                }

                // 不要なトランスフォームを削除した新しいスキニングボーンとバインドポーズのリストを作成する
                //for (int i = 0; i < oldBoneIndexArray.Length; i++) // Unity2023.1.5対応
                for (int i = 0; i < useSkinBoneMapKeyList.Length; i++) // Unity2023.1.5対応
                {
                    //int oldBoneIndex = oldBoneIndexArray[i]; // Unity2023.1.5対応
                    int oldBoneIndex = useSkinBoneMapKeyList[i]; // Unity2023.1.5対応
                    useSkinBoneMap[oldBoneIndex] = i;

                    // 新しいトランスフォームインデックス
                    newSkinBoneTransformIndices.Add(i);

                    // バインドポーズ
                    newSkinBoneBindPoses.Add(oldBindPoses[oldBoneIndex]);
                }

                // 最適化後のスキンボーン数
                //newSkinBoneCount.Value = oldBoneIndexArray.Length; // Unity2023.1.5対応
                newSkinBoneCount.Value = useSkinBoneMapKeyList.Length; // Unity2023.1.5対応
            }
        }

        //=========================================================================================
        /// <summary>
        /// 基本データ作成
        /// Line/Triangleを再編成するための基本的なデータを作成する
        /// </summary>
        /// <param name="workData"></param>
        void OrganizationCreateBasicData(ReductionWorkData workData)
        {
            int newVertexCount = workData.newVertexCount;
            int oldVertexCount = workData.oldVertexCount;

            // position/normal/tangent/attributeコピー
            var copyVertexJob = new Organize_CopyVertexJob()
            {
                joinIndices = workData.vertexJoinIndices,
                vertexRemapIndices = workData.vertexRemapIndices,
                oldAttributes = attributes.GetNativeArray(),
                oldLocalPositions = localPositions.GetNativeArray(),
                oldLocalNormals = localNormals.GetNativeArray(),
                oldLocalTangents = localTangents.GetNativeArray(),
                newAttributes = workData.newAttributes.GetNativeArray(),
                newLocalPositions = workData.newLocalPositions.GetNativeArray(),
                newLocalNormals = workData.newLocalNormals.GetNativeArray(),
                newLocalTangents = workData.newLocalTangents.GetNativeArray(),
            };
            copyVertexJob.Run(oldVertexCount);

            // 新しいuvを計算する（このUVは接線計算用でありテクスチャ用ではないので注意！）
            JobUtility.CalcUVWithSphereMappingRun(workData.newLocalPositions.GetNativeArray(), newVertexCount, workData.vmesh.boundingBox, workData.newUv.GetNativeArray());

            // 新しいボーンウエイトリストを作成する
            var remapBoneWeightJob = new Organize_RemapBoneWeightJob()
            {
                joinIndices = workData.vertexJoinIndices,
                vertexRemapIndices = workData.vertexRemapIndices,
                useSkinBoneMap = workData.useSkinBoneMap,
                oldSkinBoneIndices = skinBoneTransformIndices.GetNativeArray(),
                oldBoneWeights = boneWeights.GetNativeArray(),
                newBoneWeights = workData.newBoneWeights.GetNativeArray(),
            };
            remapBoneWeightJob.Run(oldVertexCount);

            // 新しい頂点の接続頂点リストを作成する
            var remapLinkPointArrayJob = new Organize_RemapLinkPointArrayJob()
            {
                joinIndices = workData.vertexJoinIndices,
                vertexRemapIndices = workData.vertexRemapIndices,
                oldVertexToVertexMap = workData.vertexToVertexMap,
                newVertexToVertexMap = workData.newVertexToVertexMap,
            };
            remapLinkPointArrayJob.Run(VertexCount);

        }

        /// <summary>
        /// 新しい頂点にリダクション後のPositin/Normal/Tangent/Attributeをコピーする
        /// </summary>
        [BurstCompile]
        struct Organize_CopyVertexJob : IJobParallelFor
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<int> joinIndices;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> vertexRemapIndices;
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> oldAttributes;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> oldLocalPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> oldLocalNormals;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> oldLocalTangents;

            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<VertexAttribute> newAttributes;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> newLocalPositions;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> newLocalNormals;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> newLocalTangents;

            public void Execute(int index)
            {
                int join = joinIndices[index];
                if (join < 0)
                {
                    // live
                    int remapIndex = vertexRemapIndices[index];
                    newAttributes[remapIndex] = oldAttributes[index];
                    newLocalPositions[remapIndex] = oldLocalPositions[index];
                    newLocalNormals[remapIndex] = oldLocalNormals[index];
                    newLocalTangents[remapIndex] = oldLocalTangents[index];
                }
            }
        }

        /// <summary>
        /// 新しいボーンウエイトリストを作成する
        /// </summary>
        [BurstCompile]
        struct Organize_RemapBoneWeightJob : IJobParallelFor
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<int> joinIndices;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> vertexRemapIndices;
            [Unity.Collections.ReadOnly]
            public NativeParallelHashMap<int, int> useSkinBoneMap;

            [Unity.Collections.ReadOnly]
            public NativeArray<int> oldSkinBoneIndices;
            [Unity.Collections.ReadOnly]
            public NativeArray<VirtualMeshBoneWeight> oldBoneWeights;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<VirtualMeshBoneWeight> newBoneWeights;

            public void Execute(int vindex)
            {
                int join = joinIndices[vindex];
                if (join < 0)
                {
                    // live
                    int remapIndex = vertexRemapIndices[vindex];
                    var bw = oldBoneWeights[vindex];

                    // ボーンインデックスリマップ
                    for (int j = 0; j < 4; j++)
                    {
                        if (bw.weights[j] > 0.0f)
                        {
                            int oldBoneIndex = bw.boneIndices[j];
                            bw.boneIndices[j] = useSkinBoneMap[oldBoneIndex];
                        }
                        else
                        {
                            bw.boneIndices[j] = 0;
                        }
                    }
                    newBoneWeights[remapIndex] = bw;
                }
            }
        }

        /// <summary>
        /// 新しい頂点の接続頂点リストを作成する
        /// </summary>
        [BurstCompile]
        struct Organize_RemapLinkPointArrayJob : IJobParallelFor
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<int> joinIndices;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> vertexRemapIndices;
            [Unity.Collections.ReadOnly]
            public NativeParallelMultiHashMap<ushort, ushort> oldVertexToVertexMap;
            [NativeDisableParallelForRestriction]
            public NativeParallelMultiHashMap<ushort, ushort> newVertexToVertexMap;

            public void Execute(int vindex)
            {
                int join = joinIndices[vindex];
                if (join >= 0)
                    return; // 自身は削除されている

                int newIndex = vertexRemapIndices[vindex];

                foreach (var oldVertexIndex in oldVertexToVertexMap.GetValuesForKey((ushort)vindex))
                {
                    int newIndex2 = vertexRemapIndices[oldVertexIndex];
                    //Debug.Assert(newIndex != newIndex2);
                    newVertexToVertexMap.UniqueAdd((ushort)newIndex, (ushort)newIndex2);
                }

                //Debug.Log($"[{newIndex}] cnt:{newVertexToVertexMap.CountValuesForKey((ushort)newIndex)}");
                //Debug.Assert(newVertexToVertexMap.ContainsKey((ushort)newIndex));
                //Debug.Assert(newVertexToVertexMap.CountValuesForKey((ushort)newIndex) > 0);
            }
        }

        //=========================================================================================
        /// <summary>
        /// 新しいラインとトライアングルを生成する
        /// </summary>
        /// <param name="workData"></param>
        void OrganizationCreateLineTriangle(ReductionWorkData workData)
        {
            // 新しい頂点接続情報からエッジセットを作成する
            var createLineTriangleJob = new Organize_CreateLineTriangleJob()
            {
                newVertexCount = workData.newVertexCount,
                newVertexToVertexMap = workData.newVertexToVertexMap,
                edgeSet = workData.edgeSet,
            };
            createLineTriangleJob.Run();

            // エッジセットからラインとトライアングルセットを作成する
            var createLineTriangleJob2 = new Organize_CreateLineTriangleJob2()
            {
                newVertexToVertexMap = workData.newVertexToVertexMap,
                newLineList = workData.newLineList,
                edgeSet = workData.edgeSet,
                triangleSet = workData.triangleSet,
            };
            createLineTriangleJob2.Run();

            // トライアングルセットからトライアングルリストを作成する
            var createNewTriangleJob3 = new Organize_CreateNewTriangleJob3()
            {
                newTriangleList = workData.newTriangleList,
                triangleSet = workData.triangleSet,
            };
            createNewTriangleJob3.Run();
        }

        /// <summary>
        /// エッジセットを作成する
        /// </summary>
        [BurstCompile]
        struct Organize_CreateLineTriangleJob : IJob
        {
            public int newVertexCount;

            [Unity.Collections.ReadOnly]
            public NativeParallelMultiHashMap<ushort, ushort> newVertexToVertexMap;

            [Unity.Collections.WriteOnly]
            public NativeParallelHashSet<int2> edgeSet;

            public void Execute()
            {
                // 新しい頂点接続情報からエッジセットを作成する
                //Debug.Log($"newVertexCount:{newVertexCount}");
                for (int vindex = 0; vindex < newVertexCount; vindex++)
                {
                    //Debug.Assert(newVertexToVertexMap.CountValuesForKey((ushort)vindex) > 0);

                    foreach (ushort vindex2 in newVertexToVertexMap.GetValuesForKey((ushort)vindex))
                    {
                        int2 edge = DataUtility.PackInt2(vindex, vindex2);
                        edgeSet.Add(edge);

                        //Debug.Log($"edge {vindex} -> {vindex2}");
                    }
                }
            }
        }

        /// <summary>
        /// エッジセットからラインとトライアングルセットを作成する
        /// </summary>
        [BurstCompile]
        struct Organize_CreateLineTriangleJob2 : IJob
        {
            [Unity.Collections.ReadOnly]
            public NativeParallelMultiHashMap<ushort, ushort> newVertexToVertexMap;

            [Unity.Collections.WriteOnly]
            public NativeList<int2> newLineList;

            [Unity.Collections.ReadOnly]
            public NativeParallelHashSet<int2> edgeSet;
            [Unity.Collections.WriteOnly]
            public NativeParallelHashSet<int3> triangleSet;

            public void Execute()
            {
                // エッジセットからトライアングルとラインを生成する
                foreach (int2 edge in edgeSet)
                {
                    int tcnt = 0;
                    foreach (ushort vindex in newVertexToVertexMap.GetValuesForKey((ushort)edge.x))
                    {
                        if (vindex == edge.x || vindex == edge.y)
                            continue;

                        if (newVertexToVertexMap.Contains((ushort)edge.y, vindex))
                        {
                            // トライアングル生成
                            int3 tri = DataUtility.PackInt3(edge.x, edge.y, vindex);
                            triangleSet.Add(tri);
                            tcnt++;

                            //if (math.all(tri - 927) == false)
                            //    Debug.Log($"tri:{tri}");
                        }
                    }
                    // トライアングルが生成出来ない場合はラインとして登録する
                    if (tcnt == 0)
                    {
                        newLineList.Add(edge);

                        //if (math.all(edge - 927) == false)
                        //    Debug.Log($"line:{edge}");
                    }
                }
            }
        }

        /// <summary>
        /// トライアングルセットからトライアングルリストを作成する
        /// </summary>
        [BurstCompile]
        struct Organize_CreateNewTriangleJob3 : IJob
        {
            [Unity.Collections.WriteOnly]
            public NativeList<int3> newTriangleList;

            [Unity.Collections.ReadOnly]
            public NativeParallelHashSet<int3> triangleSet;

            public void Execute()
            {
                // トライアングルマップからトライアングルリストを生成する
                foreach (int3 tri in triangleSet)
                {
                    int3 newTri = tri;
                    newTriangleList.Add(newTri);

                    //if (math.all(tri - 927) == false)
                    //    Debug.Log($"new tri:{tri}");
                }
            }
        }

        //=========================================================================================
        /// <summary>
        /// リダクション結果をvmeshに反映させる
        /// </summary>
        /// <param name="setting"></param>
        /// <param name="workData"></param>
        void OrganizeStoreVirtualMesh(ReductionWorkData workData)
        {
            try
            {
                int vcnt = workData.newVertexCount;

                // 参照インデックス
                // すべて連番で再設定する
                referenceIndices.Dispose();
                referenceIndices = new ExSimpleNativeArray<int>(vcnt);
                JobUtility.SerialNumberRun(referenceIndices.GetNativeArray(), vcnt);

                // attribute
                attributes.Dispose();
                attributes = workData.newAttributes;
                workData.newAttributes = null;

                // positin
                localPositions.Dispose();
                localPositions = workData.newLocalPositions;
                workData.newLocalPositions = null;

                // normal
                localNormals.Dispose();
                localNormals = workData.newLocalNormals;
                workData.newLocalNormals = null;

                // tangent
                localTangents.Dispose();
                localTangents = workData.newLocalTangents;
                workData.newLocalTangents = null;

                // uv
                uv.Dispose();
                uv = workData.newUv;
                workData.newUv = null;

                // bone weight
                boneWeights.Dispose();
                boneWeights = workData.newBoneWeights;
                workData.newBoneWeights = null;

                // line
                lines.Dispose();
                lines = new ExSimpleNativeArray<int2>(workData.newLineList);

                // triangle
                triangles.Dispose();
                triangles = new ExSimpleNativeArray<int3>(workData.newTriangleList);

                // トランスフォーム情報再編成
                transformData.OrganizeReductionTransform(this, workData);

                // skin bone index
                skinBoneTransformIndices.Dispose();
                skinBoneTransformIndices = new ExSimpleNativeArray<int>(workData.newSkinBoneTransformIndices);

                // skin bone bind pose
                skinBoneBindPoses.Dispose();
                skinBoneBindPoses = new ExSimpleNativeArray<float4x4>(workData.newSkinBoneBindPoseList);

                // 元の頂点の結合頂点インデックス
                joinIndices = new NativeArray<int>(workData.vertexRemapIndices, Allocator.Persistent);
            }
            catch (Exception)
            {
                result.SetError(Define.Result.Reduction_StoreVirtualMeshError);
            }
        }
    }
}
