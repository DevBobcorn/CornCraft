// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// NativeMultiHashMapの拡張メソッド
    /// </summary>
    public static class NativeMultiHashMapExtensions
    {
        /// <summary>
        /// NativeParallelMultiHashMapのキーに指定データが存在するか判定する
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="map"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
#if MC2_COLLECTIONS_200
        public static bool MC2Contains<TKey, TValue>(ref this NativeParallelMultiHashMap<TKey, TValue> map, TKey key, TValue value) where TKey : unmanaged, IEquatable<TKey> where TValue : unmanaged, IEquatable<TValue>
#else
        public static bool MC2Contains<TKey, TValue>(ref this NativeParallelMultiHashMap<TKey, TValue> map, TKey key, TValue value) where TKey : struct, IEquatable<TKey> where TValue : struct, IEquatable<TValue>
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
        /// NativeParallelMultiHashMapキーに対して重複なしのデータを追加する
        /// すでにキーに同じデータが存在する場合は追加しない。
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="map"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
#if MC2_COLLECTIONS_200
        public static void MC2UniqueAdd<TKey, TValue>(ref this NativeParallelMultiHashMap<TKey, TValue> map, TKey key, TValue value) where TKey : unmanaged, IEquatable<TKey> where TValue : unmanaged, IEquatable<TValue>
#else
        public static void MC2UniqueAdd<TKey, TValue>(ref this NativeParallelMultiHashMap<TKey, TValue> map, TKey key, TValue value) where TKey : struct, IEquatable<TKey> where TValue : struct, IEquatable<TValue>
#endif
        {
            if (map.MC2Contains(key, value) == false)
            {
                map.Add(key, value);
            }
        }

        /// <summary>
        /// NativeMultiHashMapのキーに存在するデータを削除する
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="map"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
#if MC2_COLLECTIONS_200
        public static bool MC2RemoveValue<TKey, TValue>(ref this NativeParallelMultiHashMap<TKey, TValue> map, TKey key, TValue value) where TKey : unmanaged, IEquatable<TKey> where TValue : unmanaged, IEquatable<TValue>
#else
        public static bool MC2RemoveValue<TKey, TValue>(ref this NativeParallelMultiHashMap<TKey, TValue> map, TKey key, TValue value) where TKey : struct, IEquatable<TKey> where TValue : struct, IEquatable<TValue>
#endif
        {
            NativeParallelMultiHashMapIterator<TKey> it;
            TValue item;
            if (map.TryGetFirstValue(key, out item, out it))
            {
                do
                {
                    if (item.Equals(value))
                    {
                        map.Remove(it);
                        return true;
                    }
                }
                while (map.TryGetNextValue(out item, ref it));
            }

            return false;
        }

        /// <summary>
        /// 現在のキーのデータをFixedList512Bytesに変換して返す
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
#if MC2_COLLECTIONS_200
        public static FixedList512Bytes<TValue> MC2ToFixedList512Bytes<TKey, TValue>(ref this NativeParallelMultiHashMap<TKey, TValue> map, TKey key) where TKey : unmanaged, IEquatable<TKey> where TValue : unmanaged, IEquatable<TValue>
#else
        public static FixedList512Bytes<TValue> MC2ToFixedList512Bytes<TKey, TValue>(ref this NativeParallelMultiHashMap<TKey, TValue> map, TKey key) where TKey : struct, IEquatable<TKey> where TValue : unmanaged, IEquatable<TValue>
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
        public static FixedList128Bytes<TValue> MC2ToFixedList128Bytes<TKey, TValue>(ref this NativeParallelMultiHashMap<TKey, TValue> map, TKey key) where TKey : unmanaged, IEquatable<TKey> where TValue : unmanaged, IEquatable<TValue>
#else
        public static FixedList128Bytes<TValue> MC2ToFixedList128Bytes<TKey, TValue>(ref this NativeParallelMultiHashMap<TKey, TValue> map, TKey key) where TKey : struct, IEquatable<TKey> where TValue : unmanaged, IEquatable<TValue>
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

        /// <summary>
        /// NativeParallelMultiHashMapをKeyとValueの配列に変換します
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="map"></param>
        /// <returns></returns>
#if MC2_COLLECTIONS_200
        public static (TKey[], TValue[]) MC2Serialize<TKey, TValue>(ref this NativeParallelMultiHashMap<TKey, TValue> map) where TKey : unmanaged, IEquatable<TKey> where TValue : unmanaged
#else
        public static (TKey[], TValue[]) MC2Serialize<TKey, TValue>(ref this NativeParallelMultiHashMap<TKey, TValue> map) where TKey : struct, IEquatable<TKey> where TValue : struct
#endif
        {
            if (map.IsCreated == false || map.Count() == 0 || map.IsEmpty)
                return (null, null);

            using var keyNativeArray = map.GetKeyArray(Allocator.Persistent);
            using var valueNativeArray = map.GetValueArray(Allocator.Persistent);

            return (keyNativeArray.ToArray(), valueNativeArray.ToArray());
        }

        /// <summary>
        /// KeyとValueの配列からNativeParallelMultiHashMapを復元します
        /// 高速化のためBurstを利用
        /// ジェネリック型ジョブは明示的に型を指定する必要があるため型ごとに関数が発生します
        /// </summary>
        /// <param name="keyArray"></param>
        /// <param name="valueArray"></param>
        /// <returns></returns>
        public static NativeParallelMultiHashMap<int2, ushort> MC2Deserialize(int2[] keyArray, ushort[] valueArray)
        {
            int keyCount = keyArray?.Length ?? 0;
            int valueCount = valueArray?.Length ?? 0;
            Debug.Assert(keyCount == valueCount);
            var map = new NativeParallelMultiHashMap<int2, ushort>(keyCount, Allocator.Persistent);
            if (keyCount > 0 && valueCount > 0)
            {
                using var keyNativeArray = new NativeArray<int2>(keyArray, Allocator.Persistent);
                using var valueNativeArray = new NativeArray<ushort>(valueArray, Allocator.Persistent);
                var job = new SetParallelMultiHashMapJob<int2, ushort>() { map = map, keyArray = keyNativeArray, valueArray = valueNativeArray };
                job.Run();
            }

            return map;
        }

        [BurstCompile]
#if MC2_COLLECTIONS_200
        struct SetParallelMultiHashMapJob<TKey, TValue> : IJob where TKey : unmanaged, IEquatable<TKey> where TValue : unmanaged
#else
        struct SetParallelMultiHashMapJob<TKey, TValue> : IJob where TKey : struct, IEquatable<TKey> where TValue : struct
#endif
        {
            public NativeParallelMultiHashMap<TKey, TValue> map;
            [Unity.Collections.ReadOnly]
            public NativeArray<TKey> keyArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<TValue> valueArray;

            public void Execute()
            {
                int cnt = keyArray.Length;
                for (int i = 0; i < cnt; i++)
                {
                    map.Add(keyArray[i], valueArray[i]);
                }
            }
        }
    }
}
