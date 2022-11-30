// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using System.Collections.Generic;
using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// 仮想メッシュデータ
    /// データは必要な場合のみセットされる
    /// </summary>
    [System.Serializable]
    public class MeshData : ShareDataObject
    {
        /// <summary>
        /// データバージョン
        /// </summary>
        private const int DATA_VERSION = 2;

        /// <summary>
        /// 頂点ウエイト情報
        /// </summary>
        [System.Serializable]
        public struct VertexWeight
        {
            public Vector3 localPos;
            public Vector3 localNor;
            public Vector3 localTan;
            public int parentIndex;
            public float weight;
        }

        /// <summary>
        /// スキニングメッシュかどうか
        /// </summary>
        public bool isSkinning;

        /// <summary>
        /// 頂点数（必須）
        /// </summary>
        public int vertexCount;

        /// <summary>
        /// 頂点ごとのウエイト数とウエイト情報スタートインデックス
        /// 上位4bit = ウエイト数
        /// 下位28bit = スタートインデックス
        /// </summary>
        public uint[] vertexInfoList;

        /// <summary>
        /// 頂点ウエイトリスト
        /// </summary>
        public VertexWeight[] vertexWeightList;

        /// <summary>
        /// 頂点ハッシュデータ（オプション）
        /// </summary>
        public ulong[] vertexHashList;

        /// <summary>
        /// UVリスト（接線再計算用）
        /// </summary>
        public Vector2[] uvList;

        /// <summary>
        /// ライン数
        /// </summary>
        public int lineCount;

        /// <summary>
        /// ライン構成リスト（ライン数ｘ２）
        /// </summary>
        public int[] lineList;

        /// <summary>
        /// トライアングル数
        /// </summary>
        public int triangleCount;

        /// <summary>
        /// トライアングル構成リスト（トライアングル数ｘ３）
        /// </summary>
        public int[] triangleList;

        /// <summary>
        /// ボーン数
        /// </summary>
        public int boneCount;

        /// <summary>
        /// 仮想メッシュ頂点が属するトライアングル情報
        /// 上位8bit = 接続トライアングル数
        /// 下位24bit = 接続トライアングルリスト(vertexToTriangleIndexList)の開始インデックス
        /// </summary>
        public uint[] vertexToTriangleInfoList;

        /// <summary>
        /// 仮想メッシュ頂点が属するトライアングルインデックスリスト
        /// これは頂点数とは一致しない
        /// </summary>
        public int[] vertexToTriangleIndexList;

        /// <summary>
        /// 子メッシュの情報
        /// </summary>
        [System.Serializable]
        public class ChildData : IDataHash
        {
            /// <summary>
            /// 子メッシュデータのハッシュ
            /// </summary>
            public int childDataHash;

            /// <summary>
            /// 頂点数
            /// </summary>
            public int vertexCount;

            /// <summary>
            /// 頂点ごとのウエイト数とウエイト情報スタートインデックス
            /// 上位4bit = ウエイト数
            /// 下位28bit = スタートインデックス
            /// </summary>
            public uint[] vertexInfoList;

            /// <summary>
            /// 頂点ウエイトリスト
            /// </summary>
            public VertexWeight[] vertexWeightList;

            /// <summary>
            /// 元々属していた親仮想メッシュ頂点インデックス（エディット用）
            /// </summary>
            public int[] parentIndexList;

            public int VertexCount
            {
                get
                {
                    return vertexCount;
                }
            }

            public int GetDataHash()
            {
                int hash = 0;
                hash += childDataHash;
                hash += vertexCount.GetDataHash();
                return hash;
            }
        }
        public List<ChildData> childDataList = new List<ChildData>();

        /// <summary>
        /// 設計時スケール
        /// </summary>
        public Vector3 baseScale;

        //=========================================================================================
        /// <summary>
        /// 頂点数
        /// </summary>
        public int VertexCount
        {
            get
            {
                return vertexCount;
            }
        }

        public int VertexHashCount
        {
            get
            {
                if (vertexHashList != null)
                    return vertexHashList.Length;
                return 0;
            }
        }

        public int WeightCount
        {
            get
            {
                if (vertexWeightList != null)
                    return vertexWeightList.Length;
                return 0;
            }
        }

        /// <summary>
        /// ライン数
        /// </summary>
        public int LineCount
        {
            get
            {
                return lineCount;
            }
        }

        /// <summary>
        /// トライアングル数
        /// </summary>
        public int TriangleCount
        {
            get
            {
                return triangleCount;
            }
        }

        /// <summary>
        /// ボーン数
        /// </summary>
        public int BoneCount
        {
            get
            {
                return boneCount;
            }
        }

        /// <summary>
        /// 子の数
        /// </summary>
        public int ChildCount
        {
            get
            {
                return childDataList.Count;
            }
        }

        //=========================================================================================
        /// <summary>
        /// データを識別するハッシュコードを作成して返す
        /// </summary>
        /// <returns></returns>
        public override int GetDataHash()
        {
            int hash = 0;
            hash += isSkinning.GetDataHash();
            hash += vertexCount.GetDataHash();
            hash += lineCount.GetDataHash();
            hash += triangleCount.GetDataHash();
            hash += boneCount.GetDataHash();
            hash += ChildCount.GetDataHash();

            hash += vertexInfoList.GetDataCountHash();
            hash += vertexWeightList.GetDataCountHash();
            hash += uvList.GetDataCountHash();
            hash += lineList.GetDataCountHash();
            hash += triangleList.GetDataCountHash();

            hash += childDataList.GetDataHash();

            // option
            if (vertexHashList != null && vertexHashList.Length > 0)
                hash += vertexHashList.GetDataCountHash();

            return hash;
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
            if (vertexCount == 0)
                return Define.Error.VertexCountZero;

            return Define.Error.None;
        }

        //=========================================================================================
        /// <summary>
        /// 仮想メッシュ頂点に対する最も影響が強い子頂点を辞書にして返す
        /// 仮想メッシュ頂点に影響する子頂点を辞書にして返す
        /// 子頂点はuintで上位16bitが子メッシュインデックス、下位16bitが子頂点インデックスとなる
        /// </summary>
        /// <returns></returns>
        public Dictionary<int, List<uint>> GetVirtualToChildVertexDict()
        {
            var dict = new Dictionary<int, List<uint>>();

            for (int i = 0; i < VertexCount; i++)
                dict.Add(i, new List<uint>());

            for (int i = 0; i < childDataList.Count; i++)
            {
                var cdata = childDataList[i];

                for (int j = 0; j < cdata.VertexCount; j++)
                {
                    if (j < cdata.parentIndexList.Length)
                    {
                        int mvindex = cdata.parentIndexList[j];
                        dict[mvindex].Add(DataUtility.Pack16(i, j));
                    }
                }
            }

            return dict;
        }

        /// <summary>
        /// クロスメッシュ用に頂点セレクションデータを拡張する
        /// </summary>
        /// <param name="originalSelection"></param>
        /// <param name="extendNext">無効頂点の隣接が移動／固定頂点なら拡張に変更する</param>
        /// <param name="extendWeight">移動／固定頂点に影響する子頂点に接続する無効頂点は拡張に変更する</param>
        /// <returns></returns>
        public List<int> ExtendSelection(List<int> originalSelection, bool extendNext, bool extendWeight)
        {
            var selection = new List<int>(originalSelection);

            // （１）無効頂点の隣接が移動／固定頂点なら拡張に変更する
            if (extendNext)
            {
                // ライン／トライアングル情報を分解して各頂点ごとの接続頂点をリスト化
                List<HashSet<int>> vlink = MeshUtility.GetTriangleToVertexLinkList(vertexCount, new List<int>(lineList), new List<int>(triangleList));

                // 無効頂点の隣接が移動／固定頂点なら拡張に変更する
                List<int> changeIndexList = new List<int>();
                for (int i = 0; i < vertexCount; i++)
                {
                    if (selection[i] == SelectionData.Invalid)
                    {
                        // 隣接を調べる
                        var vset = vlink[i];
                        foreach (var vindex in vset)
                        {
                            if (selection[vindex] == SelectionData.Move || selection[vindex] == SelectionData.Fixed)
                            {
                                // 拡張に変更する
                                selection[i] = SelectionData.Extend;
                            }
                        }
                    }
                }
            }

            // （２）移動／固定頂点に影響する子頂点に接続する無効頂点は拡張に変更する
            if (extendWeight)
            {
                var extendSet = new HashSet<int>();
                foreach (var cdata in childDataList)
                {
                    for (int i = 0; i < cdata.VertexCount; i++)
                    {
                        // 頂点のウエイト数とウエイト開始インデックス
                        uint pack = cdata.vertexInfoList[i];
                        int wcnt = DataUtility.Unpack4_28Hi(pack);
                        int wstart = DataUtility.Unpack4_28Low(pack);

                        bool link = false;
                        for (int j = 0; j < wcnt; j++)
                        {
                            int sindex = wstart + j;
                            var vw = cdata.vertexWeightList[sindex];

                            // この子頂点が移動／固定頂点に接続しているか判定する
                            if (vw.weight > 0.0f && (selection[vw.parentIndex] == SelectionData.Move || selection[vw.parentIndex] == SelectionData.Fixed))
                                link = true;
                        }

                        if (link)
                        {
                            for (int j = 0; j < wcnt; j++)
                            {
                                int sindex = wstart + j;
                                var vw = cdata.vertexWeightList[sindex];

                                // この子頂点が接続する頂点がInvalidの場合はExtendに変更する
                                if (vw.weight > 0.0f && selection[vw.parentIndex] == SelectionData.Invalid)
                                    extendSet.Add(vw.parentIndex);
                            }
                        }
                    }
                }
                foreach (var vindex in extendSet)
                {
                    selection[vindex] = SelectionData.Extend;
                }
            }

            return selection;
        }
    }
}
