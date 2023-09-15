// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth2
{
    public partial class VirtualMesh
    {
        /// <summary>
        /// メッシュをプロキシメッシュに変換する（スレッド可）
        /// プロキシメッシュは頂点法線接線を接続するトライアングルから求めるようになる
        /// またマッピング用の頂点ごとのバインドポーズも保持する
        /// </summary>
        public void ConvertProxyMesh(
            ClothSerializeData sdata,
            TransformRecord clothTransformRecord,
            List<TransformRecord> customSkinningBoneRecords,
            TransformRecord normalAdjustmentTransformRecord
            )
        {
            try
            {
                if (IsError)
                {
                    //Debug.LogError($"Invalid VirtualMesh! [{this.name}]");
                    throw new MagicaClothProcessingException();
                }

                // カスタムスキニングボーン追加
                if (sdata.customSkinningSetting.enable)
                {
                    SetCustomSkinningBones(clothTransformRecord, customSkinningBoneRecords);
                }

                // 頂点に接続するトライアングル
                vertexToTriangles = new NativeArray<FixedList32Bytes<uint>>(VertexCount, Allocator.Persistent);

                // 頂点ごとのバインドポーズ
                vertexBindPosePositions = new NativeArray<float3>(VertexCount, Allocator.Persistent);
                vertexBindPoseRotations = new NativeArray<quaternion>(VertexCount, Allocator.Persistent);

                // 頂点ごとのTransform変換用回転
                vertexToTransformRotations = new NativeArray<quaternion>(VertexCount, Allocator.Persistent);
                JobUtility.FillRun(vertexToTransformRotations, VertexCount, quaternion.identity);

                // 頂点に接続する頂点リストを求める
                // エッジリストを作成する
                using var vertexDataBuilder = new MultiDataBuilder<ushort>(VertexCount, VertexCount * 4);
                using var edgeSet = new NativeParallelHashSet<int2>(VertexCount * 2, Allocator.Persistent);
                if (TriangleCount > 0)
                {
                    var calcTriangleVertexToVertexJob = new Proxy_CalcVertexToVertexFromTriangleJob()
                    {
                        triangleCount = TriangleCount,
                        triangles = triangles.GetNativeArray(),
                        vertexToVertexMap = vertexDataBuilder.Map,
                        edgeSet = edgeSet,
                    };
                    calcTriangleVertexToVertexJob.Run();
                }
                if (LineCount > 0)
                {
                    var calcLineVertexToVertexJob = new Proxy_CalcVertexToVertexFromLineJob()
                    {
                        lineCount = LineCount,
                        lines = lines.GetNativeArray(),
                        vertexToVertexMap = vertexDataBuilder.Map,
                        edgeSet = edgeSet,
                    };
                    calcLineVertexToVertexJob.Run();
                }

                // エッジをリスト化して格納する
                edges = edgeSet.ToNativeArray(Allocator.Persistent);
                //Debug.Log($"edges:{edges.Length}");

                // 頂点接続頂点をリスト化して格納する
                vertexDataBuilder.ToNativeArray(out vertexToVertexIndexArray, out vertexToVertexDataArray);

#if false
                // debug
                for (int i = 0; i < VertexCount; i++)
                {
                    uint pack = vertexToVertexIndexArray[i];
                    int cnt = DataUtility.Unpack10_22Hi(pack);
                    Debug.Assert(cnt > 0);
                    Debug.Log($"[{i}] cnt:{cnt}");
                }
#endif
#if false
                // 無効属性は移動属性に接続する場合のみ固定に変更する
                var convertInvalidJob = new Proxy_ConvertInvalidToFixedJob()
                {
                    attributes = attributes.GetNativeArray(),
                    vertexToVertexIndexArray = vertexToVertexIndexArray,
                    vertexToVertexDataArray = vertexToVertexDataArray,
                };
                convertInvalidJob.Run(VertexCount);
#endif

                // エッジごとの接続トライアングルを求める
                if (TriangleCount > 0)
                {
                    // エッジごとの接続トライアングルを求める
                    edgeToTriangles = new NativeParallelMultiHashMap<int2, ushort>(TriangleCount * 2, Allocator.Persistent);
                    var calcEdgeToTriangleJob = new Proxy_CalcEdgeToTriangleJob()
                    {
                        tcnt = TriangleCount,
                        triangles = triangles.GetNativeArray(),
                        edgeToTriangles = edgeToTriangles,
                    };
                    calcEdgeToTriangleJob.Run();

                    // トライアングル法線を求める
                    using var triNormals = new NativeArray<float3>(TriangleCount, Allocator.Persistent);
                    var calcTriangleNormalJob = new Proxy_CalcTriangleNormalJob()
                    {
                        triangles = triangles.GetNativeArray(),
                        localPositins = localPositions.GetNativeArray(),
                        triangleNormals = triNormals,
                    };
                    calcTriangleNormalJob.Run(TriangleCount);

                    // トライアングルの向きをできる限り揃える
                    //OptimizeTriangleDirection(triNormals, sdata.sameSurfaceAngle);
                    OptimizeTriangleDirection(triNormals, Define.System.SameSurfaceAngle);

                    // トライアングル接線を求める
                    using var triTangents = new NativeArray<float3>(TriangleCount, Allocator.Persistent);
                    var calcTriangleTangentJob = new Proxy_CalcTriangleTangentJob()
                    {
                        triangles = triangles.GetNativeArray(),
                        localPositins = localPositions.GetNativeArray(),
                        uv = uv.GetNativeArray(),
                        triangleTangents = triTangents,
                    };
                    calcTriangleTangentJob.Run(TriangleCount);

                    // 頂点に接続するトライアングルセットを求める（最大７つ）
                    var createVertexToTriangleJob = new Proxy_CreateVertexToTrianglesJob()
                    {
                        triangles = triangles.GetNativeArray(),
                        vertexToTriangles = vertexToTriangles,
                    };
                    createVertexToTriangleJob.Run();

                    // トライアングルから頂点法線を計算するためのvertexToTrianglesを求める
                    // また頂点がトライアングルに属する場合はフラグを立てる
                    var organizeVertexToTriangleJob = new Proxy_OrganizeVertexToTrianglsJob()
                    {
                        vertexToTriangles = vertexToTriangles,
                        triangleNormals = triNormals,
                        triangleTangents = triTangents,
                        attributes = attributes.GetNativeArray(),
                    };
                    organizeVertexToTriangleJob.Run(VertexCount);

                    // トライアングル組み合わせから法線と接線を求める
                    var calcVertexNormalTangentBindposeJob = new Proxy_CalcVertexNormalTangentFromTriangleJob()
                    {
                        triangleNormals = triNormals,
                        triangleTangents = triTangents,
                        vertexToTriangles = vertexToTriangles,
                        localNormals = localNormals.GetNativeArray(),
                        localTangents = localTangents.GetNativeArray(),
                    };
                    calcVertexNormalTangentBindposeJob.Run(VertexCount);
                }
                else
                {
                    // エッジごとの接続トライアングルは空にする
                    edgeToTriangles = new NativeParallelMultiHashMap<int2, ushort>(1, Allocator.Persistent);
                }

                // シミュレーションに関係する頂点からAABBと固定頂点リストを作成する
                ProxyCreateFixedListAndAABB();

                // ベースラインの作成
                if (isBoneCloth)
                {
                    // BoneCloth
                    CreateTransformBaseLine();
                }
                else
                {
                    // MeshCloth
                    CreateMeshBaseLine();
                }

                // 法線方向の調整
                ProxyNormalAdjustment(sdata, normalAdjustmentTransformRecord);

                // BoneClothではトランスフォーム書き戻し用の回転を求める
                if (isBoneCloth)
                {
                    var calcVertexToTransformJob = new Proxy_CalcVertexToTransformJob()
                    {
                        invRot = initInverseRotation,
                        localNormals = localNormals.GetNativeArray(),
                        localTangents = localTangents.GetNativeArray(),
                        vertexToTransformRotations = vertexToTransformRotations,
                        transformRotations = transformData.rotationArray.GetNativeArray(),
                    };
                    calcVertexToTransformJob.Run(VertexCount);
                }

                // 頂点のバインドポーズを求める
                var calcVertexBindposeJob = new Proxy_CalcVertexBindPoseJob2()
                {
                    localPositions = localPositions.GetNativeArray(),
                    localNormals = localNormals.GetNativeArray(),
                    localTangents = localTangents.GetNativeArray(),
                    vertexBindPosePositions = vertexBindPosePositions,
                    vertexBindPoseRotations = vertexBindPoseRotations,
                };
                calcVertexBindposeJob.Run(VertexCount);

                // エッジ固有フラグを設定
                edgeFlags = new NativeArray<ExBitFlag8>(EdgeCount, Allocator.Persistent);
                if (EdgeCount > 0)
                {
                    var createEdgeFlagJob = new Proxy_CreateEdgeFlagJob()
                    {
                        edges = edges,
                        edgeToTriangles = edgeToTriangles,
                        edgeFlags = edgeFlags,
                    };
                    createEdgeFlagJob.Run(EdgeCount);
                }

                // ベースラインの基本姿勢を求める
                CreateBaseLinePose();

                // 頂点ごとのルートインデックスと深さを求める
                CreateVertexRootAndDepth();

                // AABB再計算
                //JobUtility.CalcAABBRun(localPositions.GetNativeArray(), VertexCount, boundingBox);

                // カスタムスキニング設定
                if (sdata.customSkinningSetting.enable)
                {
                    CreateCustomSkinning(sdata.customSkinningSetting, customSkinningBoneRecords);
                }

                // メッシュタイプ
                if (isBoneCloth)
                    meshType = MeshType.ProxyBoneMesh;
                else
                    meshType = MeshType.ProxyMesh;
            }
            catch (MagicaClothProcessingException)
            {
                if (result.IsError() == false)
                    result.SetError(Define.Result.ProxyMesh_UnknownError);
                result.DebugLog();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                result.SetError(Define.Result.ProxyMesh_Exception);
            }
            finally
            {
            }
        }

#if false
        [BurstCompile]
        struct Proxy_ConvertInvalidToFixedJob : IJobParallelFor
        {
            // proxy mesh
            public NativeArray<VertexAttribute> attributes;
            [Unity.Collections.ReadOnly]
            public NativeArray<uint> vertexToVertexIndexArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<ushort> vertexToVertexDataArray;

            public void Execute(int vindex)
            {
                var attr = attributes[vindex];

                // 無効判定
                if (attr.IsInvalid())
                {
                    // 移動属性に接続する場合のみ固定に変更する
                    var pack = vertexToVertexIndexArray[vindex];
                    DataUtility.Unpack10_22(pack, out var dcnt, out var dstart);
                    for (int i = 0; i < dcnt; i++)
                    {
                        int tvindex = vertexToVertexDataArray[dstart + i];
                        var tattr = attributes[tvindex];
                        if (tattr.IsMove())
                        {
                            // 固定に変更
                            attr.SetFlag(VertexAttribute.Flag_Ignore, true);
                            attr.SetFlag(VertexAttribute.Flag_Fixed, true);
                            attributes[vindex] = attr;
                            return;
                        }
                    }
                }
            }
        }
#endif

        /// <summary>
        /// 法線方向の調整
        /// </summary>
        void ProxyNormalAdjustment(ClothSerializeData sdata, TransformRecord normalAdjustmentTransformRecord)
        {
            int vcnt = VertexCount;
            if (vcnt == 0)
                return;

            // 配列初期化（未使用でも領域確保）
            normalAdjustmentRotations = new NativeArray<quaternion>(vcnt, Allocator.Persistent);
            JobUtility.FillRun(normalAdjustmentRotations, vcnt, quaternion.identity);

            var mode = sdata.normalAlignmentSetting.alignmentMode;
            if (mode == NormalAlignmentSettings.AlignmentMode.None)
                return;

            // 中心からの放射
            if (mode == NormalAlignmentSettings.AlignmentMode.BoundingBoxCenter || mode == NormalAlignmentSettings.AlignmentMode.Transform)
            {
                float3 center;
                if (mode == NormalAlignmentSettings.AlignmentMode.BoundingBoxCenter)
                {
                    center = boundingBox.Value.Center;
                }
                else
                {
                    center = math.transform(initWorldToLocal, normalAdjustmentTransformRecord.position);
                }

                var job1 = new ProxyNormalRadiationAdjustmentJob()
                {
                    center = center,
                    localPositions = localPositions.GetNativeArray(),
                    vertexParentIndices = vertexParentIndices,
                    vertexChildIndexArray = vertexChildIndexArray,
                    vertexChildDataArray = vertexChildDataArray,

                    localNormals = localNormals.GetNativeArray(),
                    localTangents = localTangents.GetNativeArray(),
                    normalAdjustmentRotations = normalAdjustmentRotations,
                };
                job1.Run(vcnt);
            }
#if false // ★どうもうまくいかないので一旦停止！
            // 頂点ウエイトから
            else if (mode == NormalAlignmentSettings.AlignmentMode.BoneWeight)
            {
                var job2 = new ProxyNormalWeightAdjustmentJob()
                {
                    WtoL = initWorldToLocal,
                    localPositions = localPositions.GetNativeArray(),
                    boneWeights = boneWeights.GetNativeArray(),

                    transformPositionArray = transformData.positionArray.GetNativeArray(),

                    localNormals = localNormals.GetNativeArray(),
                    localTangents = localTangents.GetNativeArray(),
                    normalAdjustmentRotations = normalAdjustmentRotations,
                };
                job2.Run(vcnt);
            }
#endif
        }

#if false
        [BurstCompile]
        struct ProxyNormalWeightAdjustmentJob : IJobParallelFor
        {
            public float4x4 WtoL;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<VirtualMeshBoneWeight> boneWeights;

            // transform
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> transformPositionArray;

            // out
            //[Unity.Collections.WriteOnly]
            public NativeArray<float3> localNormals;
            //[Unity.Collections.WriteOnly]
            public NativeArray<float3> localTangents;
            [Unity.Collections.WriteOnly]
            public NativeArray<quaternion> normalAdjustmentRotations;

            public void Execute(int vindex)
            {
                var lpos = localPositions[vindex];
                var bw = boneWeights[vindex];

                // 現在の回転
                var lrot = MathUtility.ToRotation(localNormals[vindex], localTangents[vindex]);

                // 影響するボーンへの方向をまとめる
                float3 bv = 0;
                int bcnt = bw.Count;
                if (bcnt == 0)
                    return;
                for (int i = 0; i < bcnt; i++)
                {
                    var bonePos = math.transform(WtoL, transformPositionArray[bw.boneIndices[i]]);
                    //var v = transformPositionArray[bw.boneIndices[i]] - lpos;
                    var v = lpos - bonePos;
                    v = math.normalizesafe(v, float3.zero);
                    v *= bw.weights[i];
                    bv += v;
                }
                if (math.lengthsq(bv) < Define.System.Epsilon)
                    return;

                // 法線確定
                float3 n = math.normalize(bv);

                // 法線をもとに接線を計算する
                var lnor = localNormals[vindex];
                var ltan = localTangents[vindex];
                var dotNormal = math.dot(n, lnor);
                var dotTangent = math.dot(n, ltan);
                float3 tv = dotNormal < dotTangent ? lnor : ltan;
                float3 tan = math.cross(tv, n);

                //localNormals[vindex] = tan;
                //localTangents[vindex] = n;
                localNormals[vindex] = n;
                localTangents[vindex] = tan;
                var nrot = MathUtility.ToRotation(n, tan);

                // 補正用回転を算出し格納する
                normalAdjustmentRotations[vindex] = math.mul(math.inverse(lrot), nrot);
            }
        }
#endif

        [BurstCompile]
        struct ProxyNormalRadiationAdjustmentJob : IJobParallelFor
        {
            public float3 center;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> vertexParentIndices;
            [Unity.Collections.ReadOnly]
            public NativeArray<uint> vertexChildIndexArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<ushort> vertexChildDataArray;

            // out
            public NativeArray<float3> localNormals;
            public NativeArray<float3> localTangents;
            [Unity.Collections.WriteOnly]
            public NativeArray<quaternion> normalAdjustmentRotations;

            public void Execute(int vindex)
            {
                var lpos = localPositions[vindex];
                var v = lpos - center;
                if (math.length(v) < Define.System.Epsilon)
                    return;
                v = math.normalize(v);

                // 現在の回転
                var lrot = MathUtility.ToRotation(localNormals[vindex], localTangents[vindex]);

                // 子がいる場合は子へのベクトルから算出
                var nrot = lrot;
                int pindex = vertexParentIndices[vindex];
                var pack = vertexChildIndexArray[vindex];
                DataUtility.Unpack12_20(pack, out var dcnt, out var dstart);
                if (dcnt > 0)
                {
                    float3 cv = 0;
                    for (int i = 0; i < dcnt; i++)
                    {
                        int cindex = vertexChildDataArray[dstart + i];
                        cv += localPositions[cindex] - lpos;
                    }
                    if (math.lengthsq(cv) > Define.System.Epsilon)
                    {
                        cv = math.normalize(cv);
                        float3 n = math.cross(cv, v);
                        n = math.cross(n, cv);

                        if (math.lengthsq(n) > Define.System.Epsilon)
                        {
                            n = math.normalize(n);
                            localNormals[vindex] = n;
                            localTangents[vindex] = cv;
                            nrot = MathUtility.ToRotation(n, cv);
                        }
                    }
                }
                // 子がいなく親がいる場合は親からのベクトルから算出
                else if (pindex >= 0)
                {
                    var ppos = localPositions[pindex];
                    var w = lpos - ppos;
                    w = math.normalize(w);

                    float3 n = math.cross(w, v);
                    n = math.cross(n, w);

                    if (math.lengthsq(n) > Define.System.Epsilon)
                    {
                        n = math.normalize(n);
                        localNormals[vindex] = n;
                        localTangents[vindex] = w;
                        nrot = MathUtility.ToRotation(n, w);
                    }
                }

                // 補正用回転を算出し格納する
                normalAdjustmentRotations[vindex] = math.mul(math.inverse(lrot), nrot);
            }
        }

        /// <summary>
        /// シミュレーションに関係する頂点からAABBと固定頂点のリストを作成する
        /// </summary>
        void ProxyCreateFixedListAndAABB()
        {
            localCenterPosition = new NativeReference<float3>(0, Allocator.Persistent);

            using var fixedList = new NativeList<ushort>(VertexCount / 20 + 1, Allocator.TempJob);
            var job = new ProxyCreateFixedListAndAABBJob()
            {
                vcnt = VertexCount,
                attributes = attributes.GetNativeArray(),
                localPositions = localPositions.GetNativeArray(),
                vertexToVertexIndexArray = vertexToVertexIndexArray,
                vertexToVertexDataArray = vertexToVertexDataArray,

                outAABB = boundingBox,
                fixedList = fixedList,
                localCenterPosition = localCenterPosition,
            };
            job.Run();

            // 固定頂点リスト格納
            if (fixedList.Length > 0)
            {
#if MC2_COLLECTIONS_200
                centerFixedList = fixedList.AsArray().ToArray();
#else
                centerFixedList = fixedList.ToArray();
#endif
            }
            //Debug.Log($"Local Center Pos:{localCenterPosition.Value}");
        }

        [BurstCompile]
        struct ProxyCreateFixedListAndAABBJob : IJob
        {
            public int vcnt;
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> attributes;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<uint> vertexToVertexIndexArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<ushort> vertexToVertexDataArray;

            // out
            [Unity.Collections.WriteOnly]
            public NativeReference<AABB> outAABB;
            [Unity.Collections.WriteOnly]
            public NativeList<ushort> fixedList;
            [Unity.Collections.WriteOnly]
            public NativeReference<float3> localCenterPosition;

            public void Execute()
            {
                fixedList.Clear();

                float3 lcenpos = 0;
                int fixcnt = 0;

                int cnt = 0;
                float3 min = float.MaxValue;
                float3 max = float.MinValue;
                for (int i = 0; i < vcnt; i++)
                {
                    var lpos = localPositions[i];

                    // 固定頂点の場合は接続がすべて固定ならば無効とする（シミュレーションに無関係）
                    if (attributes[i].IsMove() == false)
                    {
                        var pack = vertexToVertexIndexArray[i];
                        DataUtility.Unpack12_20(pack, out var dcnt, out var dstart);
                        int j = 0;
                        for (; j < dcnt; j++)
                        {
                            int tindex = vertexToVertexDataArray[dstart + j];
                            if (attributes[tindex].IsMove())
                            {
                                // OK
                                break;
                            }
                        }
                        if (j == dcnt && dcnt > 0)
                            continue;

                        // 固定頂点リストに追加する
                        fixedList.Add((ushort)i);

                        lcenpos += lpos;
                        fixcnt++;
                    }

                    // min/max
                    min = math.min(min, lpos);
                    max = math.max(max, lpos);
                    cnt++;
                }

                // aabb
                outAABB.Value = cnt > 0 ? new AABB(min, max) : new AABB();

                // local center position
                if (fixcnt > 0)
                    lcenpos = lcenpos / fixcnt;
                localCenterPosition.Value = lcenpos;
            }
        }


        /// <summary>
        /// トライアングルの方向を頂点法線に沿うようにできるだけ合わせる
        /// ※この処理はとても重要。この合わせにより法線の計算精度が上がる。
        /// </summary>
        void OptimizeTriangleDirection(NativeArray<float3> triangleNormals, float sameSurfaceAngle)
        {
            if (TriangleCount == 0)
                return;

            // トライアングルを隣接する法線角度によりレイヤー分けしながら法線を揃える
            int startIndex = 0;
            //var useTriSet = new HashSet<int>(TriangleCount);
            var useTriSet = new HashSet<int>();
            var triQueue = new Queue<int>(TriangleCount / 2);
            var layerList = new List<int>(TriangleCount);
            while (startIndex < TriangleCount)
            {
                // レイヤー起点トライアングル
                if (useTriSet.Contains(startIndex))
                {
                    // すでに処理済み
                    startIndex++;
                    continue;
                }
                useTriSet.Add(startIndex);
                triQueue.Clear();
                triQueue.Enqueue(startIndex);
                layerList.Clear();
                int openCount = 0;
                int closeCount = 0;

                //Debug.Log($"レイヤー起点:{startIndex}");

                // 起点トライアングル情報
                while (triQueue.Count > 0)
                {
                    // １つ取り出し
                    int tindex = triQueue.Dequeue();
                    var n = triangleNormals[tindex];
                    int3 tri = triangles[tindex];
                    layerList.Add(tindex);

                    //Debug.Log($"レイヤー起点:{tindex}, tri:{tri}");

                    // 隣接トライアングル
                    int2x3 edges = new int2x3(
                        DataUtility.PackInt2(tri.xy),
                        DataUtility.PackInt2(tri.yz),
                        DataUtility.PackInt2(tri.zx)
                        );
                    for (int i = 0; i < 3; i++)
                    {
                        int2 edge = edges[i];

                        if (edgeToTriangles.ContainsKey(edge) == false)
                            continue;

                        foreach (var data in edgeToTriangles.GetValuesForKey(edge))
                        {
                            int tindex2 = data;

                            // すでに処理済みならスキップ
                            if (useTriSet.Contains(tindex2))
                                continue;

                            // 同一レイヤー判定（トライアングルのなす角度）
                            int3 tri2 = triangles[tindex2];
                            var n2 = triangleNormals[tindex2];
                            float ang = CalcTwoTriangleAngle(tri, tri2, edge);
                            //Debug.Log($"tindex2:{tindex2}, tri2:{tri2}, ang:{ang}");

                            // 面角度が一定上ならば不連続としてスキップする
                            if (ang > sameSurfaceAngle) // 80.0f?
                                continue;

                            // 面の法線が一定方向を向くように調整する
                            if (math.dot(n, n2) < 0.0f)
                            {
                                // フリップ
                                tri2 = MathUtility.FlipTriangle(tri2);
                                triangles[tindex2] = tri2;
                                triangleNormals[tindex2] = -n2;
                                //Debug.Log($"フリップ:{tri2}");
                            }

                            // 隣接トライアングルの法線が開いているか閉じているかのカウント
                            if (CheckTwoTriangleOpen(tri, tri2, edge, n))
                                openCount++;
                            else
                                closeCount++;

                            // 同一レイヤーとして処理する
                            useTriSet.Add(tindex2);
                            triQueue.Enqueue(tindex2);
                        }
                    }
                }

                // 閉じているトライアングルのほうが多い場合はレイヤー全体の法線をフリップさせる
                //Debug.Log("layer tcnt:" + layerList.Count + " open:" + openCount + " close:" + closeCount);
                if (closeCount > openCount)
                {
                    foreach (int tindex in layerList)
                    {
                        triangles[tindex] = MathUtility.FlipTriangle(triangles[tindex]);
                        triangleNormals[tindex] = -triangleNormals[tindex];
                    }
                }
            }
        }


        /// <summary>
        /// トライアングル法線を求める
        /// </summary>
        [BurstCompile]
        struct Proxy_CalcTriangleNormalJob : IJobParallelFor
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<int3> triangles;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localPositins;

            [Unity.Collections.WriteOnly]
            public NativeArray<float3> triangleNormals;

            public void Execute(int tindex)
            {
                int3 tri = triangles[tindex];

                // トライアングル法線を求める
                var p0 = localPositins[tri.x];
                var p1 = localPositins[tri.y];
                var p2 = localPositins[tri.z];
                var n = MathUtility.TriangleNormal(p0, p1, p2);
                triangleNormals[tindex] = n;
            }
        }

        /// <summary>
        /// トライアングル接線を求める
        /// </summary>
        [BurstCompile]
        struct Proxy_CalcTriangleTangentJob : IJobParallelFor
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<int3> triangles;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localPositins;
            [Unity.Collections.ReadOnly]
            public NativeArray<float2> uv;

            [Unity.Collections.WriteOnly]
            public NativeArray<float3> triangleTangents;

            public void Execute(int tindex)
            {
                int3 tri = triangles[tindex];

                // トライアングル接線を求める
                var p0 = localPositins[tri.x];
                var p1 = localPositins[tri.y];
                var p2 = localPositins[tri.z];
                var uv0 = uv[tri.x];
                var uv1 = uv[tri.y];
                var uv2 = uv[tri.z];
                var tan = MathUtility.TriangleTangent(p0, p1, p2, uv0, uv1, uv2);
                Develop.Assert(math.lengthsq(tan) > 0.0f);
                triangleTangents[tindex] = tan;
            }
        }

        /// <summary>
        /// 頂点ごとに接続するトライアングルを求める（最大７つ）
        /// </summary>
        [BurstCompile]
        unsafe struct Proxy_CreateVertexToTrianglesJob : IJob
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<int3> triangles;

            public NativeArray<FixedList32Bytes<uint>> vertexToTriangles;

            public void Execute()
            {
                var ptr = (FixedList32Bytes<uint>*)vertexToTriangles.GetUnsafePtr();

                int tcnt = triangles.Length;
                for (uint tindex = 0; tindex < tcnt; tindex++)
                {
                    int3 tri = triangles[(int)tindex];

                    var vset_x = (ptr + tri.x);
                    var vset_y = (ptr + tri.y);
                    var vset_z = (ptr + tri.z);

                    if (vset_x->Length < 7)
                        vset_x->Set(tindex);
                    if (vset_y->Length < 7)
                        vset_y->Set(tindex);
                    if (vset_z->Length < 7)
                        vset_z->Set(tindex);
                }
            }
        }

        /// <summary>
        /// 接続トライアングルから頂点法線接線を計算するために最適なトライアングル方向を計算して格納する
        /// またトライアングルに属する頂点にはフラグを立てる
        /// </summary>
        [BurstCompile]
        struct Proxy_OrganizeVertexToTrianglsJob : IJobParallelFor
        {
            public NativeArray<FixedList32Bytes<uint>> vertexToTriangles;

            [Unity.Collections.ReadOnly]
            public NativeArray<float3> triangleNormals;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> triangleTangents;

            public NativeArray<VertexAttribute> attributes;

            public void Execute(int vindex)
            {
                var tset = vertexToTriangles[vindex];
                int tcnt = tset.Length;
                if (tcnt == 0)
                    return;

                // この頂点はトライアングルに属する
                var attr = attributes[vindex];
                attr.SetFlag(VertexAttribute.Flag_Triangle, true);
                attributes[vindex] = attr;

                // まず普通に現在のトライアングル面法線から頂点法線を求めてみる
                float3 finalNormal = 0;
                float3 finalTangent = 0;
                for (int i = 0; i < tcnt; i++)
                {
                    int tindex = (int)tset[i];
                    finalNormal += triangleNormals[tindex];
                    finalTangent += triangleTangents[tindex];
                }

                //if (math.length(finalTangent) < 0.5f)
                //    Debug.LogError($"vindex:{vindex} finalTangent:{finalTangent}");

                // 普通に求めた法線が短い場合は最適な法線を算出する
                if (math.length(finalNormal) < 0.5f)
                {
                    // すべての接続トライアングルをループ
                    // ループの最初のトライアングルを基準としてその法線方向に他のトライアングルをあわせてみる
                    // 法線の合計の長さがもっとも長いものを採用する
                    float maxDist = -1;
                    finalNormal = 0;

                    for (int i = 0; i < tcnt; i++)
                    {
                        // このトライアングルを基準として計算する
                        int tindex1 = (int)tset[i];
                        float3 n = 0;
                        float3 tn1 = triangleNormals[tindex1];

                        for (int j = 0; j < tcnt; j++)
                        {
                            int tindex2 = (int)tset[j];
                            if (tindex2 == tindex1)
                                continue;

                            float3 tn2 = triangleNormals[tindex2];
                            if (math.dot(tn1, tn2) >= 0.0f)
                                n += tn2;
                            else
                                n += -tn2;
                        }

                        // 計算された法線の長さを判定
                        // 最も長いものを基準法線として採用する
                        float ndist = math.lengthsq(n);
                        if (ndist > maxDist)
                        {
                            maxDist = ndist;
                            finalNormal = tn1;
                        }
                    }
                }
                else
                {
                    // この法線を基準法線とする
                    finalNormal = math.normalize(finalNormal);
                }

                // 普通に求めた接線が短い場合は最適な接線を算出する
                if (math.length(finalTangent) < 0.5f)
                {
                    // すべての接続トライアングルをループ
                    // ループの最初のトライアングルを基準としてその接線方向に他のトライアングルをあわせてみる
                    // 接線の合計の長さがもっとも長いものを採用する
                    float maxDist = -1;
                    finalTangent = 0;

                    for (int i = 0; i < tcnt; i++)
                    {
                        // このトライアングルを基準として計算する
                        int tindex1 = (int)tset[i];
                        float3 n = 0;
                        float3 tt1 = triangleTangents[tindex1];

                        for (int j = 0; j < tcnt; j++)
                        {
                            int tindex2 = (int)tset[j];
                            if (tindex2 == tindex1)
                                continue;

                            float3 tt2 = triangleTangents[tindex2];
                            if (math.dot(tt1, tt2) >= 0.0f)
                                n += tt2;
                            else
                                n += -tt2;
                        }

                        // 計算された接線の長さを判定
                        // 最も長いものを基準接線として採用する
                        float ndist = math.lengthsq(n);
                        if (ndist > maxDist)
                        {
                            maxDist = ndist;
                            finalTangent = tt1;
                        }
                    }
                }
                else
                {
                    // この接線を基準とする
                    finalTangent = math.normalize(finalTangent);
                }

                // トライアングルを登録する
                // 同時に法線と接線の加算方向をフラグとして追加する
                for (int i = 0; i < tcnt; i++)
                {
                    int tindex = (int)tset[i];

                    float3 tn = triangleNormals[tindex];
                    float3 tt = triangleTangents[tindex];

                    // 反転フラグ
                    int flipFlag = 0;
                    if (math.dot(finalNormal, tn) < 0.0f)
                        flipFlag |= 0x1;
                    if (math.dot(finalTangent, tt) < 0.0f)
                        flipFlag |= 0x2;

                    // 12-20bitでuintにパックする
                    tset[i] = DataUtility.Pack12_20(flipFlag, tindex);
                }

                /*
                // 算出された法線向きに合わせるようにトライアングルを登録する
                // 反転の場合はマイナスのインデックスで登録する
                for (int i = 0; i < tcnt; i++)
                {
                    int tindex = tset[i];

                    float3 tn = triangleNormals[tindex];

                    // 基準法線と逆向きならマイナスインデックスとして記録する
                    // ★インデックスは＋１して記録するので注意！（０を除外するため）
                    int registTriangleIndex = math.dot(finalNormal, tn) >= 0.0f ? (tindex + 1) : -(tindex + 1);

                    // 再登録
                    tset[i] = registTriangleIndex;
                }
                */

                // 結果格納
                vertexToTriangles[vindex] = tset;
            }
        }

        /// <summary>
        /// 現在メッシュの頂点法線接線を接続トライアングル情報から更新する
        /// </summary>
        [BurstCompile]
        struct Proxy_CalcVertexNormalTangentFromTriangleJob : IJobParallelFor
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> triangleNormals;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> triangleTangents;

            [Unity.Collections.ReadOnly]
            public NativeArray<FixedList32Bytes<uint>> vertexToTriangles;
            public NativeArray<float3> localNormals;
            public NativeArray<float3> localTangents;

            public void Execute(int vindex)
            {
                var tset = vertexToTriangles[vindex];
                int tcnt = tset.Length;
                if (tcnt > 0)
                {
                    float3 nor = 0;
                    float3 tan = 0;

                    for (int i = 0; i < tcnt; i++)
                    {
                        // 12-20bitのパックで格納されている
                        uint data = tset[i];
                        int flipFlag = DataUtility.Unpack12_20Hi(data);
                        int tindex = DataUtility.Unpack12_20Low(data);

                        nor += triangleNormals[tindex] * ((flipFlag & 0x1) == 0 ? 1 : -1);
                        tan += triangleTangents[tindex] * ((flipFlag & 0x2) == 0 ? 1 : -1);

                        //int data = tset[i];
                        //int tindex = math.abs(data) - 1;

                        // 法線フリップフラグ
                        //float flip = math.sign(data);

                        //nor += triangleNormals[tindex] * flip;
                        //tan += triangleTangents[tindex]; // 接線はフリップさせては駄目！
                    }

                    nor = math.normalize(nor);

                    // 従法線に変更(v2.1.7)
                    //tan = math.normalize(tan);
                    float3 binor = math.normalize(math.cross(nor, tan));

                    localNormals[vindex] = nor;
                    //localTangents[vindex] = tan;
                    localTangents[vindex] = binor; // 従法線に変更(v2.1.7)
                }
            }
        }

        [BurstCompile]
        struct Proxy_CalcVertexToTransformJob : IJobParallelFor
        {
            public quaternion invRot;

            // vmesh
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localNormals;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localTangents;
            [Unity.Collections.WriteOnly]
            public NativeArray<quaternion> vertexToTransformRotations;

            // transform
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> transformRotations;

            public void Execute(int vindex)
            {
                // トランスフォームのローカル回転
                var trot = math.mul(invRot, transformRotations[vindex]);

                // 頂点のローカル回転
                var vrot = MathUtility.ToRotation(localNormals[vindex], localTangents[vindex]);

                vertexToTransformRotations[vindex] = math.mul(math.inverse(vrot), trot);
            }
        }

        /// <summary>
        /// エッジごとの接続トライアングルマップを作成する
        /// </summary>
        [BurstCompile]
        struct Proxy_CalcEdgeToTriangleJob : IJob
        {
            public int tcnt;
            [Unity.Collections.ReadOnly]
            public NativeArray<int3> triangles;
            public NativeParallelMultiHashMap<int2, ushort> edgeToTriangles;

            public void Execute()
            {
                for (int i = 0; i < tcnt; i++)
                {
                    var tri = triangles[i];
                    int2x3 edges = new int2x3(
                        DataUtility.PackInt2(tri.xy),
                        DataUtility.PackInt2(tri.yz),
                        DataUtility.PackInt2(tri.zx)
                        );
                    for (int j = 0; j < 3; j++)
                    {
                        int2 edge = edges[j];
                        edgeToTriangles.UniqueAdd(edge, (ushort)i);
                    }
                }
            }
        }

        /// <summary>
        /// 頂点のバインドポーズを求める
        /// </summary>
        [BurstCompile]
        struct Proxy_CalcVertexBindPoseJob2 : IJobParallelFor
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localNormals;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localTangents;

            [Unity.Collections.WriteOnly]
            public NativeArray<float3> vertexBindPosePositions;
            [Unity.Collections.WriteOnly]
            public NativeArray<quaternion> vertexBindPoseRotations;

            public void Execute(int vindex)
            {
                float3 pos = localPositions[vindex];
                var nor = localNormals[vindex];
                var tan = localTangents[vindex];

                // マッピング用の頂点バインドポーズを求める
                quaternion rot = MathUtility.ToRotation(nor, tan);
                vertexBindPosePositions[vindex] = -pos;
                vertexBindPoseRotations[vindex] = math.inverse(rot);
            }
        }

        /// <summary>
        /// トライアングルに接続する頂点セットを求める
        /// およびエッジセットを作成する
        /// </summary>
        [BurstCompile]
        struct Proxy_CalcVertexToVertexFromTriangleJob : IJob
        {
            public int triangleCount;

            [Unity.Collections.ReadOnly]
            public NativeArray<int3> triangles;
            public NativeParallelMultiHashMap<int, ushort> vertexToVertexMap;
            public NativeParallelHashSet<int2> edgeSet;

            public void Execute()
            {
                for (int i = 0; i < triangleCount; i++)
                {
                    int3 tri = triangles[i];

                    ushort x = (ushort)tri.x;
                    ushort y = (ushort)tri.y;
                    ushort z = (ushort)tri.z;

                    vertexToVertexMap.UniqueAdd(tri.x, y);
                    vertexToVertexMap.UniqueAdd(tri.x, z);
                    vertexToVertexMap.UniqueAdd(tri.y, x);
                    vertexToVertexMap.UniqueAdd(tri.y, z);
                    vertexToVertexMap.UniqueAdd(tri.z, x);
                    vertexToVertexMap.UniqueAdd(tri.z, y);

                    edgeSet.Add(DataUtility.PackInt2(tri.xy));
                    edgeSet.Add(DataUtility.PackInt2(tri.yz));
                    edgeSet.Add(DataUtility.PackInt2(tri.zx));
                }
            }
        }

        /// <summary>
        /// ラインに接続する頂点セットを求める
        /// およびエッジセットを作成する
        /// </summary>
        [BurstCompile]
        struct Proxy_CalcVertexToVertexFromLineJob : IJob
        {
            public int lineCount;

            [Unity.Collections.ReadOnly]
            public NativeArray<int2> lines;
            public NativeParallelMultiHashMap<int, ushort> vertexToVertexMap;
            public NativeParallelHashSet<int2> edgeSet;

            public void Execute()
            {
                for (int i = 0; i < lineCount; i++)
                {
                    int2 line = lines[i];

                    vertexToVertexMap.UniqueAdd(line.x, (ushort)line.y);
                    vertexToVertexMap.UniqueAdd(line.y, (ushort)line.x);

                    edgeSet.Add(DataUtility.PackInt2(line));
                }
            }
        }

        [BurstCompile]
        struct Proxy_CreateEdgeFlagJob : IJobParallelFor
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<int2> edges;
            [Unity.Collections.ReadOnly]
            public NativeParallelMultiHashMap<int2, ushort> edgeToTriangles;

            [Unity.Collections.WriteOnly]
            public NativeArray<ExBitFlag8> edgeFlags;

            public void Execute(int eindex)
            {
                var flag = new ExBitFlag8();

                // 切り口エッジか判定する
                var edge = edges[eindex];
                if (edgeToTriangles.ContainsKey(edge))
                {
                    if (edgeToTriangles.CountValuesForKey(edge) <= 1)
                    {
                        flag.SetFlag(EdgeFlag_Cut, true);
                        //Debug.Log($"切り口エッジ. eindex:{eindex}, edge:{edge}");
                    }
                }

                edgeFlags[eindex] = flag;
            }
        }

        //=========================================================================================
        struct SkinningBoneInfo
        {
            //public int transformIndex;
            public int startTransformIndex;
            public float3 startPos;
            public int endTransformIndex;
            public float3 endPos;
        }

        /// <summary>
        /// カスタムスキニング情報の作成
        /// </summary>
        void CreateCustomSkinning(CustomSkinningSettings setting, List<TransformRecord> bones)
        {
            if (CustomSkinningBoneCount == 0)
                return;

#if false
            // ボーン情報の構築
            using var boneInfoList = new NativeList<SkinningBoneInfo>(CustomSkinningBoneCount, Allocator.Persistent);
            for (int i = 0; i < CustomSkinningBoneCount; i++)
            {
                int tindex = customSkinningBoneIndices[i];
                if (tindex == -1)
                    continue;

                // 登録
                var info = new SkinningBoneInfo();
                info.transformIndex = tindex;
                info.startPos = bones[i].localPosition;
                boneInfoList.Add(info);
                Debug.Log($"[{boneInfoList.Length - 1}] {i}, tindex:{tindex}");
            }
            if (boneInfoList.Length == 0)
                return;

            // 頂点ごとにカスタムスキニングウエイトを算出
            var job = new Proxy_CalcCustomSkinningWeightsJob2()
            {
                distanceReduction = setting.distanceReduction,
                distancePow = setting.distancePow,

                //attributes = attributes.GetNativeArray(),
                localPositions = localPositions.GetNativeArray(),
                boneInfoList = boneInfoList,
                boneWeights = boneWeights.GetNativeArray(),
            };
            job.Run(VertexCount);
#endif
#if true
            // ボーン情報の構築
            using var boneInfoList = new NativeList<SkinningBoneInfo>(CustomSkinningBoneCount * 2, Allocator.Persistent);
            for (int i = 0; i < CustomSkinningBoneCount; i++)
            {
                int tindex = customSkinningBoneIndices[i];
                if (tindex == -1)
                    continue;
                int pid = bones[i].pid;
                if (pid == 0)
                    continue;
                int pindex = bones.FindIndex(x => x.id == pid);
                if (pindex < 0)
                    continue;

                // ボーンライン情報の作成
                var info = new SkinningBoneInfo();
                //info.transformIndex = customSkinningBoneIndices[pindex];
                info.startTransformIndex = customSkinningBoneIndices[pindex];
                info.startPos = bones[pindex].localPosition;
                info.endTransformIndex = tindex;
                info.endPos = bones[i].localPosition;

                // 距離がほぼ０なら無効
                if (math.distance(info.startPos, info.endPos) < Define.System.Epsilon)
                    continue;

                // 登録
                boneInfoList.Add(info);
                //Debug.Log($"[{boneInfoList.Length - 1}] parent:{pindex} -> {i}");
            }
            if (boneInfoList.Length == 0)
                return;

            // 頂点ごとにカスタムスキニングウエイトを算出
            var job = new Proxy_CalcCustomSkinningWeightsJob()
            {
                isBoneCloth = isBoneCloth,
                //angularAttenuation = setting.angularAttenuation,
                //distanceReduction = setting.distanceReduction,
                //distancePow = setting.distancePow,
                angularAttenuation = Define.System.CustomSkinningAngularAttenuation,
                distanceReduction = Define.System.CustomSkinningDistanceReduction,
                distancePow = Define.System.CustomSkinningDistancePow,

                attributes = attributes.GetNativeArray(),
                localPositions = localPositions.GetNativeArray(),
                boneInfoList = boneInfoList,
                boneWeights = boneWeights.GetNativeArray(),
            };
            job.Run(VertexCount);
#endif
        }

#if false
        [BurstCompile]
        struct Proxy_CalcCustomSkinningWeightsJob2 : IJobParallelFor
        {
            public float distanceReduction;
            public float distancePow;

            //[Unity.Collections.ReadOnly]
            //public NativeArray<VertexAttribute> attributes;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localPositions;
            [Unity.Collections.ReadOnly]
            public NativeList<SkinningBoneInfo> boneInfoList;
            [Unity.Collections.WriteOnly]
            public NativeArray<VirtualMeshBoneWeight> boneWeights;

            public void Execute(int vindex)
            {
                // 固定は無効(※この時点ではまだ属性がない！）
                //var attr = attributes[vindex];
                //if (attr.IsMove() == false)
                //    return;

                var lpos = localPositions[vindex];

                var costList = new ExCostSortedList4(-1);
                int bcnt = boneInfoList.Length;
                for (int i = 0; i < bcnt; i++)
                {
                    var binfo = boneInfoList[i];

                    // 距離
                    float dist = math.distance(lpos, binfo.startPos);

                    // 登録。すでに登録済みならばdistがより小さい場合のみ再登録
                    int boneIndex = binfo.transformIndex;
                    int nowIndex = costList.indexOf(boneIndex);
                    if (nowIndex >= 0)
                    {
                        if (dist < costList.costs[nowIndex])
                        {
                            costList.RemoveItem(boneIndex);
                            costList.Add(dist, boneIndex);
                        }
                    }
                    else
                        costList.Add(dist, boneIndex);
                }

                // ウエイト算出
                // (0)最小距離のn%を減算する
                int cnt = costList.Count;
                //const float lengthWeight = 0.8f;
                float mindist = costList.MinCost * distanceReduction;
                costList.costs -= mindist;

                // (1)distanceをn乗する
                //const float pow = 2.0f;
                costList.costs = math.pow(costList.costs, distancePow);

                // (2)最小値の逆数にする
                float min = math.max(costList.MinCost, 1e-06f);
                float sum = 0;
                for (int i = 0; i < cnt; i++)
                {
                    costList.costs[i] = min / costList.costs[i];
                    sum += costList.costs[i];
                }

                // (3)割合を出す
                costList.costs /= sum;

                // (4)極小のウエイトは削除する
                sum = 0;
                for (int i = 0; i < 4; i++)
                {
                    if (costList.costs[i] < 0.01f || i >= cnt)
                    {
                        // 打ち切り
                        costList.costs[i] = 0.0f;
                        costList.data[i] = 0;
                    }
                    else
                    {
                        sum += costList.costs[i];
                    }
                }
                Debug.Assert(sum > 0);

                // (5)再度1.0に平均化
                costList.costs /= sum;

                //Debug.Log($"[{vindex}] :{costList}");

                // ウエイト作成
                var bw = new VirtualMeshBoneWeight(costList.data, costList.costs);
                boneWeights[vindex] = bw;
            }
        }
#endif

#if true
        [BurstCompile]
        struct Proxy_CalcCustomSkinningWeightsJob : IJobParallelFor
        {
            public bool isBoneCloth;
            public float angularAttenuation;
            public float distanceReduction;
            public float distancePow;

            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> attributes;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localPositions;
            [Unity.Collections.ReadOnly]
            public NativeList<SkinningBoneInfo> boneInfoList;
            [Unity.Collections.WriteOnly]
            public NativeArray<VirtualMeshBoneWeight> boneWeights;


            public void Execute(int vindex)
            {
                // BoneClothカスタムスキニングでは固定は動かさない
                if (isBoneCloth && attributes[vindex].IsFixed())
                    return;

                var lpos = localPositions[vindex];

                var costList = new ExCostSortedList4(-1);
                int bcnt = boneInfoList.Length;
                for (int i = 0; i < bcnt; i++)
                {
                    var binfo = boneInfoList[i];
                    float3 d = MathUtility.ClosestPtPointSegment(lpos, binfo.startPos, binfo.endPos);
                    //float dist = math.distance(lpos, d);

                    // ボーンラインとの角度により判定距離を調整する
                    // ラインと水平になるほど影響がよわくなる
                    var v = lpos - d;
                    var bv = binfo.endPos - binfo.startPos;
                    float dot = math.dot(math.normalize(v), math.normalize(bv));
                    float ratio = 1.0f + math.abs(dot) * angularAttenuation;

                    // 登録。すでに登録済みならばdistがより小さい場合のみ再登録
                    for (int j = 0; j < 2; j++)
                    {
                        int boneIndex = j == 0 ? binfo.startTransformIndex : binfo.endTransformIndex;
                        float dist = j == 0 ? math.distance(lpos, binfo.startPos) : math.distance(lpos, binfo.endPos);
                        dist *= ratio;

                        int nowIndex = costList.indexOf(boneIndex);
                        if (nowIndex >= 0)
                        {
                            if (dist < costList.costs[nowIndex])
                            {
                                costList.RemoveItem(boneIndex);
                                costList.Add(dist, boneIndex);
                            }
                        }
                        else
                            costList.Add(dist, boneIndex);
                    }
                }

                // ウエイト算出
                // (0)最小距離のn%を減算する
                int cnt = costList.Count;
                float mindist = costList.MinCost * distanceReduction;
                costList.costs -= mindist;

                // (1)distanceをn乗する
                costList.costs = math.pow(costList.costs, distancePow);

                // (2)最小値の逆数にする
                float min = math.max(costList.MinCost, 1e-06f);
                float sum = 0;
                for (int i = 0; i < cnt; i++)
                {
                    costList.costs[i] = min / costList.costs[i];
                    sum += costList.costs[i];
                }

                // (3)割合を出す
                costList.costs /= sum;

                // (4)極小のウエイトは削除する
                sum = 0;
                for (int i = 0; i < 4; i++)
                {
                    if (costList.costs[i] < 0.01f || i >= cnt)
                    {
                        // 打ち切り
                        costList.costs[i] = 0.0f;
                        costList.data[i] = 0;
                    }
                    else
                    {
                        sum += costList.costs[i];
                    }
                }
                Debug.Assert(sum > 0);

                // (5)再度1.0に平均化
                costList.costs /= sum;

                //Debug.Log($"[{vindex}] :{costList}");

                // ウエイト作成
                var bw = new VirtualMeshBoneWeight(costList.data, costList.costs);
                boneWeights[vindex] = bw;
            }
        }
#endif

        //=========================================================================================
        /// <summary>
        /// セレクションデータ属性をプロキシメッシュに反映させる（スレッド可）
        /// </summary>
        /// <param name="selectionData"></param>
        public void ApplySelectionAttribute(SelectionData selectionData)
        {
            try
            {
                //Debug.Log($"ApplySelection.SearchRadius:{searchRadius}");

                using var selectionPositions = selectionData.GetPositionNativeArray();
                using var selectionAttributes = selectionData.GetAttributeNativeArray();

                // グリッドサイズ計算
                // 検索半径（メッシュの平均接続距離とセレクションデータの最大接続距離の大きい方）
                float searchRadius = math.max(averageVertexDistance.Value, selectionData.maxConnectionDistance);
                searchRadius = math.max(searchRadius, Define.System.MinimumGridSize);
                float gridSize = searchRadius * 1.5f;
                //Develop.DebugLog($"ApplySelectionAttribute. searchRadius:{searchRadius}, gridSize:{gridSize}");

                // セレクションデータをグリッドマップに格納する
                using var gridMap = SelectionData.CreateGridMapRun(gridSize, selectionPositions, selectionAttributes);

                // メッシュ頂点ごとに最も近いセレクションデータに接続し頂点属性を決定する
                var applyJob = new Proxy_ApplySelectionJob()
                {
                    gridSize = gridSize,
                    radius = searchRadius,
                    localPositions = localPositions.GetNativeArray(),
                    attributes = attributes.GetNativeArray(),

                    gridMap = gridMap.GetMultiHashMap(),
                    selectionPositions = selectionPositions,
                    selectionAttributes = selectionAttributes,
                };
                applyJob.Run(VertexCount);

                // BoneClothの場合はTransformの書き込み方法をTransformFlagに設定する
                if (isBoneCloth)
                {
                    var transformFlagJob = new Proxy_BoneClothApplayTransformFlagJob()
                    {
                        attributes = attributes.GetNativeArray(),
                        transformFlags = transformData.flagArray.GetNativeArray(),
                    };
                    transformFlagJob.Run(VertexCount);
                }
            }
            catch (Exception)
            {
                result.SetError(Define.Result.ProxyMesh_ApplySelectionError);
            }
        }

        [BurstCompile]
        struct Proxy_ApplySelectionJob : IJobParallelFor
        {
            public float gridSize;
            public float radius;

            // proxy mesh
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localPositions;
            public NativeArray<VertexAttribute> attributes;

            // selection
            [Unity.Collections.ReadOnly]
            public NativeParallelMultiHashMap<int3, int> gridMap;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> selectionPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> selectionAttributes;

            public void Execute(int vindex)
            {
                float3 pos = localPositions[vindex];

                // 属性フラグはすでに設定されている場合があるので追加書き込みにする
                var attr = attributes[vindex];

                // 範囲グリッド走査
                float minDist = float.MaxValue;
                //VertexAttribute minAttr = default;
                VertexAttribute minAttr = VertexAttribute.Invalid;
                foreach (int3 grid in GridMap<int>.GetArea(pos, radius, gridMap, gridSize))
                {
                    if (gridMap.ContainsKey(grid) == false)
                        continue;

                    // このグリッドを検索する
                    foreach (int tindex in gridMap.GetValuesForKey(grid))
                    {
                        // 距離判定
                        float3 tpos = selectionPositions[tindex];
                        float dist = math.distance(pos, tpos);
                        if (dist > radius)
                            continue;
                        if (dist > minDist)
                            continue;

                        // 近傍属性
                        minDist = dist;
                        minAttr = selectionAttributes[tindex];
                    }
                }

                // 属性反映
                //if (minAttr.IsInvalid())
                //    minAttr = VertexAttribute.Fixed; // InvalidはFixedに変換
                //Debug.Log($"vindex:{vindex} minAttr:{minAttr.Value:X}");
                attr.SetFlag(minAttr, true); // フラグ結合
                attributes[vindex] = attr;
            }
        }

        [BurstCompile]
        struct Proxy_BoneClothApplayTransformFlagJob : IJobParallelFor
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> attributes;

            public NativeArray<ExBitFlag8> transformFlags;

            public void Execute(int vindex)
            {
                var attr = attributes[vindex];

                var flag = transformFlags[vindex];

                // 書き込み方法
                if (attr.IsMove())
                    flag.SetFlag(TransformManager.Flag_LocalPosRotWrite, true);
                else if (attr.IsFixed())
                    flag.SetFlag(TransformManager.Flag_WorldRotWrite, true);

                // 復元
                if (attr.IsInvalid() == false)
                    flag.SetFlag(TransformManager.Flag_Restore, true);

                transformFlags[vindex] = flag;
            }
        }

        //-----------------------------------------------------------------------------------------
        /// <summary>
        /// [MeshCloth]ベースラインの作成
        /// 頂点接続情報から親情報を作成する
        /// </summary>
        void CreateMeshBaseLine()
        {
            int vcnt = VertexCount;
            vertexParentIndices = new NativeArray<int>(vcnt, Allocator.Persistent);
            using var dataBuilder = new MultiDataBuilder<ushort>(vcnt, vcnt);

            // 親接続を(-1)で初期化する
            JobUtility.FillRun(vertexParentIndices, vcnt, -1);

            // 固定頂点のリストを作成する
            using var fixedList = new NativeList<int>(vcnt, Allocator.Persistent);
            var job1 = new BaseLine_Mesh_CareteFixedListJob()
            {
                vcnt = vcnt,
                attribues = attributes.GetNativeArray(),
                fixedList = fixedList,
            };
            job1.Run();

            // 固定数が０ならばベースラインは作れない！
            if (fixedList.Length == 0)
            {
                //Debug.LogWarning("BaseLine.fixedPoint count = 0!");
                vertexChildIndexArray = new NativeArray<uint>(vcnt, Allocator.Persistent, NativeArrayOptions.ClearMemory);
                vertexChildDataArray = new NativeArray<ushort>(0, Allocator.Persistent);
                return;
            }

            // 頂点接続情報から親接続を作成する
            using var nextList = new NativeList<BaseLineWork>(vcnt, Allocator.Persistent);
            using var markBuff = new NativeArray<byte>(vcnt, Allocator.Persistent, NativeArrayOptions.ClearMemory); // Unity2023.1.5対応
            using var vertexMap = new NativeParallelHashMap<int, BaseLineWork>(vcnt, Allocator.Persistent); // Unity2023.1.5対応
            var job2 = new BaseLine_Mesh_CreateParentJob2()
            {
                vcnt = vcnt,
                avgDist = averageVertexDistance.Value,

                attribues = attributes.GetNativeArray(),
                localPositions = localPositions.GetNativeArray(),
                vertexToVertexIndexArray = vertexToVertexIndexArray,
                vertexToVertexDataArray = vertexToVertexDataArray,

                vertexParentIndices = vertexParentIndices,
                vertexChildMap = dataBuilder.Map,

                fixedList = fixedList,
                nextList = nextList,

                markBuff = markBuff, // Unity2023.1.5対応
                vertexMap = vertexMap, // Unity2023.1.5対応
            };
            job2.Run();

            // ベースラインの構築
            var stack = new Stack<int>(vcnt);
            var lineFlags = new List<ExBitFlag8>(fixedList.Length);
            var startIndices = new List<ushort>(fixedList.Length);
            var dataCounts = new List<ushort>(fixedList.Length);
            var indices = new List<ushort>(vcnt);
            for (int i = 0; i < fixedList.Length; i++)
            {
                int fvindex = fixedList[i];
                // 子が接続されていない固定頂点は無効
                if (dataBuilder.GetDataCount(fvindex) == 0)
                    continue;

                // この頂点を起点としてラインを形成する
                stack.Clear();
                stack.Push(fvindex);

                ushort start = (ushort)indices.Count;
                ushort count = 0;
                ExBitFlag8 lineflag = new ExBitFlag8();

                while (stack.Count > 0)
                {
                    int vindex = stack.Pop();
                    indices.Add((ushort)vindex);
                    count++;

                    // この頂点がラインに属している場合はフラグを立てる
                    if (attributes[vindex].IsSet(VertexAttribute.Flag_Triangle) == false)
                    {
                        lineflag.SetFlag(BaseLineFlag_IncludeLine, true);
                    }

                    if (dataBuilder.Map.ContainsKey(vindex))
                    {
                        foreach (var data in dataBuilder.Map.GetValuesForKey(vindex))
                        {
                            stack.Push(data);
                        }
                    }
                }

                // 格納
                lineFlags.Add(lineflag);
                startIndices.Add(start);
                dataCounts.Add(count);
                //Debug.Log($"BaseLine data count:{count}");
            }
            baseLineFlags = new NativeArray<ExBitFlag8>(lineFlags.ToArray(), Allocator.Persistent);
            baseLineStartDataIndices = new NativeArray<ushort>(startIndices.ToArray(), Allocator.Persistent);
            baseLineDataCounts = new NativeArray<ushort>(dataCounts.ToArray(), Allocator.Persistent);
            baseLineData = new NativeArray<ushort>(indices.ToArray(), Allocator.Persistent);

            (ushort[] dataArry, uint[] indexArray) = dataBuilder.ToArray();
            vertexChildIndexArray = new NativeArray<uint>(indexArray, Allocator.Persistent);
            vertexChildDataArray = new NativeArray<ushort>(dataArry, Allocator.Persistent);
        }

        struct BaseLineWork : IComparable<BaseLineWork>
        {
            public int vindex;
            public float dist;

            public int CompareTo(BaseLineWork other)
            {
                return (int)math.sign(dist - other.dist);
            }
        }

        [BurstCompile]
        struct BaseLine_Mesh_CreateParentJob2 : IJob
        {
            public int vcnt;
            public float avgDist;

            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> attribues;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<uint> vertexToVertexIndexArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<ushort> vertexToVertexDataArray;

            public NativeArray<int> vertexParentIndices;
            public NativeParallelMultiHashMap<int, ushort> vertexChildMap;

            [Unity.Collections.ReadOnly]
            public NativeList<int> fixedList;
            public NativeList<BaseLineWork> nextList;

            public NativeArray<byte> markBuff; // Unity2023.1.5対応
            public NativeParallelHashMap<int, BaseLineWork> vertexMap; // Unity2023.1.5対応

            public void Execute()
            {
                // 処理済みマーク
                //var markBuff = new NativeArray<byte>(vcnt, Allocator.Temp, NativeArrayOptions.ClearMemory); // Unity2023.1.5対応
                //var vertexMap = new NativeParallelHashMap<int, BaseLineWork>(vcnt, Allocator.Temp); // Unity2023.1.5対応

                // 最初の作業バッファを固定頂点で初期化する
                foreach (int vindex in fixedList)
                {
                    nextList.Add(new BaseLineWork() { vindex = vindex, dist = 0 });
                }
                int level = 0;

                while (nextList.Length > 0)
                {
                    foreach (var data in nextList)
                    {
                        int vindex = data.vindex;

                        // 親への接続を作成する
                        var attr = attribues[vindex];
                        if (attr.IsDontMove())
                            continue;

                        var pos = localPositions[vindex];

                        // ■親が固定ならば距離が近い方、親が移動ならそのさらに親へのベクトル角度が浅い方を採用する
                        var cost = new ExCostSortedList1(-1, -1);
                        DataUtility.Unpack12_20(vertexToVertexIndexArray[vindex], out var dcnt, out var dstart);
                        for (int i = 0; i < dcnt; i++)
                        {
                            int tindex = vertexToVertexDataArray[dstart + i];
                            if (markBuff[tindex] == 0)
                                continue;
                            var tpos = localPositions[tindex];

                            if (attribues[tindex].IsDontMove())
                            {
                                // 親が固定なら距離優先
                                float tdist = math.distance(pos, tpos);
                                cost.Add(tdist, tindex);
                            }
                            else
                            {
                                // 親の親へのベクトル
                                int pindex = vertexParentIndices[tindex];
                                var v1 = tpos - pos;
                                var v2 = localPositions[pindex] - tpos;
                                float ang = MathUtility.Angle(v1, v2);
                                cost.Add(ang, tindex);
                            }
                        }
                        if (cost.IsValid)
                        {
                            int pindex = cost.data;
                            vertexParentIndices[vindex] = pindex;
                            markBuff[vindex] = 1;
                        }
                    }

                    // まとめ
                    foreach (var data in nextList)
                    {
                        int vindex = data.vindex;

                        // 今回のセットを処理済みとしてマークする
                        markBuff[vindex] = 2;

                        // 子の接続情報を作成
                        int pindex = vertexParentIndices[vindex];
                        if (pindex >= 0)
                        {
                            vertexChildMap.UniqueAdd(pindex, (ushort)vindex);
                        }
                    }

                    // 次の作業セットを作成する
                    vertexMap.Clear();
                    int mapcnt = 0;
                    foreach (var data in nextList)
                    {
                        int vindex = data.vindex;

                        // 自身の接続を調べる
                        DataUtility.Unpack12_20(vertexToVertexIndexArray[vindex], out var dcnt, out var dstart);
                        if (dcnt == 0)
                            continue;

                        var pos = localPositions[vindex];

                        for (int i = 0; i < dcnt; i++)
                        {
                            int tindex = vertexToVertexDataArray[dstart + i];
                            var tattr = attribues[tindex];
                            if (tattr.IsInvalid())
                                continue;
                            // 処理済みなら除外する
                            if (markBuff[tindex] != 0)
                                continue;

                            // 次の候補として登録する
                            float dist = math.distance(pos, localPositions[tindex]);
                            if (vertexMap.ContainsKey(tindex))
                            {
                                var d = vertexMap[tindex];
                                if (dist < d.dist)
                                {
                                    d.dist = dist;
                                    vertexMap[tindex] = d;
                                }
                            }
                            else
                            {
                                vertexMap.Add(tindex, new BaseLineWork() { vindex = tindex, dist = dist });
                                mapcnt++;
                            }
                        }
                    }

                    // nextリスト構築
                    nextList.Clear();
                    if (mapcnt > 0)
                    {
                        // Unity2023.1.5対応
                        //nextList.AddRange(vertexMap.GetValueArray(Allocator.Temp));
                        foreach (var kv in vertexMap)
                        {
                            nextList.Add(kv.Value);
                        }

                        // 親への距離の昇順にソート
                        nextList.Sort();
                    }

                    level++;
                }
            }
        }

        /// <summary>
        /// (Mesh)固定ポイントをリストにする
        /// </summary>
        [BurstCompile]
        struct BaseLine_Mesh_CareteFixedListJob : IJob
        {
            public int vcnt;
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> attribues;

            public NativeList<int> fixedList;

            public void Execute()
            {
                for (int i = 0; i < vcnt; i++)
                {
                    var attr = attribues[i];
                    if (attr.IsFixed())
                    {
                        fixedList.Add(i);
                    }
                }
            }
        }

        //-----------------------------------------------------------------------------------------
        /// <summary>
        /// [BoneCloth]ベースライン情報の作成
        /// BoneClothでは単純にTransformの親子構造がそのままベースラインとなる
        /// </summary>
        void CreateTransformBaseLine()
        {
            int vcnt = VertexCount;
            vertexParentIndices = new NativeArray<int>(vcnt, Allocator.Persistent);
            using var dataBuilder = new MultiDataBuilder<ushort>(vcnt, vcnt * 2);

            // トランスフォーム情報から親子関係を構築する
            // parent
            var idToIndexDict = new Dictionary<int, int>(vcnt);
            var idArray = transformData.idArray.GetNativeArray();
            var parentIdArray = transformData.parentIdArray.GetNativeArray();
            for (int i = 0; i < vcnt; i++)
            {
                idToIndexDict.Add(idArray[i], i);
            }
            for (int index = 0; index < vcnt; index++)
            {
                int pid = parentIdArray[index];
                if (idToIndexDict.ContainsKey(pid))
                    vertexParentIndices[index] = idToIndexDict[pid];
                else
                    vertexParentIndices[index] = -1;
            }

            // child
            var job = new BaseLine_Bone_CreateBoneChildInfoJob()
            {
                vcnt = vcnt,
                parentIndices = vertexParentIndices,
                childMap = dataBuilder.Map,
            };
            job.Run();

            // 親子関係からベースラインを構築する
            int rootCount = transformData.RootCount;
            Debug.Assert(rootCount > 0);
            var rootStack = new Stack<int>(vcnt);
            var stack = new Stack<int>(vcnt);
            var lineFlags = new List<ExBitFlag8>(rootCount);
            var startIndices = new List<ushort>(rootCount);
            var dataCounts = new List<ushort>(rootCount);
            var indices = new List<ushort>(vcnt);
            foreach (int id in transformData.rootIdList)
            {
                // ルートからTransformを走査して最初の移動ポイントを持つ固定を起点とする
                rootStack.Clear();
                int rootIndex = idToIndexDict[id];
                rootStack.Push(rootIndex);

                while (rootStack.Count > 0)
                {
                    int index0 = rootStack.Pop();
                    var attr = attributes[index0];
                    if (attr.IsDontMove() == false)
                        continue;

                    // 子に移動が含まれるかチェック
                    bool hasMove = false;
                    foreach (var data in dataBuilder.Map.GetValuesForKey(index0))
                    {
                        if (attributes[data].IsMove())
                            hasMove = true;
                    }

                    // 子に移動が含まれない場合はさらに深く潜る
                    if (hasMove == false)
                    {
                        foreach (var data in dataBuilder.Map.GetValuesForKey(index0))
                        {
                            if (attributes[data].IsDontMove())
                                rootStack.Push(data);
                        }
                        continue;
                    }

                    // 自身が固定で子に移動が含まれる場合はここをベースラインの起点として構築する
                    stack.Clear();
                    stack.Push(index0);
                    ushort start = (ushort)indices.Count;
                    ushort count = 0;
                    ExBitFlag8 lineflag = new ExBitFlag8();

                    while (stack.Count > 0)
                    {
                        int index = stack.Pop();
                        indices.Add((ushort)index);
                        count++;

                        // この頂点がラインに属している場合はフラグを立てる
                        if (attributes[index].IsSet(VertexAttribute.Flag_Triangle) == false)
                        {
                            lineflag.SetFlag(BaseLineFlag_IncludeLine, true);
                        }

                        // 子
                        if (dataBuilder.Map.ContainsKey(index))
                        {
                            foreach (var data in dataBuilder.Map.GetValuesForKey(index))
                            {
                                // 移動属性以外は無視する
                                if (attributes[data].IsDontMove())
                                    continue;

                                stack.Push(data);
                            }
                        }
                    }

                    // 格納
                    lineFlags.Add(lineflag);
                    startIndices.Add(start);
                    dataCounts.Add(count);
                }
            }
            baseLineFlags = new NativeArray<ExBitFlag8>(lineFlags.ToArray(), Allocator.Persistent);
            baseLineStartDataIndices = new NativeArray<ushort>(startIndices.ToArray(), Allocator.Persistent);
            baseLineDataCounts = new NativeArray<ushort>(dataCounts.ToArray(), Allocator.Persistent);
            baseLineData = new NativeArray<ushort>(indices.ToArray(), Allocator.Persistent);

            dataBuilder.ToNativeArray(out vertexChildIndexArray, out vertexChildDataArray);
        }

        /// <summary>
        /// (Bone)ベースライン上の頂点ごとの子頂点リストを求める
        /// </summary>
        [BurstCompile]
        struct BaseLine_Bone_CreateBoneChildInfoJob : IJob
        {
            public int vcnt;

            [Unity.Collections.ReadOnly]
            public NativeArray<int> parentIndices;
            //[Unity.Collections.WriteOnly]
            public NativeParallelMultiHashMap<int, ushort> childMap;

            public void Execute()
            {
                // 頂点ごとの子頂点を調べるて格納する
                for (int i = 0; i < vcnt; i++)
                {
                    int pindex = parentIndices[i];
                    if (pindex >= 0)
                    {
                        childMap.Add(pindex, (ushort)i);
                    }
                }
            }
        }

        //-----------------------------------------------------------------------------------------
        /// <summary>
        /// (Mesh/Bone)ベースラインの基準姿勢を求める
        /// </summary>
        void CreateBaseLinePose()
        {
            int dataCount = baseLineData.Length;
            vertexLocalPositions = new NativeArray<float3>(VertexCount, Allocator.Persistent);
            vertexLocalRotations = new NativeArray<quaternion>(VertexCount, Allocator.Persistent);
            var calcLinePoseJob = new BaseLine_CalcLocalPositionRotationJob()
            {
                parentIndices = vertexParentIndices,
                localPositions = localPositions.GetNativeArray(),
                localNormals = localNormals.GetNativeArray(),
                localTangents = localTangents.GetNativeArray(),
                baseLineIndices = baseLineData,
                vertexLocalPositions = vertexLocalPositions,
                vertexLocalRotations = vertexLocalRotations,
            };
            calcLinePoseJob.Run(dataCount);
        }

        /// <summary>
        /// ベースラインの基準姿勢を求める
        /// </summary>
        [BurstCompile]
        struct BaseLine_CalcLocalPositionRotationJob : IJobParallelFor
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<int> parentIndices;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localNormals;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localTangents;


            [Unity.Collections.ReadOnly]
            public NativeArray<ushort> baseLineIndices;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> vertexLocalPositions;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<quaternion> vertexLocalRotations;

            public void Execute(int index)
            {
                int vindex = baseLineIndices[index];

                int pindex = parentIndices[vindex];
                if (pindex >= 0)
                {
                    float3 ppos = localPositions[pindex];
                    float3 pnor = localNormals[pindex];
                    float3 ptan = localTangents[pindex];
                    quaternion prot = MathUtility.ToRotation(pnor, ptan);
                    quaternion iprot = math.inverse(prot);

                    float3 pos = localPositions[vindex];
                    float3 nor = localNormals[vindex];
                    float3 tan = localTangents[vindex];
                    quaternion rot = MathUtility.ToRotation(nor, tan);


                    float3 lpos = math.mul(iprot, pos - ppos);
                    quaternion lrot = math.mul(iprot, rot);
                    vertexLocalPositions[vindex] = lpos;
                    vertexLocalRotations[vindex] = lrot;
                }
                else
                {
                    vertexLocalPositions[vindex] = 0;
                    vertexLocalRotations[vindex] = quaternion.identity;
                }
            }
        }

        /// <summary>
        /// 頂点ごとのルートインデックスと深さを求める
        /// </summary>
        void CreateVertexRootAndDepth()
        {
            // ベースラインが存在しなくとも配列は用意する
            int vcnt = VertexCount;
            vertexDepths = new NativeArray<float>(vcnt, Allocator.Persistent);
            vertexRootIndices = new NativeArray<int>(vcnt, Allocator.Persistent);

            // 作業バッファ
            using var rootLengthArray = new NativeArray<float>(vcnt, Allocator.Persistent);

            // 頂点ごとのルートインデックスと深さを計算する
            var job = new BaseLine_CalcMaxBaseLineLengthJob()
            {
                vcnt = vcnt,
                attribues = attributes.GetNativeArray(),
                localPositions = localPositions.GetNativeArray(),
                vertexParentIndices = vertexParentIndices,

                vertexDepths = vertexDepths,
                vertexRootIndices = vertexRootIndices,

                rootLengthArray = rootLengthArray,
            };
            job.Run();

        }

        [BurstCompile]
        struct BaseLine_CalcMaxBaseLineLengthJob : IJob
        {
            public int vcnt;

            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> attribues;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> vertexParentIndices;

            [Unity.Collections.WriteOnly]
            public NativeArray<float> vertexDepths;
            [Unity.Collections.WriteOnly]
            public NativeArray<int> vertexRootIndices;

            public NativeArray<float> rootLengthArray;


            public void Execute()
            {
                float maxLen = 0;

                // 各移動頂点のルートまでの距離を計算する
                // およびルート頂点を記録する
                for (int i = 0; i < vcnt; i++)
                {
                    int rootIndex = -1;
                    float rootLen = 0;

                    if (attribues[i].IsMove())
                    {
                        int cindex = i;
                        int pindex = vertexParentIndices[cindex];
                        while (pindex >= 0)
                        {
                            var pos = localPositions[cindex];
                            var ppos = localPositions[pindex];
                            float dist = math.distance(pos, ppos);
                            rootLen += dist;
                            rootIndex = pindex;

                            // 親が固定ならばここがルートとなり終了
                            if (attribues[pindex].IsMove() == false)
                                break;

                            // next
                            cindex = pindex;
                            pindex = vertexParentIndices[cindex];
                        }
                    }

                    vertexRootIndices[i] = rootIndex;
                    rootLengthArray[i] = rootLen;

                    // 最大距離
                    maxLen = math.max(maxLen, rootLen);
                }

                // 深さを割り出す
                if (maxLen > Define.System.Epsilon)
                {
                    for (int i = 0; i < vcnt; i++)
                    {
                        var rootLen = rootLengthArray[i];
                        float depth = math.saturate(rootLen / maxLen);
                        vertexDepths[i] = depth;
                    }
                }
            }
        }

        //=========================================================================================
#if false // pitch/yaw個別制限はv1.0では実装しないので一旦ん停止
        /// <summary>
        /// 角度制限計算用ローカル回転の算出
        /// </summary>
        public void CreateAngleCalcLocalRotation(NormalCalcMode normalCalcMode, float3 normalCalcCenter)
        {
            if (VertexCount == 0)
                return;

            // 配列初期化
            vertexAngleCalcLocalRotations = new NativeArray<quaternion>(VertexCount, Allocator.Persistent);
            JobUtility.FillRun(vertexAngleCalcLocalRotations, VertexCount, quaternion.identity);

            // 頂点ごとに算出する
            var job = new AngleCalcLocalRotationJob()
            {
                calcMode = normalCalcMode,
                calcPoint = normalCalcCenter,
                //calcPoint = GetCenterTransform().TransformPoint(normalCalcCenter),
                //calcPoint = math.transform(initLocalToWorld, normalCalcCenter),

                attribues = attributes.GetNativeArray(),
                localPositions = localPositions.GetNativeArray(),
                localNormals = localNormals.GetNativeArray(),
                localTangents = localTangents.GetNativeArray(),
                vertexParentIndices = vertexParentIndices,
                vertexChildIndices = vertexChildIndices,
                vertexAngleCalcLocalRotations = vertexAngleCalcLocalRotations,
            };
            job.Run(VertexCount);
        }

        [BurstCompile]
        struct AngleCalcLocalRotationJob : IJobParallelFor
        {
            public NormalCalcMode calcMode;
            public float3 calcPoint;
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> attribues;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localNormals;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localTangents;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> vertexParentIndices;
            [Unity.Collections.ReadOnly]
            public NativeArray<ExFixedSet32Bytes<ushort>> vertexChildIndices;

            [Unity.Collections.WriteOnly]
            public NativeArray<quaternion> vertexAngleCalcLocalRotations;

            public void Execute(int vindex)
            {
                // 子への方向、子がいない場合は親からの方向をfowardベクトルとする
                float3 z = 0;
                var clist = vertexChildIndices[vindex];
                int pindex = vertexParentIndices[vindex];
                var pos = localPositions[vindex];
                var nor = localNormals[vindex]; // 実はforward
                var tan = localTangents[vindex]; // 実はup
                var bin = MathUtility.Binormal(nor, tan);
                if (clist.Count > 0)
                {
                    // 子への方向の平均ベクトル
                    for (int i = 0; i < clist.Count; i++)
                    {
                        int cindex = clist.Get(i);
                        var cpos = localPositions[cindex];
                        z += (cpos - pos);
                    }
                    z = math.normalize(z);
                }
                else if (pindex >= 0)
                {
                    // 親からのベクトル
                    var ppos = localPositions[pindex];
                    z = math.normalize(pos - ppos);
                }
                else
                    return;

                // upベクトル
                float3 y = 0;
                if (calcMode == NormalCalcMode.Auto)
                {
                    // z方向と内積がもっとも0に近いものを見つける（つまり直角）
                    // その軸をupベクトルとする
                    float norDot = math.abs(math.dot(z, nor));
                    float tanDot = math.abs(math.dot(z, tan));
                    y = norDot < tanDot ? nor : tan;
                }
                else if (calcMode == NormalCalcMode.X_Axis)
                {
                    // 元のX軸をupベクトルとする
                    y = bin;
                }
                else if (calcMode == NormalCalcMode.Y_Axis)
                {
                    // 元のY軸をupベクトルとする
                    y = tan;
                }
                else if (calcMode == NormalCalcMode.Z_Axis)
                {
                    // 元のZ軸をupベクトルとする
                    y = nor;
                }
                else if (calcMode == NormalCalcMode.Point_Outside)
                {
                    // 指定された中心点から外側へ
                    y = math.normalize(pos - calcPoint);
                }
                else if (calcMode == NormalCalcMode.Point_Inside)
                {
                    // 指定された中心点から内側へ
                    y = math.normalize(calcPoint - pos);
                }
                else
                {
                    Debug.LogError("まだ未実装！");
                    return;
                }

                // rightベクトルを求める
                float3 x = math.cross(z, y);

                // もう一度upベクトルを求める
                y = math.cross(x, z);

                // このz/y軸で回転を作成
                var angleRot = quaternion.LookRotation(z, y);

                // 元の頂点回転姿勢
                var rot = MathUtility.ToRotation(nor, tan);

                // ローカル回転に変換
                angleRot = math.mul(math.inverse(rot), angleRot);


                vertexAngleCalcLocalRotations[vindex] = angleRot;
            }
        }
#endif
    }
}
