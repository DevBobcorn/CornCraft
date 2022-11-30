// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using System.Collections.Generic;
using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// クロス選択データ
    /// </summary>
    [System.Serializable]
    public class SelectionData : ShareDataObject
    {
        /// <summary>
        /// データバージョン
        /// </summary>
        private const int DATA_VERSION = 2;

        /// <summary>
        /// 頂点選択データタイプ
        /// </summary>
        public const int Invalid = 0;
        public const int Move = 1;
        public const int Fixed = 2;
        public const int Extend = 3; // 固定としてマークするがローテーションライン計算からは除外する

        /// <summary>
        /// レンダーデフォーマーごとの選択データ
        /// </summary>
        [System.Serializable]
        public class DeformerSelection : IDataHash
        {
            /// <summary>
            /// レンダーデフォーマーの頂点と１対１に対応
            /// </summary>
            public List<int> selectData = new List<int>();

            /// <summary>
            /// 頂点ハッシュリスト(オプション)
            /// </summary>
            public List<ulong> vertexHashList = new List<ulong>();

            public int GetDataHash()
            {
                return selectData.GetDataHash();
            }

            public bool Compare(DeformerSelection data)
            {
                if (selectData.Count != data.selectData.Count)
                    return false;
                for (int i = 0; i < selectData.Count; i++)
                {
                    if (selectData[i] != data.selectData[i])
                        return false;
                }

                if (vertexHashList.Count != data.vertexHashList.Count)
                    return false;
                for (int i = 0; i < vertexHashList.Count; i++)
                {
                    if (vertexHashList[i] != data.vertexHashList[i])
                        return false;
                }

                return true;
            }
        }
        public List<DeformerSelection> selectionList = new List<DeformerSelection>();

        //=========================================================================================
        public int DeformerCount
        {
            get
            {
                return selectionList.Count;
            }
        }

        //=========================================================================================
        /// <summary>
        /// データハッシュ計算
        /// </summary>
        /// <returns></returns>
        public override int GetDataHash()
        {
            int hash = 0;
            hash += selectionList.GetDataHash();
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

            if (selectionList.Count == 0)
                return Define.Error.SelectionCountZero;

            return Define.Error.None;
        }

        //=========================================================================================
        /// <summary>
        /// 引数の選択データの内容を比較する
        /// </summary>
        /// <param name="sel"></param>
        /// <returns></returns>
        public bool Compare(SelectionData sel)
        {
            if (selectionList.Count != sel.selectionList.Count)
                return false;

            for (int i = 0; i < selectionList.Count; i++)
            {
                if (selectionList[i].Compare(sel.selectionList[i]) == false)
                    return false;
            }

            return true;
        }


        /// <summary>
        /// メッシュデータの各頂点の選択情報を取得する
        /// </summary>
        /// <param name="meshData"></param>
        /// <returns></returns>
        public List<int> GetSelectionData(MeshData meshData, List<MeshData> childMeshDataList)
        {
            List<int> selects = new List<int>();

            if (meshData != null)
            {
                // 親頂点に影響する子頂点情報
                Dictionary<int, List<uint>> dict = meshData.GetVirtualToChildVertexDict();

                // 現在の選択データの頂点ハッシュ辞書リスト
                var hashList = GetSelectionVertexHashList();

                int vcnt = meshData.VertexCount;
                for (int i = 0; i < vcnt; i++)
                {
                    int data = GetSelection(meshData, i, dict, childMeshDataList, hashList);
                    selects.Add(data);
                }
            }
            else
            {
                // そのまま
                if (selectionList.Count > 0)
                {
                    selects = new List<int>(selectionList[0].selectData);
                }
            }

            return selects;
        }

        /// <summary>
        /// メッシュデータの指定インデクスの選択情報を取得する
        /// </summary>
        /// <param name="meshData"></param>
        /// <param name="vindex"></param>
        /// <returns></returns>
        private int GetSelection(MeshData meshData, int vindex, Dictionary<int, List<uint>> dict, List<MeshData> childMeshDataList, List<Dictionary<ulong, int>> hashList)
        {
            int data = Invalid;

            // セレクションデータ読み込み
            if (meshData != null && meshData.ChildCount > 0)
            {
                // 親頂点に影響する子頂点情報から取得
                if (dict.ContainsKey(vindex))
                {
                    foreach (var pack in dict[vindex])
                    {
                        int cmindex = DataUtility.Unpack16Hi(pack);
                        int cvindex = DataUtility.Unpack16Low(pack);

                        if (cmindex < selectionList.Count && cvindex < selectionList[cmindex].selectData.Count)
                        {
                            // 頂点ハッシュがある場合はハッシュからインデックスを取得する
                            // 現在メッシュの頂点ハッシュ
                            ulong vhash = 0;
                            if (childMeshDataList != null && cmindex < childMeshDataList.Count)
                            {
                                var cmdata = childMeshDataList[cmindex];
                                if (cmdata != null && cvindex < cmdata.VertexHashCount)
                                {
                                    vhash = cmdata.vertexHashList[cvindex];
                                }
                            }

                            // セレクションデータに頂点ハッシュが記録されているならば照合する
                            if (vhash != 0 && cmindex < hashList.Count)
                            {
                                if (hashList[cmindex].ContainsKey(vhash))
                                {
                                    // ハッシュ値に紐づく頂点ペイントデータに入れ替える
                                    cvindex = hashList[cmindex][vhash];
                                }
                            }

                            data = Mathf.Max(selectionList[cmindex].selectData[cvindex], data);
                        }
                    }
                }
            }
            else
            {
                // そのまま
                int dindex = 0;
                if (dindex < selectionList.Count)
                {
                    if (vindex < selectionList[dindex].selectData.Count)
                        data = selectionList[dindex].selectData[vindex];
                }
            }

            return data;
        }

        /// <summary>
        /// メッシュ頂点の選択データを設定する
        /// </summary>
        /// <param name="meshData"></param>
        /// <param name="selects"></param>
        public void SetSelectionData(MeshData meshData, List<int> selects, List<MeshData> childMeshDataList)
        {
            // 選択データ初期化
            selectionList.Clear();
            if (meshData != null && meshData.ChildCount > 0)
            {
                for (int i = 0; i < meshData.ChildCount; i++)
                {
                    var dsel = new DeformerSelection();
                    int cvcnt = meshData.childDataList[i].VertexCount;
                    for (int j = 0; j < cvcnt; j++)
                    {
                        dsel.selectData.Add(Invalid);
                        dsel.vertexHashList.Add(0); // ハッシュ0=無効
                    }

                    selectionList.Add(dsel);
                }
            }
            else
            {
                // そのまま
                var dsel = new DeformerSelection();
                int cvcnt = selects.Count;
                for (int j = 0; j < cvcnt; j++)
                {
                    dsel.selectData.Add(Invalid);
                    dsel.vertexHashList.Add(0); // ハッシュ0=無効
                }

                selectionList.Add(dsel);
            }

            // 選択データに追加
            for (int i = 0; i < selects.Count; i++)
            {
                int data = selects[i];
                if (meshData != null && meshData.ChildCount > 0)
                {
                    // 親頂点に影響する子頂点情報
                    Dictionary<int, List<uint>> dict = meshData.GetVirtualToChildVertexDict();

                    // 親頂点に影響する子頂点に記録
                    if (dict.ContainsKey(i))
                    {
                        foreach (var pack in dict[i])
                        {
                            int cmindex = DataUtility.Unpack16Hi(pack);
                            int cvindex = DataUtility.Unpack16Low(pack);

                            selectionList[cmindex].selectData[cvindex] = data;

                            // 頂点ハッシュも記録
                            if (cmindex < childMeshDataList.Count)
                            {
                                var cmdata = childMeshDataList[cmindex];
                                if (cmdata != null && cvindex < cmdata.VertexHashCount)
                                {
                                    selectionList[cmindex].vertexHashList[cvindex] = cmdata.vertexHashList[cvindex];
                                }
                            }
                        }
                    }
                }
                else
                {
                    // そのまま
                    selectionList[0].selectData[i] = data;
                }
            }

            // データハッシュ設定
            CreateVerifyData();
        }

        /// <summary>
        /// セレクションデータに格納されている子メッシュの頂点ハッシュを辞書にして返す
        /// </summary>
        /// <param name="childMeshDataList"></param>
        /// <returns></returns>
        private List<Dictionary<ulong, int>> GetSelectionVertexHashList()
        {
            var hashList = new List<Dictionary<ulong, int>>();

            foreach (var sel in selectionList)
            {
                Dictionary<ulong, int> hashDict = new Dictionary<ulong, int>();

                for (int i = 0; i < sel.vertexHashList.Count; i++)
                {
                    hashDict[sel.vertexHashList[i]] = i;
                }

                hashList.Add(hashDict);
            }

            return hashList;
        }
    }
}
