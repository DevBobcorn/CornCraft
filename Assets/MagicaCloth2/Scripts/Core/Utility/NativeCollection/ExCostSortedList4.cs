// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// コストの昇順に４つまでデータを格納できる固定SortedList
    /// コストは０以上でなければならない
    /// 必ずコンストラクタでマイナスコストを指定してから利用すること
    /// var dlist = new ExCostSortedList4(-1);
    /// </summary>
    public struct ExCostSortedList4
    {
        internal float4 costs;
        internal int4 data;

        /// <summary>
        /// 必ずマイナス距離で初期化すること
        /// </summary>
        /// <param name="invalidCost"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ExCostSortedList4(float invalidCost)
        {
            costs = invalidCost;
            data = 0;
        }

        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                for (int i = 3; i >= 0; i--)
                {
                    if (costs[i] >= 0.0f)
                        return i + 1;
                }

                return 0;
            }
        }

        public bool IsValid => costs[0] >= 0.0f;

        public bool Add(float cost, int item)
        {
            Debug.Assert(cost >= 0.0f);

            // すでにデータが一杯で最大コストより上なら入らない
            if (costs[3] >= 0.0f && cost > costs[3])
                return false;


            // 距離の昇順で挿入する、最大数は４
            for (int i = 0; i < 4; i++)
            {
                float d = costs[i];
                if (d < 0.0f)
                {
                    // 追加
                    costs[i] = cost;
                    data[i] = item;
                    return true;
                }
                else if (cost < d)
                {
                    // 挿入
                    for (int j = 2; j >= i; j--)
                    {
                        costs[j + 1] = costs[j];
                        data[j + 1] = data[j];
                    }
                    costs[i] = cost;
                    data[i] = item;
                    return true;
                }
            }

            // 入らない
            return false;
        }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public (float, int) Get(int index)
        //{
        //    return (costs[index], data[index]);
        //}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(int item)
        {
            for (int i = 0; i < 4; i++)
            {
                if (costs[i] >= 0.0f && data[i] == item)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// データ内のアイテムを検索してその登録インデックスを返す。(-1=なし)
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public int indexOf(int item)
        {
            for (int i = 0; i < 4; i++)
            {
                if (costs[i] >= 0.0f && data[i] == item)
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// データ内のアイテムを削除しデータを１つ詰める
        /// </summary>
        /// <param name="item"></param>
        public void RemoveItem(int item)
        {
            int itemIndex = -1;
            for (int i = 0; i < 4; i++)
            {
                if (costs[i] >= 0.0f && data[i] == item)
                {
                    itemIndex = i;
                    break;
                }
            }
            if (itemIndex < 0)
                return;

            for (int j = itemIndex; j < 3; j++)
            {
                costs[j] = costs[j + 1];
                data[j] = data[j + 1];
            }
            costs[3] = -1;
        }

        /// <summary>
        /// データ内の最小のコストを返す
        /// </summary>
        public float MinCost
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return costs[0];
            }
        }

        /// <summary>
        /// データ内の最大のコストを返す
        /// </summary>
        public float MaxCost
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                for (int i = 3; i >= 0; i--)
                {
                    if (costs[i] >= 0.0f)
                        return costs[i];
                }
                return 0.0f;
            }
        }

        public override string ToString()
        {
            var s = new FixedString512Bytes();
            for (int i = 0; i < Count; i++)
            {
                s.Append($"({costs[i]} : {data[i]}) ");
            }
            return s.ToString();
        }
    }
}
