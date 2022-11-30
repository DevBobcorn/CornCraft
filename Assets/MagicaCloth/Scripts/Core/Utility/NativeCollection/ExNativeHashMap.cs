// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using System;
using System.Collections.Generic;
using Unity.Collections;

namespace MagicaCloth
{
    /// <summary>
    /// NativeHashMapの機能拡張版
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class ExNativeHashMap<TKey, TValue>
        where TKey : struct, IEquatable<TKey>
        where TValue : struct
    {
#if MAGICACLOTH_USE_COLLECTIONS_130
        NativeParallelHashMap<TKey, TValue> nativeHashMap;
#else
        NativeHashMap<TKey, TValue> nativeHashMap;
#endif

        /// <summary>
        /// ネイティブリストの配列数
        /// ※ジョブでエラーが出ないように事前に確保しておく
        /// </summary>
        int nativeLength;

        /// <summary>
        /// 使用キーの記録
        /// </summary>
        HashSet<TKey> useKeySet = new HashSet<TKey>();

        //=========================================================================================
        public ExNativeHashMap()
        {
#if MAGICACLOTH_USE_COLLECTIONS_130
            nativeHashMap = new NativeParallelHashMap<TKey, TValue>(1, Allocator.Persistent);
#else
            nativeHashMap = new NativeHashMap<TKey, TValue>(1, Allocator.Persistent);
#endif
            nativeLength = NativeCount;
        }

        public void Dispose()
        {
            if (nativeHashMap.IsCreated)
            {
                nativeHashMap.Dispose();
            }
        }

        private int NativeCount
        {
            get
            {
                return nativeHashMap.Count();
            }
        }

        //=========================================================================================
        /// <summary>
        /// データ追加
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Add(TKey key, TValue value)
        {
            if (nativeHashMap.TryAdd(key, value) == false)
            {
                // すでにデータが存在するため一旦削除して再追加
                nativeHashMap.Remove(key);
                nativeHashMap.TryAdd(key, value);
            }
            useKeySet.Add(key);
            nativeLength = NativeCount;
        }

        /// <summary>
        /// データ取得
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public TValue Get(TKey key)
        {
            TValue data;
            nativeHashMap.TryGetValue(key, out data);
            return data;
        }

        /// <summary>
        /// 条件判定削除
        /// </summary>
        /// <param name="func">trueを返せば削除</param>
        public void Remove(Func<TKey, TValue, bool> func)
        {
            List<TKey> removeKey = new List<TKey>();
            foreach (TKey key in useKeySet)
            {
                TValue data;
                if (nativeHashMap.TryGetValue(key, out data))
                {
                    // 削除判定
                    if (func(key, data))
                    {
                        // 削除
                        nativeHashMap.Remove(key);
                        removeKey.Add(key);
                    }
                }
            }

            foreach (var key in removeKey)
                useKeySet.Remove(key);
            nativeLength = NativeCount;
        }

        /// <summary>
        /// データ置き換え
        /// </summary>
        /// <param name="func">trueを返せば置換</param>
        /// <param name="rdata">引数にデータを受け取り、修正したデータを返し置換する</param>
        public void Replace(Func<TKey, TValue, bool> func, Func<TValue, TValue> datafunc)
        {
            foreach (var key in useKeySet)
            {
                TValue data;
                if (nativeHashMap.TryGetValue(key, out data))
                {
                    // 置換判定
                    if (func(key, data))
                    {
                        // 置き換え
                        var newdata = datafunc(data);
                        nativeHashMap.Remove(key); // 一旦削除しないと置き換えられない
                        nativeHashMap.TryAdd(key, newdata);
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// キーの削除
        /// </summary>
        /// <param name="key"></param>
        public void Remove(TKey key)
        {
            nativeHashMap.Remove(key);
            nativeLength = 0;
            useKeySet.Remove(key);
        }


        /// <summary>
        /// 実際に利用されている要素数を返す
        /// </summary>
        public int Count
        {
            get
            {
                return nativeLength;
            }
        }

        public void Clear()
        {
            nativeHashMap.Clear();
            nativeLength = 0;
            useKeySet.Clear();
        }

        /// <summary>
        /// 内部のNativeHashMapを取得する
        /// </summary>
        /// <returns></returns>
#if MAGICACLOTH_USE_COLLECTIONS_130
        public NativeParallelHashMap<TKey, TValue> Map
#else
        public NativeHashMap<TKey, TValue> Map
#endif
        {
            get
            {
                return nativeHashMap;
            }
        }

        /// <summary>
        /// 使用キーセットを取得する
        /// </summary>
        public HashSet<TKey> UseKeySet
        {
            get
            {
                return useKeySet;
            }
        }
    }
}
