// Magica Cloth 2.
// Copyright (c) 2024 MagicaSoft.
// https://magicasoft.jp
using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace MagicaCloth2
{
    public partial class RenderSetupData
    {
        /// <summary>
        /// PreBuildの共有部分保存データ
        /// </summary>
        [System.Serializable]
        public class ShareSerializationData
        {
            public ResultCode result;
            public string name;
            public SetupType setupType;

            // Mesh ---------------------------------------------------------------
            public Mesh originalMesh;
            public int vertexCount;
            public bool hasSkinnedMesh;
            public bool hasBoneWeight;
            public int skinRootBoneIndex;
            public int skinBoneCount;
            // MeshDataでは取得できないメッシュ情報
            public List<Matrix4x4> bindPoseList;
            public byte[] bonesPerVertexArray;
            public byte[] boneWeightArray;
            public Vector3[] localPositions;
            public Vector3[] localNormals;

            // Bone ---------------------------------------------------------------
            public BoneConnectionMode boneConnectionMode;

            // Common -------------------------------------------------------------
            public int renderTransformIndex;
        }

        public ShareSerializationData ShareSerialize()
        {
            var sdata = new ShareSerializationData();
            try
            {
                sdata.result = result;
                sdata.name = name;
                sdata.setupType = setupType;

                // Mesh
                sdata.originalMesh = originalMesh;
                sdata.vertexCount = vertexCount;
                sdata.hasSkinnedMesh = hasSkinnedMesh;
                sdata.hasBoneWeight = hasBoneWeight;
                sdata.skinRootBoneIndex = skinRootBoneIndex;
                sdata.skinBoneCount = skinBoneCount;
                sdata.bindPoseList = new List<Matrix4x4>(bindPoseList);
                sdata.bonesPerVertexArray = bonesPerVertexArray.MC2ToRawBytes();
                sdata.boneWeightArray = boneWeightArray.MC2ToRawBytes();
                sdata.localPositions = originalMesh.vertices;
                sdata.localNormals = originalMesh.normals;

                // Bone
                sdata.boneConnectionMode = boneConnectionMode;

                // Common
                sdata.renderTransformIndex = renderTransformIndex;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }

            return sdata;
        }

        public static RenderSetupData ShareDeserialize(ShareSerializationData sdata)
        {
            var setup = new RenderSetupData();
            setup.isManaged = true;

            try
            {
                setup.name = sdata.name;
                setup.setupType = sdata.setupType;

                // Mesh
                setup.originalMesh = sdata.originalMesh;
                setup.vertexCount = sdata.vertexCount;
                setup.hasSkinnedMesh = sdata.hasSkinnedMesh;
                setup.hasBoneWeight = sdata.hasBoneWeight;
                setup.skinRootBoneIndex = sdata.skinRootBoneIndex;
                setup.skinBoneCount = sdata.skinBoneCount;
                setup.bindPoseList = new List<Matrix4x4>(sdata.bindPoseList);
                setup.bonesPerVertexArray = NativeArrayExtensions.MC2FromRawBytes<byte>(sdata.bonesPerVertexArray, Allocator.Persistent);
                setup.boneWeightArray = NativeArrayExtensions.MC2FromRawBytes<BoneWeight1>(sdata.boneWeightArray, Allocator.Persistent);

                // PreBuildではmeshDataArrayを生成しない
                // その代わりに保存したlocalPositions/Normalsを復元する
                setup.localPositions = new NativeArray<Vector3>(sdata.localPositions, Allocator.Persistent);
                setup.localNormals = new NativeArray<Vector3>(sdata.localNormals, Allocator.Persistent);

                // Bone
                setup.boneConnectionMode = sdata.boneConnectionMode;

                // Common
                setup.renderTransformIndex = sdata.renderTransformIndex;

                setup.result.SetSuccess();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                setup.result.SetError(Define.Result.PreBuild_InvalidRenderSetupData);
            }

            return setup;
        }

        //=========================================================================================
        /// <summary>
        /// PreBuild固有部分の保存データ
        /// </summary>
        [System.Serializable]
        public class UniqueSerializationData : ITransform
        {
            public ResultCode result;

            // Mesh ---------------------------------------------------------------
            public Renderer renderer;
            public SkinnedMeshRenderer skinRenderer;
            public MeshFilter meshFilter;
            public Mesh originalMesh;

            // Common -------------------------------------------------------------
            public List<Transform> transformList;

            public void GetUsedTransform(HashSet<Transform> transformSet)
            {
                transformList?.ForEach(x =>
                {
                    if (x)
                        transformSet.Add(x);
                });
            }

            public void ReplaceTransform(Dictionary<int, Transform> replaceDict)
            {
                if (transformList != null)
                {
                    for (int i = 0; i < transformList.Count; i++)
                    {
                        var t = transformList[i];
                        if (t)
                        {
                            int id = t.GetInstanceID();
                            if (id != 0 && replaceDict.ContainsKey(id))
                            {
                                transformList[i] = replaceDict[id];
                            }
                        }
                    }
                }
            }
        }

        public UniqueSerializationData UniqueSerialize()
        {
            var sdata = new UniqueSerializationData();
            try
            {
                sdata.result = result;

                // Mesh
                sdata.renderer = renderer;
                sdata.skinRenderer = skinRenderer;
                sdata.meshFilter = meshFilter;
                sdata.originalMesh = originalMesh;

                // Common
                sdata.transformList = new List<Transform>(transformList);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }

            return sdata;
        }
    }
}
