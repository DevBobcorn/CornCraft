// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace MagicaCloth2
{
    /// <summary>
    /// 拡張可能なNativeArrayクラス
    /// 領域が不足すると自動で拡張する
    /// データはChankDataにより開始インデックスと長さが管理される
    /// データは削除可能で削除された領域は管理され再利用される
    /// 領域の管理が必要なためExSimpleNativeArrayに比べてやや重いので注意！
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ExNativeArray<T> : IDisposable where T : unmanaged
    {
        NativeArray<T> nativeArray;

        List<DataChunk> emptyChunks = new List<DataChunk>();

        int useCount;

        public void Dispose()
        {
            if (nativeArray.IsCreated)
            {
                nativeArray.Dispose();
            }
            emptyChunks.Clear();
            useCount = 0;
        }

        public bool IsValid => nativeArray.IsCreated;

        /// <summary>
        /// NativeArrayの領域サイズ
        /// 実際に利用されているサイズとは異なるので注意！
        /// </summary>
        public int Length => nativeArray.IsCreated ? nativeArray.Length : 0;

        /// <summary>
        /// 実際に利用されているデータ数（最後のチャンクの最後尾＋１）
        /// </summary>
        public int Count => useCount;

        //=========================================================================================
        public ExNativeArray()
        {
        }

        public ExNativeArray(int emptyLength, bool create = false) : this()
        {
            if (emptyLength > 0)
            {
                nativeArray = new NativeArray<T>(emptyLength, Allocator.Persistent);
                var chunk = new DataChunk(0, emptyLength);
                emptyChunks.Add(chunk);

                if (create)
                {
                    // 領域を確保する
                    AddRange(emptyLength);
                }
            }
            else if (create)
            {
                // Native配列のみ０で確保（主にジョブでのエラー対策）
                nativeArray = new NativeArray<T>(0, Allocator.Persistent);
            }
        }

        public ExNativeArray(int emptyLength, T fillData) : this(emptyLength)
        {
            if (emptyLength > 0)
                Fill(fillData);
        }

        public ExNativeArray(NativeArray<T> dataArray) : this()
        {
            AddRange(dataArray);
        }

        public ExNativeArray(T[] dataArray) : this()
        {
            AddRange(dataArray);
        }

        //=========================================================================================
#if false
        /// <summary>
        /// 使用配列カウントを設定する
        /// 有効数を書き換えすべてのデータを１つのチャンクとして使用中とする
        /// かなり強力な機能なので扱いには注意すること！
        /// </summary>
        /// <param name="count"></param>
        public void SetUseCount(int count)
        {
            useCount = count;
            emptyChunks.Clear();
            if (useCount > Length)
            {
                // 未使用領域を１つの空チャンクとして登録する
                var chunk = new DataChunk(useCount, Length - useCount);
                emptyChunks.Add(chunk);
            }
        }
#endif

        /// <summary>
        /// 指定サイズの領域を追加しそのチャンクを返す
        /// </summary>
        /// <param name="dataLength"></param>
        /// <returns></returns>
        public DataChunk AddRange(int dataLength)
        {
            // サイズ0対応
            if (dataLength == 0)
            {
                // 領域だけは0で確保する
                if (nativeArray.IsCreated == false)
                    nativeArray = new NativeArray<T>(0, Allocator.Persistent);

                return DataChunk.Empty;
            }

            var chunk = GetEmptyChunk(dataLength);

            if (chunk.IsValid == false)
            {
                // 空きを増やす
                int nowLength = Length;
                int nextLength = Length + math.max(dataLength, nowLength);
                if (nowLength == 0)
                {
                    // 新規
                    if (nativeArray.IsCreated)
                        nativeArray.Dispose();
                    nativeArray = new NativeArray<T>(nextLength, Allocator.Persistent);
                    chunk.dataLength = dataLength;
                }
                else
                {
                    // 拡張
                    var newNativeArray = new NativeArray<T>(nextLength, Allocator.Persistent);

                    // copy
                    NativeArray<T>.Copy(nativeArray, newNativeArray, nowLength);
                    nativeArray.Dispose();
                    nativeArray = newNativeArray;

                    // data chunk
                    chunk.startIndex = nowLength;
                    chunk.dataLength = dataLength;

                    int last = nowLength + dataLength;
                    if (last < nextLength)
                    {
                        var emptyChunk = new DataChunk(last, nextLength - last);
                        AddEmptyChunk(emptyChunk);
                    }
                }
            }

            // 使用量
            useCount = math.max(useCount, chunk.startIndex + chunk.dataLength);

            return chunk;
        }

        public DataChunk AddRange(int dataLength, T fillData = default(T))
        {
            var chunk = AddRange(dataLength);
            Fill(chunk, fillData);
            return chunk;
        }

        public DataChunk AddRange(T[] array)
        {
            if (array == null || array.Length == 0)
                return DataChunk.Empty;

            int dataLength = array.Length;
            var chunk = AddRange(dataLength);

            // copy
            NativeArray<T>.Copy(array, 0, nativeArray, chunk.startIndex, dataLength);

            return chunk;
        }

        public DataChunk AddRange(NativeArray<T> narray, int length = 0)
        {
            if (narray.IsCreated == false || narray.Length == 0)
                return DataChunk.Empty;

            int dataLength = length > 0 ? length : narray.Length;
            var chunk = AddRange(dataLength);
            // copy
            NativeArray<T>.Copy(narray, 0, nativeArray, chunk.startIndex, dataLength);

            return chunk;
        }

        public DataChunk AddRange(ExNativeArray<T> exarray)
        {
            return AddRange(exarray.GetNativeArray(), exarray.Count);
        }

        public DataChunk AddRange(ExSimpleNativeArray<T> exarray)
        {
            return AddRange(exarray.GetNativeArray(), exarray.Count);
        }

        /// <summary>
        /// 型は異なるが型のサイズは同じ配列を追加する。Vector3->float3など。
        /// </summary>
        /// <typeparam name="U"></typeparam>
        /// <param name="array"></param>
        /// <returns></returns>
        public unsafe DataChunk AddRange<U>(U[] array) where U : struct
        {
            if (array == null || array.Length == 0)
                return DataChunk.Empty;

            int dstSize = UnsafeUtility.SizeOf<T>();
            int dataLength = array.Length;
            var chunk = AddRange(dataLength);

            ulong src_gcHandle;
            void* src_p = UnsafeUtility.PinGCArrayAndGetDataAddress(array, out src_gcHandle);
            byte* dst_p = (byte*)nativeArray.GetUnsafePtr();

            UnsafeUtility.MemCpy(dst_p + chunk.startIndex * dstSize, src_p, dataLength * dstSize);
            UnsafeUtility.ReleaseGCObject(src_gcHandle);

            return chunk;
        }

        /// <summary>
        /// 型は異なるが型のサイズは同じNativeArrayを追加する。Vector3->float3など。
        /// </summary>
        /// <typeparam name="U"></typeparam>
        /// <param name="udata"></param>
        /// <returns></returns>
        public DataChunk AddRange<U>(NativeArray<U> udata) where U : struct
        {
            if (udata.IsCreated == false || udata.Length == 0)
                return DataChunk.Empty;

            int dataLength = udata.Length;
            var chunk = AddRange(dataLength);

            // copy
            NativeArray<T>.Copy(udata.Reinterpret<T>(), 0, nativeArray, chunk.startIndex, dataLength);

            return chunk;
        }

        /// <summary>
        /// 型もサイズも異なる配列を追加する。int[] -> int3[]など。
        /// データはそのままメモリコピーされる。例えばint[]からint3[]へ追加すると次のようになる。
        /// int[]{1, 2, 3, 4, 5, 6} => int3[]{{1, 2, 3}, {4, 5, 6}}
        /// </summary>
        /// <typeparam name="U"></typeparam>
        /// <param name="array"></param>
        /// <returns></returns>
        public unsafe DataChunk AddRangeTypeChange<U>(U[] array) where U : struct
        {
            if (array == null || array.Length == 0)
                return DataChunk.Empty;

            int srcSize = UnsafeUtility.SizeOf<U>();
            int dstSize = UnsafeUtility.SizeOf<T>();

            int dataLength = (array.Length * srcSize) / dstSize;
            var chunk = AddRange(dataLength);

            ulong src_gcHandle;
            void* src_p = UnsafeUtility.PinGCArrayAndGetDataAddress(array, out src_gcHandle);
            byte* dst_p = (byte*)nativeArray.GetUnsafePtr();

            UnsafeUtility.MemCpy(dst_p + chunk.startIndex * dstSize, src_p, dataLength * dstSize);
            UnsafeUtility.ReleaseGCObject(src_gcHandle);

            return chunk;
        }

        /// <summary>
        /// 型もサイズも異なる配列を部分的にコピーする。Vector4[] -> float3など。
        /// </summary>
        /// <typeparam name="U"></typeparam>
        /// <param name="array"></param>
        /// <returns></returns>
        public unsafe DataChunk AddRangeStride<U>(U[] array) where U : struct
        {
            if (array == null || array.Length == 0)
                return DataChunk.Empty;

            int srcSize = UnsafeUtility.SizeOf<U>();
            int dstSize = UnsafeUtility.SizeOf<T>();
            int dataLength = array.Length;
            var chunk = AddRange(dataLength);

            ulong src_gcHandle;
            void* src_p = UnsafeUtility.PinGCArrayAndGetDataAddress(array, out src_gcHandle);
            byte* dst_p = (byte*)nativeArray.GetUnsafePtr();
            int elementSize = math.min(srcSize, dstSize);

            UnsafeUtility.MemCpyStride(dst_p + chunk.startIndex * dstSize, dstSize, src_p, srcSize, elementSize, dataLength);
            UnsafeUtility.ReleaseGCObject(src_gcHandle);

            return chunk;
        }

        public DataChunk Add(T data)
        {
            var chunk = AddRange(1);
            nativeArray[chunk.startIndex] = data;
            return chunk;
        }

        /// <summary>
        /// 指定チャンクのデータ数を拡張し新しいチャンクを返す
        /// 古いチャンクのデータは新しいチャンクにコピーされる
        /// </summary>
        /// <param name="c"></param>
        /// <param name="newDataLength"></param>
        /// <returns></returns>
        public DataChunk Expand(DataChunk c, int newDataLength)
        {
            Develop.Assert(c.IsValid);
            if (c.IsValid == false)
                return c;
            if (newDataLength <= c.dataLength)
                return c;

            // 新しい領域を確保する
            var nc = AddRange(newDataLength);

            // 古い領域をコピーする
            NativeArray<T>.Copy(nativeArray, c.startIndex, nativeArray, nc.startIndex, c.dataLength);

            // 古い領域を開放する
            Remove(c);

            return nc;
        }

        /// <summary>
        /// 指定チャンクのデータ数を拡張し新しいチャンクを返す
        /// 古いチャンクのデータは新しいチャンクにコピーされる
        /// </summary>
        /// <param name="c"></param>
        /// <param name="newDataLength"></param>
        /// <returns></returns>
        public DataChunk ExpandAndFill(DataChunk c, int newDataLength, T fillData = default(T), T clearData = default(T))
        {
            Develop.Assert(c.IsValid);
            if (c.IsValid == false)
                return c;
            if (newDataLength <= c.dataLength)
                return c;

            // 新しい領域を確保する
            var nc = AddRange(newDataLength, fillData);

            // 古い領域をコピーする
            NativeArray<T>.Copy(nativeArray, c.startIndex, nativeArray, nc.startIndex, c.dataLength);

            // 古い領域を開放する
            RemoveAndFill(c, clearData);

            return nc;
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

        public void CopyFrom(NativeArray<T> array)
        {
            NativeArray<T>.Copy(array, nativeArray);
        }

        public void CopyFrom<U>(NativeArray<U> array) where U : struct
        {
            NativeArray<T>.Copy(array.Reinterpret<T>(), nativeArray);
        }

        /// <summary>
        /// 型もサイズも異なる配列にデータをコピーする。
        /// int3 -> int[]など
        /// </summary>
        /// <typeparam name="U"></typeparam>
        /// <param name="array"></param>
        public unsafe void CopyTypeChange<U>(U[] array) where U : struct
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
        public unsafe void CopyTypeChangeStride<U>(U[] array) where U : struct
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

        /// <summary>
        /// すぐに利用できる空領域のみ追加する
        /// </summary>
        /// <param name="dataLength"></param>
        public void AddEmpty(int dataLength)
        {
            var chunk = AddRange(dataLength);
            Remove(chunk);
        }

        public void Remove(DataChunk chunk)
        {
            if (chunk.IsValid == false)
                return;

            AddEmptyChunk(chunk);

            // 使用量の再計算
            if ((chunk.startIndex + chunk.dataLength) == useCount)
            {
                useCount = 0;
                foreach (var echunk in emptyChunks)
                {
                    useCount = math.max(useCount, echunk.startIndex);
                }
            }
        }

        public void RemoveAndFill(DataChunk chunk, T clearData = default(T))
        {
            Remove(chunk);

            // データクリア
            // C#
            //Parallel.For(0, chunk.dataLength, i =>
            //{
            //    nativeArray[chunk.startIndex + i] = clearData;
            //});
            //FillInternal(chunk.startIndex, chunk.dataLength, clearData);
            Fill(chunk, clearData);
        }

        public void Fill(T fillData = default(T))
        {
            if (IsValid == false)
                return;

            // C#
            //Parallel.For(0, nativeArray.Length, i =>
            //{
            //    nativeArray[i] = fillData;
            //});
            FillInternal(0, nativeArray.Length, fillData);
        }

        public void Fill(DataChunk chunk, T fillData = default(T))
        {
            if (IsValid == false || chunk.IsValid == false)
                return;

            // C#
            //Parallel.For(0, chunk.dataLength, i =>
            //{
            //    nativeArray[chunk.startIndex + i] = fillData;
            //});
            FillInternal(chunk.startIndex, chunk.dataLength, fillData);
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


        public void Clear()
        {
            emptyChunks.Clear();
            useCount = 0;

            // empty chunk
            if (IsValid && Length > 0)
            {
                var chunk = new DataChunk(0, Length);
                emptyChunks.Add(chunk);
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

        public unsafe ref T GetRef(int index)
        {
            T* p = (T*)nativeArray.GetUnsafePtr();
            return ref *(p + index);
        }

        //public unsafe ref T GetRef(int index)
        //{
        //    var span = new Span<T>(nativeArray.GetUnsafePtr(), nativeArray.Length);
        //    return ref span[index];
        //}

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
        DataChunk GetEmptyChunk(int dataLength)
        {
            if (dataLength <= 0)
                return new DataChunk();

            for (int i = 0; i < emptyChunks.Count; i++)
            {
                var c = emptyChunks[i];
                if (dataLength == c.dataLength)
                {
                    // このチャンクをすべて利用する
                    emptyChunks.RemoveAtSwapBack(i);
                    return c;
                }
                else if (dataLength < c.dataLength)
                {
                    // このチャンクを一部利用する
                    var chunk = new DataChunk();
                    chunk.startIndex = c.startIndex;
                    chunk.dataLength = dataLength;
                    c.startIndex += dataLength;
                    c.dataLength -= dataLength;
                    emptyChunks[i] = c;
                    return chunk;
                }
            }

            // 利用できるチャンクはなし
            return new DataChunk();
        }

        void AddEmptyChunk(DataChunk chunk)
        {
            if (chunk.IsValid == false)
                return;

            // 後ろに連結できる場所を探す
            for (int i = 0; i < emptyChunks.Count; i++)
            {
                var c = emptyChunks[i];
                if ((c.startIndex + c.dataLength) == chunk.startIndex)
                {
                    // ここに連結する
                    c.dataLength += chunk.dataLength;
                    chunk = c;

                    // cを削除する
                    emptyChunks.RemoveAtSwapBack(i);
                    break;
                }
            }

            // 前に連結できる場所を探す
            for (int i = 0; i < emptyChunks.Count; i++)
            {
                var c = emptyChunks[i];
                if (c.startIndex == (chunk.startIndex + chunk.dataLength))
                {
                    // ここに連結する
                    chunk.dataLength += c.dataLength;

                    // cを削除する
                    emptyChunks.RemoveAtSwapBack(i);
                    break;
                }
            }

            // chunkを追加する
            emptyChunks.Add(chunk);
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

            sb.AppendLine("---- Empty Chunks ----");
            foreach (var c in emptyChunks)
            {
                sb.AppendLine(c.ToString());
            }
            sb.AppendLine();

            return sb.ToString();
        }

        public string ToSummary()
        {
            return $"ExNativeArray Length:{Length} Count:{Count} IsValid:{IsValid}";
        }
    }
}
