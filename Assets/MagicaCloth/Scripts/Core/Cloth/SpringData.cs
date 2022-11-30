// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp

using UnityEngine;

namespace MagicaCloth
{
    [System.Serializable]
    public class SpringData : ShareDataObject
    {
        /// <summary>
        /// データバージョン
        /// </summary>
        private const int DATA_VERSION = 2;

        /// <summary>
        /// 各デフォーマーの状態
        /// </summary>
        [System.Serializable]
        public class DeformerData : IDataHash
        {
            /// <summary>
            /// デフォーマーハッシュ
            /// </summary>
            public int deformerDataHash;

            /// <summary>
            /// デフォーマーの頂点数
            /// </summary>
            public int vertexCount;

            /// <summary>
            /// 使用頂点番号リスト
            /// </summary>
            public int[] useVertexIndexList;

            /// <summary>
            /// 使用頂点のウエイト値
            /// </summary>
            public float[] weightList;

            public int UseVertexCount
            {
                get
                {
                    if (useVertexIndexList != null)
                        return useVertexIndexList.Length;

                    return 0;
                }
            }

            public int GetDataHash()
            {
                int hash = 0;
                hash += deformerDataHash;
                hash += vertexCount.GetDataHash();
                hash += UseVertexCount.GetDataHash();
                return hash;
            }
        }

        public DeformerData deformerData;

        /// <summary>
        /// 設計時のスケール(WorldInfluenceのInfluenceTargetが基準)
        /// </summary>
        public Vector3 initScale;

        //=========================================================================================
        /// <summary>
        /// データハッシュ計算
        /// </summary>
        /// <returns></returns>
        public override int GetDataHash()
        {
            int hash = 0;
            if (deformerData != null)
                hash += deformerData.GetDataHash();
            return hash;
        }

        //=========================================================================================
        public int UseVertexCount
        {
            get
            {
                int cnt = 0;
                if (deformerData != null)
                    cnt += deformerData.UseVertexCount;

                return cnt;
            }
        }

        //=========================================================================================
        public override int GetVersion()
        {
            return DATA_VERSION;
        }

        /// <summary>
        /// 現在のデータが正常（実行できる状態）か返す
        /// </summary>
        /// <returns></returns>
        public override Define.Error VerifyData()
        {
            if (dataHash == 0)
                return Define.Error.InvalidDataHash;
            //if (dataVersion != GetVersion())
            //    return Define.Error.DataVersionMismatch;

            if (deformerData == null)
                return Define.Error.DeformerNull;

            return Define.Error.None;
        }
    }
}
