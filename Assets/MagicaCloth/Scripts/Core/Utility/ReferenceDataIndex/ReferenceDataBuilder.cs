// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp

using System.Collections.Generic;

namespace MagicaCloth
{
    /// <summary>
    /// データとそれを参照するインデックスの関係をジョブシステムで扱いやすいように加工する
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ReferenceDataBuilder<T> where T : struct
    {
        private int indexCount;
        private List<T> dataList = new List<T>();
        private List<List<int>> indexToDataIndexList = new List<List<int>>();
        private List<List<int>> dataIndexToIndexList = new List<List<int>>();

        /// <summary>
        /// （１）初期化
        /// </summary>
        /// <param name="indexCount">最大のインデックス数</param>
        public void Init(int indexCount)
        {
            this.indexCount = indexCount;
            for (int i = 0; i < indexCount; i++)
            {
                indexToDataIndexList.Add(new List<int>());
            }
        }

        /// <summary>
        /// （２）データ追加
        /// データとそれを参照するインデックス（複数可）を指定する
        /// </summary>
        /// <param name="data">データ</param>
        /// <param name="indexes">データを参照するインデックスリスト</param>
        public void AddData(T data, params int[] indexes)
        {
            int dataIndex = dataList.Count;
            dataList.Add(data);
            dataIndexToIndexList.Add(new List<int>());

            foreach (var index in indexes)
            {
                indexToDataIndexList[index].Add(dataIndex);

                // 逆参照
                dataIndexToIndexList[dataIndex].Add(index);
            }
        }

        /// <summary>
        /// （３）加工データ取得
        /// 各インデックスが参照するデータのインデックス情報とデータ自体をそのインデックスに並び替えて返す
        /// （インデックスが１つのデータを参照する場合はこちら）
        /// </summary>
        /// <returns></returns>
        public (List<ReferenceDataIndex>, List<T>) GetDirectReferenceData()
        {
            var referenceDataList = new List<ReferenceDataIndex>();
            var sortDataList = new List<T>();

            // インデックスごとのデータ参照開始と数の情報を作成
            int start = 0;
            for (int i = 0; i < indexToDataIndexList.Count; i++)
            {
                var work = indexToDataIndexList[i];

                var refdata = new ReferenceDataIndex();
                refdata.startIndex = start;
                refdata.count = work.Count;
                referenceDataList.Add(refdata);

                // データは単純に参照順番で並べる
                foreach (var dataIndex in work)
                {
                    sortDataList.Add(dataList[dataIndex]);
                }

                start += work.Count;
            }
            //int count = start;

            return (referenceDataList, sortDataList);
        }

        /// <summary>
        /// （３）加工データ取得
        /// 各インデックスが参照するデータのインデックス情報と、それに対応するデータインデックスを一次元配列に格納したもの、
        /// および各データが参照するデータインデックス配列のインデックス情報を返す
        /// （インデックスが複数のデータを参照する場合はこちら）
        /// </summary>
        /// <returns></returns>
        public (List<ReferenceDataIndex>, List<int>, List<List<int>>) GetIndirectReferenceData()
        {
            var referenceDataList = new List<ReferenceDataIndex>();

            // インデックスごとのデータ参照開始と数の情報を作成
            int start = 0;
            for (int i = 0; i < indexToDataIndexList.Count; i++)
            {
                var work = indexToDataIndexList[i];

                var refdata = new ReferenceDataIndex();
                refdata.startIndex = start;
                refdata.count = work.Count;
                referenceDataList.Add(refdata);

                start += work.Count;
            }
            //int count = start;

            // インデックスごとの参照データインデックスを一次元配列にする
            List<int> dataIndexList = new List<int>();
            foreach (var work in indexToDataIndexList)
            {
                foreach (var dataIndex in work)
                {
                    dataIndexList.Add(dataIndex);
                }
            }

            // データのインデックスに対応する参照インデックス情報を作成
            List<List<int>> dataToDataIndexList = new List<List<int>>();
            for (int dataIndex = 0; dataIndex < dataIndexToIndexList.Count; dataIndex++)
            {
                var indexList = dataIndexToIndexList[dataIndex];
                var dataIndexIndexList = new List<int>();

                foreach (var index in indexList)
                {
                    start = referenceDataList[index].startIndex;
                    int dataIndexIndex = indexToDataIndexList[index].IndexOf(dataIndex);

                    dataIndexIndexList.Add(start + dataIndexIndex);
                }

                dataToDataIndexList.Add(dataIndexIndexList);
            }

            return (referenceDataList, dataIndexList, dataToDataIndexList);
        }
    }
}
