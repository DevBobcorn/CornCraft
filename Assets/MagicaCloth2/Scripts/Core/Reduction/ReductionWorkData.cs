// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using Unity.Collections;
using Unity.Mathematics;

namespace MagicaCloth2
{
    public class ReductionWorkData : IDisposable
    {
        public VirtualMesh vmesh;

        //=========================================================================================
        // リダクション作業データ
        //=========================================================================================
        /// <summary>
        /// 頂点の結合先インデックス(-1=生存している, 0以上=削除されている)
        /// </summary>
        public NativeArray<int> vertexJoinIndices;

        /// <summary>
        /// 頂点ごとの接続頂点インデックスリスト
        /// </summary>
        public NativeParallelMultiHashMap<ushort, ushort> vertexToVertexMap;

        //=========================================================================================
        // メッシュ最適化作業データ
        //=========================================================================================
        /// <summary>
        /// 古い頂点インデックスから新しい頂点インデックスへの変換リスト
        /// </summary>
        public NativeArray<int> vertexRemapIndices;

        /// <summary>
        /// スキニングボーンのインデックス変換辞書
        /// キー：最適化前のボーンインデックス、データ：最適化後のボーンインデックス
        /// </summary>
        public NativeParallelHashMap<int, int> useSkinBoneMap;

        /// <summary>
        /// 新しい頂点の頂点接続リスト
        /// </summary>
        public NativeParallelMultiHashMap<ushort, ushort> newVertexToVertexMap;

        public NativeParallelHashSet<int2> edgeSet;
        public NativeParallelHashSet<int3> triangleSet;


        //=========================================================================================
        // 最終的なメッシュデータ
        //=========================================================================================
        public int oldVertexCount;
        public int newVertexCount;
        public int removeVertexCount;

        public ExSimpleNativeArray<VertexAttribute> newAttributes;
        public ExSimpleNativeArray<float3> newLocalPositions;
        public ExSimpleNativeArray<float3> newLocalNormals;
        public ExSimpleNativeArray<float3> newLocalTangents;
        public ExSimpleNativeArray<float2> newUv;
        public ExSimpleNativeArray<VirtualMeshBoneWeight> newBoneWeights;

        public NativeReference<int> newSkinBoneCount;
        public NativeList<int> newSkinBoneTransformIndices;
        public NativeList<float4x4> newSkinBoneBindPoseList;

        public NativeList<int2> newLineList;
        public NativeList<int3> newTriangleList;

        //=========================================================================================
        public ReductionWorkData(VirtualMesh vmesh)
        {
            this.vmesh = vmesh;
        }

        public void Dispose()
        {
            if (vertexJoinIndices.IsCreated)
                vertexJoinIndices.Dispose();
            if (vertexToVertexMap.IsCreated)
                vertexToVertexMap.Dispose();
            if (vertexRemapIndices.IsCreated)
                vertexRemapIndices.Dispose();
            if (useSkinBoneMap.IsCreated)
                useSkinBoneMap.Dispose();
            if (newVertexToVertexMap.IsCreated)
                newVertexToVertexMap.Dispose();
            if (edgeSet.IsCreated)
                edgeSet.Dispose();
            if (triangleSet.IsCreated)
                triangleSet.Dispose();

            // 最終メッシュデータ
            newAttributes?.Dispose();
            newLocalPositions?.Dispose();
            newLocalNormals?.Dispose();
            newLocalTangents?.Dispose();
            newUv?.Dispose();
            newBoneWeights?.Dispose();
            if (newSkinBoneCount.IsCreated)
                newSkinBoneCount.Dispose();
            if (newSkinBoneTransformIndices.IsCreated)
                newSkinBoneTransformIndices.Dispose();
            if (newSkinBoneBindPoseList.IsCreated)
                newSkinBoneBindPoseList.Dispose();
            if (newLineList.IsCreated)
                newLineList.Dispose();
            if (newTriangleList.IsCreated)
                newTriangleList.Dispose();
        }

#if false

        public void DebugVerify()
        {
            // vertexToVertexMap
            //using var keyArray = vertexToVertexMap.GetKeyArray(Allocator.TempJob);
            //foreach (var key in keyArray)
            //{
            //    int cnt = vertexToVertexMap.CountValuesForKey(key);
            //    if (cnt == 0)
            //    {
            //        Develop.DebugLogWarning($"vertexToVertexMap. no data! :{key}");
            //    }
            //}
        }

        public void DebugInfo()
        {
            // vertexToVertexMap
            //using var keyArray = vertexToVertexMap.GetKeyArray(Allocator.TempJob);
            //foreach (var key in keyArray)
            //{
            //    int cnt = vertexToVertexMap.CountValuesForKey(key);
            //    Develop.DebugLog($"vertexToVertexMap [{key}] cnt:{cnt}");
            //}
        }
#endif
    }
}
