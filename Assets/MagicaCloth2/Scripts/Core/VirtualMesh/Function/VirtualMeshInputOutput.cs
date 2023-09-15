// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth2
{
    public partial class VirtualMesh
    {
        /// <summary>
        /// レンダー情報からインポートする（スレッド可）
        /// </summary>
        /// <param name="rsetup"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public void ImportFrom(RenderSetupData rsetup)
        {
            try
            {
                // ★基本的に一連の作業が完了するまでキャンセルはさせない（安全性のため）
                if (rsetup == null)
                {
                    result.SetError(Define.Result.VirtualMesh_InvalidSetup);
                    throw new MagicaClothProcessingException();
                }
                if (rsetup.IsFaild())
                {
                    result.SetError(Define.Result.VirtualMesh_InvalidSetup);
                    throw new MagicaClothProcessingException();
                }

                // ========== Transform =========
                // セットアップ情報からトランスフォームを登録する
                if (transformData == null)
                    transformData = new TransformData(rsetup.TransformCount);
                int[] indices = transformData.AddTransformRange(
                    rsetup.transformList,
                    rsetup.transformIdList,
                    rsetup.transformParentIdList,
                    rsetup.rootTransformIdList,
                    rsetup.transformLocalPositins,
                    rsetup.transformLocalRotations,
                    rsetup.transformPositions,
                    rsetup.transformRotations,
                    rsetup.transformScales,
                    rsetup.transformInverseRotations
                    );

                // center
                centerTransformIndex = indices[rsetup.renderTransformIndex];

                // 初期化時のマトリックスを記録
                initLocalToWorld = rsetup.initRenderLocalToWorld;
                initWorldToLocal = rsetup.initRenderWorldtoLocal;
                initRotation = rsetup.initRenderRotation;
                initInverseRotation = math.inverse(initRotation);
                initScale = rsetup.initRenderScale;

                // ========== ここからMesh/Boneで分岐 ==========
                if (rsetup.setupType == RenderSetupData.SetupType.Mesh)
                {
                    // メッシュタイプ
                    meshType = MeshType.NormalMesh;
                    isBoneCloth = false;
                    ImportMeshType(rsetup, indices);

                    // スキニングメッシュでは１回スキニングを行いクロスローカル空間に姿勢を変換する
                    if (rsetup.hasBoneWeight)
                    {
                        ImportMeshSkinning();
                    }
                }
                else if (rsetup.setupType == RenderSetupData.SetupType.Bone)
                {
                    // ボーンタイプ
                    meshType = MeshType.NormalBoneMesh;
                    isBoneCloth = true;
                    ImportBoneType(rsetup, indices);
                }
                else
                {
                    result.SetError(Define.Result.RenderSetup_InvalidType);
                    throw new IndexOutOfRangeException();
                }

                // AABB
                boundingBox = new NativeReference<AABB>(Allocator.Persistent);
                JobUtility.CalcAABBRun(localPositions.GetNativeArray(), VertexCount, boundingBox);

                // UV
                if (rsetup.setupType == RenderSetupData.SetupType.Bone && TriangleCount > 0)
                {
                    // ボーンタイプでトライアングルを含む場合
                    JobUtility.CalcUVWithSphereMappingRun(
                        localPositions.GetNativeArray(),
                        VertexCount,
                        boundingBox,
                        uv.GetNativeArray()
                        );
                }

                // 頂点平均接続距離算出
                CalcAverageAndMaxVertexDistanceRun();
            }
            catch (Exception)
            {
                if (result.IsNone()) result.SetError(Define.Result.VirtualMesh_ImportError);
                throw;
            }
            finally
            {
            }
        }

        /// <summary>
        /// Meshタイプのインポート
        /// </summary>
        /// <param name="rsetup"></param>
        /// <param name="transformIndices"></param>
        void ImportMeshType(RenderSetupData rsetup, int[] transformIndices)
        {
            // root bone
            skinRootIndex = transformIndices[rsetup.skinRootBoneIndex];

            // skin bones
            skinBoneTransformIndices.AddRange(transformIndices, rsetup.skinBoneCount);

            // bind pose
            skinBoneBindPoses.AddRange(rsetup.bindPoseList.ToArray());

            // ========== MeshData =========
            var meshData = rsetup.meshDataArray[0];
            int vcnt = meshData.vertexCount;
            //flags.AddRange(vcnt);
            localPositions.AddRange(vcnt);
            localNormals.AddRange(vcnt);
            localTangents.AddRange(vcnt);
            uv.AddRange(vcnt);
            boneWeights.AddRange(vcnt);


            meshData.GetVertices(localPositions.GetNativeArray<Vector3>());
            meshData.GetNormals(localNormals.GetNativeArray<Vector3>());
            if (meshData.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Tangent))
            {
                using var tangents = new NativeArray<Vector4>(vcnt, Allocator.TempJob);
                meshData.GetTangents(tangents);
                // tangent変換(Vector4->float3)
                localTangents.CopyFromWithTypeChangeStride(tangents);
            }
            else
            {
                Develop.DebugLogWarning($"[{name}] Tangents not found!");
                // tangentを生成する
                // このtangentは描画用では無く姿勢制御用なのである意味適当でも大丈夫
                var job = new Import_GenerateTangentJob()
                {
                    localNormals = localNormals.GetNativeArray(),
                    localTangents = localTangents.GetNativeArray(),
                };
                job.Run(vcnt);
            }
            if (meshData.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord0))
            {
                meshData.GetUVs(0, uv.GetNativeArray<Vector2>());
            }
            else
            {
                Debug.LogWarning($"[{name}] UV not found!");
            }

            // 属性
            attributes.AddRange(vcnt);

            // 参照インデックス
            referenceIndices.AddRange(vcnt);

            // bone weights
            using var startBoneWeightIndices = new NativeArray<int>(vcnt, Allocator.TempJob);

            // 参照インデックスに連番を振る
            JobUtility.SerialNumberRun(referenceIndices.GetNativeArray(), vcnt);

            // bone weights
            if (rsetup.hasBoneWeight)
            {
                // bonesPerVertexArrayから頂点ごとのデータ開始インデックスを算出する
                var importBoneWeightJob1 = new Import_BoneWeightJob1()
                {
                    vcnt = vcnt,
                    bonesPerVertexArray = rsetup.bonesPerVertexArray,
                    startBoneWeightIndices = startBoneWeightIndices,
                };
                importBoneWeightJob1.Run();

                // 頂点ごとのボーンウエイトをVirtualMeshBoneWeights構造体として格納する
                // ただし最大ウエイト数は４で打ち切る
                var importBoneWeightJob2 = new Import_BoneWeightJob2()
                {
                    startBoneWeightIndices = startBoneWeightIndices,
                    boneWeightArray = rsetup.boneWeightArray,
                    bonesPerVertexArray = rsetup.bonesPerVertexArray,
                    boneWeights = boneWeights.GetNativeArray(),
                };
                importBoneWeightJob2.Run(vcnt);
            }
            else
            {
                // 通常メッシュはセンタートランスフォーム100%でウエイトを付ける
                JobUtility.FillRun(boneWeights.GetNativeArray(), vcnt, new VirtualMeshBoneWeight(0, new float4(1, 0, 0, 0)));
            }

            // triangle
            for (int i = 0; i < meshData.subMeshCount; i++)
            {
                var submeshData = meshData.GetSubMesh(i);
                using var triangleIndices = new NativeArray<int>(submeshData.indexCount, Allocator.Persistent);
                meshData.GetIndices(triangleIndices, i);

                // 追加
                triangles.AddRangeTypeChange(triangleIndices);
            }
        }

        /// <summary>
        /// tangentを擬似生成する
        /// このtangentは描画用では無く姿勢制御用なのである意味適当でも大丈夫
        /// </summary>
        [BurstCompile]
        struct Import_GenerateTangentJob : IJobParallelFor
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localNormals;
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> localTangents;

            public void Execute(int vindex)
            {
                var nor = localNormals[vindex];
                float3 tan = math.up();
                if (math.dot(nor, tan) < 0.9)
                {
                    tan = math.normalize(math.cross(nor, tan));
                }
                else
                {
                    tan = math.normalize(math.cross(nor, math.right()));
                }
                localTangents[vindex] = tan;
            }
        }


        /// <summary>
        /// スキニングメッシュの頂点をスキニングして元のローカル空間に変換する
        /// </summary>
        void ImportMeshSkinning()
        {
            var job = new Import_CalcSkinningJob()
            {
                localPositions = localPositions.GetNativeArray(),
                localNormals = localNormals.GetNativeArray(),
                localTangents = localTangents.GetNativeArray(),
                boneWeights = boneWeights.GetNativeArray(),
                skinBoneTransformIndices = skinBoneTransformIndices.GetNativeArray(),
                bindPoses = skinBoneBindPoses.GetNativeArray(),

                transformPositionArray = transformData.positionArray.GetNativeArray(),
                transformRotationArray = transformData.rotationArray.GetNativeArray(),
                transformScaleArray = transformData.scaleArray.GetNativeArray(),

                toM = initWorldToLocal,
            };
            job.Run(VertexCount);
        }

        /// <summary>
        /// 頂点スキニングを行いワールド座標・法線・接線を求める
        /// </summary>
        [BurstCompile]
        struct Import_CalcSkinningJob : IJobParallelFor
        {
            //[Unity.Collections.ReadOnly]
            public NativeArray<float3> localPositions;
            //[Unity.Collections.ReadOnly]
            public NativeArray<float3> localNormals;
            //[Unity.Collections.ReadOnly]
            public NativeArray<float3> localTangents;
            [Unity.Collections.ReadOnly]
            public NativeArray<VirtualMeshBoneWeight> boneWeights;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> skinBoneTransformIndices;
            [Unity.Collections.ReadOnly]
            public NativeArray<float4x4> bindPoses;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> transformPositionArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> transformRotationArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> transformScaleArray;

            public float4x4 toM;

            public void Execute(int vindex)
            {
                var bw = boneWeights[vindex];
                int wcnt = bw.Count;
                float3 wpos = 0;
                float3 wnor = 0;
                float3 wtan = 0;
                for (int i = 0; i < wcnt; i++)
                {
                    float w = bw.weights[i];

                    int boneIndex = bw.boneIndices[i];
                    float4x4 bp = bindPoses[boneIndex];
                    float4 lpos = new float4(localPositions[vindex], 1);
                    float4 lnor = new float4(localNormals[vindex], 0);
                    float4 ltan = new float4(localTangents[vindex], 0);

                    float3 pos = math.mul(bp, lpos).xyz;
                    float3 nor = math.mul(bp, lnor).xyz;
                    float3 tan = math.mul(bp, ltan).xyz;

                    int tindex = skinBoneTransformIndices[boneIndex];
                    var tpos = transformPositionArray[tindex];
                    var trot = transformRotationArray[tindex];
                    var tscl = transformScaleArray[tindex];
                    MathUtility.TransformPositionNormalTangent(tpos, trot, tscl, ref pos, ref nor, ref tan);

                    wpos += pos * w;
                    wnor += nor * w;
                    wtan += tan * w;
                }

                // 再びローカル空間に変換する
                localPositions[vindex] = MathUtility.TransformPoint(wpos, toM);
                localNormals[vindex] = MathUtility.TransformDirection(wnor, toM);
                localTangents[vindex] = MathUtility.TransformDirection(wtan, toM);
            }
        }


        [BurstCompile]
        struct Import_BoneWeightJob1 : IJob
        {
            public int vcnt;

            [Unity.Collections.ReadOnly]
            public NativeArray<byte> bonesPerVertexArray;

            [Unity.Collections.WriteOnly]
            public NativeArray<int> startBoneWeightIndices;

            public void Execute()
            {
                int sindex = 0;
                for (int i = 0; i < vcnt; i++)
                {
                    startBoneWeightIndices[i] = sindex;
                    sindex += bonesPerVertexArray[i];
                }
            }
        }

        [BurstCompile]
        struct Import_BoneWeightJob2 : IJobParallelFor
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<int> startBoneWeightIndices;
            [Unity.Collections.ReadOnly]
            public NativeArray<BoneWeight1> boneWeightArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<byte> bonesPerVertexArray;

            [Unity.Collections.WriteOnly]
            public NativeArray<VirtualMeshBoneWeight> boneWeights;

            public void Execute(int vindex)
            {
                int sindex = startBoneWeightIndices[vindex];
                int wcnt = bonesPerVertexArray[vindex];
                var bw = new VirtualMeshBoneWeight();

                // 最大ウエイト数は４まで
                int index = 0;
                for (int i = 0; i < wcnt && i < 4; i++)
                {
                    var bw1 = boneWeightArray[sindex + i];
                    if (bw1.weight > 0.0f)
                    {
                        bw.weights[index] = bw1.weight;
                        bw.boneIndices[index] = bw1.boneIndex;
                        index++;
                    }
                }

                // ウエイト数が５以上あった場合はウエイト値を単位化する
                if (wcnt > 4)
                {
                    bw.AdjustWeight();
                }

                boneWeights[vindex] = bw;
            }
        }

        /// <summary>
        /// Boneタイプのインポート
        /// </summary>
        /// <param name="rsetup"></param>
        /// <param name="transformIndices"></param>
        void ImportBoneType(RenderSetupData rsetup, int[] transformIndices)
        {
            // Transform情報からメッシュを構築する
            int vcnt = rsetup.TransformCount - 1;
            //flags.AddRange(vcnt);
            localPositions.AddRange(vcnt);
            localNormals.AddRange(vcnt);
            localTangents.AddRange(vcnt);
            uv.AddRange(vcnt);
            boneWeights.AddRange(vcnt);
            attributes.AddRange(vcnt);
            referenceIndices.AddRange(vcnt);

            // BoneClothもスキニング処理に統一するためスキニング用ボーンを登録
            skinBoneTransformIndices.AddRange(transformIndices, rsetup.skinBoneCount);
            skinBoneBindPoses.AddRange(vcnt);

            // Transformの情報をローカル空間に変換し頂点情報に割り当てる
            // およびバインドポーズの算出
            var WtoL = rsetup.initRenderWorldtoLocal;
            var LtoW = rsetup.initRenderLocalToWorld;
            var boneVertexJob = new Import_BoneVertexJob()
            {
                WtoL = WtoL,
                LtoW = LtoW,

                transformPositions = rsetup.transformPositions,
                transformRotations = rsetup.transformRotations,
                transformScales = rsetup.transformScales,

                localPositions = localPositions.GetNativeArray(),
                localNormals = localNormals.GetNativeArray(),
                localTangents = localTangents.GetNativeArray(),
                boneWeights = boneWeights.GetNativeArray(),
                skinBoneBindPoses = skinBoneBindPoses.GetNativeArray(),
            };
            boneVertexJob.Run(vcnt);

            // 参照インデックスに連番を振る
            JobUtility.SerialNumberRun(referenceIndices.GetNativeArray(), vcnt);

            // Line/Triangleの形成
            // ★ここは数も少なくあまりBurstの恩恵を受けられないので普通にC#で構成する
            if (rsetup.boneConnectionMode == RenderSetupData.BoneConnectionMode.Line)
            {
                // Line接続
                var lineList = new List<int2>(vcnt);
                for (int i = 0; i < vcnt; i++)
                {
                    // 親に接続させる
                    int pindex = rsetup.GetParentTransformIndex(i, true); // センタートランスフォームは除外
                    if (pindex >= 0)
                    {
                        int2 line = DataUtility.PackInt2(pindex, i);
                        lineList.Add(line);
                    }
                }
                if (lineList.Count > 0)
                    lines = new ExSimpleNativeArray<int2>(lineList.ToArray());
            }
            else
            {
                // Mesh接続
                // トランスフォームIDからインデックスへの辞書を作成
                var idToIndexDict = new Dictionary<int, int>(vcnt);
                for (int i = 0; i < vcnt; i++)
                {
                    if (idToIndexDict.ContainsKey(rsetup.transformIdList[i]) == false)
                        idToIndexDict.Add(rsetup.transformIdList[i], i);
                }

                // ループ接続フラグ
                bool loopConnection = rsetup.boneConnectionMode == RenderSetupData.BoneConnectionMode.SequentialLoopMesh;

                // 順次接続フラグ
                bool sequentialConnection = rsetup.boneConnectionMode == RenderSetupData.BoneConnectionMode.SequentialLoopMesh
                    || rsetup.boneConnectionMode == RenderSetupData.BoneConnectionMode.SequentialNonLoopMesh;

                // ルートリスト
                var rootTransformIdList = new List<int>(rsetup.rootTransformIdList); // copy
                int rootCnt = rootTransformIdList.Count;
                const int firstRootIndex = 0;
                int lastRootIndex = rootCnt - 1;

                // オート接続の場合はルート同士が最近接点になるように並べ替える
                if (rsetup.boneConnectionMode == RenderSetupData.BoneConnectionMode.AutomaticMesh)
                {
                    var tempRootIdList = new List<int>(rootTransformIdList);

                    rootTransformIdList.Clear();
                    rootTransformIdList.Add(tempRootIdList[0]);
                    float lastDist = 0;
                    while (tempRootIdList.Count > 0)
                    {
                        int rootId = rootTransformIdList[rootTransformIdList.Count - 1];
                        tempRootIdList.Remove(rootId);
                        int vindex = idToIndexDict[rootId];
                        var pos = localPositions[vindex];

                        // next connection
                        float minDist = float.MaxValue;
                        int minId = 0;
                        for (int i = 0; i < tempRootIdList.Count; i++)
                        {
                            int rootId2 = tempRootIdList[i];
                            int vindex2 = idToIndexDict[rootId2];
                            var pos2 = localPositions[vindex2];

                            float dist = math.distance(pos, pos2);
                            if (dist < minDist)
                            {
                                minDist = dist;
                                minId = rootId2;
                            }

                        }
                        if (minId != 0)
                        {
                            if (lastDist == 0 || minDist < lastDist * 1.5f)
                            {
                                rootTransformIdList.Add(minId);
                                lastDist = lastDist == 0 ? minDist : (lastDist + minDist) * 0.5f;
                            }
                            else
                            {
                                // reverse
                                rootTransformIdList.Reverse();
                                lastDist = 0;
                            }
                        }
                    }

                    // 最初と最後のルート距離が平均以下ならばループ接続にする
                    if (rootTransformIdList.Count >= 3)
                    {
                        int rootId1 = rootTransformIdList[0];
                        int rootId2 = rootTransformIdList[rootTransformIdList.Count - 1];
                        int vindex1 = idToIndexDict[rootId1];
                        int vindex2 = idToIndexDict[rootId2];
                        var pos1 = localPositions[vindex1];
                        var pos2 = localPositions[vindex2];
                        float dist = math.distance(pos1, pos2);
                        if (dist < lastDist * 1.5f)
                        {
                            loopConnection = true;
                        }
                    }


                    // debug
                    //Debug.Log($"rootTransformIdList.Count:{rootTransformIdList.Count}");
                    //foreach (var rid in rootTransformIdList)
                    //{
                    //    Debug.Log($"[{rid}]");
                    //}
                }

                // 頂点ごとの接続情報の作成
                var linkList = new FixedList128Bytes<int>[vcnt];
                var vertexLvList = new int[vcnt];
                var vertexRootIndex = new int[vcnt];
                var lvIndexList = new List<FixedList512Bytes<int>>(); // レベルごとのリスト
                var mainEdgeSet = new HashSet<uint>(); // メインエッジ

                // まずトランスフォームの親子関係に接続
                var stack = new Stack<int>(vcnt);
                var lvstack = new Stack<int>(vcnt);
                for (int i = 0; i < rootCnt; i++)
                {
                    stack.Clear();
                    stack.Push(rootTransformIdList[i]); // root id
                    lvstack.Clear();
                    lvstack.Push(0);

                    while (stack.Count > 0)
                    {
                        int id = stack.Pop();
                        int lv = lvstack.Pop();
                        int vindex = idToIndexDict[id];
                        var pos = localPositions[vindex];

                        if (lvIndexList.Count <= lv)
                            lvIndexList.Add(new FixedList512Bytes<int>());
                        var indexList = lvIndexList[lv];
                        indexList.Add(vindex);
                        lvIndexList[lv] = indexList;

                        var link = new FixedList128Bytes<int>();

                        // parent
                        int pid = rsetup.transformParentIdList[vindex];
                        if (idToIndexDict.ContainsKey(pid))
                        {
                            int vindex2 = idToIndexDict[pid];
                            link.Add(vindex2);

                            // main edge
                            uint mainEdge = DataUtility.Pack32Sort(vindex, vindex2);
                            mainEdgeSet.Add(mainEdge);
                        }

                        // child
                        var clist = rsetup.transformChildIdList[vindex];
                        if (clist.Length > 0)
                        {
                            for (int j = 0; j < clist.Length; j++)
                            {
                                int cid = clist[j];
                                stack.Push(cid);
                                lvstack.Push(lv + 1);

                                int vindex2 = idToIndexDict[cid];
                                link.Add(vindex2);

                                // main edge
                                uint mainEdge = DataUtility.Pack32Sort(vindex, vindex2);
                                mainEdgeSet.Add(mainEdge);
                            }
                        }

                        linkList[vindex] = link;
                        vertexLvList[vindex] = lv;
                        vertexRootIndex[vindex] = i;
                    }
                }

                // debug
                //foreach (var mainEdge in mainEdgeSet)
                //    Debug.Log($"mainEdge:{DataUtility.Unpack32Hi(mainEdge)} - {DataUtility.Unpack32Low(mainEdge)}");

                // 次に同レベルの横を接続する
                uint startEndRootIndexPack = DataUtility.Pack32Sort(firstRootIndex, lastRootIndex);
                for (int i = 0; i < vcnt; i++)
                {
                    int lv = vertexLvList[i];
                    var lvList = lvIndexList[lv];
                    var pos = localPositions[i];
                    var link = linkList[i];
                    var rootIndex = vertexRootIndex[i];

                    // まず最近点をつなげる
                    float firstDist = float.MaxValue;
                    int firstIndex = -1;
                    foreach (var vindex in lvList)
                    {
                        if (vindex == i)
                            continue;

                        // 非ループならば始点と終点のルートラインは接続しない
                        var rootIndex2 = vertexRootIndex[vindex];
                        bool firstLast = startEndRootIndexPack == DataUtility.Pack32Sort(rootIndex, rootIndex2) && startEndRootIndexPack > 0;
                        if (loopConnection == false && firstLast)
                            continue;

                        // 順次接続なら自身の前後のルートラインのみ接続
                        if (sequentialConnection && !(loopConnection && firstLast))
                        {

                            if (math.abs(rootIndex - rootIndex2) > 1)
                                continue;
                        }

                        var pos2 = localPositions[vindex];
                        float dist = math.distance(pos, pos2);
                        if (dist < firstDist)
                        {
                            firstDist = dist;
                            firstIndex = vindex;
                        }
                    }
                    if (firstIndex >= 0)
                    {
                        link.Add(firstIndex);

                        // 次に最初に接続した距離の少し大きめの範囲にあるすべての頂点をつなげる
                        // ただし順次接続の場合は距離は無視する
                        firstDist = sequentialConnection ? float.MaxValue : firstDist * 1.5f; // 1.5f?
                        foreach (var vindex in lvList)
                        {
                            if (vindex == i || vindex == firstIndex)
                                continue;

                            // 非ループならば始点と終点のルートラインは接続しない
                            var rootIndex2 = vertexRootIndex[vindex];
                            bool firstLast = startEndRootIndexPack == DataUtility.Pack32Sort(rootIndex, rootIndex2) && startEndRootIndexPack > 0;
                            if (loopConnection == false && firstLast)
                                continue;

                            // 順次接続なら自身の前後のルートラインのみ接続
                            if (sequentialConnection && !(loopConnection && firstLast))
                            {
                                if (math.abs(rootIndex - rootIndex2) > 1)
                                    continue;
                            }

                            var pos2 = localPositions[vindex];
                            float dist = math.distance(pos, pos2);
                            if (dist <= firstDist)
                            {
                                link.Add(vindex);
                            }
                        }
                    }

                    linkList[i] = link;

                    // debug
                    //Debug.Log($"vindex:{i}, root:{vertexRootIndex[i]}, lv:{lv}, linkCount:{link.Length}");
                    //for (int l = 0; l < link.Length; l++)
                    //    Debug.Log($"->{link[l]}");
                }

                // 頂点の接続情報からトライアングルを形成する
                var edgeSet = new HashSet<int2>();
                var triangleEdgeSet = new HashSet<int2>();
                var triangleSet = new HashSet<int3>();
                for (int i = 0; i < vcnt; i++)
                {
                    var link = linkList[i];
                    if (link.Length == 0)
                    {
                        Debug.LogError($"Connection 0! [{i}]");
                        continue;
                    }
                    if (link.Length == 1)
                    {
                        // lineのみ
                        edgeSet.Add(DataUtility.PackInt2(i, link[0]));
                    }
                    else
                    {
                        // まずエッジとしてすべて登録
                        for (int j = 0; j < link.Length; j++)
                        {
                            int vindex1 = link[j];
                            edgeSet.Add(DataUtility.PackInt2(i, vindex1));
                        }

                        int rootIndex = vertexRootIndex[i];

                        // トライアングルの形成
                        var pos = localPositions[i];
                        for (int j = 0; j < link.Length - 1; j++)
                        {
                            int vindex1 = link[j];
                            var pos1 = localPositions[vindex1];
                            var v1 = pos1 - pos;
                            for (int k = j + 1; k < link.Length; k++)
                            {
                                int vindex2 = link[k];
                                var pos2 = localPositions[vindex2];
                                var v2 = pos2 - pos;

                                // 頂点位置が同じ座標を考慮
                                if (math.lengthsq(v1) < 1e-06f || math.lengthsq(v2) < 1e-06f)
                                    continue;

                                // ペアの角度が一定以上ならばスキップする
                                var ang = math.degrees(MathUtility.Angle(v1, v2));
                                if (ang >= Define.System.ProxyMeshBoneClothTriangleAngle)
                                    continue;

                                // ３つのルートラインにまたがる接続は行わない
                                int rootIndex1 = vertexRootIndex[vindex1];
                                int rootIndex2 = vertexRootIndex[vindex2];
                                if (rootIndex1 != rootIndex && rootIndex2 != rootIndex && rootIndex1 != rootIndex2)
                                    continue;

                                // トライアングルは１つ以上のメインエッジを含んでいなければならない
                                int mainEdgeCount = 0;
                                mainEdgeCount += mainEdgeSet.Contains(DataUtility.Pack32Sort(i, vindex1)) ? 1 : 0;
                                mainEdgeCount += mainEdgeSet.Contains(DataUtility.Pack32Sort(i, vindex2)) ? 1 : 0;
                                mainEdgeCount += mainEdgeSet.Contains(DataUtility.Pack32Sort(vindex1, vindex2)) ? 1 : 0;
                                if (mainEdgeCount == 0)
                                    continue;

                                // トライアングル生成
                                int3 tri = DataUtility.PackInt3(i, vindex1, vindex2);

                                if (triangleSet.Contains(tri) == false)
                                {
                                    //Debug.Log($"v:{i}, Tri:{tri}");
                                    triangleSet.Add(tri);

                                    // このトライアングルで利用されたエッジを記録
                                    triangleEdgeSet.Add(DataUtility.PackInt2(i, vindex1));
                                    triangleEdgeSet.Add(DataUtility.PackInt2(i, vindex2));
                                }
                            }
                        }
                    }
                }

                // トライアングル登録
                if (triangleSet.Count > 0)
                {
                    triangles = new ExSimpleNativeArray<int3>(triangleSet.Count);
                    int index = 0;
                    foreach (int3 tri in triangleSet)
                    {
                        //Debug.Log($"Tri:{tri}");
                        triangles[index] = tri;
                        index++;
                    }
                }

                // ライン登録
                // トライアングルに利用されなかったエッジのみ登録
                foreach (int2 edge in triangleEdgeSet)
                {
                    edgeSet.Remove(edge);
                }
                if (edgeSet.Count > 0)
                {
                    lines = new ExSimpleNativeArray<int2>(edgeSet.Count);
                    int index = 0;
                    foreach (int2 line in edgeSet)
                    {
                        //Debug.Log($"Line:{line}");
                        lines[index] = line;
                        index++;
                    }
                }
            }
        }

        [BurstCompile]
        struct Import_BoneVertexJob : IJobParallelFor
        {
            public float4x4 WtoL;
            public float4x4 LtoW;

            [Unity.Collections.ReadOnly]
            public NativeArray<float3> transformPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> transformRotations;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> transformScales;

            [Unity.Collections.WriteOnly]
            public NativeArray<float3> localPositions;
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> localNormals;
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> localTangents;
            [Unity.Collections.WriteOnly]
            public NativeArray<VirtualMeshBoneWeight> boneWeights;
            [Unity.Collections.WriteOnly]
            public NativeArray<float4x4> skinBoneBindPoses;

            public void Execute(int vindex)
            {
                // トランスフォーム姿勢
                float3 pos = transformPositions[vindex];
                quaternion rot = transformRotations[vindex];
                float3 scl = transformScales[vindex];

                // トランスフォーム姿勢をローカル空間に変換する
                float3 lpos = MathUtility.InverseTransformPoint(pos, WtoL);
                float3 lnor, ltan;
                lnor = math.mul(rot, math.up());
                ltan = math.mul(rot, math.forward());
                lnor = MathUtility.InverseTransformDirection(lnor, WtoL);
                ltan = MathUtility.InverseTransformDirection(ltan, WtoL);

                localPositions[vindex] = lpos;
                localNormals[vindex] = lnor;
                localTangents[vindex] = ltan;

                // bone weight
                var bw = new VirtualMeshBoneWeight(new int4(vindex, 0, 0, 0), new float4(1, 0, 0, 0));
                boneWeights[vindex] = bw;

                // bind pose
                var ltow = float4x4.TRS(pos, rot, scl);
                var wtol = math.inverse(ltow);
                var bindPose = math.mul(wtol, LtoW);
                skinBoneBindPoses[vindex] = bindPose;
            }
        }


        /// <summary>
        /// レンダーデータからインポートする
        /// </summary>
        /// <param name="renderData"></param>
        public void ImportFrom(RenderData renderData)
        {
            try
            {
                if (renderData == null)
                {
                    result.SetError(Define.Result.VirtualMesh_InvalidRenderData);
                    throw new MagicaClothProcessingException();
                }

                ImportFrom(renderData.setupData);
            }
            catch (MagicaClothProcessingException)
            {
                if (result.IsError() == false)
                    result.SetError(Define.Result.VirtualMesh_ImportError);
                throw;
            }
            catch (Exception)
            {
                //Debug.LogException(e);
                result.SetError(Define.Result.VirtualMesh_ImportError);
                throw;
            }
        }

        //=========================================================================================
        /// <summary>
        /// セレクションデータをもとにメッシュを切り取る（スレッド可）
        /// セレクションデータの移動属性に影響するトライアングルのみを摘出する
        /// 結果的にメッシュの頂点／トライアングル数が０になる場合もあるので注意！
        /// </summary>
        /// <param name="selectionData"></param>
        /// <param name="selectionLocalToWorldMatrix">セレクションデータの基準姿勢</param>
        /// <param name="mergin">検索距離</param>
        public void SelectionMesh(
            SelectionData selectionData,
            float4x4 selectionLocalToWorldMatrix,
            float mergin
            )
        {
            try
            {
                if (selectionData == null || selectionData.IsValid() == false)
                {
                    result.SetError(Define.Result.VirtualMesh_InvalidSelection);
                    throw new MagicaClothProcessingException();
                }

                // ジョブ作業用
                int selectionCount = selectionData.Count;
                using var selectionPositions = selectionData.GetPositionNativeArray();
                using var selectionAttribues = selectionData.GetAttributeNativeArray();

                // 座標空間が異なる場合はセレクションデータをメッシュ空間に合わせる
                bool sameSpace = MathUtility.CompareMatrix(selectionLocalToWorldMatrix, initLocalToWorld);
                if (sameSpace == false)
                {
                    // 座標変換
                    var toM = MathUtility.Transform(selectionLocalToWorldMatrix, initWorldToLocal);
                    JobUtility.TransformPositionRun(selectionPositions, selectionCount, toM);
                }

                //Develop.DebugLog($"sameSpace:{sameSpace}");
                Develop.DebugLog($"Selection.mergin:{mergin}");

                // セレクションデータを配置したグリッドマップを作成する
                float gridSize = mergin * 1.0f;
                using var gridMap = SelectionData.CreateGridMapRun(gridSize, selectionPositions, selectionAttribues, move: true, fix: true, ignore: true, invalid: false);

                // 移動ポイントから利用するトライアングルと頂点情報を選別する
                using var newTriangleList = new NativeList<int3>(TriangleCount, Allocator.Persistent);
                using var newVertexRemapIndices = new NativeArray<int>(VertexCount, Allocator.Persistent);
                using var newVertexCount = new NativeReference<int>(Allocator.Persistent);
                var selectGridJob = new Select_GridJob()
                {
                    gridSize = gridSize,
                    gridMap = gridMap.GetMultiHashMap(),

                    selectionCount = selectionCount,
                    selectionPositions = selectionPositions,
                    selectionAttributes = selectionAttribues,

                    vertexCount = VertexCount,
                    triangleCount = TriangleCount,
                    searchRadius = mergin,
                    meshPositions = localPositions.GetNativeArray(),
                    meshTriangles = triangles.GetNativeArray(),

                    newTriangles = newTriangleList,
                    newVertexRemapIndices = newVertexRemapIndices,
                    newVertexCount = newVertexCount,
                };
                selectGridJob.Run();

                // 結果的に有効な頂点が減った場合は頂点およびトライアングル情報を再構築する
                if (newVertexCount.Value < VertexCount)
                {
                    // 頂点情報を入れ替える
                    int nvcnt = newVertexCount.Value;
                    var newReferenceIndices = new ExSimpleNativeArray<int>(nvcnt);
                    var newAttributes = new ExSimpleNativeArray<VertexAttribute>(nvcnt);
                    var newLocalPositions = new ExSimpleNativeArray<float3>(nvcnt);
                    var newLocalNormals = new ExSimpleNativeArray<float3>(nvcnt);
                    var newLocalTangents = new ExSimpleNativeArray<float3>(nvcnt);
                    var newUv = new ExSimpleNativeArray<float2>(nvcnt);
                    var newBoneWeights = new ExSimpleNativeArray<VirtualMeshBoneWeight>(nvcnt);

                    var packVertexJob = new Select_PackVertexJob()
                    {
                        vertexCount = VertexCount,
                        newVertexRemapIndices = newVertexRemapIndices,

                        attributes = attributes.GetNativeArray(),
                        localPositions = localPositions.GetNativeArray(),
                        localNormals = localNormals.GetNativeArray(),
                        localTangents = localTangents.GetNativeArray(),
                        uv = uv.GetNativeArray(),
                        boneWeights = boneWeights.GetNativeArray(),

                        newReferenceIndices = newReferenceIndices.GetNativeArray(),
                        newAttributes = newAttributes.GetNativeArray(),
                        newLocalPositions = newLocalPositions.GetNativeArray(),
                        newLocalNormals = newLocalNormals.GetNativeArray(),
                        newLocalTangents = newLocalTangents.GetNativeArray(),
                        newUv = newUv.GetNativeArray(),
                        newBoneWeights = newBoneWeights.GetNativeArray(),

                    };
                    packVertexJob.Run();

                    referenceIndices.Dispose();
                    attributes.Dispose();
                    localPositions.Dispose();
                    localNormals.Dispose();
                    localTangents.Dispose();
                    uv.Dispose();
                    boneWeights.Dispose();

                    referenceIndices = newReferenceIndices;
                    attributes = newAttributes;
                    localPositions = newLocalPositions;
                    localNormals = newLocalNormals;
                    localTangents = newLocalTangents;
                    uv = newUv;
                    boneWeights = newBoneWeights;

                    // トライアングル情報を入れ替える
                    var newTriangles = new ExSimpleNativeArray<int3>(newTriangleList);
                    triangles.Dispose();
                    triangles = newTriangles;
                }
            }
            catch (MagicaClothProcessingException)
            {
                if (result.IsError() == false)
                    result.SetError(Define.Result.VirtualMesh_SelectionUnknownError);
                result.DebugLog();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                result.SetError(Define.Result.VirtualMesh_SelectionException);
            }
            finally
            {
            }
        }

        [BurstCompile]
        struct Select_PackVertexJob : IJob
        {
            public int vertexCount;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> newVertexRemapIndices;

            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> attributes;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localNormals;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localTangents;
            [Unity.Collections.ReadOnly]
            public NativeArray<float2> uv;
            [Unity.Collections.ReadOnly]
            public NativeArray<VirtualMeshBoneWeight> boneWeights;

            [Unity.Collections.WriteOnly]
            public NativeArray<int> newReferenceIndices;
            [Unity.Collections.WriteOnly]
            public NativeArray<VertexAttribute> newAttributes;
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> newLocalPositions;
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> newLocalNormals;
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> newLocalTangents;
            [Unity.Collections.WriteOnly]
            public NativeArray<float2> newUv;
            [Unity.Collections.WriteOnly]
            public NativeArray<VirtualMeshBoneWeight> newBoneWeights;

            public void Execute()
            {
                // 頂点パッキング
                for (int i = 0; i < vertexCount; i++)
                {
                    int remapIndex = newVertexRemapIndices[i];
                    if (remapIndex >= 0)
                    {
                        newReferenceIndices[remapIndex] = i; // 元のインデックス
                        newAttributes[remapIndex] = attributes[i];
                        newLocalPositions[remapIndex] = localPositions[i];
                        newLocalNormals[remapIndex] = localNormals[i];
                        newLocalTangents[remapIndex] = localTangents[i];
                        newUv[remapIndex] = uv[i];
                        newBoneWeights[remapIndex] = boneWeights[i];
                    }
                }
            }
        }

        [BurstCompile]
        struct Select_GridJob : IJob
        {
            public float gridSize;
            [Unity.Collections.ReadOnly]
            public NativeParallelMultiHashMap<int3, int> gridMap;

            public int selectionCount;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> selectionPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> selectionAttributes;

            public int vertexCount;
            public int triangleCount;
            public float searchRadius;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> meshPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<int3> meshTriangles;

            public NativeList<int3> newTriangles;
            public NativeArray<int> newVertexRemapIndices;
            [Unity.Collections.WriteOnly]
            public NativeReference<int> newVertexCount;

            public void Execute()
            {
                // (1)移動ポイントの一定距離にあるメッシュ頂点をマークする
                for (int vindex = 0; vindex < vertexCount; vindex++)
                {
                    var pos = meshPositions[vindex];
                    var remapIndex = -1;

#if true
                    // 範囲グリッド走査
                    // 範囲内に無効以外のポイントがある場合は有効になる
                    foreach (int3 grid in GridMap<int>.GetArea(pos, searchRadius, gridMap, gridSize))
                    {
                        if (gridMap.ContainsKey(grid) == false)
                            continue;

                        // このグリッドを検索する
                        foreach (int sindex in gridMap.GetValuesForKey(grid))
                        {
                            // 距離判定
                            float3 spos = selectionPositions[sindex];
                            float dist = math.distance(pos, spos);
                            if (dist > searchRadius)
                                continue;

                            // この頂点は利用する
                            remapIndex = 1;
                            break;
                        }

                        if (remapIndex >= 0)
                            break;
                    }
#endif
#if false
                    // 頂点に最も近いセレクション属性を調べる
                    // (1)一定範囲内にmove/fixed/ignoreがある場合は利用する
                    var nearAttr = VertexAttribute.Invalid;
                    float nearDist = float.MaxValue;
                    bool nearMove = false; // 一定範囲内に移動頂点が存在するかどうか
                    // 範囲グリッド走査
                    foreach (int3 grid in GridMap<int>.GetArea(pos, searchRadius, gridMap, gridSize))
                    {
                        if (gridMap.ContainsKey(grid) == false)
                            continue;

                        // このグリッドを検索する
                        foreach (int sindex in gridMap.GetValuesForKey(grid))
                        {
                            // 距離判定
                            float3 spos = selectionPositions[sindex];
                            float dist = math.distance(pos, spos);
                            var attr = selectionAttributes[sindex];

                            // 最も近い属性
                            if (dist < nearDist)
                            {
                                nearDist = dist;
                                nearAttr = attr;
                            }

                            // 特定範囲内に移動属性が存在するかどうか
                            if (dist <= searchRadius && attr.IsMove())
                            {
                                nearMove = true;
                            }
                        }
                    }
                    // 最も近い属性により利用判定する
                    if (nearAttr.IsInvalid() == false)
                    {
                        // move/fixed/ignoreは利用する
                        remapIndex = 1;
                    }
                    else
                    {
                        // 無効頂点の場合は一定範囲内に移動頂点がある場合のみ固定として利用する
                        if (nearMove)
                            remapIndex = 1;
                    }
#endif

                    newVertexRemapIndices[vindex] = remapIndex;
                }

                // (2)有効な頂点を含むトライアングルを摘出する
                for (int tindex = 0; tindex < triangleCount; tindex++)
                {
                    int3 tri = meshTriangles[tindex];
                    if (newVertexRemapIndices[tri.x] >= 0 || newVertexRemapIndices[tri.y] >= 0 || newVertexRemapIndices[tri.z] >= 0)
                    {
                        // このトライアングルは利用する
                        newTriangles.Add(tri);
                    }
                }

                // (3)有効トライアングルを利用する頂点に使用フラグをつける
                int ntcnt = newTriangles.Length;
                for (int i = 0; i < ntcnt; i++)
                {
                    int3 tri = newTriangles[i];

                    // 頂点利用マーク
                    newVertexRemapIndices[tri.x] = 1;
                    newVertexRemapIndices[tri.y] = 1;
                    newVertexRemapIndices[tri.z] = 1;
                }

                // (4)使用頂点のリマップインデックスを作成する
                int newIndex = 0;
                for (int i = 0; i < vertexCount; i++)
                {
                    int remapIndex = newVertexRemapIndices[i];
                    if (remapIndex >= 0)
                    {
                        // この頂点は使用する
                        newVertexRemapIndices[i] = newIndex;
                        newIndex++;
                    }
                }
                // 残った利用頂点数
                newVertexCount.Value = newIndex;

                // (5)トライアングル頂点をリマップする
                int newTriCount = newTriangles.Length;
                for (int i = 0; i < newTriCount; i++)
                {
                    int3 tri = newTriangles[i];
                    tri.x = newVertexRemapIndices[tri.x];
                    tri.y = newVertexRemapIndices[tri.y];
                    tri.z = newVertexRemapIndices[tri.z];
                    newTriangles[i] = tri;
                }
            }
        }

        /// <summary>
        /// 現在のメッシュに対して最適なセレクションの余白距離を算出する
        /// </summary>
        /// <param name="useReduction"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public float CalcSelectionMergin(ReductionSettings settings)
        {
            float aveDist = averageVertexDistance.Value;
            float reductionDist = 0;
            if (settings != null && settings.IsEnabled)
            {
                reductionDist = boundingBox.Value.MaxSideLength * settings.GetMaxConnectionDistance();
            }

            // 大きい方を採用
            float mergin = math.max(aveDist, reductionDist);

            // 大きめにする
            //mergin *= 2.0f;
            mergin *= 1.5f;

            // 少し小さくする
            //mergin *= 0.7f;

            return mergin;
        }


        //=========================================================================================
        /// <summary>
        /// メッシュを追加する（スレッド可）
        /// </summary>
        /// <param name="cmesh"></param>
        public void AddMesh(VirtualMesh cmesh)
        {
            try
            {
                if (IsError)
                    throw new InvalidOperationException();

                if (cmesh == null || cmesh.IsSuccess == false || cmesh.IsError)
                    throw new InvalidOperationException();

                // 実行前状態
                int skinBoneStart = SkinBoneCount;
                int vertexStart = VertexCount;
                int triangleStart = TriangleCount;
                int lineStart = LineCount;
                int transformStart = transformData.Count;

                // 追加メッシュの座標空間を変換するマトリックス
                float4x4 toM = cmesh.CenterTransformTo(this);

                // スキンボーンのトランスフォームを登録する
                int skinBoneCount = cmesh.SkinBoneCount;
                skinBoneTransformIndices.AddRange(skinBoneCount);
                for (int i = 0; i < skinBoneCount; i++)
                {
                    int stindex = cmesh.skinBoneTransformIndices[i];
                    // 重複を弾いて追加する
                    int tindex = transformData.AddTransform(cmesh.transformData, stindex, checkDuplicate: true);
                    skinBoneTransformIndices[skinBoneStart + i] = tindex;
                }

                // 結合範囲
                int vcnt = cmesh.VertexCount;
                cmesh.mergeChunk = new DataChunk(VertexCount, vcnt);

                // 領域確保
                attributes.AddRange(vcnt);
                localPositions.AddRange(vcnt);
                localNormals.AddRange(vcnt);
                localTangents.AddRange(vcnt);
                uv.AddRange(vcnt);
                boneWeights.AddRange(vcnt);

                // 追加メッシュの座標を追加先のローカル空間に変換してコピーする
                var copyVerticesJob = new Add_CopyVerticesJob()
                {
                    vertexOffset = vertexStart,
                    skinBoneOffset = skinBoneStart,

                    toM = toM,

                    // 追加メッシュ
                    srcAttributes = cmesh.attributes.GetNativeArray(),
                    srclocalPositions = cmesh.localPositions.GetNativeArray(),
                    srclocalNormals = cmesh.localNormals.GetNativeArray(),
                    srclocalTangents = cmesh.localTangents.GetNativeArray(),
                    srcUV = cmesh.uv.GetNativeArray(),
                    srcBoneWeights = cmesh.boneWeights.GetNativeArray(),

                    // 追加先
                    dstAttributes = attributes.GetNativeArray(),
                    dstlocalPositions = localPositions.GetNativeArray(),
                    dstlocalNormals = localNormals.GetNativeArray(),
                    dstlocalTangents = localTangents.GetNativeArray(),
                    dstUV = uv.GetNativeArray(),
                    dstBoneWeights = boneWeights.GetNativeArray(),
                    dstSkinBoneIndices = skinBoneTransformIndices.GetNativeArray(),
                };
                copyVerticesJob.Run(cmesh.VertexCount);


                // skin bone bindpose
                {
                    // このメッシュの座標空間に合わせてバインドポーズを再計算する
                    skinBoneBindPoses.AddRange(skinBoneCount);
                    var job = new Add_CalcBindPoseJob()
                    {
                        skinBoneOffset = skinBoneStart,
                        srcSkinBoneTransformIndices = cmesh.skinBoneTransformIndices.GetNativeArray(),
                        srcTransformPositionArray = cmesh.transformData.positionArray.GetNativeArray(),
                        srcTransformRotationArray = cmesh.transformData.rotationArray.GetNativeArray(),
                        srcTransformScaleArray = cmesh.transformData.scaleArray.GetNativeArray(),
                        dstCenterLocalToWorldMatrix = initLocalToWorld,
                        dstSkinBoneBindPoses = skinBoneBindPoses.GetNativeArray(),
                    };
                    job.Run(skinBoneCount);
                }

                // triangle
                if (cmesh.TriangleCount > 0)
                {
                    // add copy
                    triangles.AddRange(cmesh.TriangleCount);
                    var copyTriangleJob = new JobUtility.AddInt3DataCopyJob()
                    {
                        dstOffset = triangleStart,
                        addData = vertexStart,
                        srcData = cmesh.triangles.GetNativeArray(),
                        dstData = triangles.GetNativeArray(),
                    };
                    copyTriangleJob.Run(cmesh.TriangleCount);
                }

                // line
                if (cmesh.LineCount > 0)
                {
                    // add copy
                    lines.AddRange(cmesh.LineCount);
                    var copyLineJob = new JobUtility.AddInt2DataCopyJob()
                    {
                        dstOffset = lineStart,
                        addData = vertexStart,
                        srcData = cmesh.lines.GetNativeArray(),
                        dstData = lines.GetNativeArray(),
                    };
                    copyLineJob.Run(cmesh.LineCount);
                }

                // bounding box
                // プロキシメッシュ空間に変換して追加
                float3 bmin = cmesh.boundingBox.Value.Min;
                float3 bmax = cmesh.boundingBox.Value.Max;
                bmin = math.transform(toM, bmin);
                bmax = math.transform(toM, bmax);
                var aabb = new AABB(bmin, bmax);
                if (boundingBox.IsCreated)
                {
                    var bounds = boundingBox.Value;
                    bounds.Encapsulate(aabb);
                    boundingBox.Value = bounds;
                }
                else
                    boundingBox = new NativeReference<AABB>(aabb, Allocator.Persistent);
                //Develop.DebugLog($"merge boundingBox:{boundingBox.Value}");

                // 頂点間距離
                // プロキシメッシュ空間に変換し大きい方を採用する
                float scaleRatio = math.length(cmesh.initScale) / math.length(initScale);
                //Debug.Log($"cmesh.initScale:{cmesh.initScale}, proxyMesh.initScale:{initScale}, scaleRatio:{scaleRatio}");
                averageVertexDistance.Value = math.max(averageVertexDistance.Value, cmesh.averageVertexDistance.Value * scaleRatio);
                maxVertexDistance.Value = math.max(maxVertexDistance.Value, cmesh.maxVertexDistance.Value * scaleRatio);


#if false
                // test
                int maxBoneIndex = 0;
                for (int i = 0; i < VertexCount; i++)
                {
                    var bw = boneWeights[i];
                    for (int j = 0; j < 4; j++)
                    {
                        if (bw.weights[j] > 0.0f)
                            maxBoneIndex = math.max(maxBoneIndex, bw.boneIndices[j]);
                    }
                }
                Debug.Log($"MaxBoneIndex:{maxBoneIndex}");
#endif
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                result.SetError();
                throw;
            }
            finally
            {
            }
        }

        /// <summary>
        /// 空間が変更された場合はバインドポーズを再計算する
        /// </summary>
        [BurstCompile]
        struct Add_CalcBindPoseJob : IJobParallelFor
        {
            public int skinBoneOffset;

            [Unity.Collections.ReadOnly]
            public NativeArray<int> srcSkinBoneTransformIndices;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> srcTransformPositionArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> srcTransformRotationArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> srcTransformScaleArray;

            public float4x4 dstCenterLocalToWorldMatrix;

            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<float4x4> dstSkinBoneBindPoses;

            public void Execute(int boneIndex)
            {
                // 空間が変更された場合はバインドポーズを再計算する必要がある
                int srcIndex = srcSkinBoneTransformIndices[boneIndex];
                var tpos = srcTransformPositionArray[srcIndex];
                var trot = srcTransformRotationArray[srcIndex];
                var tscl = srcTransformScaleArray[srcIndex];

                var ltow = float4x4.TRS(tpos, trot, tscl);
                var wtol = math.inverse(ltow);

                var bindPose = math.mul(wtol, dstCenterLocalToWorldMatrix);
                dstSkinBoneBindPoses[skinBoneOffset + boneIndex] = bindPose;
            }
        }

        /// <summary>
        /// 頂点データを新しい領域にコピーする
        /// </summary>
        [BurstCompile]
        struct Add_CopyVerticesJob : IJobParallelFor
        {
            public int vertexOffset;
            public int skinBoneOffset;

            // 座標空間変換
            //public int same; // 追加先と同じ空間なら(1)
            public float4x4 toM;

            // src
            //[Unity.Collections.ReadOnly]
            //public NativeArray<ExBitFlag8> srcFlags;
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> srcAttributes;
            //[Unity.Collections.ReadOnly]
            //public NativeArray<float3> srcWorldPositions;
            //[Unity.Collections.ReadOnly]
            //public NativeArray<quaternion> srcWorldRotations;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> srclocalPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> srclocalNormals;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> srclocalTangents;
            [Unity.Collections.ReadOnly]
            public NativeArray<float2> srcUV;
            [Unity.Collections.ReadOnly]
            public NativeArray<VirtualMeshBoneWeight> srcBoneWeights;

            // dst
            //[NativeDisableParallelForRestriction]
            //[Unity.Collections.WriteOnly]
            //public NativeArray<ExBitFlag8> dstFlags;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<VertexAttribute> dstAttributes;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> dstlocalPositions;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> dstlocalNormals;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> dstlocalTangents;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<float2> dstUV;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<VirtualMeshBoneWeight> dstBoneWeights;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> dstSkinBoneIndices;

            public void Execute(int vindex)
            {
                int dindex = vertexOffset + vindex;

                // positin / normal
                //float3 wpos = srcWorldPositions[vindex];
                //quaternion wrot = srcWorldRotations[vindex];
                //float3 wnor = MathUtility.ToNormal(wrot);
                //float3 wtan = MathUtility.ToTangent(wrot);
                float3 lpos = srclocalPositions[vindex];
                float3 lnor = srclocalNormals[vindex];
                float3 ltan = srclocalTangents[vindex];
                //if (same == 0)
                //{
                //    // 座標空間が異なるのでコンバートする
                //    lpos = MathUtility.TransformPoint(lpos, toM);
                //    lnor = MathUtility.TransformDirection(lnor, toM);
                //    ltan = MathUtility.TransformDirection(ltan, toM);
                //}

                // ローカル変換
                //float3 lpos = MathUtility.TransformPoint(wpos, toM);
                //float3 lnor = MathUtility.TransformDirection(wnor, toM);
                //float3 ltan = MathUtility.TransformDirection(wtan, toM);

                // 座標空間の変換
                lpos = MathUtility.TransformPoint(lpos, toM);
                lnor = MathUtility.TransformDirection(lnor, toM);
                ltan = MathUtility.TransformDirection(ltan, toM);

                dstlocalPositions[dindex] = lpos;
                dstlocalNormals[dindex] = lnor;
                dstlocalTangents[dindex] = ltan;
                dstUV[dindex] = srcUV[vindex];

                // boneWeights
                if (vindex < srcBoneWeights.Length)
                {
                    var bw = srcBoneWeights[vindex];
                    bw.boneIndices += skinBoneOffset;

                    // boneIndexの重複がないようにより若いインデックスに組み替える
                    for (int i = 0; i < 4; i++)
                    {
                        if (bw.weights[i] < 1e-06f)
                            continue;

                        int bindex = bw.boneIndices[i];
                        int tindex = dstSkinBoneIndices[bindex];
                        for (int j = 0; j < bindex; j++)
                        {
                            if (dstSkinBoneIndices[j] == tindex)
                            {
                                // 組み換え
                                bw.boneIndices[i] = j;
                                break;
                            }
                        }
                    }

                    dstBoneWeights[dindex] = bw;
                }

                // attribute
                dstAttributes[dindex] = srcAttributes[vindex];

                // flag
                //dstFlags[dindex] = srcFlags[vindex];
            }
        }

        //=========================================================================================
        /// <summary>
        /// メッシュの基準トランスフォームを設定する（メインスレッドのみ）
        /// </summary>
        /// <param name="center"></param>
        /// <param name="skinRoot"></param>
        public void SetTransform(Transform center, Transform skinRoot = null, int centerId = 0, int skinRootId = 0)
        {
            SetCenterTransform(center, centerId);
            if (skinRoot != null)
                SetSkinRoot(skinRoot, skinRootId);
            else
                SetSkinRoot(center, centerId);

            // 基準トランスフォーム情報を記録
            initLocalToWorld = center.localToWorldMatrix;
            initWorldToLocal = math.inverse(initLocalToWorld);
            initRotation = center.rotation;
            initInverseRotation = math.inverse(initRotation);
            initScale = center.lossyScale;
        }

        /// <summary>
        /// レコード情報からメッシュの基準トランスフォームを設定する（スレッド可）
        /// </summary>
        /// <param name="record"></param>
        public void SetTransform(TransformRecord centerRecord, TransformRecord skinRootRecord = null)
        {
            centerTransformIndex = transformData.AddTransform(centerRecord);
            if (skinRootRecord != null)
                skinRootIndex = transformData.AddTransform(skinRootRecord);
            else
                skinRootIndex = centerTransformIndex;

            // 基準トランスフォーム情報を記録
            initLocalToWorld = centerRecord.localToWorldMatrix;
            initWorldToLocal = centerRecord.worldToLocalMatrix;
            initRotation = centerRecord.rotation;
            initInverseRotation = math.inverse(initRotation);
            initScale = centerRecord.scale;
        }

        public void SetCenterTransform(Transform t, int tid = 0)
        {
            if (t)
            {
                // すでに存在する場合は入れ替え
                if (centerTransformIndex >= 0)
                {
                    transformData.ReplaceTransform(centerTransformIndex, t, tid);
                }
                else
                {
                    centerTransformIndex = transformData.AddTransform(t, tid);
                }
            }
        }

        public void SetSkinRoot(Transform t, int tid = 0)
        {
            if (t)
            {
                // すでに存在する場合は入れ替え
                if (skinRootIndex >= 0)
                {
                    transformData.ReplaceTransform(skinRootIndex, t, tid);
                }
                else
                {
                    skinRootIndex = transformData.AddTransform(t, tid);
                }
            }
        }

        public Transform GetCenterTransform()
        {
            return transformData.GetTransformFromIndex(centerTransformIndex);
        }

        public float4x4 GetCenterLocalToWorldMatrix()
        {
            return transformData.GetLocalToWorldMatrix(centerTransformIndex);
        }

        public float4x4 GetCenterWorldToLocalMatrix()
        {
            return transformData.GetWorldToLocalMatrix(centerTransformIndex);
        }

        /// <summary>
        /// カスタムスキニング用ボーンを登録する
        /// </summary>
        /// <param name="bones"></param>
        public void SetCustomSkinningBones(TransformRecord clothTransformRecord, List<TransformRecord> bones)
        {
            if (bones == null || bones.Count == 0)
                return;

            customSkinningBoneIndices = new int[bones.Count];
            for (int i = 0; i < bones.Count; i++)
            {
                var rd = bones[i];
                int index = -1;
                if (rd.IsValid())
                {
                    // クロストランスフォームのローカル空間に変換して登録する
                    rd.localPosition = clothTransformRecord.worldToLocalMatrix.MultiplyPoint(rd.position);

                    // ボーンの登録。スキニング用ボーンとしても登録。
                    index = skinBoneTransformIndices.Count;
                    int tindex = transformData.AddTransform(rd, checkDuplicate: false); // 重複ありで最後に追加する
                    skinBoneTransformIndices.Add(tindex);
                    var bindPose = math.mul(rd.worldToLocalMatrix, initLocalToWorld); // bind pose
                    skinBoneBindPoses.Add(bindPose);
                }
                customSkinningBoneIndices[i] = index;
            }
        }

        /// <summary>
        /// このメッシュと対象メッシュの座標空間が同じか判定する
        /// これはそれぞれ初期化時のマトリックスで比較される
        /// </summary>
        /// <param name="to"></param>
        /// <returns></returns>
        public bool CompareSpace(VirtualMesh target)
        {
            return MathUtility.CompareMatrix(initLocalToWorld, target.initLocalToWorld);
        }

        /// <summary>
        /// このメッシュの座標空間をtoメッシュの座標空間に変換するマトリックスを返す
        /// これはそれぞれ初期化時のマトリックスで計算される
        /// </summary>
        /// <param name="to"></param>
        /// <returns></returns>
        public float4x4 CenterTransformTo(VirtualMesh to)
        {
            bool sameSpace = CompareSpace(to);
            return sameSpace ? float4x4.identity : MathUtility.Transform(initLocalToWorld, to.initWorldToLocal);
        }

        //=========================================================================================
        /// <summary>
        /// UnityMeshに出力する（メインスレッドのみ）
        /// ※ほぼデバッグ用
        /// </summary>
        /// <returns></returns>
        public Mesh ExportToMesh(bool buildSkinning = false, bool recalculationNormals = false, bool recalculationBounds = true)
        {
            Debug.Assert(IsSuccess);

            var mesh = new Mesh();
            mesh.MarkDynamic();

            // vertices
            var newVertices = new Vector3[VertexCount];
            var newNormals = new Vector3[VertexCount];
            localPositions.CopyTo(newVertices);
            localNormals.CopyTo(newNormals);

            // 接線はwを(-1)にして書き込む
            using var tangentArray = new NativeArray<Vector4>(VertexCount, Allocator.TempJob);
            JobUtility.FillRun(tangentArray, VertexCount, new Vector4(0, 0, 0, -1));
            var newTangents = tangentArray.ToArray();
            localTangents.CopyToWithTypeChangeStride(newTangents); // float3[] -> Vector4[]コピー

            mesh.vertices = newVertices;
            if (recalculationNormals == false)
                mesh.normals = newNormals;
            mesh.tangents = newTangents;

            // dymmy uv
            var newUvs = new Vector2[VertexCount];
            uv.CopyTo(newUvs); // 一応コピー（VirtualMeshのUVはTangent計算用、テクスチャマッピング用ではない）
            mesh.uv = newUvs;

            // triangle
            if (TriangleCount > 0)
            {
                var newTriangles = new int[TriangleCount * 3];
                triangles.CopyToWithTypeChange(newTriangles);
                mesh.triangles = newTriangles;
            }

            // skinning
            if (buildSkinning)
            {
                // bone weight
                var newBoneWeights = new BoneWeight[VertexCount];
                boneWeights.CopyTo(newBoneWeights);
                mesh.boneWeights = newBoneWeights;

                // bind poses
                var newBindPoses = new Matrix4x4[SkinBoneCount];
                skinBoneBindPoses.CopyTo(newBindPoses);
                mesh.bindposes = newBindPoses;
            }

            if (recalculationNormals)
                mesh.RecalculateNormals();
            //mesh.RecalculateTangents();
            if (recalculationBounds)
                mesh.RecalculateBounds();


            return mesh;
        }

        /// <summary>
        /// メッシュの基準トランスフォームを返す
        /// 通常はレンダラーのtransform
        /// </summary>
        /// <returns></returns>
        public Transform ExportCenterTransform()
        {
            return transformData.GetTransformFromIndex(centerTransformIndex);
        }

        public Transform ExportSkinRootBone()
        {
            return transformData.GetTransformFromIndex(skinRootIndex);
        }

        /// <summary>
        /// メッシュのスキニング用ボーンリストを返す
        /// </summary>
        /// <returns></returns>
        public List<Transform> ExportSkinningBones()
        {
            var sbones = new List<Transform>(SkinBoneCount);
            for (int i = 0; i < SkinBoneCount; i++)
            {
                sbones.Add(transformData.GetTransformFromIndex(skinBoneTransformIndices[i]));
            }
            return sbones;
        }

        /// <summary>
        /// メッシュのバウンディングボックスを返す
        /// スキニングの場合はスキニングルートボーンからのバウンディングボックスとなる
        /// それ以外はセンターボーンからのバウンディングボックスとなる
        /// </summary>
        /// <returns></returns>
        /*public Bounds ExportBounds()
        {
            float3 offset = 0;
            if (skinRootIndex >= 0 && centerTransformIndex != skinRootIndex)
            {
                // スキニングのルートボーンが別の場合
                // ちょっと面倒
                float3 wmin = transformData.TransformPoint(centerTransformIndex, boundingBox.Value.Min);
                float3 wmax = transformData.TransformPoint(centerTransformIndex, boundingBox.Value.Max);
                float3 lmin = transformData.InverseTransformPoint(skinRootIndex, wmin);
                float3 lmax = transformData.InverseTransformPoint(skinRootIndex, wmax);
                float3 cen = (lmax + lmin) * 0.5f;
                float3 size = math.abs(lmax - lmin);
                return new Bounds(cen, size);
            }
            else
            {
                return new Bounds(boundingBox.Value.Center, boundingBox.Value.Extents);
            }
        }*/

        /// <summary>
        /// 現在のメッシュをレンダラーに反映させる（主にデバッグ用）
        /// </summary>
        /// <param name="ren"></param>
        public Mesh ToRenderer(Renderer ren)
        {
            Mesh mesh = null;
            if (IsSuccess == false)
                return mesh;

            if (ren is MeshRenderer)
            {
                mesh = ExportToMesh();
                var filter = ren.GetComponent<MeshFilter>();
                filter.mesh = mesh;
            }
            else if (ren is SkinnedMeshRenderer)
            {
                var sren = ren as SkinnedMeshRenderer;
                mesh = ExportToMesh(true);
                var rootBone = ExportSkinRootBone();
                var skinBones = ExportSkinningBones().ToArray();

                sren.rootBone = rootBone;
                sren.bones = skinBones;
                sren.sharedMesh = mesh;
            }

            return mesh;
        }

    }
}
