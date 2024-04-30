// Magica Cloth 2.
// Copyright (c) 2024 MagicaSoft.
// https://magicasoft.jp
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth2
{
    public partial class VirtualMesh
    {
        /// <summary>
        /// PreBuildの共有部分データ
        /// </summary>
        [System.Serializable]
        public class ShareSerializationData
        {
            public string name;
            public MeshType meshType;
            public bool isBoneCloth;

            // 基本
            public ExSimpleNativeArray<int>.SerializationData referenceIndices;
            public ExSimpleNativeArray<VertexAttribute>.SerializationData attributes;
            public ExSimpleNativeArray<float3>.SerializationData localPositions;
            public ExSimpleNativeArray<float3>.SerializationData localNormals;
            public ExSimpleNativeArray<float3>.SerializationData localTangents;
            public ExSimpleNativeArray<float2>.SerializationData uv;
            public ExSimpleNativeArray<VirtualMeshBoneWeight>.SerializationData boneWeights;
            public ExSimpleNativeArray<int3>.SerializationData triangles;
            public ExSimpleNativeArray<int2>.SerializationData lines;
            public int centerTransformIndex;
            public float4x4 initLocalToWorld;
            public float4x4 initWorldToLocal;
            public quaternion initRotation;
            public quaternion initInverseRotation;
            public float3 initScale;
            public int skinRootIndex;
            public ExSimpleNativeArray<int>.SerializationData skinBoneTransformIndices;
            public ExSimpleNativeArray<float4x4>.SerializationData skinBoneBindPoses;
            public TransformData.ShareSerializationData transformData;
            public AABB boundingBox;
            public float averageVertexDistance;
            public float maxVertexDistance;

            // プロキシメッシュ
            public byte[] vertexToTriangles;
            public byte[] vertexToVertexIndexArray;
            public byte[] vertexToVertexDataArray;
            public byte[] edges;
            public byte[] edgeFlags;
            public int2[] edgeToTrianglesKeys;
            public ushort[] edgeToTrianglesValues;
            public byte[] vertexBindPosePositions;
            public byte[] vertexBindPoseRotations;
            public byte[] vertexToTransformRotations;
            public byte[] vertexDepths;
            public byte[] vertexRootIndices;
            public byte[] vertexParentIndices;
            public byte[] vertexChildIndexArray;
            public byte[] vertexChildDataArray;
            public byte[] vertexLocalPositions;
            public byte[] vertexLocalRotations;
            public byte[] normalAdjustmentRotations;
            public byte[] baseLineFlags;
            public byte[] baseLineStartDataIndices;
            public byte[] baseLineDataCounts;
            public byte[] baseLineData;
            public int[] customSkinningBoneIndices;
            public ushort[] centerFixedList;
            public float3 localCenterPosition;

            // マッピングメッシュ
            public float3 centerWorldPosition;
            public quaternion centerWorldRotation;
            public float3 centerWorldScale;
            public float4x4 toProxyMatrix;
            public quaternion toProxyRotation;

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder(1024);

                sb.AppendLine("===== VirtualMesh.SerializeData =====");
                sb.AppendLine($"name:{name}");
                sb.AppendLine($"meshType:{meshType}");
                sb.AppendLine($"isBoneCloth:{isBoneCloth}");
                sb.AppendLine($"VertexCount:{attributes.count}");
                sb.AppendLine($"TriangleCount:{triangles.count}");
                sb.AppendLine($"LineCount:{lines.count}");

                return sb.ToString();
            }
        }

        public ShareSerializationData ShareSerialize()
        {
            var sdata = new ShareSerializationData();
            try
            {
                sdata.name = name;
                sdata.meshType = meshType;
                sdata.isBoneCloth = isBoneCloth;

                // 基本
                sdata.referenceIndices = referenceIndices.Serialize();
                sdata.attributes = attributes.Serialize();
                sdata.localPositions = localPositions.Serialize();
                sdata.localNormals = localNormals.Serialize();
                sdata.localTangents = localTangents.Serialize();
                sdata.uv = uv.Serialize();
                sdata.boneWeights = boneWeights.Serialize();
                sdata.triangles = triangles.Serialize();
                sdata.lines = lines.Serialize();
                sdata.centerTransformIndex = centerTransformIndex;
                sdata.initLocalToWorld = initLocalToWorld;
                sdata.initWorldToLocal = initWorldToLocal;
                sdata.initRotation = initRotation;
                sdata.initInverseRotation = initInverseRotation;
                sdata.initScale = initScale;
                sdata.skinRootIndex = skinRootIndex;
                sdata.skinBoneTransformIndices = skinBoneTransformIndices.Serialize();
                sdata.skinBoneBindPoses = skinBoneBindPoses.Serialize();
                sdata.transformData = transformData?.ShareSerialize();
                if (boundingBox.IsCreated)
                    sdata.boundingBox = boundingBox.Value;
                if (averageVertexDistance.IsCreated)
                    sdata.averageVertexDistance = averageVertexDistance.Value;
                if (maxVertexDistance.IsCreated)
                    sdata.maxVertexDistance = maxVertexDistance.Value;

                // プロキシメッシュ
                sdata.vertexToTriangles = vertexToTriangles.MC2ToRawBytes();
                sdata.vertexToVertexIndexArray = vertexToVertexIndexArray.MC2ToRawBytes();
                sdata.vertexToVertexDataArray = vertexToVertexDataArray.MC2ToRawBytes();
                sdata.edges = edges.MC2ToRawBytes();
                sdata.edgeFlags = edgeFlags.MC2ToRawBytes();
                (sdata.edgeToTrianglesKeys, sdata.edgeToTrianglesValues) = edgeToTriangles.MC2Serialize();
                sdata.vertexBindPosePositions = vertexBindPosePositions.MC2ToRawBytes();
                sdata.vertexBindPoseRotations = vertexBindPoseRotations.MC2ToRawBytes();
                sdata.vertexToTransformRotations = vertexToTransformRotations.MC2ToRawBytes();
                sdata.vertexDepths = vertexDepths.MC2ToRawBytes();
                sdata.vertexRootIndices = vertexRootIndices.MC2ToRawBytes();
                sdata.vertexParentIndices = vertexParentIndices.MC2ToRawBytes();
                sdata.vertexChildIndexArray = vertexChildIndexArray.MC2ToRawBytes();
                sdata.vertexChildDataArray = vertexChildDataArray.MC2ToRawBytes();
                sdata.vertexLocalPositions = vertexLocalPositions.MC2ToRawBytes();
                sdata.vertexLocalRotations = vertexLocalRotations.MC2ToRawBytes();
                sdata.normalAdjustmentRotations = normalAdjustmentRotations.MC2ToRawBytes();
                sdata.baseLineFlags = baseLineFlags.MC2ToRawBytes();
                sdata.baseLineStartDataIndices = baseLineStartDataIndices.MC2ToRawBytes();
                sdata.baseLineDataCounts = baseLineDataCounts.MC2ToRawBytes();
                sdata.baseLineData = baseLineData.MC2ToRawBytes();
                DataUtility.ArrayCopy(customSkinningBoneIndices, ref sdata.customSkinningBoneIndices);
                DataUtility.ArrayCopy(centerFixedList, ref sdata.centerFixedList);
                if (localCenterPosition.IsCreated)
                    sdata.localCenterPosition = localCenterPosition.Value;

                // マッピングメッシュ
                sdata.centerWorldPosition = centerWorldPosition;
                sdata.centerWorldRotation = centerWorldRotation;
                sdata.centerWorldScale = centerWorldScale;
                sdata.toProxyMatrix = toProxyMatrix;
                sdata.toProxyRotation = toProxyRotation;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }

            return sdata;
        }

        public static VirtualMesh ShareDeserialize(ShareSerializationData sdata)
        {
            var vmesh = new VirtualMesh();
            vmesh.isManaged = true;

            try
            {
                vmesh.name = sdata.name;
                vmesh.meshType = sdata.meshType;
                vmesh.isBoneCloth = sdata.isBoneCloth;

                // 基本
                vmesh.referenceIndices.Deserialize(sdata.referenceIndices);
                vmesh.attributes.Deserialize(sdata.attributes);
                vmesh.localPositions.Deserialize(sdata.localPositions);
                vmesh.localNormals.Deserialize(sdata.localNormals);
                vmesh.localTangents.Deserialize(sdata.localTangents);
                vmesh.uv.Deserialize(sdata.uv);
                vmesh.boneWeights.Deserialize(sdata.boneWeights);
                vmesh.triangles.Deserialize(sdata.triangles);
                vmesh.lines.Deserialize(sdata.lines);
                vmesh.centerTransformIndex = sdata.centerTransformIndex;
                vmesh.initLocalToWorld = sdata.initLocalToWorld;
                vmesh.initWorldToLocal = sdata.initWorldToLocal;
                vmesh.initRotation = sdata.initRotation;
                vmesh.initInverseRotation = sdata.initInverseRotation;
                vmesh.initScale = sdata.initScale;
                vmesh.skinRootIndex = sdata.skinRootIndex;
                vmesh.skinBoneTransformIndices.Deserialize(sdata.skinBoneTransformIndices);
                vmesh.skinBoneBindPoses.Deserialize(sdata.skinBoneBindPoses);
                vmesh.transformData = TransformData.ShareDeserialize(sdata.transformData);
                vmesh.boundingBox = new NativeReference<AABB>(sdata.boundingBox, Allocator.Persistent);
                vmesh.averageVertexDistance = new NativeReference<float>(sdata.averageVertexDistance, Allocator.Persistent);
                vmesh.maxVertexDistance = new NativeReference<float>(sdata.maxVertexDistance, Allocator.Persistent);

                // プロキシメッシュ
                vmesh.vertexToTriangles = NativeArrayExtensions.MC2FromRawBytes<FixedList32Bytes<uint>>(sdata.vertexToTriangles);
                vmesh.vertexToVertexIndexArray = NativeArrayExtensions.MC2FromRawBytes<uint>(sdata.vertexToVertexIndexArray);
                vmesh.vertexToVertexDataArray = NativeArrayExtensions.MC2FromRawBytes<ushort>(sdata.vertexToVertexDataArray);
                vmesh.edges = NativeArrayExtensions.MC2FromRawBytes<int2>(sdata.edges);
                vmesh.edgeFlags = NativeArrayExtensions.MC2FromRawBytes<ExBitFlag8>(sdata.edgeFlags);
                vmesh.edgeToTriangles = NativeMultiHashMapExtensions.MC2Deserialize(sdata.edgeToTrianglesKeys, sdata.edgeToTrianglesValues);
                vmesh.vertexBindPosePositions = NativeArrayExtensions.MC2FromRawBytes<float3>(sdata.vertexBindPosePositions);
                vmesh.vertexBindPoseRotations = NativeArrayExtensions.MC2FromRawBytes<quaternion>(sdata.vertexBindPoseRotations);
                vmesh.vertexToTransformRotations = NativeArrayExtensions.MC2FromRawBytes<quaternion>(sdata.vertexToTransformRotations);
                vmesh.vertexDepths = NativeArrayExtensions.MC2FromRawBytes<float>(sdata.vertexDepths);
                vmesh.vertexRootIndices = NativeArrayExtensions.MC2FromRawBytes<int>(sdata.vertexRootIndices);
                vmesh.vertexParentIndices = NativeArrayExtensions.MC2FromRawBytes<int>(sdata.vertexParentIndices);
                vmesh.vertexChildIndexArray = NativeArrayExtensions.MC2FromRawBytes<uint>(sdata.vertexChildIndexArray);
                vmesh.vertexChildDataArray = NativeArrayExtensions.MC2FromRawBytes<ushort>(sdata.vertexChildDataArray);
                vmesh.vertexLocalPositions = NativeArrayExtensions.MC2FromRawBytes<float3>(sdata.vertexLocalPositions);
                vmesh.vertexLocalRotations = NativeArrayExtensions.MC2FromRawBytes<quaternion>(sdata.vertexLocalRotations);
                vmesh.normalAdjustmentRotations = NativeArrayExtensions.MC2FromRawBytes<quaternion>(sdata.normalAdjustmentRotations);
                vmesh.baseLineFlags = NativeArrayExtensions.MC2FromRawBytes<ExBitFlag8>(sdata.baseLineFlags);
                vmesh.baseLineStartDataIndices = NativeArrayExtensions.MC2FromRawBytes<ushort>(sdata.baseLineStartDataIndices);
                vmesh.baseLineDataCounts = NativeArrayExtensions.MC2FromRawBytes<ushort>(sdata.baseLineDataCounts);
                vmesh.baseLineData = NativeArrayExtensions.MC2FromRawBytes<ushort>(sdata.baseLineData);
                DataUtility.ArrayCopy(sdata.customSkinningBoneIndices, ref vmesh.customSkinningBoneIndices);
                DataUtility.ArrayCopy(sdata.centerFixedList, ref vmesh.centerFixedList);
                vmesh.localCenterPosition = new NativeReference<float3>(sdata.localCenterPosition, Allocator.Persistent);

                // マッピングメッシュ
                vmesh.centerWorldPosition = sdata.centerWorldPosition;
                vmesh.centerWorldRotation = sdata.centerWorldRotation;
                vmesh.centerWorldScale = sdata.centerWorldScale;
                vmesh.toProxyMatrix = sdata.toProxyMatrix;
                vmesh.toProxyRotation = sdata.toProxyRotation;

                vmesh.result.SetSuccess();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                vmesh.result.SetError(Define.Result.PreBuildData_VirtualMeshDeserializationException);
            }

            return vmesh;
        }

        //=========================================================================================
        /// <summary>
        /// PreBuildの固有部分データ
        /// </summary>
        [System.Serializable]
        public class UniqueSerializationData : ITransform
        {
            public TransformData.UniqueSerializationData transformData;

            public void GetUsedTransform(HashSet<Transform> transformSet)
            {
                transformData?.GetUsedTransform(transformSet);
            }

            public void ReplaceTransform(Dictionary<int, Transform> replaceDict)
            {
                transformData?.ReplaceTransform(replaceDict);
            }
        }

        public UniqueSerializationData UniqueSerialize()
        {
            var sdata = new UniqueSerializationData();
            try
            {
                sdata.transformData = transformData?.UniqueSerialize();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }

            return sdata;
        }
    }
}
