// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using System.Collections.Generic;
using UnityEngine;

namespace MagicaReductionMesh
{
    /// <summary>
    /// リダクションメッシュ
    /// ・複数のメッシュを１つにマージ
    /// ・リダクション機能
    /// ・加工後のメッシュデータ取得
    /// </summary>
    public class ReductionMesh
    {
        /// <summary>
        /// 結合頂点ウエイト計算方式
        /// </summary>
        public enum ReductionWeightMode
        {
            /// <summary>
            /// 共有頂点からの距離によるウエイト付（従来方式）
            /// </summary>
            Distance = 0,

            /// <summary>
            /// 共有頂点に属するメッシュ頂点ウエイトの平均値
            /// </summary>
            Average = 1,

            /// <summary>
            /// 共有頂点からの距離によりウエイト（改良版）
            /// </summary>
            DistanceAverage = 2,
        }
        public ReductionWeightMode WeightMode { get; set; } = ReductionWeightMode.Distance;


        //=========================================================================================
        private MeshData meshData = new MeshData();

        private ReductionData reductionData = new ReductionData();

        private DebugData debugData = new DebugData();

        //=========================================================================================
        public MeshData MeshData
        {
            get
            {
                meshData.SetParent(this);
                return meshData;
            }
        }

        public ReductionData ReductionData
        {
            get
            {
                reductionData.SetParent(this);
                return reductionData;
            }
        }

        public DebugData DebugData
        {
            get
            {
                debugData.SetParent(this);
                return debugData;
            }
        }

        //=========================================================================================
        /// <summary>
        /// メッシュを追加する
        /// 登録したメッシュインデックスを返す
        /// </summary>
        /// <param name="isSkinning"></param>
        /// <param name="mesh"></param>
        /// <param name="bones"></param>
        public int AddMesh(bool isSkinning, Mesh mesh, List<Transform> bones, Matrix4x4[] bindPoseList, BoneWeight[] boneWeightList)
        {
            return MeshData.AddMesh(isSkinning, mesh, bones, bindPoseList, boneWeightList);
        }

        /// <summary>
        /// メッシュを追加する
        /// 登録したメッシュインデックスを返す(-1=エラー)
        /// </summary>
        /// <param name="ren"></param>
        public int AddMesh(Renderer ren)
        {
            if (ren == null)
            {
                Debug.LogError("Renderer is NUll!");
                return -1;
            }

            if (ren is SkinnedMeshRenderer)
            {
                var sren = ren as SkinnedMeshRenderer;
                return MeshData.AddMesh(true, sren.sharedMesh, new List<Transform>(sren.bones), sren.sharedMesh.bindposes, sren.sharedMesh.boneWeights);
            }
            else
            {
                var mfilter = ren.GetComponent<MeshFilter>();
                var bones = new List<Transform>();
                bones.Add(ren.transform);
                return MeshData.AddMesh(false, mfilter.sharedMesh, bones, null, null);
            }
        }

        /// <summary>
        /// メッシュを追加する
        /// 登録したメッシュインデックスを返す
        /// </summary>
        /// <param name="root"></param>
        /// <param name="posList"></param>
        /// <param name="norList"></param>
        /// <param name="tanList"></param>
        /// <param name="uvList"></param>
        /// <returns></returns>
        public int AddMesh(Transform root, List<Vector3> posList, List<Vector3> norList = null, List<Vector4> tanList = null, List<Vector2> uvList = null, List<int> triangleList = null)
        {
            return MeshData.AddMesh(root, posList, norList, tanList, uvList, triangleList);
        }

        /// <summary>
        /// リダクション実行
        /// </summary>
        /// <param name="zeroRadius">重複頂点のマージ距離(0.0f=実行しない)</param>
        /// <param name="radius">周辺頂点のマージ距離(0.0f=実行しない)</param>
        /// <param name="polygonLength">ポリゴン接続のマージ距離(0.0f=実行しない)</param>
        public void Reduction(float zeroRadius, float radius, float polygonLength, bool createTetra)
        {
            // ゼロ距離頂点をマージする
            if (zeroRadius > 0.0f)
                ReductionData.ReductionZeroDistance(zeroRadius);

            // 範囲内の頂点をマージする
            if (radius > 0.0f)
                ReductionData.ReductionRadius(radius);

            // ポリゴン接続から範囲内の頂点をマージする
            if (polygonLength > 0.0f)
                ReductionData.ReductionPolygonLink(polygonLength);

            // メッシュデータ更新
            MeshData.UpdateMeshData(createTetra);

            // 未使用ボーンの削除
            ReductionData.ReductionBone();
        }

        /// <summary>
        /// 最終メッシュデータを計算して返す
        /// 子頂点の親頂点に対するスキニングが不要な場合は(weightLength=0)に設定します。
        /// </summary>
        /// <param name="root">メッシュの基準トランスフォーム（この姿勢を元にローカル座標変換される）</param>
        public FinalData GetFinalData(Transform root)
        {
            return MeshData.GetFinalData(root);
        }

    }
}
