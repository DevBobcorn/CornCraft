// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using Unity.Collections;

namespace MagicaCloth2
{
    /// <summary>
    /// NativeMultiHashMapの拡張メソッド
    /// </summary>
    static class NativeMultiHashMapExtensions
    {
        /// <summary>
        /// NativeMultiHashMapのキーに指定データが存在するか判定する
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="map"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
#if MC2_COLLECTIONS_200
        public static bool Contains<TKey, TValue>(ref this NativeParallelMultiHashMap<TKey, TValue> map, TKey key, TValue value) where TKey : unmanaged, IEquatable<TKey> where TValue : unmanaged, IEquatable<TValue>
#else
        public static bool Contains<TKey, TValue>(ref this NativeParallelMultiHashMap<TKey, TValue> map, TKey key, TValue value) where TKey : struct, IEquatable<TKey> where TValue : struct, IEquatable<TValue>
#endif
        {
            foreach (TValue val in map.GetValuesForKey(key))
            {
                if (val.Equals(value))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// NativeMultiHashMapのキーに対して重複なしのデータを追加する
        /// すでにキーに同じデータが存在する場合は追加しない。
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="map"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
#if MC2_COLLECTIONS_200
        public static void UniqueAdd<TKey, TValue>(ref this NativeParallelMultiHashMap<TKey, TValue> map, TKey key, TValue value) where TKey : unmanaged, IEquatable<TKey> where TValue : unmanaged, IEquatable<TValue>
#else
        public static void UniqueAdd<TKey, TValue>(ref this NativeParallelMultiHashMap<TKey, TValue> map, TKey key, TValue value) where TKey : struct, IEquatable<TKey> where TValue : struct, IEquatable<TValue>
#endif
        {
            if (map.Contains(key, value) == false)
            {
                map.Add(key, value);
            }
        }

        /// <summary>
        /// 現在のキーのデータをFixedList512Bytesに変換して返す
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
#if MC2_COLLECTIONS_200
        public static FixedList512Bytes<TValue> ToFixedList512Bytes<TKey, TValue>(ref this NativeParallelMultiHashMap<TKey, TValue> map, TKey key) where TKey : unmanaged, IEquatable<TKey> where TValue : unmanaged, IEquatable<TValue>
#else
        public static FixedList512Bytes<TValue> ToFixedList512Bytes<TKey, TValue>(ref this NativeParallelMultiHashMap<TKey, TValue> map, TKey key) where TKey : struct, IEquatable<TKey> where TValue : unmanaged, IEquatable<TValue>
#endif
        {
            var fixlist = new FixedList512Bytes<TValue>();
            if (map.ContainsKey(key))
            {
                foreach (var data in map.GetValuesForKey(key))
                {
                    fixlist.Add(data);
                }
            }

            return fixlist;
        }

        /// <summary>
        /// 現在のキーのデータをFixedList128Bytesに変換して返す
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
#if MC2_COLLECTIONS_200
        public static FixedList128Bytes<TValue> ToFixedList128Bytes<TKey, TValue>(ref this NativeParallelMultiHashMap<TKey, TValue> map, TKey key) where TKey : unmanaged, IEquatable<TKey> where TValue : unmanaged, IEquatable<TValue>
#else
        public static FixedList128Bytes<TValue> ToFixedList128Bytes<TKey, TValue>(ref this NativeParallelMultiHashMap<TKey, TValue> map, TKey key) where TKey : struct, IEquatable<TKey> where TValue : unmanaged, IEquatable<TValue>
#endif
        {
            var fixlist = new FixedList128Bytes<TValue>();
            if (map.ContainsKey(key))
            {
                foreach (var data in map.GetValuesForKey(key))
                {
                    fixlist.Add(data);
                }
            }

            return fixlist;
        }
    }
}
