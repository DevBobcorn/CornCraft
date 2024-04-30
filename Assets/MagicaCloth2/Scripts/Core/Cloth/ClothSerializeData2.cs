// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System.Collections.Generic;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// Serialize data (2)
    /// Parts that cannot be exported externally.
    /// </summary>
    [System.Serializable]
    public class ClothSerializeData2 : IDataValidate, IValid, ITransform
    {
        /// <summary>
        /// 頂点ペイントデータ
        /// vertex paint data.
        /// </summary>
        [SerializeField]
        public SelectionData selectionData = new SelectionData();

        /// <summary>
        /// Transformと頂点属性辞書データ
        /// 実行時でのBoneCloth/BoneSpring作成時にはこの辞書にTransformと頂点属性のペアを格納することで頂点ペイントデータの代わりにすることができます。
        /// Transform and vertex attribute dictionary data.
        /// When creating BoneCloth/BoneSpring at runtime, you can store Transform and vertex attribute pairs in this dictionary and use it instead of vertex paint data.
        /// </summary>
        public Dictionary<Transform, VertexAttribute> boneAttributeDict = new Dictionary<Transform, VertexAttribute>();

        /// <summary>
        /// PreBuild Data.
        /// </summary>
        public PreBuildSerializeData preBuildData = new PreBuildSerializeData();

        //=========================================================================================
        public ClothSerializeData2()
        {
        }

        /// <summary>
        /// クロスを構築するための最低限の情報が揃っているかチェックする
        /// Check if you have the minimum information to construct the cloth.
        /// </summary>
        /// <returns></returns>
        public bool IsValid()
        {
            return true;
        }

        public void DataValidate()
        {
        }

        /// <summary>
        /// エディタメッシュの更新を判定するためのハッシュコード
        /// Hashcode for determining editor mesh updates.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            int hash = 0;
            return hash;
        }

        public void GetUsedTransform(HashSet<Transform> transformSet)
        {
            preBuildData.GetUsedTransform(transformSet);
        }

        public void ReplaceTransform(Dictionary<int, Transform> replaceDict)
        {
            preBuildData.ReplaceTransform(replaceDict);
        }
    }
}
