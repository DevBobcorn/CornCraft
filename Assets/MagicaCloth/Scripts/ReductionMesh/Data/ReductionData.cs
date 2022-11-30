// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using System.Collections.Generic;
using UnityEngine;

namespace MagicaReductionMesh
{
    /// <summary>
    /// リダクション
    /// </summary>
    public class ReductionData : ReductionMeshAccess
    {
        //=========================================================================================
        /// <summary>
        /// ゼロ距離の頂点をマージする
        /// </summary>
        public void ReductionZeroDistance(float radius = 0.0001f)
        {
            var reduction = new NearPointReduction(radius);
            reduction.Create(MeshData);
            reduction.Reduction();
        }

        //=========================================================================================
        /// <summary>
        /// 指定半径の頂点をまとめる
        /// </summary>
        /// <param name="radius"></param>
        public void ReductionRadius(float radius)
        {
            var reduction = new NearPointReduction(radius);
            reduction.Create(MeshData);
            reduction.Reduction();
        }

        //=========================================================================================
        /// <summary>
        /// ポリゴンの接続のみを利用した距離マージ
        /// </summary>
        /// <param name="length"></param>
        public void ReductionPolygonLink(float length)
        {
            var reduction = new PolygonLinkReduction(length);
            reduction.Create(MeshData);
            reduction.Reduction();
        }

        //=========================================================================================
        /// <summary>
        /// 未使用のボーンを削除する
        /// </summary>
        public void ReductionBone()
        {
            var boneSet = new HashSet<Transform>();

            foreach (var sv in MeshData.shareVertexList)
            {
                for (int i = 0; i < sv.boneWeightList.Count; i++)
                {
                    var w = sv.boneWeightList[i];
                    if (w.boneWeight > 0.0f)
                    {
                        boneSet.Add(MeshData.boneList[w.boneIndex]);
                    }
                }
            }

            // 新しいボーンリスト
            var newBoneList = new List<Transform>(boneSet);

            // ボーンインデックスを変更する
            foreach (var sv in MeshData.shareVertexList)
            {
                for (int i = 0; i < sv.boneWeightList.Count; i++)
                {
                    var w = sv.boneWeightList[i];
                    if (w.boneWeight > 0.0f)
                    {
                        w.boneIndex = newBoneList.IndexOf(MeshData.boneList[w.boneIndex]);
                    }
                }
            }

            // 新しいボーンリストに変更
            MeshData.boneList = newBoneList;
        }
    }
}
