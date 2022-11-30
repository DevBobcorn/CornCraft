// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using System.Collections.Generic;
using UnityEngine;

namespace MagicaReductionMesh
{
    /// <summary>
    /// 最終メッシュデータ
    /// ※シリアライズ設定していますが基本的には使い捨てを想定して作成されています
    /// </summary>
    [System.Serializable]
    public class FinalData
    {
        //=========================================================================================
        // マージメッシュ情報
        public List<Vector3> vertices = new List<Vector3>();
        public List<Vector3> normals = new List<Vector3>();
        public List<Vector4> tangents = new List<Vector4>();
        public List<Vector2> uvs = new List<Vector2>();
        public List<BoneWeight> boneWeights = new List<BoneWeight>();
        public List<Matrix4x4> bindPoses = new List<Matrix4x4>();
        public List<Transform> bones = new List<Transform>();
        public List<int> lines = new List<int>();
        public List<int> triangles = new List<int>();
        public List<int> tetras = new List<int>();
        public List<float> tetraSizes = new List<float>();

        /// <summary>
        /// マージ頂点ごとのバインドポーズ
        /// </summary>
        public List<Matrix4x4> vertexBindPoses = new List<Matrix4x4>();

        /// <summary>
        /// マージ頂点が影響を与える子メッシュ頂点リスト
        /// データはuintでパッキングされ、上位16bitが[子メッシュインデックス]、下位16bitが[子頂点インデックス]
        /// </summary>
        [System.Serializable]
        public class MeshIndexData
        {
            public List<uint> meshIndexPackList = new List<uint>();
        }
        public List<MeshIndexData> vertexToMeshIndexList = new List<MeshIndexData>();

        /// <summary>
        /// マージ頂点が所属するトライアングル情報リスト
        /// </summary>
        public List<int> vertexToTriangleCountList = new List<int>();   // 所属トライアングルの数
        public List<int> vertexToTriangleStartList = new List<int>();   // vertexToTriangleIndexListの開始位置
        public List<int> vertexToTriangleIndexList = new List<int>();   // 所属トライアングルインデックスリスト（これは頂点数とは一致しない）

        //=========================================================================================
        /// <summary>
        /// 子メッシュ情報
        /// </summary>
        [System.Serializable]
        public class MeshInfo
        {
            public int meshIndex;
            public Mesh mesh;

            public List<Vector3> vertices = new List<Vector3>();
            public List<Vector3> normals = new List<Vector3>();
            public List<Vector4> tangents = new List<Vector4>();
            public List<BoneWeight> boneWeights = new List<BoneWeight>();

            /// <summary>
            /// 元々属していた親マージ頂点インデックス
            /// </summary>
            public List<int> parents = new List<int>();

            /// <summary>
            /// 頂点数
            /// </summary>
            public int VertexCount
            {
                get
                {
                    return vertices.Count;
                }
            }
        }
        public List<MeshInfo> meshList = new List<MeshInfo>();

        //=========================================================================================
        /// <summary>
        /// データが有効か判定する
        /// </summary>
        public bool IsValid
        {
            get
            {
                return vertices.Count > 0;
            }
        }

        /// <summary>
        /// 頂点数
        /// </summary>
        public int VertexCount
        {
            get
            {
                return vertices.Count;
            }
        }

        /// <summary>
        /// ライン数
        /// </summary>
        public int LineCount
        {
            get
            {
                return lines.Count / 2;
            }
        }

        /// <summary>
        /// トライアングル数
        /// </summary>
        public int TriangleCount
        {
            get
            {
                return triangles.Count / 3;
            }
        }

        /// <summary>
        /// テトラ数
        /// </summary>
        public int TetraCount
        {
            get
            {
                return tetras.Count / 4;
            }
        }

        /// <summary>
        /// ボーン数
        /// </summary>
        public int BoneCount
        {
            get
            {
                return bones.Count;
            }
        }

        /// <summary>
        /// スキンメッシュかどうか
        /// </summary>
        public bool IsSkinning
        {
            get
            {
                //return bones.Count > 1;
                return true; // 基本的にすべてスキニング
            }
        }

        /// <summary>
        /// 子メッシュ数
        /// </summary>
        public int MeshCount
        {
            get
            {
                return meshList.Count;
            }
        }
    }
}
