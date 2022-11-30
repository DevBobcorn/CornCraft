// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using System;
using System.Collections.Generic;
using Unity.Collections;

namespace MagicaCloth
{
    /// <summary>
    /// 固定インデックスNativeList
    /// 一度確保したインデックスはズレない（ここ重要）
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class FixedNativeList<T> : IDisposable where T : struct
    {
        /// <summary>
        /// ネイティブ配列
        /// </summary>
        NativeArray<T> nativeArray0;

        NativeArray<T> nativeArray1;

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

        int useLength;

        //=========================================================================================
        public FixedNativeList()
        {
            nativeArray0 = new NativeArray<T>(8, Allocator.Persistent);
            nativeLength = nativeArray0.Length;
            useLength = 0;
        }

        public void Dispose()
        {
            if (nativeArray0.IsCreated)
            {
                nativeArray0.Dispose();
            }
            if (nativeArray1.IsCreated)
            {
                nativeArray1.Dispose();
            }
            nativeLength = 0;
            emptyStack.Clear();
            useIndexSet.Clear();
            useLength = 0;
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

            if (emptyStack.Count > 0)
            {
                // 再利用
                index = emptyStack.Dequeue();
                nativeArray0[index] = element;
            }
            else
            {
                // 新規
                if (nativeArray0.Length <= useLength)
                {
                    // 拡張
                    int len = nativeArray0.Length;
                    while (len <= useLength)
                        len += len;
                    var nativeArray2 = new NativeArray<T>(len, Allocator.Persistent);
                    nativeArray2.CopyFromFast(nativeArray0);
                    nativeArray0.Dispose();

                    nativeArray0 = nativeArray2;
                }

                index = useLength;
                nativeArray0[index] = element;
                nativeLength = nativeArray0.Length;

                useLength++;
            }
            useIndexSet.Add(index);

            return index;
        }

        /// <summary>
        /// データ削除
        /// 削除されたインデックスは再利用される
        /// </summary>
        /// <param name="index"></param>
        public void Remove(int index)
        {
            if (useIndexSet.Contains(index))
            {
                // 削除データはデフォルト値で埋める
                nativeArray0[index] = default(T);

                emptyStack.Enqueue(index);
                useIndexSet.Remove(index);
            }
        }

        public bool Exists(int index)
        {
            return useIndexSet.Contains(index);
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
                return nativeArray0[index];
            }
            set
            {
                nativeArray0[index] = value;
            }
        }

        public void Clear()
        {
            nativeLength = 0;
            emptyStack.Clear();
            useIndexSet.Clear();
            useLength = 0;
        }

        /// <summary>
        /// Jobで利用する場合はこの関数でNativeArrayに変換して受け渡す
        /// </summary>
        /// <returns></returns>
        public NativeArray<T> ToJobArray()
        {
            return nativeArray0;
        }

        public NativeArray<T> ToJobArray(int bufferIndex)
        {
            return bufferIndex == 0 ? nativeArray0 : nativeArray1;
        }

        /*public void SwapBuffer()
        {
            var back = nativeArray1;
            nativeArray1 = nativeArray0;

            // サイズを合わせる
            if (back.IsCreated == false || back.Length != nativeArray0.Length)
            {
                if (back.IsCreated)
                    back.Dispose();
                back = new NativeArray<T>(nativeArray0.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                back.CopyFromFast(nativeArray0);

                //Debug.Log("★サイズ変更!");
            }

            nativeArray0 = back;
        }*/

        public void SyncBuffer()
        {
            // サイズを合わせる
            if (nativeArray1.IsCreated == false || nativeArray1.Length != nativeArray0.Length)
            {
                if (nativeArray1.IsCreated)
                    nativeArray1.Dispose();
                nativeArray1 = new NativeArray<T>(nativeArray0.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                //nativeArray1.CopyFromFast(nativeArray0);

                //Debug.Log("★サイズ変更!");
            }
        }
    }
}
