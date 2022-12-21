// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using System;
using System.Collections.Generic;
using Unity.Collections;

namespace MagicaCloth
{
    /// <summary>
    /// 参照カウント付き固定インデックスNativeList
    /// データは重複しない、参照カウントが増減する
    /// 一度確保したインデックスはズレない（ここ重要）
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class FixedNativeListWithCount<T> : IDisposable where T : unmanaged
    {
        /// <summary>
        /// ネイティブリスト
        /// </summary>
        NativeList<T> nativeList;

        /// <summary>
        /// ネイティブリストの配列数
        /// ※ジョブでエラーが出ないように事前に確保しておく
        /// </summary>
        int nativeLength;

        /// <summary>
        /// 空インデックススタック
        /// </summary>
        Queue<int> emptyStack = new Queue<int>();

        /// <summary>
        /// 使用インデックスセット
        /// </summary>
        HashSet<int> useIndexSet = new HashSet<int>();

        Dictionary<T, int> indexDict = new Dictionary<T, int>();

        Dictionary<T, int> countDict = new Dictionary<T, int>();

        T emptyElement;

        //=========================================================================================
        public FixedNativeListWithCount()
        {
            nativeList = new NativeList<T>(Allocator.Persistent);
            nativeLength = nativeList.Length;
            emptyElement = new T();
        }

        public FixedNativeListWithCount(int capacity)
        {
            nativeList = new NativeList<T>(capacity, Allocator.Persistent);
            nativeLength = nativeList.Length;
        }

        public void Dispose()
        {
            if (nativeList.IsCreated)
            {
                nativeList.Dispose();
            }
            nativeLength = 0;
            emptyStack.Clear();
            useIndexSet.Clear();
            indexDict.Clear();
            countDict.Clear();
        }

        public void SetEmptyElement(T empty)
        {
            emptyElement = empty;
        }

        //=========================================================================================
        /// <summary>
        /// データ追加
        /// 追加したインデックスを返す
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public int Add(T element)
        {
            int index = 0;

            // 参照チェック
            if (indexDict.ContainsKey(element))
            {
                // カウンタ+
                index = indexDict[element];
                countDict[element] = countDict[element] + 1;
            }
            else
            {
                // 新規
                if (emptyStack.Count > 0)
                {
                    // 再利用
                    index = emptyStack.Dequeue();
                    nativeList[index] = element;
                }
                else
                {
                    // 新規
                    index = nativeList.Length;
                    nativeList.Add(element);
                    nativeLength = nativeList.Length;
                }
                useIndexSet.Add(index);
                indexDict[element] = index;
                countDict[element] = 1;
            }

            return index;
        }

        /// <summary>
        /// データ削除
        /// 削除されたインデックスは再利用される
        /// </summary>
        /// <param name="element"></param>
        public void Remove(T element)
        {
            if (indexDict.ContainsKey(element))
            {
                int cnt = countDict[element];
                if (cnt <= 1)
                {
                    // 削除
                    int index = indexDict[element];

                    // 削除データはデフォルト値で埋める
                    nativeList[index] = emptyElement;

                    emptyStack.Enqueue(index);
                    useIndexSet.Remove(index);
                    indexDict.Remove(element);
                    countDict.Remove(element);
                }
                else
                {
                    // 参照カウント-
                    countDict[element] = cnt - 1;
                }
            }
        }

        public bool Exist(T element)
        {
            return indexDict.ContainsKey(element);
        }

        public int GetUseCount(T element)
        {
            if (countDict.ContainsKey(element))
                return countDict[element];

            return 0;
        }

        /// <summary>
        /// 確保されているネイティブ配列の要素数を返す
        /// </summary>
        public int Length
        {
            get
            {
                return nativeLength;
            }
        }

        /// <summary>
        /// 実際に利用されている要素数を返す
        /// </summary>
        public int Count
        {
            get
            {
                return useIndexSet.Count;
            }
        }

        public T this[int index]
        {
            get
            {
                return nativeList[index];
            }
            set
            {
                nativeList[index] = value;
            }
        }

        public void Clear()
        {
            nativeList.Clear();
            nativeLength = 0;
            emptyStack.Clear();
            useIndexSet.Clear();
            indexDict.Clear();
            countDict.Clear();
        }

        //public T[] ToArray()
        //{
        //    return nativeList.ToArray();
        //}

        /// <summary>
        /// Jobで利用する場合はこの関数でNativeArrayに変換して受け渡す
        /// </summary>
        /// <returns></returns>
        public NativeArray<T> ToJobArray()
        {
            return nativeList.AsArray();
        }
    }
}
