// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System.Collections.Generic;

namespace MagicaCloth2
{
    /// <summary>
    /// 最小距離のデータのみを保持するクラス
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    public class MinimumData<T1, T2> where T1 : unmanaged where T2 : unmanaged
    {
        T1 minDist;
        T2 minData;
        bool isValid = false;

        /// <summary>
        /// データの追加。現在のデータより距離が短い場合のみ上書きする
        /// </summary>
        /// <param name="distance"></param>
        /// <param name="data"></param>
        public void Add(T1 distance, T2 data)
        {
            if (isValid)
            {
                if (Comparer<T1>.Default.Compare(distance, minDist) < 0)
                {
                    minDist = distance;
                    minData = data;
                }
            }
            else
            {
                minDist = distance;
                minData = data;
                isValid = true;
            }
        }

        public void Clear()
        {
            isValid = false;
        }


        public bool IsValid => isValid;
        public T1 MinDistance => minDist;
        public T2 MinData => minData;

        public override string ToString()
        {
            return $"MinimumData. IsValid:{isValid}, minDist:{minDist}, minData:{minData}";
        }
    }
}
