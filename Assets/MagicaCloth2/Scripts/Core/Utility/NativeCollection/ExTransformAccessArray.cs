// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Jobs;

namespace MagicaCloth2
{
    /// <summary>
    /// 固定インデックス型の固定インデックスTransformAccessArray管理クラス
    /// 一度確保したインデックスはズレない（ここ重要）
    /// 同じトランスフォームに関しては参照カウンタでまとめる（TransformAccessArrayは重複を許さないため）
    /// </summary>
    public class ExTransformAccessArray : IDisposable
    {
        TransformAccessArray transformArray;

        /// <summary>
        /// ネイティブリストの配列数
        /// ※ジョブでエラーが出ないように事前に確保しておく
        /// </summary>
        int nativeLength;

        /// <summary>
        /// 空インデックススタック
        /// </summary>
        Queue<int> emptyStack;

        /// <summary>
        /// 使用インデックス辞書
        /// </summary>
        Dictionary<int, int> useIndexDict;

        /// <summary>
        /// トランスフォームインデックス辞書
        /// </summary>
        Dictionary<int, int> indexDict;

        /// <summary>
        /// トランスフォーム参照カウンタ辞書
        /// </summary>
        Dictionary<int, int> referenceDict;

        //=========================================================================================
        public ExTransformAccessArray(int capacity, int desiredJobCount = -1)
        {
            transformArray = new TransformAccessArray(capacity, desiredJobCount);
            nativeLength = transformArray.length;

            emptyStack = new Queue<int>(capacity);
            useIndexDict = new Dictionary<int, int>(capacity);
            indexDict = new Dictionary<int, int>(capacity);
            referenceDict = new Dictionary<int, int>(capacity);
        }

        public void Dispose()
        {
            if (transformArray.isCreated)
                transformArray.Dispose();
            emptyStack.Clear();
            useIndexDict.Clear();
            indexDict.Clear();
            referenceDict.Clear();
            nativeLength = 0;
        }

        /// <summary>
        /// TransformAccessArrayを取得する
        /// </summary>
        /// <returns></returns>
        public TransformAccessArray GetTransformAccessArray()
        {
            return transformArray;
        }

        /// データ追加
        /// 追加したインデックスを返す
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public int Add(Transform element)
        {
            int index = 0;

            int id = element.GetInstanceID();

            if (referenceDict.ContainsKey(id))
            {
                // 参照カウンタ＋
                referenceDict[id] = referenceDict[id] + 1;
                return indexDict[id];
            }

            if (emptyStack.Count > 0)
            {
                // 再利用
                index = emptyStack.Dequeue();
                transformArray[index] = element;
            }
            else
            {
                // 新規
                index = transformArray.length;
                transformArray.Add(element);
            }
            useIndexDict.Add(index, id);
            indexDict.Add(id, index);
            referenceDict.Add(id, 1);
            nativeLength = transformArray.length;

            return index;
        }

        /// <summary>
        /// データ削除
        /// 削除されたインデックスは再利用される
        /// </summary>
        /// <param name="index"></param>
        public void Remove(int index)
        {
            if (useIndexDict.ContainsKey(index))
            {
                int id = useIndexDict[index];
                int cnt = referenceDict[id] - 1;
                if (cnt > 0)
                {
                    // 参照カウンタ-
                    referenceDict[id] = cnt;
                    return;
                }

                // 削除
                transformArray[index] = null;
                emptyStack.Enqueue(index);
                useIndexDict.Remove(index);
                indexDict.Remove(id);
                referenceDict.Remove(id);
                nativeLength = transformArray.length;
            }
        }

        public bool Exist(int index)
        {
            return useIndexDict.ContainsKey(index);
        }

        public bool Exist(Transform element)
        {
            if (element == null)
                return false;
            return indexDict.ContainsKey(element.GetInstanceID());
        }

        /// <summary>
        /// データ使用量
        /// </summary>
        public int Count
        {
            get
            {
                return useIndexDict.Count;
            }
        }

        /// <summary>
        /// データ配列数
        /// </summary>
        public int Length
        {
            get
            {
                return nativeLength;
            }
        }

        public Transform this[int index]
        {
            get
            {
                return transformArray[index];
            }
        }

        public int GetIndex(Transform element)
        {
            if (element == null)
                return -1;
            int id = element.GetInstanceID();
            if (indexDict.ContainsKey(id))
                return indexDict[id];
            else
                return -1;
        }

        public void Clear()
        {
            // 配列数はそのままにクリアする
            foreach (var index in useIndexDict.Keys)
                emptyStack.Enqueue(index);
            useIndexDict.Clear();
            for (int i = 0, cnt = Length; i < cnt; i++)
                transformArray[i] = null;
            indexDict.Clear();
            referenceDict.Clear();
            nativeLength = 0;
        }
    }
}
