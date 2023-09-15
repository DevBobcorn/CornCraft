// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth2
{
    public partial class VirtualMesh
    {
        struct MappingWorkData
        {
            public float3 position;
            public int vertexIndex;
            public int proxyVertexIndex;
            public float proxyVertexDistance;
        }

        /// <summary>
        /// メッシュをプロキシメッシュにマッピングする（スレッド可）
        /// </summary>
        /// <param name="proxyMesh"></param>
        public void Mapping(VirtualMesh proxyMesh)
        {
            try
            {
                if (IsError)
                {
                    throw new MagicaClothProcessingException();
                }

                if (proxyMesh == null || proxyMesh.IsSuccess == false)
                {
                    result.SetError(Define.Result.MappingMesh_ProxyError);
                    throw new MagicaClothProcessingException();
                }

                // 処理中に切り替え
                result.SetProcess();

                // このマッピングメッシュのローカル座標をプロキシメッシュ空間に変換するマトリックスを求める
                var toP = CenterTransformTo(proxyMesh);

                // 頂点をプロキシメッシュのどの頂点にマッピングすべきかの情報
                using var mappingWorkData = new NativeArray<MappingWorkData>(VertexCount, Allocator.Persistent);

                if (mergeChunk.IsValid)
                {
                    // ■ダイレクトマッピング（プロキシメッシュに利用されたメッシュの場合）
                    // リダクション結果から接続するので確実で速い！
                    Develop.DebugLog($"Direct Mapping！");
                    var directConnectionJob = new Mapping_DirectConnectionVertexDataJob()
                    {
                        toP = toP,
                        vcnt = VertexCount,
                        mergeChunk = mergeChunk,
                        localPositions = localPositions.GetNativeArray(),
                        attributes = attributes.GetNativeArray(),

                        joinIndices = proxyMesh.joinIndices,
                        proxyAttributes = proxyMesh.attributes.GetNativeArray(),
                        proxyLocalPositions = proxyMesh.localPositions.GetNativeArray(),

                        mappingWorkData = mappingWorkData,
                    };
                    directConnectionJob.Run();

                    // 頂点マッピング
                    // 平均接続距離
                    var avgDist = proxyMesh.averageVertexDistance.Value;
                    float weightLength = avgDist * 1.5f;
                    //Debug.Log($"avgDist:{avgDist}, weightLength:{weightLength}");
                    using var useSet = new NativeParallelHashSet<ushort>(1024, Allocator.Persistent); // Unity2023.1.5対応
                    var calcDirectWeightJob = new Mapping_CalcDirectWeightJob()
                    {
                        vcnt = mappingWorkData.Length,
                        weightLength = weightLength,
                        mappingWorkData = mappingWorkData,

                        attributes = attributes.GetNativeArray(),
                        boneWeights = boneWeights.GetNativeArray(),

                        proxyLocalPositions = proxyMesh.localPositions.GetNativeArray(),
                        proxyVertexToVertexIndexArray = proxyMesh.vertexToVertexIndexArray,
                        proxyVertexToVertexDataArray = proxyMesh.vertexToVertexDataArray,

                        useSet = useSet, // Unity2023.1.5対応
                    };
                    calcDirectWeightJob.Run();
                }
                else
                {
                    // ■ 検索マッピング。これは精度が悪いので注意！
                    // 検索半径
                    // これはプロキシメッシュ座標空間での長さとなる
                    float averageDistance = MathUtility.TransformLength(averageVertexDistance.Value, toP);
                    averageDistance = math.max(averageDistance, Define.System.MinimumGridSize);
                    float searchRadius = averageDistance * 2.5f; // test(2.0?)
                    Develop.DebugLog($"Search Mapping! searchRadius:{searchRadius}");

                    // プロキシ頂点インデックスを格納したグリッドマップを作成する
                    float gridSize = averageDistance * 1.5f;
                    using var gridMap = proxyMesh.CreateVertexIndexGridMapRun(gridSize);

                    // 頂点をプロキシメッシュのどの頂点にマッピングすべきか判定する
                    var calcVertexDataJob = new Mapping_CalcConnectionVertexDataJob()
                    {
                        gridSize = gridSize,
                        searchRadius = searchRadius,

                        toP = toP,
                        vcnt = VertexCount,
                        localPositions = localPositions.GetNativeArray(),
                        boneWeights = boneWeights.GetNativeArray(),
                        transformIds = transformData.idArray.GetNativeArray(),
                        attributes = attributes.GetNativeArray(),

                        gridMap = gridMap.GetMultiHashMap(),
                        proxyAttributes = proxyMesh.attributes.GetNativeArray(),
                        proxyLocalPositions = proxyMesh.localPositions.GetNativeArray(),
                        proxyBoneWeights = proxyMesh.boneWeights.GetNativeArray(),
                        proxyTransformIds = proxyMesh.transformData.idArray.GetNativeArray(),

                        mappingWorkData = mappingWorkData,
                    };
                    calcVertexDataJob.Run();

                    // 頂点をマッピングする
                    if (mappingWorkData.Length > 0)
                    {
                        var calcWeightJob = new Mapping_CalcWeightJob()
                        {
                            mappingWorkData = mappingWorkData,
                            attributes = attributes.GetNativeArray(),
                            boneWeights = boneWeights.GetNativeArray(),

                            proxyAttributes = proxyMesh.attributes.GetNativeArray(),
                            proxyLocalPositions = proxyMesh.localPositions.GetNativeArray(),
                            proxyLocalNormals = proxyMesh.localNormals.GetNativeArray(),
                            proxyVertexToVertexIndexArray = proxyMesh.vertexToVertexIndexArray,
                            proxyVertexToVertexDataArray = proxyMesh.vertexToVertexDataArray,
                        };
                        calcWeightJob.Run(mappingWorkData.Length);
                    }
                }

                // 接続プロキシメッシュ
                mappingProxyMesh = proxyMesh;

                // プロキシメッシュへの変換マトリックスを記録
                toProxyMatrix = toP;
                toProxyRotation = math.mul(proxyMesh.initInverseRotation, initRotation);

                // メッシュタイプ
                meshType = MeshType.Mapping;

                // 完了
                result.SetSuccess();
            }
            catch (MagicaClothProcessingException)
            {
                if (result.IsError() == false)
                    result.SetError(Define.Result.MappingMesh_UnknownError);
                result.DebugLog();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                result.SetError(Define.Result.MappingMesh_Exception);
            }
        }

        [BurstCompile]
        struct Mapping_DirectConnectionVertexDataJob : IJob
        {
            // render meshの座標をproxyのローカル空間に変換するTransform
            public float4x4 toP;

            // render mesh
            public int vcnt;
            public DataChunk mergeChunk;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localPositions;
            [Unity.Collections.WriteOnly]
            public NativeArray<VertexAttribute> attributes;

            // proxy mesh
            [Unity.Collections.ReadOnly]
            public NativeArray<int> joinIndices;
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> proxyAttributes;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> proxyLocalPositions;

            // out
            [Unity.Collections.WriteOnly]
            public NativeArray<MappingWorkData> mappingWorkData;

            public void Execute()
            {
                for (int vindex = 0; vindex < vcnt; vindex++)
                {
                    // 接続プロキシメッシュ頂点インデックス
                    int proxyVertexIndex = joinIndices[mergeChunk.startIndex + vindex];

                    // プロキシ頂点の属性
                    var proxyAttr = proxyAttributes[proxyVertexIndex];

                    // プロキシ頂点が無効ならばマッピングしない
                    if (proxyAttr.IsInvalid())
                    {
                        // 頂点属性を無効としてマークする
                        attributes[vindex] = VertexAttribute.Invalid;
                        continue;
                    }

                    // 頂点を記録
                    // プロキシメッシュの座標空間に変換する
                    float3 pos = MathUtility.TransformPoint(localPositions[vindex], toP);

                    float3 proxyPos = proxyLocalPositions[proxyVertexIndex];

                    var wdata = new MappingWorkData()
                    {
                        position = pos,
                        vertexIndex = vindex,
                        proxyVertexIndex = proxyVertexIndex,
                        proxyVertexDistance = math.distance(pos, proxyPos),
                    };
                    mappingWorkData[vindex] = wdata;
                    attributes[vindex] = proxyAttr;
                }
            }
        }

        struct Mapping_CalcDirectWeightJob : IJob
        {
            // data
            public int vcnt;
            public float weightLength;
            [Unity.Collections.ReadOnly]
            public NativeArray<MappingWorkData> mappingWorkData;

            // render mesh
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> attributes;
            [Unity.Collections.WriteOnly]
            public NativeArray<VirtualMeshBoneWeight> boneWeights;

            // proxy
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> proxyLocalPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<uint> proxyVertexToVertexIndexArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<ushort> proxyVertexToVertexDataArray;

            public NativeParallelHashSet<ushort> useSet; // Unity2023.1.5対応

            public void Execute()
            {
                // 処理済みセット
                //var useSet = new NativeParallelHashSet<ushort>(1024, Allocator.Temp); // Unity2023.1.5対応
                var stack = new FixedList4096Bytes<ushort>();

                for (int vindex = 0; vindex < vcnt; vindex++)
                {
                    // すでにInvalidなら無効
                    if (attributes[vindex].IsInvalid())
                        continue;

                    var wdata = mappingWorkData[vindex];
                    ushort pindex = (ushort)wdata.proxyVertexIndex;

                    useSet.Clear();
                    stack.Clear();

                    // ウエイトバッファ
                    var weights = new ExCostSortedList4(-1);

                    stack.Push(pindex);
                    while (stack.IsEmpty == false)
                    {
                        pindex = stack.Pop();

                        if (useSet.Contains(pindex))
                            continue;
                        useSet.Add(pindex);

                        // 距離チェック
                        float dist = math.distance(wdata.position, proxyLocalPositions[pindex]);
                        if (dist > weightLength)
                            continue;

                        // ウエイト算出
                        const float weightPow = 3.0f;
                        //float w = Mathf.Clamp01((1.0f - dist / weightLength) + 0.001f);
                        float w = Mathf.Clamp01(1.0f - dist / weightLength);
                        w = Mathf.Pow(w, weightPow); // powのデフォルトは(3.0)
                        weights.Add(1.0f - w, pindex); // ExCostSortedList4は昇順格納なので一旦1.0から引く

                        // 次の接続
                        DataUtility.Unpack12_20(proxyVertexToVertexIndexArray[pindex], out var dcnt, out var dstart);
                        for (int i = 0; i < dcnt && stack.IsCapacity() == false; i++)
                        {
                            ushort tindex = proxyVertexToVertexDataArray[dstart + i];

                            if (useSet.Contains(tindex))
                                continue;

                            // 距離チェック
                            dist = math.distance(wdata.position, proxyLocalPositions[tindex]);
                            if (dist > weightLength)
                                continue;

                            stack.Push(tindex);
                        }
                    }

                    if (weights.Count == 0)
                    {
                        // 何も接続出来ない場合はデフォルトのプロキシメッシュ頂点をウエイト100%で接続させる
                        weights.Add(1.0f, pindex);
                    }
                    else
                    {
                        // ウエイトを合計１に調整する
                        int wcnt = weights.Count;
                        for (int i = 0; i < 4; i++)
                        {
                            if (i < wcnt)
                            {
                                // ExCostSortedList4は昇順格納なのでもとに戻す
                                weights.costs[i] = 1.0f - weights.costs[i];
                            }
                            else
                            {
                                weights.costs[i] = 0;
                                weights.data[i] = 0;
                            }
                        }
                        float total = math.csum(weights.costs);

                        // すべての頂点がweightLengthの場合は合計ウエイト０があり得るので特別に平均化する
                        if (total == 0.0f)
                        {
                            float w = 1.0f / wcnt;
                            for (int i = 0; i < wcnt; i++)
                                weights.costs[i] = w;
                        }
                        else
                            weights.costs = math.saturate(weights.costs / total);
                    }

                    // ウエイト格納
                    boneWeights[vindex] = new VirtualMeshBoneWeight(weights.data, weights.costs);
                }
            }
        }

        /// <summary>
        /// 頂点ごとにproxy頂点を検索しウエイト算出しboneWeightsに格納する
        /// </summary>
        [BurstCompile]
        struct Mapping_CalcConnectionVertexDataJob : IJob
        {
            public float gridSize;
            public float searchRadius;

            // vmeshの座標をproxyのローカル空間に変換するTransform
            public float4x4 toP;

            // vmesh
            public int vcnt;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<VirtualMeshBoneWeight> boneWeights;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> transformIds;
            [Unity.Collections.WriteOnly]
            public NativeArray<VertexAttribute> attributes;

            // proxy vmesh
            [Unity.Collections.ReadOnly]
            public NativeParallelMultiHashMap<int3, int> gridMap;
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> proxyAttributes;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> proxyLocalPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<VirtualMeshBoneWeight> proxyBoneWeights;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> proxyTransformIds;

            // out
            [Unity.Collections.WriteOnly]
            public NativeArray<MappingWorkData> mappingWorkData;

            public void Execute()
            {
                for (int vindex = 0; vindex < vcnt; vindex++)
                {
                    // posはproxyの座標空間に変換する
                    float3 pos = MathUtility.TransformPoint(localPositions[vindex], toP);

                    // オリジナルのボーンウエイト
                    VirtualMeshBoneWeight bw = boneWeights[vindex];
                    Debug.Assert(bw.IsValid);

                    // もっともウエイトが重いボーンのハッシュ
                    int boneId = transformIds[bw.boneIndices[0]];


                    // グリッド範囲を検索する
                    // 同じボーンを含む最も近い頂点を１つ選択する
                    var nearVertex = new ExCostSortedList1(-1);
                    var weightVertex = new ExCostSortedList1(-1);
                    foreach (int3 grid in GridMap<int>.GetArea(pos, searchRadius, gridMap, gridSize))
                    {
                        if (gridMap.ContainsKey(grid) == false)
                            continue;

                        foreach (int tindex in gridMap.GetValuesForKey(grid))
                        {
                            var tpos = proxyLocalPositions[tindex];

                            // 検索距離チェック
                            float dist = math.distance(pos, tpos);
                            if (dist > searchRadius)
                                continue;

                            // 対象頂点のウエイトにマッピング頂点のボーンが含まれているかチェックする
                            var tbw = proxyBoneWeights[tindex];
                            Debug.Assert(tbw.IsValid);
                            bool hasBone = false;
                            for (int j = 0; j < tbw.Count && hasBone == false; j++)
                            {
                                int tboneId = proxyTransformIds[tbw.boneIndices[j]];
                                if (tboneId == boneId)
                                    hasBone = true;
                            }

                            if (hasBone)
                            {
                                // 最も近い距離のみ記録する
                                weightVertex.Add(dist, tindex);
                            }
                            //else
                            {
                                // ウエイト頂点が見つからない場合は最も近い頂点を記録する
                                nearVertex.Add(dist, tindex);
                            }
                        }
                    }

#if false
                    // 同じボーンを含む近傍頂点を優先、見つからない場合は最も近い頂点にマッピング
                    var connectionVertex = weightVertex.IsValid ? weightVertex : nearVertex;
#else
                    // 同じボーンを含む近傍頂点を優先、見つからない場合は最も近い頂点にマッピング
                    // ただし距離が大きく離れる場合は近い方を優先する
                    ExCostSortedList1 connectionVertex = nearVertex;
                    if (weightVertex.IsValid && weightVertex.Cost < nearVertex.Cost * 3.0f)
                    {
                        connectionVertex = weightVertex;
                    }
#endif

                    // 検索範囲内にまったく頂点がない場合はマッピングしない
                    // つまり未接続頂点となる
                    if (connectionVertex.IsValid == false)
                    {
                        // 頂点属性を無効としてマークする
                        attributes[vindex] = VertexAttribute.Invalid;
                        continue;
                    }

                    // 見つかった近傍頂点の属性
                    var connectionAttr = proxyAttributes[connectionVertex.Data];

                    // 近傍頂点が無効ならばマッピングしない
                    if (connectionAttr.IsInvalid())
                    {
                        // 頂点属性を無効としてマークする
                        attributes[vindex] = VertexAttribute.Invalid;
                        continue;
                    }

                    // 見つかった近傍頂点を記録
                    var wdata = new MappingWorkData()
                    {
                        position = pos,
                        vertexIndex = vindex,
                        proxyVertexIndex = connectionVertex.Data,
                        proxyVertexDistance = connectionVertex.Cost,
                    };
                    mappingWorkData[vindex] = wdata;
                    attributes[vindex] = connectionAttr;
                }
            }
        }

        /// <summary>
        /// 近傍プロキシメッシュ頂点を基準に頂点ウエイトを算出する
        /// </summary>
        [BurstCompile]
        struct Mapping_CalcWeightJob : IJobParallelFor
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<MappingWorkData> mappingWorkData;

            public NativeArray<VertexAttribute> attributes;
            [Unity.Collections.WriteOnly]
            public NativeArray<VirtualMeshBoneWeight> boneWeights;

            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> proxyAttributes;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> proxyLocalPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> proxyLocalNormals;
            [Unity.Collections.ReadOnly]
            public NativeArray<uint> proxyVertexToVertexIndexArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<ushort> proxyVertexToVertexDataArray;

            public void Execute(int vindex)
            {
                // すでにInvalidなら無効
                if (attributes[vindex].IsInvalid())
                    return;

                var wdata = mappingWorkData[vindex];
                int pindex = wdata.proxyVertexIndex;

#if false
                // まず近傍頂点の平面にマッピング頂点を投影する
                // これは異なる形状のメッシュをマッピングするときに頂点が多く離れている場合への対処
                var pos = wdata.position;
                var ppos = proxyLocalPositions[pindex];
                var pnor = proxyLocalNormals[pindex];
                var v = pos - ppos;
                var vlen = math.length(v);
                if (vlen > 1e-06f)
                {
                    var pv = math.project(v, pnor);
                    //var nlen = math.dot(v, pnor) * 0.5f;
                    var nlen = math.length(pv) * 0.5f;
                    pos = ppos + v * (vlen - nlen) / vlen;
                }
                float vertexDistance = math.distance(pos, ppos);
#endif
#if true
                // まず近傍頂点の平面にマッピング頂点を投影する
                // これは異なる形状のメッシュをマッピングするときに頂点が多く離れている場合への対処
                var pos = wdata.position;
                var ppos = proxyLocalPositions[pindex];
                var pnor = proxyLocalNormals[pindex];
                var v = pos - ppos;
                pos = pos - math.project(v, pnor);
                float vertexDistance = math.distance(pos, ppos);
#endif
#if false
                float vertexDistance = wdata.proxyVertexDistance;
                var pos = wdata.position;
#endif


                // 頂点ウエイトの計算
                // 見つかった近傍頂点から接続頂点を調べて距離の昇順に４つまで選択する
                //float wradius = wdata.proxyVertexDistance * 4.0f; // 検索範囲は近傍頂点距離のｘNまで
                float wradius = vertexDistance * 4.0f; // 検索範囲は近傍頂点距離のｘNまで
                var vertexDist = new ExCostSortedList4(-1);

                // チェックは近傍頂点の接続２レベルのみとする
                // ★結果：良い。最初のバージョンと結果が同じで負荷は激減した。
                vertexDist.Add(vertexDistance, pindex);
                DataUtility.Unpack12_20(proxyVertexToVertexIndexArray[pindex], out var dcnt, out var dstart);
                // レベル１の接続
                for (int i = 0; i < dcnt; i++)
                {
                    int index2 = proxyVertexToVertexDataArray[dstart + i];
                    if (vertexDist.Contains(index2))
                        continue;

                    float3 tpos = proxyLocalPositions[index2];

                    // 直線距離
                    float dist = math.distance(pos, tpos);

                    // 距離が範囲内なら記録
                    if (dist <= wradius)
                    {
                        vertexDist.Add(dist, index2);
                    }

                    // レベル２の接続
                    DataUtility.Unpack12_20(proxyVertexToVertexIndexArray[index2], out var dcnt2, out var dstart2);
                    for (int j = 0; j < dcnt2; j++)
                    {
                        int index3 = proxyVertexToVertexDataArray[dstart2 + j];
                        if (index3 == pindex || index3 == index2)
                            continue;
                        if (vertexDist.Contains(index3))
                            continue;

                        // 距離
                        tpos = proxyLocalPositions[index3];
                        dist = math.distance(pos, tpos);

                        // 距離が範囲内なら記録
                        if (dist <= wradius)
                        {
                            vertexDist.Add(dist, index3);
                        }
                    }
                }
                Debug.Assert(vertexDist.IsValid);

                // 近傍頂点の距離をウエイトに変換する
                float4 weights = CalcVertexWeights(vertexDist.costs);
                Debug.Assert(weights[0] > 0.0f);

                // ウエイトを格納する
                var bw = new VirtualMeshBoneWeight(vertexDist.data, weights);
                boneWeights[vindex] = bw;

                // 頂点属性
                // 近傍のプロキシ頂点の属性を受け継ぐ
                //attributes[vindex] = proxyAttributes[pindex];
                // 近傍のプロキシ頂点属性にウエイトを掛けて算出する
                // 移動と固定の影響力が高い方を採用する
                float fixedValue = 0;
                float moveValue = 0;
                int wcnt = bw.Count;
                for (int i = 0; i < wcnt; i++)
                {
                    pindex = bw.boneIndices[i];
                    var attr = proxyAttributes[pindex];
                    if (attr.IsMove())
                        moveValue += bw.weights[i];
                    else if (attr.IsFixed())
                        fixedValue += bw.weights[i];
                }
                attributes[vindex] = moveValue > fixedValue ? VertexAttribute.Move : VertexAttribute.Fixed;
            }
        }

        /// <summary>
        /// 距離リストからウエイト値を算出して返す
        /// </summary>
        /// <param name="distances"></param>
        /// <returns></returns>
        static float4 CalcVertexWeights(float4 distances)
        {
            Debug.Assert(distances[0] >= 0.0f);

#if false
            // 最大距離のn%を減算する
            // 最近接のウエイトを強くするため
            const float lengthCut = 0.5f;
            float cutdist = distances[0] * lengthCut;
            distances = distances - cutdist;
#endif

            // マイナス（無効値）は０にする
            distances = math.max(distances, 0);

#if true
            // 距離をn乗する
            // 距離によるウエイトの減衰
            const float pow = 4.0f; // 2.0?
            distances = math.pow(distances, pow);
#endif

            // 最小値の逆数にする
            float min = distances[0];
            for (int i = 0; i < 4; i++)
                distances[i] = distances[i] > 0.0f ? min / distances[i] : 0.0f;

            // 単位化する(1.0)
            float sum = math.csum(distances);
            if (sum <= 0.0f)
            {
                // この時点で０ならば０距離なので[0]=100%で返す
                return new float4(1, 0, 0, 0);
            }
            distances /= sum;

            // 極小のウエイトは削除する
            const float removeWeight = 0.01f;
            for (int i = 3; i >= 1; i--)
            {
                if (distances[i] < removeWeight)
                    distances[i] = 0;
            }

            // 再度単位化(1.0)
            distances /= math.csum(distances);

            return distances;
        }
    }
}
