// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using System.Collections.Generic;
using Unity.Collections;

namespace MagicaCloth2
{
    /// <summary>
    /// T型のデータリストを構築し要素ごとにそのスタートインデックスとデータカウンタを生成する
    /// 出力はT型のデータ配列と、要素ごとのスタートインデックスとカウンタが１つのuintにパックされた配列の２つ
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class MultiDataBuilder<T> : IDisposable where T : unmanaged
    {
        int indexCount;

        public NativeParallelMultiHashMap<int, T> Map;

        //=========================================================================================
        public MultiDataBuilder(int indexCount, int dataCapacity)
        {
            this.indexCount = indexCount;
            Map = new NativeParallelMultiHashMap<int, T>(dataCapacity, Allocator.Persistent);
        }


        public void Dispose()
        {
            if (Map.IsCreated)
                Map.Dispose();
        }

        public int Count() => Map.Count();

        public int GetDataCount(int index)
        {
            if (Map.ContainsKey(index) == false)
                return 0;

            return Map.CountValuesForKey(index);
        }

        public void Add(int key, T data)
        {
            Map.Add(key, data);
        }

        //public int AddAndReturnIndex(int key, T data)
        //{
        //    int cnt = Map.CountValuesForKey(key);
        //    Map.Add(key, data);
        //    return cnt;
        //}

        public int CountValuesForKey(int key)
        {
            return Map.CountValuesForKey(key);
        }

        //=========================================================================================
        /// <summary>
        /// 内部HashMapのデータをT型配列と要素ごとのスタートインデックスとカウンタ配列の２つに分離して返す
        /// 出力はT型のデータ配列と、要素ごとのスタートインデックス(20bit)とカウンタ(12bit)を１つのuintにパックした配列となる
        /// </summary>
        /// <returns></returns>
        public (T[], uint[]) ToArray()
        {
            if (Map.IsCreated == false || indexCount == 0)
                return (null, null);

            var indexArray = new uint[indexCount];
            var dataList = new List<T>(Map.Capacity);

            for (int i = 0; i < indexCount; i++)
            {
                int start = dataList.Count;
                int cnt = 0;

                if (Map.ContainsKey(i))
                {
                    foreach (var data in Map.GetValuesForKey(i))
                    {
                        dataList.Add(data);
                        cnt++;
                    }
                }

                indexArray[i] = DataUtility.Pack12_20(cnt, start);
            }

            return (dataList.ToArray(), indexArray);
        }

        public uint[] ToIndexArray()
        {
            (var _, uint[] indexArray) = ToArray();
            return indexArray;
        }

        /// <summary>
        /// 内部HashMapのデータをT型配列と要素ごとのスタートインデックス+カウンタの２つのNativeArrayに分離して返す
        /// 出力はT型のデータ配列と、要素ごとのスタートインデックス(20bit)とカウンタ(12bit)を１つのuintにパックした配列となる
        /// </summary>
        /// <param name="indexArray"></param>
        /// <param name="dataArray"></param>
        public void ToNativeArray(out NativeArray<uint> indexArray, out NativeArray<T> dataArray)
        {
            indexArray = new NativeArray<uint>(indexCount, Allocator.Persistent);
            var dataList = new List<T>(Map.Capacity);

            for (int i = 0; i < indexCount; i++)
            {
                int start = dataList.Count;
                int cnt = 0;

                if (Map.ContainsKey(i))
                {
                    foreach (var data in Map.GetValuesForKey(i))
                    {
                        dataList.Add(data);
                        cnt++;
                    }
                }

                indexArray[i] = DataUtility.Pack12_20(cnt, start);
            }

            dataArray = new NativeArray<T>(dataList.ToArray(), Allocator.Persistent);
        }
    }
}
