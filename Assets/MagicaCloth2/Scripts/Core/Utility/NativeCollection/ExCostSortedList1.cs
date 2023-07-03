// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// 最も低いコストとデータを１つ格納するSortedList
    /// コストは０以上でなければならない
    /// 必ずコンストラクタでマイナスコストを指定してから利用すること
    /// var dlist = new ExCostSortedList1(-1);
    /// </summary>
    public struct ExCostSortedList1 : IComparable<ExCostSortedList1>
    {
        internal float cost;
        internal int data;

        /// <summary>
        /// 必ずマイナス距離で初期化すること
        /// </summary>
        /// <param name="invalidCost"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ExCostSortedList1(float invalidCost)
        {
            cost = invalidCost;
            data = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ExCostSortedList1(float invalidCost, int initData)
        {
            cost = invalidCost;
            data = initData;
        }


        public bool IsValid => cost >= 0.0f;

        public int Count => IsValid ? 1 : 0;

        public float Cost => cost;

        public int Data => data;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(float cost, int item)
        {
            Debug.Assert(cost >= 0.0f);
            if (IsValid == false || cost < this.cost)
            {
                this.cost = cost;
                data = item;
            }
        }

        public int CompareTo(ExCostSortedList1 other)
        {
            // コストの昇順
            if (cost != other.cost)
                return cost < other.cost ? -1 : 1;
            else
                return 0;
        }

        public override string ToString()
        {
            return $"({cost} : {data})";
        }
    }
}
