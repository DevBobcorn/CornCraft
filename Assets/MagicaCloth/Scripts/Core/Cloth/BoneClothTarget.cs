// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using System.Collections.Generic;
using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// ボーンクロスのターゲットトランスフォーム
    /// </summary>
    [System.Serializable]
    public class BoneClothTarget : IDataHash, IBoneReplace
    {
        /// <summary>
        /// ルートトランスフォーム
        /// </summary>
        [SerializeField]
        private List<Transform> rootList = new List<Transform>();

        /// <summary>
        /// 接続モード
        /// </summary>
        public enum ConnectionMode
        {
            Line = 0,
            MeshAutomatic = 1,
            MeshSequentialLoop = 2,
            MeshSequentialNoLoop = 3,
        }
        [SerializeField]
        private ConnectionMode connection = ConnectionMode.Line;

        /// <summary>
        /// メッシュ構築時に同一とみなす面角度（面法線方向に影響）
        /// </summary>
        [SerializeField]
        [Range(10.0f, 90.0f)]
        private float sameSurfaceAngle = 80.0f;

        //=========================================================================================
        /// <summary>
        /// ルートの親トランスフォームの登録インデックス
        /// </summary>
        private int[] parentIndexList = null;

        //=========================================================================================
        /// <summary>
        /// データを識別するハッシュコードを作成して返す
        /// </summary>
        /// <returns></returns>
        public int GetDataHash()
        {
            int hash = 0;
            hash += rootList.GetDataHash();
            return hash;
        }

        //=========================================================================================
        /// <summary>
        /// ルートトランスフォームの数
        /// </summary>
        public int RootCount
        {
            get
            {
                return rootList.Count;
            }
        }

        /// <summary>
        /// ルートトランスフォーム取得
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public Transform GetRoot(int index)
        {
            if (index < rootList.Count)
                return rootList[index];

            return null;
        }

        /// <summary>
        /// ルートトランスフォームのインデックスを返す。無い場合は(-1)
        /// </summary>
        /// <param name="root"></param>
        /// <returns></returns>
        public int GetRootIndex(Transform root)
        {
            return rootList.IndexOf(root);
        }

        /// <summary>
        /// ルートの親トランスフォームをすべて登録する
        /// </summary>
        public void AddParentTransform()
        {
            if (rootList.Count > 0)
            {
                HashSet<Transform> parentSet = new HashSet<Transform>();
                foreach (var t in rootList)
                {
                    if (t && t.parent)
                        parentSet.Add(t.parent);
                }

                parentIndexList = new int[parentSet.Count];

                int i = 0;
                foreach (var parent in parentSet)
                {
                    int index = -1;
                    if (parent)
                    {
                        index = MagicaPhysicsManager.Instance.Bone.AddBone(parent);
                    }
                    parentIndexList[i] = index;
                    i++;
                }
            }
        }

        /// <summary>
        /// ルートの親トランスフォームをすべて解除する
        /// </summary>
        public void RemoveParentTransform()
        {
            if (MagicaPhysicsManager.IsInstance())
            {
                if (parentIndexList != null && parentIndexList.Length > 0)
                {
                    for (int i = 0; i < parentIndexList.Length; i++)
                    {
                        var index = parentIndexList[i];
                        if (index >= 0)
                        {
                            MagicaPhysicsManager.Instance.Bone.RemoveBone(index);
                        }
                    }
                }
            }

            parentIndexList = null;
        }

        /// <summary>
        /// ルートの親トランスフォームの未来予測をリセットする
        /// </summary>
        public void ResetFuturePredictionParentTransform()
        {
            if (parentIndexList != null && parentIndexList.Length > 0)
            {
                for (int i = 0; i < parentIndexList.Length; i++)
                {
                    var index = parentIndexList[i];
                    if (index >= 0)
                    {
                        MagicaPhysicsManager.Instance.Bone.ResetFuturePrediction(index);
                    }
                }
            }
        }

        /// <summary>
        /// ボーンのUnityPhysics利用カウンタを増減させる
        /// </summary>
        /// <param name="sw"></param>
        public void ChangeUnityPhysicsCount(bool sw)
        {
            if (parentIndexList != null && parentIndexList.Length > 0)
            {
                for (int i = 0; i < parentIndexList.Length; i++)
                {
                    var index = parentIndexList[i];
                    if (index >= 0)
                    {
                        MagicaPhysicsManager.Instance.Bone.ChangeUnityPhysicsCount(index, sw);
                    }
                }
            }
        }

        /// <summary>
        /// 接続モード取得
        /// </summary>
        public ConnectionMode Connection
        {
            get
            {
                return connection;
            }
        }

        public float SameSurfaceAngle
        {
            get
            {
                return sameSurfaceAngle;
            }
        }

        public bool IsMeshConnection
        {
            get
            {
                return connection == ConnectionMode.MeshAutomatic || connection == ConnectionMode.MeshSequentialLoop || connection == ConnectionMode.MeshSequentialNoLoop;
            }
        }

        //=========================================================================================
        /// <summary>
        /// ボーン置換
        /// </summary>
        /// <param name="boneReplaceDict"></param>
        public void ReplaceBone<T>(Dictionary<T, Transform> boneReplaceDict) where T : class
        {
            for (int i = 0; i < rootList.Count; i++)
            {
                rootList[i] = MeshUtility.GetReplaceBone(rootList[i], boneReplaceDict);
            }
        }

        /// <summary>
        /// 現在使用しているボーンを格納して返す
        /// </summary>
        /// <returns></returns>
        public HashSet<Transform> GetUsedBones()
        {
            return new HashSet<Transform>(rootList);
        }
    }
}
