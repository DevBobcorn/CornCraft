// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using UnityEngine;

namespace MagicaCloth2
{
    public static class FixedList32BytesExtensions
    {
        //=====================================================================
        // Common
        //=====================================================================
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool MC2IsCapacity<T>(ref this FixedList32Bytes<T> fixedList) where T : unmanaged, IEquatable<T>
        {
            return fixedList.Length >= fixedList.Capacity;
        }

        //=====================================================================
        // Set
        //=====================================================================
        /// <summary>
        /// データが存在しない場合のみ追加する
        /// 容量がオーバーすると例外が発生する
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fixedList"></param>
        /// <param name="item"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MC2Set<T>(ref this FixedList32Bytes<T> fixedList, T item) where T : unmanaged, IEquatable<T>
        {
            if (fixedList.Contains(item) == false)
            {
                fixedList.Add(item);
            }
        }

        /// <summary>
        /// データが存在しない場合のみ追加する
        /// すでに容量が一杯の場合は警告を表示し追加しない。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fixedList"></param>
        /// <param name="item"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MC2SetLimit<T>(ref this FixedList32Bytes<T> fixedList, T item) where T : unmanaged, IEquatable<T>
        {
            if (fixedList.Length >= fixedList.Capacity)
            {
                Debug.LogWarning($"FixedSet32.Limit!:{fixedList.Capacity}");
                return;
            }
            if (fixedList.Contains(item) == false)
            {
                fixedList.Add(item);
            }
        }

        /// <summary>
        /// リストからデータを検索して削除する
        /// 削除領域にはリストの最後のデータが移動する(SwapBack)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fixedList"></param>
        /// <param name="item"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MC2RemoveItemAtSwapBack<T>(ref this FixedList32Bytes<T> fixedList, T item) where T : unmanaged, IEquatable<T>
        {
            for (int i = 0; i < fixedList.Length; i++)
            {
                if (fixedList.ElementAt(i).Equals(item))
                {
                    fixedList.RemoveAtSwapBack(i);
                    break;
                }
            }
        }

        //=====================================================================
        // Stack
        //=====================================================================
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MC2Push<T>(ref this FixedList32Bytes<T> fixedList, T item) where T : unmanaged, IEquatable<T>
        {
            fixedList.Add(item);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T MC2Pop<T>(ref this FixedList32Bytes<T> fixedList) where T : unmanaged, IEquatable<T>
        {
            int index = fixedList.Length - 1;
            T item = fixedList[index];
            fixedList.RemoveAt(index);
            return item;
        }

        //=====================================================================
        // Queue
        //=====================================================================
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MC2Enqueue<T>(ref this FixedList32Bytes<T> fixedList, T item) where T : unmanaged, IEquatable<T>
        {
            fixedList.Add(item);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T MC2Dequque<T>(ref this FixedList32Bytes<T> fixedList) where T : unmanaged, IEquatable<T>
        {
            T item = fixedList[0];
            fixedList.RemoveAt(0);
            return item;
        }
    }
}
