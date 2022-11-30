// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp

using System.Collections.Generic;
using UnityEngine;

namespace MagicaCloth
{
    [System.Serializable]
    public abstract class ShareDataObject : ScriptableObject, IDataVerify, IDataHash
    {
        [SerializeField]
        protected int dataHash;
        [SerializeField]
        protected int dataVersion;

        //=========================================================================================
        /// <summary>
        /// データを識別するハッシュコードを作成して返す
        /// </summary>
        /// <returns></returns>
        public abstract int GetDataHash();

        public int SaveDataHash
        {
            set
            {
                dataHash = value;
            }
            get
            {
                return dataHash;
            }
        }

        public int SaveDataVersion
        {
            set
            {
                dataVersion = value;
            }
            get
            {
                return dataVersion;
            }
        }


        //=========================================================================================
        /// <summary>
        /// データバージョンを取得する
        /// </summary>
        /// <returns></returns>
        public abstract int GetVersion();

        /// <summary>
        /// 現在のデータが正常（実行できる状態）か返す
        /// </summary>
        /// <returns></returns>
        public abstract Define.Error VerifyData();

        /// <summary>
        /// データを検証して結果を格納する
        /// </summary>
        /// <returns></returns>
        public virtual void CreateVerifyData()
        {
            dataHash = GetDataHash();
            dataVersion = GetVersion();
        }

        /// <summary>
        /// データ検証の結果テキストを取得する
        /// </summary>
        /// <returns></returns>
        public virtual string GetInformation()
        {
            return "No information.";
        }

        //=========================================================================================
        /// <summary>
        /// 共通データ作成ユーティリティ（無い場合は作成する）
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="shareData"></param>
        /// <param name="rebuild"></param>
        /// <returns>新規作成／もしくは既存のクリアされた共有データを返す</returns>
        public static T CreateShareData<T>(string dataName) where T : ShareDataObject
        {
            // 新規作成
            T shareData = CreateInstance<T>();

            // 名前
            shareData.name = dataName;

            return shareData;
        }

        /// <summary>
        /// リストからNullや重複を削除する
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <returns>リストに変更があった場合はtrue</returns>
        public static bool RemoveNullAndDuplication<T>(List<T> data)
        {
            bool change = false;
            for (int i = 0; i < data.Count;)
            {
                var val = data[i];
                if (val == null)
                {
                    data.RemoveAt(i);
                    change = true;
                    continue;
                }
                int search = data.IndexOf(val);
                if (search < i)
                {
                    data.RemoveAt(i);
                    change = true;
                    continue;
                }
                i++;
            }

            return change;
        }

        /// <summary>
        /// 共有データのクローンを作成して返す（名前は変えない）
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        public static T Clone<T>(T source) where T : ShareDataObject
        {
            if (source == null)
                return null;

            var newdata = Instantiate(source);
            newdata.name = source.name;

            return newdata;
        }
    }
}
