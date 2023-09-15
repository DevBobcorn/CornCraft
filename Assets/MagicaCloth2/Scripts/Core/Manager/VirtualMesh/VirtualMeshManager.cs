// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System.Text;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// VirtualMeshの管理マネージャ
    /// </summary>
    public class VirtualMeshManager : IManager, IValid
    {
        //=========================================================================================
        // ■ProxyMesh
        //=========================================================================================
        // ■共通
        /// <summary>
        /// 対応するチームID
        /// </summary>
        public ExNativeArray<short> teamIds;

        /// <summary>
        /// 頂点属性
        /// </summary>
        public ExNativeArray<VertexAttribute> attributes;

        /// <summary>
        /// 頂点ごとの接続トライアングルインデックスと法線接線フリップフラグ（最大７つ）
        /// これは法線を再計算するために用いられるもので７つあれば十分であると判断したもの。
        /// そのため正確なトライアングル接続を表していない。
        /// データは12-20bitのuintでパックされている
        /// 12(hi) = 法線接線のフリップフラグ(法線:0x1,接線:0x2)。ONの場合フリップ。
        /// 20(low) = トライアングルインデックス。
        /// </summary>
        public ExNativeArray<FixedList32Bytes<uint>> vertexToTriangles;

        /// <summary>
        /// 頂点ごとの接続頂点インデックス
        /// ※現在未使用
        /// </summary>
        //public ExNativeArray<uint> vertexToVertexIndexArray;
        //public ExNativeArray<ushort> vertexToVertexDataArray;

        /// <summary>
        /// 頂点ごとのバインドポーズ
        /// 頂点バインドにはスケール値は不要
        /// </summary>
        public ExNativeArray<float3> vertexBindPosePositions;
        public ExNativeArray<quaternion> vertexBindPoseRotations;

        /// <summary>
        /// 各頂点の深さ(0.0-1.0)
        /// </summary>
        public ExNativeArray<float> vertexDepths;

        /// <summary>
        /// 各頂点のルートインデックス(-1=なし)
        /// </summary>
        public ExNativeArray<int> vertexRootIndices;

        /// <summary>
        /// 各頂点の親からの基準ローカル座標
        /// </summary>
        public ExNativeArray<float3> vertexLocalPositions;

        /// <summary>
        /// 各頂点の親からの基準ローカル回転
        /// </summary>
        public ExNativeArray<quaternion> vertexLocalRotations;

        /// <summary>
        /// 各頂点の親頂点インデックス(-1=なし)
        /// </summary>
        public ExNativeArray<int> vertexParentIndices;

        /// <summary>
        /// 各頂点の子頂点インデックスリスト
        /// </summary>
        public ExNativeArray<uint> vertexChildIndexArray;
        public ExNativeArray<ushort> vertexChildDataArray;

        /// <summary>
        /// 法線調整用回転
        /// </summary>
        public ExNativeArray<quaternion> normalAdjustmentRotations;

        /// <summary>
        /// 各頂点の角度計算用のローカル回転
        /// pitch/yaw個別制限はv1.0では実装しないので一旦停止させる
        /// </summary>
        //public ExNativeArray<quaternion> vertexAngleCalcLocalRotations;

        /// <summary>
        /// UV
        /// VirtualMeshのUVはTangent計算用でありテクスチャマッピング用ではないので注意！
        /// </summary>
        public ExNativeArray<float2> uv;


        public int VertexCount => teamIds?.Count ?? 0;

        // ■トライアングル -----------------------------------------------------
        public ExNativeArray<short> triangleTeamIdArray;

        /// <summary>
        /// トライアングル頂点インデックス
        /// </summary>
        public ExNativeArray<int3> triangles;

        /// <summary>
        /// トライアングル法線
        /// </summary>
        public ExNativeArray<float3> triangleNormals;

        /// <summary>
        /// トライアングル接線
        /// </summary>
        public ExNativeArray<float3> triangleTangents;

        public int TriangleCount => triangles?.Count ?? 0;

        // ■エッジ -------------------------------------------------------------
        public ExNativeArray<short> edgeTeamIdArray;

        /// <summary>
        /// エッジ頂点インデックス
        /// </summary>
        public ExNativeArray<int2> edges;

        /// <summary>
        /// エッジ固有フラグ(VirtualMesh.EdgeFlag_~)
        /// </summary>
        public ExNativeArray<ExBitFlag8> edgeFlags;

        public int EdgeCount => edges?.Count ?? 0;

        // ■ベースライン -------------------------------------------------------
        /// <summary>
        /// ベースラインごとのフラグ
        /// </summary>
        public ExNativeArray<ExBitFlag8> baseLineFlags;

        /// <summary>
        /// ベースラインごとのチームID
        /// </summary>
        public ExNativeArray<short> baseLineTeamIds;

        /// <summary>
        /// ベースラインごとのデータ開始インデックス
        /// </summary>
        public ExNativeArray<ushort> baseLineStartDataIndices;

        /// <summary>
        /// ベースラインごとのデータ数
        /// </summary>
        public ExNativeArray<ushort> baseLineDataCounts;

        /// <summary>
        /// ベースラインデータ（頂点インデックス）
        /// </summary>
        public ExNativeArray<ushort> baseLineData;

        public int BaseLineCount => baseLineFlags?.Count ?? 0;

        // ■メッシュ基本(共通) -------------------------------------------------
        public ExNativeArray<float3> localPositions;
        public ExNativeArray<float3> localNormals;
        public ExNativeArray<float3> localTangents;
        public ExNativeArray<VirtualMeshBoneWeight> boneWeights;
        public ExNativeArray<int> skinBoneTransformIndices;
        public ExNativeArray<float4x4> skinBoneBindPoses;

        // ■MeshClothのみ -----------------------------------------------------
        public int MeshClothVertexCount => localPositions?.Count ?? 0;

        // ■BoneClothのみ -----------------------------------------------------
        public ExNativeArray<quaternion> vertexToTransformRotations;

        // ■最終頂点姿勢
        public ExNativeArray<float3> positions;
        public ExNativeArray<quaternion> rotations;

        //=========================================================================================
        // ■MappingMesh
        //=========================================================================================
        public ExNativeArray<short> mappingIdArray; // (+1)されているので注意！
        public ExNativeArray<int> mappingReferenceIndices;
        public ExNativeArray<VertexAttribute> mappingAttributes;
        public ExNativeArray<float3> mappingLocalPositins;
        public ExNativeArray<float3> mappingLocalNormals;
        //public ExNativeArray<float3> mappingLocalTangents;
        public ExNativeArray<VirtualMeshBoneWeight> mappingBoneWeights;
        public ExNativeArray<float3> mappingPositions;
        public ExNativeArray<float3> mappingNormals;


        public int MappingVertexCount => mappingIdArray?.Count ?? 0;


        //=========================================================================================
        bool isValid = false;

        //=========================================================================================
        public void Dispose()
        {
            isValid = false;

            // 作業バッファ
            teamIds?.Dispose();
            attributes?.Dispose();
            vertexToTriangles?.Dispose();
            //vertexToVertexIndexArray?.Dispose();
            //vertexToVertexDataArray?.Dispose();
            vertexBindPosePositions?.Dispose();
            vertexBindPoseRotations?.Dispose();
            vertexDepths?.Dispose();
            vertexRootIndices?.Dispose();
            vertexLocalPositions?.Dispose();
            vertexLocalRotations?.Dispose();
            vertexParentIndices?.Dispose();
            vertexChildIndexArray?.Dispose();
            vertexChildDataArray?.Dispose();
            normalAdjustmentRotations?.Dispose();
            //vertexAngleCalcLocalRotations?.Dispose();
            uv?.Dispose();
            teamIds = null;
            attributes = null;
            vertexToTriangles = null;
            //vertexToVertexIndexArray = null;
            //vertexToVertexDataArray = null;
            vertexBindPosePositions = null;
            vertexBindPoseRotations = null;
            vertexDepths = null;
            vertexRootIndices = null;
            vertexLocalPositions = null;
            vertexLocalRotations = null;
            vertexParentIndices = null;
            vertexChildIndexArray = null;
            vertexChildDataArray = null;
            normalAdjustmentRotations = null;
            //vertexAngleCalcLocalRotations = null;
            uv = null;

            triangleTeamIdArray?.Dispose();
            triangles?.Dispose();
            triangleNormals?.Dispose();
            triangleTangents?.Dispose();
            triangleTeamIdArray = null;
            triangles = null;
            triangleNormals = null;
            triangleTangents = null;

            edgeTeamIdArray?.Dispose();
            edges?.Dispose();
            edgeFlags?.Dispose();
            edgeTeamIdArray = null;
            edges = null;
            edgeFlags = null;

            positions?.Dispose();
            rotations?.Dispose();
            positions = null;
            rotations = null;

            baseLineFlags?.Dispose();
            baseLineTeamIds?.Dispose();
            baseLineStartDataIndices?.Dispose();
            baseLineDataCounts?.Dispose();
            baseLineData?.Dispose();
            baseLineFlags = null;
            baseLineTeamIds = null;
            baseLineStartDataIndices = null;
            baseLineDataCounts = null;
            baseLineData = null;

            localPositions?.Dispose();
            localNormals?.Dispose();
            localTangents?.Dispose();
            boneWeights?.Dispose();
            skinBoneTransformIndices?.Dispose();
            skinBoneBindPoses?.Dispose();
            localPositions = null;
            localNormals = null;
            localTangents = null;
            boneWeights = null;
            skinBoneTransformIndices = null;
            skinBoneBindPoses = null;

            vertexToTransformRotations?.Dispose();
            vertexToTransformRotations = null;

            mappingIdArray?.Dispose();
            mappingReferenceIndices?.Dispose();
            mappingAttributes?.Dispose();
            mappingLocalPositins?.Dispose();
            mappingLocalNormals?.Dispose();
            //mappingLocalTangents?.Dispose();
            mappingBoneWeights?.Dispose();
            mappingPositions?.Dispose();
            mappingNormals?.Dispose();
            mappingIdArray = null;
            mappingReferenceIndices = null;
            mappingAttributes = null;
            mappingLocalPositins = null;
            mappingLocalNormals = null;
            //mappingLocalTangents = null;
            mappingBoneWeights = null;
            mappingPositions = null;
            mappingNormals = null;
        }

        public void EnterdEditMode()
        {
            Dispose();
        }

        public void Initialize()
        {
            Dispose();

            // 作業バッファ
            const int capacity = 0;
            const bool create = true;
            teamIds = new ExNativeArray<short>(capacity, create);
            attributes = new ExNativeArray<VertexAttribute>(capacity, create);
            vertexToTriangles = new ExNativeArray<FixedList32Bytes<uint>>(capacity, create);
            //vertexToVertexIndexArray = new ExNativeArray<uint>(capacity, create);
            //vertexToVertexDataArray = new ExNativeArray<ushort>(capacity, create);
            vertexBindPosePositions = new ExNativeArray<float3>(capacity, create);
            vertexBindPoseRotations = new ExNativeArray<quaternion>(capacity, create);
            vertexDepths = new ExNativeArray<float>(capacity, create);
            vertexRootIndices = new ExNativeArray<int>(capacity, create);
            vertexLocalPositions = new ExNativeArray<float3>(capacity, create);
            vertexLocalRotations = new ExNativeArray<quaternion>(capacity, create);
            vertexParentIndices = new ExNativeArray<int>(capacity, create);
            vertexChildIndexArray = new ExNativeArray<uint>(capacity, create);
            vertexChildDataArray = new ExNativeArray<ushort>(capacity, create);
            normalAdjustmentRotations = new ExNativeArray<quaternion>(capacity, create);
            //vertexAngleCalcLocalRotations = new ExNativeArray<quaternion>(capacity, create);
            uv = new ExNativeArray<float2>(capacity, create);

            triangleTeamIdArray = new ExNativeArray<short>(capacity, create);
            triangles = new ExNativeArray<int3>(capacity, create);
            triangleNormals = new ExNativeArray<float3>(capacity, create);
            triangleTangents = new ExNativeArray<float3>(capacity, create);

            edgeTeamIdArray = new ExNativeArray<short>(capacity, create);
            edges = new ExNativeArray<int2>(capacity, create);
            edgeFlags = new ExNativeArray<ExBitFlag8>(capacity, create);

            positions = new ExNativeArray<float3>(capacity, create);
            rotations = new ExNativeArray<quaternion>(capacity, create);

            baseLineFlags = new ExNativeArray<ExBitFlag8>(capacity, create);
            baseLineTeamIds = new ExNativeArray<short>(capacity, create);
            baseLineStartDataIndices = new ExNativeArray<ushort>(capacity, create);
            baseLineDataCounts = new ExNativeArray<ushort>(capacity, create);
            baseLineData = new ExNativeArray<ushort>(capacity, create);

            localPositions = new ExNativeArray<float3>(capacity, create);
            localNormals = new ExNativeArray<float3>(capacity, create);
            localTangents = new ExNativeArray<float3>(capacity, create);
            boneWeights = new ExNativeArray<VirtualMeshBoneWeight>(capacity, create);
            skinBoneTransformIndices = new ExNativeArray<int>(capacity, create);
            skinBoneBindPoses = new ExNativeArray<float4x4>(capacity, create);

            vertexToTransformRotations = new ExNativeArray<quaternion>(capacity, create);

            mappingIdArray = new ExNativeArray<short>(capacity, create);
            mappingReferenceIndices = new ExNativeArray<int>(capacity, create);
            mappingAttributes = new ExNativeArray<VertexAttribute>(capacity, create);
            mappingLocalPositins = new ExNativeArray<float3>(capacity, create);
            mappingLocalNormals = new ExNativeArray<float3>(capacity, create);
            //mappingLocalTangents = new ExNativeArray<float3>(capacity, create);
            mappingBoneWeights = new ExNativeArray<VirtualMeshBoneWeight>(capacity, create);
            mappingPositions = new ExNativeArray<float3>(capacity, create);
            mappingNormals = new ExNativeArray<float3>(capacity, create);

            isValid = true;
        }

        public bool IsValid()
        {
            return isValid;
        }

        //=========================================================================================
        /// <summary>
        /// プロキシメッシュをマネージャに登録する
        /// </summary>
        public void RegisterProxyMesh(int teamId, VirtualMesh proxyMesh)
        {
            if (isValid == false)
                return;

            ref var tdata = ref MagicaManager.Team.GetTeamDataRef(teamId);

            // mesh type
            tdata.proxyMeshType = proxyMesh.meshType;

            // Transform
            tdata.proxyTransformChunk = MagicaManager.Bone.AddTransform(proxyMesh.transformData, teamId);

            // center transform
            tdata.centerTransformIndex = proxyMesh.centerTransformIndex + tdata.proxyTransformChunk.startIndex;

            // 作業用バッファ
            // 共通
            int vcnt = proxyMesh.VertexCount;
            tdata.proxyCommonChunk = teamIds.AddRange(vcnt, (short)teamId);
            attributes.AddRange(proxyMesh.attributes);
            vertexToTriangles.AddRange(proxyMesh.vertexToTriangles);
            //vertexToVertexIndexArray.AddRange(proxyMesh.vertexToVertexIndexArray);
            vertexBindPosePositions.AddRange(proxyMesh.vertexBindPosePositions);
            vertexBindPoseRotations.AddRange(proxyMesh.vertexBindPoseRotations);
            vertexDepths.AddRange(proxyMesh.vertexDepths);
            vertexRootIndices.AddRange(proxyMesh.vertexRootIndices);
            vertexLocalPositions.AddRange(proxyMesh.vertexLocalPositions);
            vertexLocalRotations.AddRange(proxyMesh.vertexLocalRotations);
            vertexParentIndices.AddRange(proxyMesh.vertexParentIndices);
            vertexChildIndexArray.AddRange(proxyMesh.vertexChildIndexArray);
            normalAdjustmentRotations.AddRange(proxyMesh.normalAdjustmentRotations);
            //vertexAngleCalcLocalRotations.AddRange(proxyMesh.vertexAngleCalcLocalRotations);
            uv.AddRange(proxyMesh.uv);
            positions.AddRange(vcnt);
            rotations.AddRange(vcnt);

            // 頂点接続データ
            //tdata.proxyVertexToVertexDataChunk = vertexToVertexDataArray.AddRange(proxyMesh.vertexToVertexDataArray);

            // 子頂点データ
            tdata.proxyVertexChildDataChunk = vertexChildDataArray.AddRange(proxyMesh.vertexChildDataArray);

            // トライアングル
            if (proxyMesh.TriangleCount > 0)
            {
                tdata.proxyTriangleChunk = triangleTeamIdArray.AddRange(proxyMesh.TriangleCount, (short)teamId);
                triangles.AddRange(proxyMesh.triangles);
                triangleNormals.AddRange(proxyMesh.TriangleCount);
                triangleTangents.AddRange(proxyMesh.TriangleCount);
            }

            // エッジ（エッジは利用時のみ）
            if (proxyMesh.EdgeCount > 0)
            {
                tdata.proxyEdgeChunk = edgeTeamIdArray.AddRange(proxyMesh.EdgeCount, (short)teamId);
                edges.AddRange(proxyMesh.edges);
                edgeFlags.AddRange(proxyMesh.edgeFlags);
            }

            // ベースライン
            if (proxyMesh.BaseLineCount > 0)
            {
                tdata.baseLineChunk = baseLineFlags.AddRange(proxyMesh.baseLineFlags);
                baseLineStartDataIndices.AddRange(proxyMesh.baseLineStartDataIndices);
                baseLineDataCounts.AddRange(proxyMesh.baseLineDataCounts);
                baseLineTeamIds.AddRange(proxyMesh.BaseLineCount, (short)teamId);

                tdata.baseLineDataChunk = baseLineData.AddRange(proxyMesh.baseLineData);
            }

            // メッシュ基本
            tdata.proxyMeshChunk = localPositions.AddRange(proxyMesh.localPositions);
            localNormals.AddRange(proxyMesh.localNormals);
            localTangents.AddRange(proxyMesh.localTangents);
            boneWeights.AddRange(proxyMesh.boneWeights);

            // スキニング
            tdata.proxySkinBoneChunk = skinBoneTransformIndices.AddRange(proxyMesh.skinBoneTransformIndices);
            skinBoneBindPoses.AddRange(proxyMesh.skinBoneBindPoses);

            // BoneClothのみ
            if (proxyMesh.meshType == VirtualMesh.MeshType.ProxyBoneMesh)
            {
                tdata.proxyBoneChunk = vertexToTransformRotations.AddRange(proxyMesh.vertexToTransformRotations);
            }
        }

        /// <summary>
        /// プロキシメッシュをマネージャから解除する
        /// </summary>
        public void ExitProxyMesh(int teamId)
        {
            if (isValid == false)
                return;

            ref var tdata = ref MagicaManager.Team.GetTeamDataRef(teamId);

            // Transform
            MagicaManager.Bone.RemoveTransform(tdata.proxyTransformChunk);
            tdata.proxyTransformChunk.Clear();

            // 作業用バッファ
            teamIds.RemoveAndFill(tdata.proxyCommonChunk); // 0で埋める
            attributes.RemoveAndFill(tdata.proxyCommonChunk); // 0で埋める
            vertexToTriangles.Remove(tdata.proxyCommonChunk);
            //vertexToVertexIndexArray.Remove(tdata.proxyCommonChunk);
            vertexBindPosePositions.Remove(tdata.proxyCommonChunk);
            vertexBindPoseRotations.Remove(tdata.proxyCommonChunk);
            vertexDepths.Remove(tdata.proxyCommonChunk);
            vertexRootIndices.Remove(tdata.proxyCommonChunk);
            vertexLocalPositions.Remove(tdata.proxyCommonChunk);
            vertexLocalRotations.Remove(tdata.proxyCommonChunk);
            vertexParentIndices.Remove(tdata.proxyCommonChunk);
            vertexChildIndexArray.Remove(tdata.proxyCommonChunk);
            normalAdjustmentRotations.Remove(tdata.proxyCommonChunk);
            //vertexAngleCalcLocalRotations.Remove(tdata.proxyCommonChunk);
            uv.Remove(tdata.proxyCommonChunk);
            positions.Remove(tdata.proxyCommonChunk);
            rotations.Remove(tdata.proxyCommonChunk);
            tdata.proxyCommonChunk.Clear();

            //vertexToVertexDataArray.Remove(tdata.proxyVertexToVertexDataChunk);
            //tdata.proxyVertexToVertexDataChunk.Clear();

            vertexChildDataArray.Remove(tdata.proxyVertexChildDataChunk);
            tdata.proxyVertexChildDataChunk.Clear();

            triangleTeamIdArray.RemoveAndFill(tdata.proxyTriangleChunk, 0); // 0で埋める
            triangles.Remove(tdata.proxyTriangleChunk);
            triangleNormals.Remove(tdata.proxyTriangleChunk);
            triangleTangents.Remove(tdata.proxyTriangleChunk);
            tdata.proxyTriangleChunk.Clear();

            edgeTeamIdArray.RemoveAndFill(tdata.proxyEdgeChunk, 0); // 0で埋める
            edges.Remove(tdata.proxyEdgeChunk);
            edgeFlags.Remove(tdata.proxyEdgeChunk);

            baseLineFlags.RemoveAndFill(tdata.baseLineChunk); // 0で埋める
            baseLineTeamIds.Remove(tdata.baseLineChunk);
            baseLineStartDataIndices.Remove(tdata.baseLineChunk);
            baseLineDataCounts.Remove(tdata.baseLineChunk);
            tdata.baseLineChunk.Clear();

            baseLineData.Remove(tdata.baseLineDataChunk);
            tdata.baseLineDataChunk.Clear();

            localPositions.Remove(tdata.proxyMeshChunk);
            localNormals.Remove(tdata.proxyMeshChunk);
            localTangents.Remove(tdata.proxyMeshChunk);
            boneWeights.Remove(tdata.proxyMeshChunk);
            tdata.proxyMeshChunk.Clear();

            skinBoneTransformIndices.Remove(tdata.proxySkinBoneChunk);
            skinBoneBindPoses.Remove(tdata.proxySkinBoneChunk);
            tdata.proxySkinBoneChunk.Clear();

            vertexToTransformRotations.Remove(tdata.proxyBoneChunk);
            tdata.proxyBoneChunk.Clear();

            // 同時に連動するマッピングメッシュも解放する
            var mappingIndices = tdata.mappingDataIndexSet.ToArray();
            int mcnt = tdata.mappingDataIndexSet.Length;
            for (int i = 0; i < mcnt; i++)
            {
                int mappingIndex = mappingIndices[i];
                ExitMappingMesh(teamId, mappingIndex);
            }
        }

        //=========================================================================================
        /// <summary>
        /// マッピングメッシュをマネージャに登録する（チームにも登録される）
        /// </summary>
        /// <param name="cbase"></param>
        /// <param name="mappingMesh"></param>
        /// <returns></returns>
        public DataChunk RegisterMappingMesh(int teamId, VirtualMesh mappingMesh)
        {
            if (isValid == false)
                return DataChunk.Empty;

            ref var tdata = ref MagicaManager.Team.GetTeamDataRef(teamId);

            var mdata = new TeamManager.MappingData();

            mdata.teamId = teamId;

            // transform
            var ct = mappingMesh.GetCenterTransform();
            var c = MagicaManager.Bone.AddTransform(ct, new ExBitFlag8(TransformManager.Flag_Read | TransformManager.Flag_Enable), teamId);
            mdata.centerTransformIndex = c.startIndex;

            // プロキシメッシュへの変換
            mdata.toProxyMatrix = mappingMesh.toProxyMatrix;
            mdata.toProxyRotation = mappingMesh.toProxyRotation;

            // 登録インデックスが必要なので先に登録する
            c = MagicaManager.Team.mappingDataArray.Add(mdata);
            int mappingIndex = c.startIndex;

            // 基本データ
            int vcnt = mappingMesh.VertexCount;
            mdata.mappingCommonChunk = mappingIdArray.AddRange(vcnt, (short)(mappingIndex + 1)); // (+1)されるので注意！

            mappingReferenceIndices.AddRange(mappingMesh.referenceIndices);
            mappingAttributes.AddRange(mappingMesh.attributes);
            mappingLocalPositins.AddRange(mappingMesh.localPositions);
            mappingLocalNormals.AddRange(mappingMesh.localNormals);
            //mappingLocalTangents.AddRange(mappingMesh.localTangents);
            mappingBoneWeights.AddRange(mappingMesh.boneWeights);
            mappingPositions.AddRange(vcnt);
            mappingNormals.AddRange(vcnt);

            // 再登録
            MagicaManager.Team.mappingDataArray[mappingIndex] = mdata;
            tdata.mappingDataIndexSet.Set((short)mappingIndex);

            // vmeshにも記録する
            mappingMesh.mappingId = mappingIndex;

            Develop.DebugLog($"RegisterMappingMesh. team:{teamId}, mappingIndex:{mappingIndex}");

            // マッピングメッシュの登録チャンクを返す
            return mdata.mappingCommonChunk;
        }

        public void ExitMappingMesh(int teamId, int mappingIndex)
        {
            if (isValid == false)
                return;

            ref var tdata = ref MagicaManager.Team.GetTeamDataRef(teamId);
            ref var mdata = ref MagicaManager.Team.mappingDataArray.GetRef(mappingIndex);

            // Transform解放
            MagicaManager.Bone.RemoveTransform(new DataChunk(mdata.centerTransformIndex, 1));

            // 作業バッファ解放
            mappingIdArray.RemoveAndFill(mdata.mappingCommonChunk, 0);
            mappingReferenceIndices.Remove(mdata.mappingCommonChunk);
            mappingAttributes.Remove(mdata.mappingCommonChunk);
            mappingLocalPositins.Remove(mdata.mappingCommonChunk);
            mappingLocalNormals.Remove(mdata.mappingCommonChunk);
            //mappingLocalTangents.Remove(mdata.mappingCommonChunk);
            mappingBoneWeights.Remove(mdata.mappingCommonChunk);
            mappingPositions.Remove(mdata.mappingCommonChunk);
            mappingNormals.Remove(mdata.mappingCommonChunk);

            // チームから削除する
            tdata.mappingDataIndexSet.RemoveItemAtSwapBack((short)mappingIndex);

            MagicaManager.Team.mappingDataArray.RemoveAndFill(new DataChunk(mappingIndex, 1));

            Develop.DebugLog($"ExitMappingMesh. team:{teamId}, mappingIndex:{mappingIndex}");
        }

        //=========================================================================================
        /// <summary>
        /// ProxyMeshの現在の姿勢を計算する
        /// </summary>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        unsafe internal JobHandle PreProxyMeshUpdate(JobHandle jobHandle)
        {
            if (VertexCount == 0)
                return jobHandle;

            var sm = MagicaManager.Simulation;
            var tm = MagicaManager.Team;
            var bm = MagicaManager.Bone;

            // 作業バッファ
            int bufferCount = math.max(VertexCount, TriangleCount);
            sm.processingStepParticle.UpdateBuffer(bufferCount);
            sm.processingStepTriangleBending.UpdateBuffer(bufferCount); // mesh cloth (skinning)
            sm.processingStepEdgeCollision.UpdateBuffer(BaseLineCount);
            sm.processingStepMotionParticle.UpdateBuffer(TriangleCount); // triangle

            // バッファクリア
            var clearJob = new ClearProxyMeshUpdateBufferJob()
            {
                processingCounter0 = sm.processingStepParticle.Counter,
                processingCounter1 = sm.processingStepTriangleBending.Counter,
                processingCounter2 = sm.processingStepEdgeCollision.Counter,
                processingCounter3 = sm.processingStepMotionParticle.Counter,
            };
            jobHandle = clearJob.Schedule(jobHandle);

            // [BoneCloht(1)][MeshCloth(2)]それぞれの処理頂点インデックスリストを作成する
            var job1 = new CreateProxyMeshUpdateVertexList()
            {
                teamDataArray = tm.teamDataArray.GetNativeArray(),

                processingCounter1 = sm.processingStepTriangleBending.Counter,
                processingList1 = sm.processingStepTriangleBending.Buffer,
            };
            jobHandle = job1.Schedule(tm.TeamCount, 1, jobHandle);

#if false
            // [BoneCloth] Transform姿勢を頂点姿勢として取り込む
            var calcBoneClothJob = new CalcTransformDirectJob()
            {
                jobVertexIndexList = sm.processingIntList0.Buffer,

                teamDataArray = tm.teamDataArray.GetNativeArray(),

                transformPositions = bm.positionArray.GetNativeArray(),
                transformRotations = bm.rotationArray.GetNativeArray(),

                teamIds = teamIds.GetNativeArray(),
                positions = positions.GetNativeArray(),
                rotations = rotations.GetNativeArray(),
            };
            jobHandle = calcBoneClothJob.Schedule(sm.processingIntList0.GetJobSchedulePtr(), 8, jobHandle);
#endif

            // [MeshCloth] ProxyMeshをスキニングして頂点姿勢を求める
            var calcMeshClothJob = new CalcTransformOnlySkinningJob()
            {
                jobVertexIndexList = sm.processingStepTriangleBending.Buffer,

                teamDataArray = tm.teamDataArray.GetNativeArray(),

                teamIds = teamIds.GetNativeArray(),
                attributes = attributes.GetNativeArray(),
                localPositions = localPositions.GetNativeArray(),
                localNormals = localNormals.GetNativeArray(),
                localTangents = localTangents.GetNativeArray(),
                boneWeights = boneWeights.GetNativeArray(),
                skinBoneTransformIndices = skinBoneTransformIndices.GetNativeArray(),
                skinBoneBindPoses = skinBoneBindPoses.GetNativeArray(),
                positions = positions.GetNativeArray(),
                rotations = rotations.GetNativeArray(),

                transformPositionArray = bm.positionArray.GetNativeArray(),
                transformRotationArray = bm.rotationArray.GetNativeArray(),
                transformScaleArray = bm.scaleArray.GetNativeArray(),
            };
            jobHandle = calcMeshClothJob.Schedule(sm.processingStepTriangleBending.GetJobSchedulePtr(), 32, jobHandle);

            return jobHandle;
        }

        [BurstCompile]
        struct ClearProxyMeshUpdateBufferJob : IJob
        {
            public NativeReference<int> processingCounter0;
            public NativeReference<int> processingCounter1;
            public NativeReference<int> processingCounter2;
            public NativeReference<int> processingCounter3;

            public void Execute()
            {
                processingCounter0.Value = 0;
                processingCounter1.Value = 0;
                processingCounter2.Value = 0;
                processingCounter3.Value = 0;
            }
        }

        [BurstCompile]
        struct CreateProxyMeshUpdateVertexList : IJobParallelFor
        {
            public NativeArray<TeamManager.TeamData> teamDataArray;

            // bone (direct transform)
            //[NativeDisableParallelForRestriction]
            //[Unity.Collections.WriteOnly]
            //public NativeReference<int> processingCounter0;
            //[NativeDisableParallelForRestriction]
            //[Unity.Collections.WriteOnly]
            //public NativeArray<int> processingList0; // bone

            // mesh (skinning)
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeReference<int> processingCounter1;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<int> processingList1; // mesh

            public void Execute(int teamId)
            {
                // [0]はグローバルチームなのでスキップ
                if (teamId == 0)
                    return;

                var tdata = teamDataArray[teamId];
                if (tdata.IsEnable == false)
                    return;
                if (tdata.IsCullingInvisible)
                    return;

                // 頂点リストに追加
                var c = tdata.proxyCommonChunk;
                if (c.dataLength == 0)
                    return;

                // BoneClothもMeshClothもすべてスキニングとして登録
                int start = processingCounter1.InterlockedStartIndex(c.dataLength);
                for (int j = 0; j < c.dataLength; j++)
                {
                    int vindex = c.startIndex + j;
                    processingList1[start + j] = vindex;
                }

#if false
                if (tdata.proxyMeshType == VirtualMesh.MeshType.ProxyMesh || tdata.flag.IsSet(TeamManager.Flag_CustomSkinning))
                {
                    int start = processingCounter1.InterlockedStartIndex(c.dataLength);
                    for (int j = 0; j < c.dataLength; j++)
                    {
                        int vindex = c.startIndex + j;
                        processingList1[start + j] = vindex;
                    }
                }
                if (tdata.proxyMeshType == VirtualMesh.MeshType.ProxyBoneMesh)
                {
                    int start = processingCounter0.InterlockedStartIndex(c.dataLength);
                    for (int j = 0; j < c.dataLength; j++)
                    {
                        int vindex = c.startIndex + j;
                        processingList0[start + j] = vindex;
                    }
                }
#endif

                //Debug.Log($"Vmesh Bone:{jobVertexIndexList1.Length}, Mesh:{jobVertexIndexList2.Length}");
            }
        }

#if false
        /// <summary>
        /// Transformから対応する頂点に姿勢をコピーする
        /// </summary>
        [BurstCompile]
        struct CalcTransformDirectJob : IJobParallelForDefer
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<int> jobVertexIndexList;

            // team
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;

            // transform
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> transformPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> transformRotations;

            // vmesh
            [Unity.Collections.ReadOnly]
            public NativeArray<short> teamIds;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> positions;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<quaternion> rotations;

            public void Execute(int index)
            {
                int vindex = jobVertexIndexList[index];
                int teamId = teamIds[vindex];
                var tdata = teamDataArray[teamId];
                int l_index = vindex - tdata.proxyCommonChunk.startIndex;

                // 現在のトランスフォームワールド姿勢
                int tindex = tdata.proxyTransformChunk.startIndex + l_index;
                var wpos = transformPositions[tindex];
                quaternion wrot = transformRotations[tindex];

                positions[vindex] = wpos;
                rotations[vindex] = wrot;
            }
        }
#endif

        /// <summary>
        /// 頂点スキニングを行い座標・法線・接線を求める
        /// 姿勢はワールド座標で格納される
        /// </summary>
        [BurstCompile]
        struct CalcTransformOnlySkinningJob : IJobParallelForDefer
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<int> jobVertexIndexList;

            // team
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;

            // vmesh
            [Unity.Collections.ReadOnly]
            public NativeArray<short> teamIds;
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> attributes;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localNormals;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localTangents;
            [Unity.Collections.ReadOnly]
            public NativeArray<VirtualMeshBoneWeight> boneWeights;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> skinBoneTransformIndices;
            [Unity.Collections.ReadOnly]
            public NativeArray<float4x4> skinBoneBindPoses;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> positions;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<quaternion> rotations;

            // transform
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> transformPositionArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> transformRotationArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> transformScaleArray;

            public void Execute(int index)
            {
                int vindex = jobVertexIndexList[index];
                int teamId = teamIds[vindex];
                var tdata = teamDataArray[teamId];
                int l_index = vindex - tdata.proxyCommonChunk.startIndex;

                int mvindex = tdata.proxyMeshChunk.startIndex + l_index;
                int sb_start = tdata.proxySkinBoneChunk.startIndex;
                int t_start = tdata.proxyTransformChunk.startIndex;

                var bw = boneWeights[mvindex];
                int wcnt = bw.Count;
                float3 wpos = 0;
                float3 wnor = 0;
                float3 wtan = 0;
                for (int i = 0; i < wcnt; i++)
                {
                    float w = bw.weights[i];

                    int l_boneIndex = bw.boneIndices[i];
                    float4x4 bp = skinBoneBindPoses[sb_start + l_boneIndex];
                    float4 lpos = new float4(localPositions[mvindex], 1);
                    float4 lnor = new float4(localNormals[mvindex], 0);
                    float4 ltan = new float4(localTangents[mvindex], 0);

                    float3 pos = math.mul(bp, lpos).xyz;
                    float3 nor = math.mul(bp, lnor).xyz;
                    float3 tan = math.mul(bp, ltan).xyz;

                    int tindex = skinBoneTransformIndices[sb_start + l_boneIndex] + t_start;
                    var tpos = transformPositionArray[tindex];
                    var trot = transformRotationArray[tindex];
                    var tscl = transformScaleArray[tindex];
                    MathUtility.TransformPositionNormalTangent(tpos, trot, tscl, ref pos, ref nor, ref tan);

                    wpos += pos * w;
                    wnor += nor * w;
                    wtan += tan * w;
                }

                // バインドポーズにスケールが入るので単位化する必要がある
#if MC2_DEBUG
                Develop.Assert(math.length(wnor) > 0.0f);
                Develop.Assert(math.length(wtan) > 0.0f);
#endif
                wnor = math.normalize(wnor);
                wtan = math.normalize(wtan);
                var wrot = MathUtility.ToRotation(wnor, wtan);

                positions[vindex] = wpos;
                rotations[vindex] = wrot;
            }
        }

        //=========================================================================================
        /// <summary>
        /// クロスシミュレーションの結果をProxyMeshへ反映させる
        /// </summary>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        internal unsafe JobHandle PostProxyMeshUpdate(JobHandle jobHandle)
        {
            if (VertexCount == 0)
                return jobHandle;

            var sm = MagicaManager.Simulation;
            var tm = MagicaManager.Team;
            var bm = MagicaManager.Bone;

            // バッファクリア
            var clearJob = new ClearProxyMeshUpdateBufferJob()
            {
                processingCounter0 = sm.processingStepParticle.Counter,
                processingCounter1 = sm.processingStepTriangleBending.Counter,
                processingCounter2 = sm.processingStepEdgeCollision.Counter,
                processingCounter3 = sm.processingStepMotionParticle.Counter,
            };
            jobHandle = clearJob.Schedule(jobHandle);

            // 今回更新が必要な各インデックスリストを作成する
            var createUpdateListJob = new CreatePostProxyMeshUpdateListJob()
            {
                teamDataArray = tm.teamDataArray.GetNativeArray(),

                // Triangle Vertex
                processingCounter0 = sm.processingStepParticle.Counter,
                processingList0 = sm.processingStepParticle.Buffer,

                // Transform Vertex
                processingCounter1 = sm.processingStepTriangleBending.Counter,
                processingList1 = sm.processingStepTriangleBending.Buffer,

                // Base line
                processingCounter2 = sm.processingStepEdgeCollision.Counter,
                processingList2 = sm.processingStepEdgeCollision.Buffer,

                // Triangle
                processingCounter3 = sm.processingStepMotionParticle.Counter,
                processingList3 = sm.processingStepMotionParticle.Buffer,
            };
            jobHandle = createUpdateListJob.Schedule(tm.TeamCount, 1, jobHandle);

            // ラインがある場合はベースラインごとに姿勢を整える
            if (BaseLineCount > 0)
            {
                // ラインの法線・接線を求める
                // （ローカル座標空間）
                var calcBaseLineNormalTangentJob = new CalcBaseLineNormalTangentJob()
                {
                    jobBaseLineList = sm.processingStepEdgeCollision.Buffer,

                    teamDataArray = tm.teamDataArray.GetNativeArray(),
                    parameterArray = tm.parameterArray.GetNativeArray(),

                    attributes = attributes.GetNativeArray(),
                    positions = positions.GetNativeArray(),
                    rotations = rotations.GetNativeArray(),
                    vertexLocalPositions = vertexLocalPositions.GetNativeArray(),
                    vertexLocalRotations = vertexLocalRotations.GetNativeArray(),
                    parentIndices = vertexParentIndices.GetNativeArray(),
                    childIndexArray = vertexChildIndexArray.GetNativeArray(),
                    childDataArray = vertexChildDataArray.GetNativeArray(),

                    baseLineFlags = baseLineFlags.GetNativeArray(),
                    baseLineTeamIds = baseLineTeamIds.GetNativeArray(),
                    baseLineStartIndices = baseLineStartDataIndices.GetNativeArray(),
                    baseLineCounts = baseLineDataCounts.GetNativeArray(),
                    baseLineIndices = baseLineData.GetNativeArray(),
                };
                jobHandle = calcBaseLineNormalTangentJob.Schedule(sm.processingStepEdgeCollision.GetJobSchedulePtr(), 8, jobHandle);
            }

            // トライアングルがある場合はトライアングル接続情報から最終的な姿勢を求める
            if (TriangleCount > 0)
            {
                // トライアングルの法線・接線を求める
                // （ローカル座標空間）
                var triangleNormalTangentJob = new CalcTriangleNormalTangentJob()
                {
                    jobTriangleList = sm.processingStepMotionParticle.Buffer,

                    teamDataArray = tm.teamDataArray.GetNativeArray(),

                    triangleTeamIdArray = triangleTeamIdArray.GetNativeArray(),
                    triangles = triangles.GetNativeArray(),
                    outTriangleNormals = triangleNormals.GetNativeArray(),
                    outTriangleTangents = triangleTangents.GetNativeArray(),

                    positions = positions.GetNativeArray(),
                    uv = uv.GetNativeArray(),
                };
                jobHandle = triangleNormalTangentJob.Schedule(sm.processingStepMotionParticle.GetJobSchedulePtr(), 16, jobHandle);

                // トライアングルの法線接線から頂点法線接線を平均化して求める
                // （ローカル座標空間）
                var vertexNormalTangentFromTriangleJob = new CalcVertexNormalTangentFromTriangleJob()
                {
                    jobVertexIndexList = sm.processingStepParticle.Buffer,

                    teamDataArray = tm.teamDataArray.GetNativeArray(),

                    teamIds = teamIds.GetNativeArray(),
                    triangleNormals = triangleNormals.GetNativeArray(),
                    triangleTangents = triangleTangents.GetNativeArray(),
                    vertexToTriangles = vertexToTriangles.GetNativeArray(),
                    normalAdjustmentRotations = normalAdjustmentRotations.GetNativeArray(),
                    outRotations = rotations.GetNativeArray(),
                };
                jobHandle = vertexNormalTangentFromTriangleJob.Schedule(sm.processingStepParticle.GetJobSchedulePtr(), 32, jobHandle);
            }

            // Transformパーティクルの場合はvertexToTransform回転を乗算してTransformDataに情報を書き戻す
            var writeTransformDataJob = new WriteTransformDataJob()
            {
                jobVertexIndexList = sm.processingStepTriangleBending.Buffer,

                teamDataArray = tm.teamDataArray.GetNativeArray(),

                transformPositionArray = bm.positionArray.GetNativeArray(),
                transformRotationArray = bm.rotationArray.GetNativeArray(),

                teamIds = teamIds.GetNativeArray(),
                positions = positions.GetNativeArray(),
                rotations = rotations.GetNativeArray(),
                vertexToTransformRotations = vertexToTransformRotations.GetNativeArray(),
            };
            jobHandle = writeTransformDataJob.Schedule(sm.processingStepTriangleBending.GetJobSchedulePtr(), 32, jobHandle);

            // Transformパーティクルは親からのローカル姿勢を計算してTransformData情報に書き込む
            var writeLocalTransformDataJob = new WriteTransformLocalDataJob()
            {
                jobVertexIndexList = sm.processingStepTriangleBending.Buffer,

                teamDataArray = tm.teamDataArray.GetNativeArray(),

                teamIds = teamIds.GetNativeArray(),
                attributes = attributes.GetNativeArray(),
                vertexParentIndices = vertexParentIndices.GetNativeArray(),

                transformPositionArray = bm.positionArray.GetNativeArray(),
                transformRotationArray = bm.rotationArray.GetNativeArray(),
                transformScaleArray = bm.scaleArray.GetNativeArray(),
                transformLocalPositionArray = bm.localPositionArray.GetNativeArray(),
                transformLocalRotationArray = bm.localRotationArray.GetNativeArray(),
            };
            jobHandle = writeLocalTransformDataJob.Schedule(sm.processingStepTriangleBending.GetJobSchedulePtr(), 32, jobHandle);


            return jobHandle;
        }

        [BurstCompile]
        struct CreatePostProxyMeshUpdateListJob : IJobParallelFor
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;

            // triangle vertex update
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeReference<int> processingCounter0;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<int> processingList0;

            // transform vertex update
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeReference<int> processingCounter1;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<int> processingList1;

            // base line
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeReference<int> processingCounter2;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<int> processingList2;

            // triangle
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeReference<int> processingCounter3;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<int> processingList3;

            public void Execute(int teamId)
            {
                // [0]はグローバルチームなのでスキップ
                if (teamId == 0)
                    return;
                var tdata = teamDataArray[teamId];
                if (tdata.IsEnable == false)
                    return;
                if (tdata.IsCullingInvisible)
                    return;

                // トライアングル[0]
                if (tdata.TriangleCount > 0 && tdata.proxyCommonChunk.IsValid)
                {
                    int start = processingCounter0.InterlockedStartIndex(tdata.proxyCommonChunk.dataLength);
                    for (int j = 0; j < tdata.proxyCommonChunk.dataLength; j++)
                    {
                        int vindex = tdata.proxyCommonChunk.startIndex + j;
                        processingList0[start + j] = vindex;
                    }
                }

                // トランスフォーム書き込み[1]
                if (tdata.proxyMeshType == VirtualMesh.MeshType.ProxyBoneMesh)
                {
                    int start = processingCounter1.InterlockedStartIndex(tdata.proxyCommonChunk.dataLength);
                    for (int j = 0; j < tdata.proxyCommonChunk.dataLength; j++)
                    {
                        int vindex = tdata.proxyCommonChunk.startIndex + j;
                        processingList1[start + j] = vindex;
                    }
                }

                // ベースライン[2]
                if (tdata.baseLineChunk.IsValid)
                {
                    int start = processingCounter2.InterlockedStartIndex(tdata.baseLineChunk.dataLength);
                    for (int j = 0; j < tdata.baseLineChunk.dataLength; j++)
                    {
                        int bindex = tdata.baseLineChunk.startIndex + j;
                        processingList2[start + j] = bindex;
                    }
                }

                // トライアングル2
                if (tdata.TriangleCount > 0)
                {
                    int start = processingCounter3.InterlockedStartIndex(tdata.proxyTriangleChunk.dataLength);
                    for (int j = 0; j < tdata.proxyTriangleChunk.dataLength; j++)
                    {
                        int tindex = tdata.proxyTriangleChunk.startIndex + j;
                        processingList3[start + j] = tindex;
                    }
                }

                //Debug.Log($"BaseLine:{jobBaseLineList.Length}, Triangle Vertex:{jobVertexIndexList1.Length}, Transform Vertex:{jobVertexIndexList2.Length}");
            }
        }

        /// <summary>
        /// ベースラインの法線接線を求める
        /// </summary>
        [BurstCompile]
        struct CalcBaseLineNormalTangentJob : IJobParallelForDefer
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<int> jobBaseLineList;

            // team
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<ClothParameters> parameterArray;

            // vmesh
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> attributes;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> positions;
            [NativeDisableParallelForRestriction]
            public NativeArray<quaternion> rotations;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> vertexLocalPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> vertexLocalRotations;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> parentIndices;
            [Unity.Collections.ReadOnly]
            public NativeArray<uint> childIndexArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<ushort> childDataArray;

            [Unity.Collections.ReadOnly]
            public NativeArray<ExBitFlag8> baseLineFlags;
            [Unity.Collections.ReadOnly]
            public NativeArray<short> baseLineTeamIds;
            [Unity.Collections.ReadOnly]
            public NativeArray<ushort> baseLineStartIndices;
            [Unity.Collections.ReadOnly]
            public NativeArray<ushort> baseLineCounts;
            [Unity.Collections.ReadOnly]
            public NativeArray<ushort> baseLineIndices;

            // ベースラインごと
            public void Execute(int index)
            {
                int bindex = jobBaseLineList[index];

                // ラインを含む場合のみ実行する
                var bflag = baseLineFlags[bindex];
                if (bflag.IsSet(VirtualMesh.BaseLineFlag_IncludeLine) == false)
                    return;

                // team
                int teamId = baseLineTeamIds[bindex];
                if (teamId == 0)
                    return;
                var tdata = teamDataArray[teamId];
                if (tdata.IsEnable == false)
                    return;
                if (tdata.IsCullingInvisible)
                    return;

                // parameter
                var param = parameterArray[teamId];
                float averageRate = param.rotationalInterpolation; // 回転平均化割合
                float rootInterpolation = param.rootRotation;

                int s_vindex = tdata.proxyCommonChunk.startIndex;
                int s_dataIndex = tdata.baseLineDataChunk.startIndex;
                int s_childDataIndex = tdata.proxyVertexChildDataChunk.startIndex;

                // ベースラインをルートから走査する
                int dataIndex = baseLineStartIndices[bindex] + s_dataIndex;
                int dataCnt = baseLineCounts[bindex];
                for (int i = 0; i < dataCnt; i++, dataIndex++)
                {
                    // 自身を親とする
                    int vindex = baseLineIndices[dataIndex] + s_vindex;
                    var pos = positions[vindex];
                    var rot = rotations[vindex];
                    var attr = attributes[vindex];

                    // 子の情報
                    var pack = childIndexArray[vindex];
                    int cstart = DataUtility.Unpack12_20Low(pack);
                    int ccnt = DataUtility.Unpack12_20Hi(pack);

                    int movecnt = 0;
                    if (ccnt > 0)
                    {
                        // 子への平均ベクトル
                        float3 ctv = 0;
                        float3 cv = 0;

                        // 自身を基準に子の回転を求める、また子への平均ベクトルを加算する
                        for (int j = 0; j < ccnt; j++)
                        {
                            int cvindex = childDataArray[s_childDataIndex + cstart + j] + s_vindex;

                            // 子の属性
                            var cattr = attributes[cvindex];

                            // 子の座標
                            var cpos = positions[cvindex];

                            // 子の本来のベクトル
                            float3 tv = math.mul(rot, vertexLocalPositions[cvindex]);
                            ctv += tv;

                            if (cattr.IsMove())
                            {
                                // 子の現在ベクトル
                                float3 v = cpos - pos;
                                cv += v;

                                // 回転
                                var q = MathUtility.FromToRotation(tv, v);

                                // 子の姿勢を決定
                                var crot = math.mul(rot, vertexLocalRotations[cvindex]);
                                crot = math.mul(q, crot);
                                rotations[cvindex] = crot;

                                movecnt++;
                            }
                            else
                            {
                                // 子が固定の場合
                                cv += tv;
                            }
                        }

                        // 子がすべて固定の場合は回転調整を行わない
                        if (movecnt == 0)
                            continue;

                        // 子の移動方向変化に伴う回転調整
                        float t = attr.IsMove() ? averageRate : rootInterpolation;
                        var cq = MathUtility.FromToRotation(ctv, cv, t);

                        // 自身の姿勢を確定させる
                        rot = math.mul(cq, rot);
                        rotations[vindex] = rot;
                    }
                    else
                    {
#if false
                        // 末端
                        if (param.boneParameters.leafRotation)
                        {
                            // 親からの角度分さらに曲げる（この方が見た目が良くなる）
                            int pvindex = parentIndices[vindex] + s_vindex;
                            if (pvindex >= 0)
                            {
                                var ppos = positions[pvindex];
                                var prot = rotations[pvindex];

                                // 本来のベクトル
                                float3 tv = math.mul(prot, vertexLocalPositions[vindex]);

                                // 現在のベクトル
                                float3 v = pos - ppos;

                                // 回転
                                var q = MathUtility.FromToRotation(tv, v, averageRate);

                                // 親からの角度分さらに回転させる
                                rot = math.mul(q, rot);
                                rotations[vindex] = rot;
                            }
                        }
#endif
                    }
                }
            }
        }

        /// <summary>
        /// トライアングルの法線と接線を求める
        /// 座標系の変換は行わない
        /// </summary>
        [BurstCompile]
        struct CalcTriangleNormalTangentJob : IJobParallelForDefer
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<int> jobTriangleList;

            // team
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;

            // triangle
            [Unity.Collections.ReadOnly]
            public NativeArray<short> triangleTeamIdArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<int3> triangles;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> outTriangleNormals;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> outTriangleTangents;

            // vmesh
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> positions;
            [Unity.Collections.ReadOnly]
            public NativeArray<float2> uv;


            // トライアングルごと
            public void Execute(int index)
            {
                int tindex = jobTriangleList[index];

                int teamId = triangleTeamIdArray[tindex];
                if (teamId == 0)
                    return;
                var tdata = teamDataArray[teamId];
                if (tdata.IsEnable == false)
                    return;
                if (tdata.IsCullingInvisible)
                    return;

                int3 tri = triangles[tindex];

                // トライアングル法線を求める
                int start = tdata.proxyCommonChunk.startIndex;
                var pos1 = positions[start + tri.x];
                var pos2 = positions[start + tri.y];
                var pos3 = positions[start + tri.z];
                float3 cross = math.cross(pos2 - pos1, pos3 - pos1);
                float len = math.length(cross);
                if (len > Define.System.Epsilon)
                    outTriangleNormals[tindex] = cross / len;
#if MC2_DEBUG
                else
                    Debug.LogWarning("CalcTriangleNormalTangentJob.normal = 0!");
#endif

                // トライアングル接線を求める
                var uv1 = uv[start + tri.x];
                var uv2 = uv[start + tri.y];
                var uv3 = uv[start + tri.z];
                var tan = MathUtility.TriangleTangent(pos1, pos2, pos3, uv1, uv2, uv3);
                if (math.lengthsq(tan) > 0.0f)
                    outTriangleTangents[tindex] = tan;
#if MC2_DEBUG
                else
                    Debug.LogWarning("CalcTriangleNormalTangentJob.tangent = 0!");
#endif
                //                len = math.length(tan);
                //                if (len > 1e-06f)
                //                    outTriangleTangents[tindex] = tan / len;
                //#if MC2_DEBUG
                //                else
                //                    Debug.LogWarning("CalcTriangleNormalTangentJob.tangent = 0!");
                //#endif

            }
        }

        /// <summary>
        /// 接続するトライアングルの法線接線を平均化して頂点法線接線を求める
        /// </summary>
        [BurstCompile]
        struct CalcVertexNormalTangentFromTriangleJob : IJobParallelForDefer
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<int> jobVertexIndexList;

            // team
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;

            // vmesh
            [Unity.Collections.ReadOnly]
            public NativeArray<short> teamIds;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> triangleNormals;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> triangleTangents;
            [Unity.Collections.ReadOnly]
            public NativeArray<FixedList32Bytes<uint>> vertexToTriangles;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> normalAdjustmentRotations;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<quaternion> outRotations;

            // 頂点ごと
            public void Execute(int index)
            {
                int vindex = jobVertexIndexList[index];
                int teamId = teamIds[vindex];
                if (teamId == 0)
                    return;
                var tdata = teamDataArray[teamId];

                var tlist = vertexToTriangles[vindex];
                if (tlist.Length > 0)
                {
                    float3 nor = 0;
                    float3 tan = 0;
                    for (int i = 0; i < tlist.Length; i++)
                    {
                        // 12-20bitのパックで格納されている
                        // 12(hi) = 法線と接線のフリップフラグ
                        // 20(low) = トライアングルインデックス
                        uint data = tlist[i];
                        int flipFlag = DataUtility.Unpack12_20Hi(data);
                        int tindex = DataUtility.Unpack12_20Low(data);

                        tindex += tdata.proxyTriangleChunk.startIndex;
                        nor += triangleNormals[tindex] * ((flipFlag & 0x1) == 0 ? 1 : -1);
                        tan += triangleTangents[tindex] * ((flipFlag & 0x2) == 0 ? 1 : -1);

                        /*
                        // トライアングルインデックスが負の値ならば法線をフリップさせる
                        // トライアングルインデックスは＋１の値が格納されているので注意！
                        int tindex = tlist[i];
                        float flip = math.sign(tindex);
                        tindex = math.abs(tindex) - 1;
                        tindex += tdata.proxyTriangleChunk.startIndex;

                        nor += triangleNormals[tindex] * flip;
                        tan += triangleTangents[tindex]; // 接線はフリップさせては駄目！
                        */
                    }

                    //Debug.Log($"Vertex:{vindex} nor:{nor}, tan:{tan}");


                    // 法線０を考慮する。法線を０にするとポリゴンが欠けるため
                    float ln = math.length(nor);
                    float lt = math.length(tan);
                    if (ln > 1e-06f && lt > 1e-06f)
                    {
                        nor = nor / ln;
                        tan = tan / lt;
                        float dot = math.dot(nor, tan);
                        if (dot != 1.0f && dot != -1.0f)
                        {
                            // トライアングル回転は従法線から算出するように変更(v2.1.7)
                            //var rot = quaternion.LookRotation(tan, nor);
                            float3 binor = math.normalize(math.cross(nor, tan));
                            var rot = quaternion.LookRotation(binor, nor);

                            // 法線調整用回転を乗算する（不要な場合は単位回転が入っている）
                            rot = math.mul(rot, normalAdjustmentRotations[vindex]);

                            outRotations[vindex] = rot;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// パーティクルの姿勢をTransformDataに書き込む
        /// </summary>
        [BurstCompile]
        struct WriteTransformDataJob : IJobParallelForDefer
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<int> jobVertexIndexList;

            // team
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;

            // transform
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> transformPositionArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<quaternion> transformRotationArray;

            // vmesh
            [Unity.Collections.ReadOnly]
            public NativeArray<short> teamIds;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> positions;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> rotations;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> vertexToTransformRotations;

            // トランスフォームパーティクルごと
            public void Execute(int index)
            {
                int vindex = jobVertexIndexList[index];
                int teamId = teamIds[vindex];
                if (teamId == 0)
                    return;
                var tdata = teamDataArray[teamId];

                int l_vindex = vindex - tdata.proxyCommonChunk.startIndex;

                var pos = positions[vindex];
                var rot = rotations[vindex];

                // 本来のTransformの姿勢を求める回転を掛ける
                int boneIndex = tdata.proxyBoneChunk.startIndex + l_vindex;
                rot = math.mul(rot, vertexToTransformRotations[boneIndex]);

                // ワールド姿勢
                int tindex = tdata.proxyTransformChunk.startIndex + l_vindex;
                transformPositionArray[tindex] = pos;
                transformRotationArray[tindex] = rot;

                //Debug.Log($"[{teamId}] vindex:{vindex}, l_vlindex:{l_vindex}, tindex:{tindex}");
            }
        }

        /// <summary>
        /// TransformパーティクルのTransformについて親からのローカル姿勢を計算する
        /// </summary>
        [BurstCompile]
        struct WriteTransformLocalDataJob : IJobParallelForDefer
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<int> jobVertexIndexList;

            // team
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;

            // vmeah
            [Unity.Collections.ReadOnly]
            public NativeArray<short> teamIds;
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> attributes;
            [NativeDisableParallelForRestriction]
            public NativeArray<int> vertexParentIndices;

            // transform
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> transformPositionArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> transformRotationArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> transformScaleArray;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> transformLocalPositionArray;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<quaternion> transformLocalRotationArray;

            // Transformパーティクルごと
            public void Execute(int index)
            {
                int vindex = jobVertexIndexList[index];
                int teamId = teamIds[vindex];
                if (teamId == 0)
                    return;
                var tdata = teamDataArray[teamId];

                int l_vindex = vindex - tdata.proxyCommonChunk.startIndex;

                int parentIndex = vertexParentIndices[vindex];
                if (parentIndex < 0)
                    return;

                var attr = attributes[vindex];
                if (attr.IsMove() == false)
                    return;

                // 親からのローカル姿勢を計算しトランスフォーム情報に書き込む
                int tindex = tdata.proxyTransformChunk.startIndex + l_vindex;
                int ptindex = tdata.proxyTransformChunk.startIndex + parentIndex;
                var ppos = transformPositionArray[ptindex];
                var prot = transformRotationArray[ptindex];
                var pscl = transformScaleArray[ptindex];
                var pos = transformPositionArray[tindex];
                var rot = transformRotationArray[tindex];

                var iprot = math.inverse(prot);
                var v = pos - ppos;
                var lpos = math.mul(iprot, v);

                Develop.Assert(pscl.x > 0.0f && pscl.y > 0.0f && pscl.z > 0.0f);
                lpos /= pscl;
                var lrot = math.mul(iprot, rot);

                // todo:マイナススケール対応

                transformLocalPositionArray[tindex] = lpos;
                transformLocalRotationArray[tindex] = lrot;
            }
        }

        //=========================================================================================
        /// <summary>
        /// マッピングメッシュの頂点姿勢を連動するプロキシメッシュから頂点スキニングして求める
        /// </summary>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        internal JobHandle PostMappingMeshUpdate(JobHandle jobHandle)
        {
            if (MagicaManager.Team.MappingCount == 0)
                return jobHandle;

            var tm = MagicaManager.Team;
            var bm = MagicaManager.Bone;

            // マッピングメッシュとプロキシメッシュの座標変換マトリックスを求める
            var calcMeshConvertJob = new CalcMeshConvertMatrixJob()
            {
                mappingDataArray = tm.mappingDataArray.GetNativeArray(),

                teamDataArray = tm.teamDataArray.GetNativeArray(),

                transformPositionArray = bm.positionArray.GetNativeArray(),
                transformRotationArray = bm.rotationArray.GetNativeArray(),
                transformScaleArray = bm.scaleArray.GetNativeArray(),
                transformInverseRotationArray = bm.inverseRotationArray.GetNativeArray(),
            };
            jobHandle = calcMeshConvertJob.Schedule(tm.MappingCount, 1, jobHandle);

            // プロキシスキニングの実行
            // （マッピングメッシュのローカル座標空間）
            // todo:カリングを考えてバッファにすべきかも
            var calcProxySkinningJob = new CalcProxySkinningJob()
            {
                teamDataArray = tm.teamDataArray.GetNativeArray(),

                mappingDataArray = tm.mappingDataArray.GetNativeArray(),

                mappingIdArray = mappingIdArray.GetNativeArray(),
                mappingAttributes = mappingAttributes.GetNativeArray(),
                mappingLocalPositions = mappingLocalPositins.GetNativeArray(),
                mappingLocalNormals = mappingLocalNormals.GetNativeArray(),
                //mappingLocalTangents = mappingLocalTangents.GetNativeArray(),
                mappingBoneWeights = mappingBoneWeights.GetNativeArray(),
                mappingPositions = mappingPositions.GetNativeArray(),
                mappingNormals = mappingNormals.GetNativeArray(),

                proxyPositions = positions.GetNativeArray(),
                proxyRotations = rotations.GetNativeArray(),
                proxyVertexBindPosePositions = vertexBindPosePositions.GetNativeArray(),
                proxyVertexBindPoseRotations = vertexBindPoseRotations.GetNativeArray(),
            };
            jobHandle = calcProxySkinningJob.Schedule(MappingVertexCount, 32, jobHandle);

            return jobHandle;
        }

        /// <summary>
        /// // マッピングメッシュとプロキシメッシュの座標変換マトリックスを求める
        /// </summary>
        [BurstCompile]
        struct CalcMeshConvertMatrixJob : IJobParallelFor
        {
            public NativeArray<TeamManager.MappingData> mappingDataArray;

            // team
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;

            // transform
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> transformPositionArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> transformRotationArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> transformScaleArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> transformInverseRotationArray;

            // マッピングメッシュごと
            public void Execute(int index)
            {
                var mdata = mappingDataArray[index];
                if (mdata.IsValid() == false)
                    return;

                var tdata = teamDataArray[mdata.teamId];
                if (tdata.IsEnable == false)
                    return;
                if (tdata.IsCullingInvisible)
                    return;

                // mapping
                var pos = transformPositionArray[mdata.centerTransformIndex];
                var rot = transformRotationArray[mdata.centerTransformIndex];
                var scl = transformScaleArray[mdata.centerTransformIndex];
                var irot = transformInverseRotationArray[mdata.centerTransformIndex];

                // proxy
                var ppos = transformPositionArray[tdata.centerTransformIndex];
                var prot = transformRotationArray[tdata.centerTransformIndex];
                var pscl = transformScaleArray[tdata.centerTransformIndex];

                // プロキシメッシュとマッピングメッシュの座標空間が等しいか判定
                bool sameSpace = MathUtility.CompareTransform(pos, rot, scl, ppos, prot, pscl);
                mdata.sameSpace = sameSpace;
                //Debug.Log($"sameSpace:{sameSpace}, scl:{scl}, pscl:{pscl}");

                // ワールド空間からマッピングメッシュへの座標空間変換
                mdata.toMappingMatrix = math.inverse(MathUtility.LocalToWorldMatrix(pos, rot, scl));
                mdata.toMappingRotation = irot;

                // マッピングメッシュ用のスケール比率
                // チームのステップ実行とは無関係に毎フレーム適用する必要があるためチームスケール比率と分離する
                var initScaleLength = math.length(tdata.initScale);
                Develop.Assert(initScaleLength > 0.0f);
                mdata.scaleRatio = math.length(pscl) / initScaleLength;

                mappingDataArray[index] = mdata;

                //Debug.Log($"Mapping [{mdata.teamId}] sclRatio:{tdata.scaleRatio}");
            }
        }

        /// <summary>
        /// プロキシメッシュからマッピングメッシュの頂点の座標・法線・接線をスキニングして計算する
        /// </summary>
        [BurstCompile]
        struct CalcProxySkinningJob : IJobParallelFor
        {
            // team
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;

            // mapping data
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.MappingData> mappingDataArray;

            // mapping mesh
            [Unity.Collections.ReadOnly]
            public NativeArray<short> mappingIdArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> mappingAttributes;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> mappingLocalPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> mappingLocalNormals;
            //[Unity.Collections.ReadOnly]
            //public NativeArray<float3> mappingLocalTangents;
            [Unity.Collections.ReadOnly]
            public NativeArray<VirtualMeshBoneWeight> mappingBoneWeights;
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> mappingPositions;
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> mappingNormals;

            // proxy mesh
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> proxyPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> proxyRotations;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> proxyVertexBindPosePositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> proxyVertexBindPoseRotations;

            // マッピングメッシュ頂点ごと
            public void Execute(int mvindex)
            {
                int mindex = mappingIdArray[mvindex];
                if (mindex == 0)
                    return;

                // (+1)されているので１引く
                mindex--;

                var mdata = mappingDataArray[mindex];
                if (mdata.IsValid() == false)
                    return;

                // team
                var tdata = teamDataArray[mdata.teamId];
                if (tdata.IsEnable == false)
                    return;
                if (tdata.IsCullingInvisible)
                    return;

                // 無効頂点は無視する
                var attr = mappingAttributes[mvindex];
                if (attr.IsInvalid())
                    return;

                // 固定も無視する(todo:一旦こうする）
                if (attr.IsFixed())
                    return;

                // マッピングメッシュ姿勢
                float3 lpos = mappingLocalPositions[mvindex];
                float3 lnor = mappingLocalNormals[mvindex];
                //float3 ltan = mappingLocalTangents[mvindex];

                // プロキシメッシュの座標空間に変換する
                if (mdata.sameSpace == false)
                {
                    // 現在の姿勢ではなくマッピング時の姿勢で変換を行う
                    lpos = math.transform(mdata.toProxyMatrix, lpos);
                    lnor = math.mul(mdata.toProxyRotation, lnor);
                    //ltan = math.mul(mdata.toProxyRotation, ltan);
                }

                // 以降計算はすべてプロキシメッシュのローカル空間で行う
                var bw = mappingBoneWeights[mvindex];
                int wcnt = bw.Count;
                float3 opos = 0;
                float3 onor = 0;
                //float3 otan = 0;
                // ProxyMeshスケール
                float3 pscl = tdata.initScale * mdata.scaleRatio; // 初期スケール x 現在のスケール比率
                for (int i = 0; i < wcnt; i++)
                {
                    float w = bw.weights[i];

                    int tvindex = bw.boneIndices[i] + tdata.proxyCommonChunk.startIndex;

                    // バインドポーズの逆座標と逆回転
                    float3 bipos = proxyVertexBindPosePositions[tvindex];
                    quaternion birot = proxyVertexBindPoseRotations[tvindex];

                    float3 pos = math.mul(birot, lpos + bipos);
                    float3 nor = math.mul(birot, lnor);
                    //float3 tan = math.mul(birot, ltan);

                    float3 ppos = proxyPositions[tvindex];
                    quaternion prot = proxyRotations[tvindex];

                    // ワールド変換
                    pos = math.mul(prot, pos * pscl) + ppos;
                    nor = math.mul(prot, nor);
                    //tan = math.mul(prot, tan);

                    opos += pos.xyz * w;
                    onor += nor.xyz * w;
                    //otan += tan.xyz * w;
                }

                // 単位化しないと駄目な状況が発生。仕方なし。
                //onor = math.normalize(onor);
                //otan = math.normalize(otan);

                // ここまでのopos/orotはワールド空間
                // マッピングメッシュのローカル空間に変換する
                opos = math.transform(mdata.toMappingMatrix, opos);
                //onor = MathUtility.TransformDirection(onor, mdata.toMappingMatrix);
                onor = math.normalize(MathUtility.TransformVector(onor, mdata.toMappingMatrix));

                // 格納
                mappingPositions[mvindex] = opos;
                mappingNormals[mvindex] = onor;
            }
        }

        //=========================================================================================
        public void InformationLog(StringBuilder allsb)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"========== VMesh Manager ==========");
            if (IsValid() == false)
            {
                sb.AppendLine($"VirtualMesh Manager. Invalid.");
            }
            else
            {
                sb.AppendLine($"VirtualMesh Manager.");
                sb.AppendLine($"  -VertexCount:{VertexCount}");
                sb.AppendLine($"  -EdgeCount:{EdgeCount}");
                sb.AppendLine($"  -TriangleCount:{TriangleCount}");
                sb.AppendLine($"  -BaseLineCount:{BaseLineCount}");
                sb.AppendLine($"  -MeshClothVertexCount:{MeshClothVertexCount}");

                sb.AppendLine($"  [ProxyMesh]");
                sb.AppendLine($"    -teamIds:{teamIds.ToSummary()}");
                sb.AppendLine($"    -attributes:{attributes.ToSummary()}");
                sb.AppendLine($"    -vertexToTriangles:{vertexToTriangles.ToSummary()}");
                sb.AppendLine($"    -vertexBindPosePositions:{vertexBindPosePositions.ToSummary()}");
                sb.AppendLine($"    -vertexBindPoseRotations:{vertexBindPoseRotations.ToSummary()}");
                sb.AppendLine($"    -vertexDepths:{vertexDepths.ToSummary()}");
                sb.AppendLine($"    -vertexRootIndices:{vertexRootIndices.ToSummary()}");
                sb.AppendLine($"    -vertexLocalPositions:{vertexLocalPositions.ToSummary()}");
                sb.AppendLine($"    -vertexLocalRotations:{vertexLocalRotations.ToSummary()}");
                sb.AppendLine($"    -vertexParentIndices:{vertexParentIndices.ToSummary()}");
                sb.AppendLine($"    -vertexChildIndexArray:{vertexChildIndexArray.ToSummary()}");
                sb.AppendLine($"    -vertexChildDataArray:{vertexChildDataArray.ToSummary()}");
                sb.AppendLine($"    -normalAdjustmentRotations:{normalAdjustmentRotations.ToSummary()}");
                sb.AppendLine($"    -uv:{uv.ToSummary()}");

                sb.AppendLine($"    -triangleTeamIdArray:{triangleTeamIdArray.ToSummary()}");
                sb.AppendLine($"    -triangles:{triangles.ToSummary()}");
                sb.AppendLine($"    -triangleNormals:{triangleNormals.ToSummary()}");
                sb.AppendLine($"    -triangleTangents:{triangleTangents.ToSummary()}");

                sb.AppendLine($"    -edgeTeamIdArray:{edgeTeamIdArray.ToSummary()}");
                sb.AppendLine($"    -edges:{edges.ToSummary()}");
                sb.AppendLine($"    -edgeFlags:{edgeFlags.ToSummary()}");

                sb.AppendLine($"    -baseLineFlags:{baseLineFlags.ToSummary()}");
                sb.AppendLine($"    -baseLineTeamIds:{baseLineTeamIds.ToSummary()}");
                sb.AppendLine($"    -baseLineStartDataIndices:{baseLineStartDataIndices.ToSummary()}");
                sb.AppendLine($"    -baseLineDataCounts:{baseLineDataCounts.ToSummary()}");
                sb.AppendLine($"    -baseLineData:{baseLineData.ToSummary()}");

                sb.AppendLine($"  [Mesh Common]");
                sb.AppendLine($"    -localPositions:{localPositions.ToSummary()}");
                sb.AppendLine($"    -localNormals:{localNormals.ToSummary()}");
                sb.AppendLine($"    -localTangents:{localTangents.ToSummary()}");
                sb.AppendLine($"    -boneWeights:{boneWeights.ToSummary()}");
                sb.AppendLine($"    -skinBoneTransformIndices:{skinBoneTransformIndices.ToSummary()}");
                sb.AppendLine($"    -skinBoneBindPoses:{skinBoneBindPoses.ToSummary()}");

                sb.AppendLine($"  [Mesh Other]");
                sb.AppendLine($"    -vertexToTransformRotations:{vertexToTransformRotations.ToSummary()}");
                sb.AppendLine($"    -positions:{positions.ToSummary()}");
                sb.AppendLine($"    -rotations:{rotations.ToSummary()}");

                sb.AppendLine($"  [Mapping]");
                sb.AppendLine($"    -MappingVertexCount:{MappingVertexCount}");
                sb.AppendLine($"    -mappingReferenceIndices:{mappingReferenceIndices.ToSummary()}");
                sb.AppendLine($"    -mappingAttributes:{mappingAttributes.ToSummary()}");
                sb.AppendLine($"    -mappingLocalPositins:{mappingLocalPositins.ToSummary()}");
                sb.AppendLine($"    -mappingLocalNormals:{mappingLocalNormals.ToSummary()}");
                sb.AppendLine($"    -mappingBoneWeights:{mappingBoneWeights.ToSummary()}");
                sb.AppendLine($"    -mappingPositions:{mappingPositions.ToSummary()}");
                sb.AppendLine($"    -mappingNormals:{mappingNormals.ToSummary()}");
            }
            sb.AppendLine();
            Debug.Log(sb.ToString());
            allsb.Append(sb);
        }
    }
}
