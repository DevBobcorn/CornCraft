// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// Serialize data (2)
    /// Parts that cannot be exported externally.
    /// </summary>
    [System.Serializable]
    public class ClothSerializeData2 : IDataValidate, IValid
    {
        /// <summary>
        /// vertex paint data.
        /// </summary>
        [SerializeField]
        public SelectionData selectionData = new SelectionData();

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
    }
}
