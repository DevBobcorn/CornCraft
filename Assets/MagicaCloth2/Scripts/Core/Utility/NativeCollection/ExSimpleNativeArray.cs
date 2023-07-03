// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using System.Text;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// サイズ拡張可能なNativeArray管理クラス
    /// 領域が不足すると自動でサイズを拡張する
    /// ただし領域の拡張のみで削除や領域の再利用はできない
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ExSimpleNativeArray<T> : IDisposable where T : unmanaged
    {
        NativeArray<T> nativeArray;

        int count;
        int length;

        //=========================================================================================
        public ExSimpleNativeArray()
        {
            count = 0;
            length = 0;
        }

        /// <summary>
        /// 領域を確保する
        /// </summary>
        /// <param name="dataLength"></param>
        /// <param name="areaOnly">true=領域のみで利用カウントを進めない</param>
        public ExSimpleNativeArray(int dataLength, bool areaOnly = false) : this()
        {
            nativeArray = new NativeArray<T>(dataLength, Allocator.Persistent);
            length = dataLength;
            if (areaOnly == false)
                count = length;
        }

        public ExSimpleNativeArray(T[] dataArray) : this()
        {
            Debug.Assert(dataArray != null);
            nativeArray = new NativeArray<T>(dataArray, Allocator.Persistent);
            length = dataArray.Length;
            count = length;
        }

        public ExSimpleNativeArray(NativeArray<T> array) : this()
        {
            Debug.Assert(array.IsCreated);
            nativeArray = new NativeArray<T>(array, Allocator.Persistent);
            length = array.Length;
            count = length;
        }

        public ExSimpleNativeArray(NativeList<T> array) : this()
        {
            Debug.Assert(array.IsCreated);
            nativeArray = new NativeArray<T>(array.AsArray(), Allocator.Persistent);
            length = array.Length;
            count = length;
        }

        public void Dispose()
        {
            if (nativeArray.IsCreated)
            {
                nativeArray.Dispose();
            }
            count = 0;
            length = 0;
        }

        public bool IsValid
        {
            get
            {
                return nativeArray.IsCreated;
            }
        }

        /// <summary>
        /// 実際に使用されている要素数
        /// </summary>
        public int Count => count;

        /// <summary>
        /// 確保されている配列の要素数
        /// </summary>
        public int Length => length;

        /// <summary>
        /// 使用配列カウントを設定する
        /// </summary>
        /// <param name="newCount"></param>
        public void SetCount(int newCount)
        {
            Debug.Assert(newCount <= length && newCount >= 0);
            count = newCount;
        }

        //=========================================================================================
        /// <summary>
        /// 領域のみ拡張する
        /// </summary>
        /// <param name="capacity"></param>
        public void AddCapacity(int capacity)
        {
            Expand(capacity, true);
        }

        /// <summary>
        /// サイズ分の空データを追加する
        /// </summary>
        /// <param name="dataLength"></param>
        public void AddRange(int dataLength)
        {
            Expand(dataLength);
            count += dataLength;
        }

        /// <summary>
        /// 配列データを追加する
        /// </summary>
        /// <param name="dataArray"></param>
        public void AddRange(T[] dataArray)
        {
            Debug.Assert(dataArray != null);

            if (length == 0)
            {
                if (nativeArray.IsCreated)
                    nativeArray.Dispose();

                nativeArray = new NativeArray<T>(dataArray, Allocator.Persistent);
                length = dataArray.Length;
                count = length;
            }
            else
            {
                int dataLength = dataArray.Length;
                Expand(dataLength);
                // copy
                NativeArray<T>.Copy(dataArray, 0, nativeArray, count, dataLength);
                count += dataLength;
            }
        }

        /// <summary>
        /// 配列データを追加する
        /// </summary>
        /// <param name="dataArray"></param>
        public void AddRange(T[] dataArray, int cnt)
        {
            Debug.Assert(dataArray != null);

            if (length == 0)
            {
                if (nativeArray.IsCreated)
                    nativeArray.Dispose();

                nativeArray = new NativeArray<T>(cnt, Allocator.Persistent);
                // copy
                NativeArray<T>.Copy(dataArray, 0, nativeArray, 0, cnt);
                length = cnt;
                count = length;
            }
            else
            {
                int dataLength = cnt;
                Expand(dataLength);
                // copy
                NativeArray<T>.Copy(dataArray, 0, nativeArray, count, dataLength);
                count += dataLength;
            }
        }

        /// <summary>
        /// 領域を確保し設定値で埋める（それなりのコストが発生するので注意！）
        /// </summary>
        /// <param name="dataLength"></param>
        /// <param name="fillData"></param>
        public void AddRange(int dataLength, T fillData = default(T))
        {
            Expand(dataLength);
            Fill(count, dataLength, fillData);
            count += dataLength;
        }

        public void AddRange(NativeArray<T> narray)
        {
            Debug.Assert(narray.IsCreated);

            if (length == 0)
            {
                if (nativeArray.IsCreated)
                    nativeArray.Dispose();

                nativeArray = new NativeArray<T>(narray, Allocator.Persistent);
                length = narray.Length;
                count = length;
            }
            else
            {
                int dataLength = narray.Length;
                Expand(dataLength);
                // copy
                NativeArray<T>.Copy(narray, 0, nativeArray, count, dataLength);
                count += dataLength;
            }
        }

        public void AddRange(NativeList<T> nlist)
        {
            Debug.Assert(nlist.IsCreated);
            AddRange(nlist.AsArray());
        }

        public void AddRange(ExSimpleNativeArray<T> exarray)
        {
            AddRange(exarray.GetNativeArray());
        }

        /// <summary>
        /// 型は異なるが型のサイズは同じ配列を追加する。Vector3->float3など。
        /// </summary>
        /// <typeparam name="U"></typeparam>
        /// <param name="array"></param>
        /// <returns></returns>
        public unsafe void AddRange<U>(U[] array) where U : struct
        {
            Debug.Assert(array != null);

            int dataLength = array.Length;
            Expand(dataLength);

            // copy
            int dstSize = UnsafeUtility.SizeOf<T>();
            ulong src_gcHandle;
            void* src_p = UnsafeUtility.PinGCArrayAndGetDataAddress(array, out src_gcHandle);
            byte* dst_p = (byte*)nativeArray.GetUnsafePtr();
            UnsafeUtility.MemCpy(dst_p + count * dstSize, src_p, dataLength * dstSize);
            UnsafeUtility.ReleaseGCObject(src_gcHandle);
            count += dataLength;
        }

        /// <summary>
        /// 型もサイズも異なる配列を追加する。int[] -> int3[]など。
        /// データはそのままメモリコピーされる。例えばint[]からint3[]へ追加すると次のようになる。
        /// int[]{1, 2, 3, 4, 5, 6} => int3[]{{1, 2, 3}, {4, 5, 6}}
        /// </summary>
        /// <typeparam name="U"></typeparam>
        /// <param name="array"></param>
        /// <returns></returns>
        public unsafe void AddRangeTypeChange<U>(U[] array) where U : struct
        {
            Debug.Assert(array != null);

            int srcSize = UnsafeUtility.SizeOf<U>();
            int dstSize = UnsafeUtility.SizeOf<T>();
            int dataLength = (array.Length * srcSize) / dstSize;

            Expand(dataLength);

            ulong src_gcHandle;
            void* src_p = UnsafeUtility.PinGCArrayAndGetDataAddress(array, out src_gcHandle);
            byte* dst_p = (byte*)nativeArray.GetUnsafePtr();

            UnsafeUtility.MemCpy(dst_p + count * dstSize, src_p, dataLength * dstSize);
            UnsafeUtility.ReleaseGCObject(src_gcHandle);
            count += dataLength;
        }

        public unsafe void AddRangeTypeChange<U>(NativeArray<U> array) where U : struct
        {
            Debug.Assert(array.IsCreated);

            int srcSize = UnsafeUtility.SizeOf<U>();
            int dstSize = UnsafeUtility.SizeOf<T>();
            int dataLength = (array.Length * srcSize) / dstSize;

            Expand(dataLength);

            byte* src_p = (byte*)array.GetUnsafePtr();
            byte* dst_p = (byte*)nativeArray.GetUnsafePtr();

            UnsafeUtility.MemCpy(dst_p + count * dstSize, src_p, dataLength * dstSize);
            count += dataLength;
        }

        /// <summary>
        /// 型もサイズも異なる配列を部分的にコピーする。Vector4[] -> float3など。
        /// </summary>
        /// <typeparam name="U"></typeparam>
        /// <param name="array"></param>
        /// <returns></returns>
        public unsafe void AddRangeStride<U>(U[] array) where U : struct
        {
            Debug.Assert(array != null);
            int dataLength = array.Length;
            Expand(dataLength);

            int srcSize = UnsafeUtility.SizeOf<U>();
            int dstSize = UnsafeUtility.SizeOf<T>();

            ulong src_gcHandle;
            void* src_p = UnsafeUtility.PinGCArrayAndGetDataAddress(array, out src_gcHandle);
            byte* dst_p = (byte*)nativeArray.GetUnsafePtr();
            int elementSize = math.min(srcSize, dstSize);

            UnsafeUtility.MemCpyStride(dst_p + count * dstSize, dstSize, src_p, srcSize, elementSize, dataLength);
            UnsafeUtility.ReleaseGCObject(src_gcHandle);
            count += dataLength;
        }

        public void Add(T data)
        {
            if (Length == 0)
            {
                // ある程度のバッファを確保
                Expand(16);
            }
            else if (count == Length)
            {
                // 倍に拡張する
                Expand(Length);
            }

            nativeArray[count] = data;
            count++;
        }

        public T[] ToArray()
        {
            return nativeArray.ToArray();
        }

        public void CopyTo(T[] array)
        {
            NativeArray<T>.Copy(nativeArray, array);
        }

        public void CopyTo<U>(U[] array) where U : struct
        {
            NativeArray<U>.Copy(nativeArray.Reinterpret<U>(), array);
        }

        /// <summary>
        /// 型もサイズも異なる配列にデータをコピーする。
        /// int3 -> int[]など
        /// </summary>
        /// <typeparam name="U"></typeparam>
        /// <param name="array"></param>
        public unsafe void CopyToWithTypeChange<U>(U[] array) where U : struct
        {
            int srcSize = UnsafeUtility.SizeOf<T>();
            int dstSize = UnsafeUtility.SizeOf<U>();
            int dataLength = (Length * srcSize) / dstSize;

            byte* src_p = (byte*)nativeArray.GetUnsafePtr();
            ulong dst_gcHandle;
            void* dst_p = UnsafeUtility.PinGCArrayAndGetDataAddress(array, out dst_gcHandle);

            UnsafeUtility.MemCpy(dst_p, src_p, dataLength * dstSize);
            UnsafeUtility.ReleaseGCObject(dst_gcHandle);
        }

        /// <summary>
        /// 型もサイズも異なる配列にデータを断片的にコピーする。
        /// float3 -> Vector4[]など。この場合はVector4にはxyzのみ書き込まれる。
        /// </summary>
        /// <typeparam name="U"></typeparam>
        /// <param name="array"></param>
        public unsafe void CopyToWithTypeChangeStride<U>(U[] array) where U : struct
        {
            int srcSize = UnsafeUtility.SizeOf<T>();
            int dstSize = UnsafeUtility.SizeOf<U>();
            int dataLength = Length;

            byte* src_p = (byte*)nativeArray.GetUnsafePtr();
            ulong dst_gcHandle;
            void* dst_p = UnsafeUtility.PinGCArrayAndGetDataAddress(array, out dst_gcHandle);

            int elementSize = srcSize;

            UnsafeUtility.MemCpyStride(dst_p, dstSize, src_p, srcSize, elementSize, dataLength);
            UnsafeUtility.ReleaseGCObject(dst_gcHandle);
        }

        public void CopyFrom(NativeArray<T> array)
        {
            NativeArray<T>.Copy(array, nativeArray);
        }

        public void CopyFrom<U>(NativeArray<U> array) where U : struct
        {
            NativeArray<T>.Copy(array.Reinterpret<T>(), nativeArray);
        }

        public unsafe void CopyFromWithTypeChangeStride<U>(NativeArray<U> array) where U : struct
        {
            int srcSize = UnsafeUtility.SizeOf<U>();
            int dstSize = UnsafeUtility.SizeOf<T>();
            int dataLength = array.Length;

            byte* src_p = (byte*)array.GetUnsafePtr();
            byte* dst_p = (byte*)nativeArray.GetUnsafePtr();

            int elementSize = dstSize;
            UnsafeUtility.MemCpyStride(dst_p, dstSize, src_p, srcSize, elementSize, dataLength);
        }

        /// <summary>
        /// 設定値で埋める（それなりのコストが発生するので注意！）
        /// </summary>
        /// <param name="startIndex"></param>
        /// <param name="dataLength"></param>
        /// <param name="fillData"></param>
        public void Fill(int startIndex, int dataLength, T fillData = default(T))
        {
            // C#
            //Parallel.For(0, dataLength, i =>
            //{
            //    nativeArray[startIndex + i] = fillData;
            //});
            FillInternal(startIndex, dataLength, fillData);
        }

        unsafe void FillInternal(int start, int size, T fillData = default(T))
        {
            //byte* dst_p = (byte*)nativeArray.GetUnsafePtr();
            void* dst_p = nativeArray.GetUnsafePtr();
            int index = start;
            for (int i = 0; i < size; i++, index++)
            {
                UnsafeUtility.WriteArrayElement<T>(dst_p, index, fillData);
            }
        }


        public T this[int index]
        {
            get
            {
                return nativeArray[index];
            }
            set
            {
                nativeArray[index] = value;
            }
        }

        /// <summary>
        /// Jobで利用する場合はこの関数でNativeArrayに変換して受け渡す
        /// </summary>
        /// <returns></returns>
        public NativeArray<T> GetNativeArray()
        {
            return nativeArray;
        }

        /// <summary>
        /// Jobで利用する場合はこの関数でNativeArrayに変換して受け渡す(型変更あり)
        /// </summary>
        /// <typeparam name="U"></typeparam>
        /// <returns></returns>
        public NativeArray<U> GetNativeArray<U>() where U : struct
        {
            return nativeArray.Reinterpret<U>();
        }

        //=========================================================================================
        /// <summary>
        /// 領域を拡張する（必要がなければ何もしない）
        /// </summary>
        /// <param name="dataLength"></param>
        /// <param name="force">強制的に領域を追加</param>
        void Expand(int dataLength, bool force = false)
        {
            int newlength = force ? length + dataLength : count + dataLength;

            if (length == 0)
            {
                if (nativeArray.IsCreated)
                    nativeArray.Dispose();

                nativeArray = new NativeArray<T>(dataLength, Allocator.Persistent);
                length = dataLength;
            }
            else if (newlength > Length)
            {
                // 拡張
                var newNativeArray = new NativeArray<T>(newlength, Allocator.Persistent);

                // copy
                // コピーは使用分だけ
                NativeArray<T>.Copy(nativeArray, newNativeArray, count);

                nativeArray.Dispose();
                nativeArray = newNativeArray;
                length = newlength;
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"ExNativeArray Length:{Length} Count:{Count} IsValid:{IsValid}");
            sb.AppendLine("---- Datas[100] ----");
            if (IsValid)
            {
                for (int i = 0; i < Length && i < 100; i++)
                {
                    sb.AppendLine(nativeArray[i].ToString());
                }
            }

            return sb.ToString();
        }
    }
}
