// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using Unity.Collections;

namespace MagicaCloth2
{
    /// <summary>
    /// NativeArrayの拡張メソッド
    /// </summary>
    static class NativeArrayExtensions
    {
        /// <summary>
        /// NativeArrayが確保されている場合のみDispose()する
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
        public static void DisposeSafe<T>(ref this NativeArray<T> array) where T : unmanaged
        {
            if (array.IsCreated)
                array.Dispose();
        }

        /// <summary>
        /// NativeArrayをリサイズする
        /// 指定サイズ未満の場合にメモリを解放して新しいサイズで確保し直す
        /// リサイズ時に内容はコピーしない
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
        /// <param name="size"></param>
        /// <param name="allocator"></param>
        /// <param name="options"></param>
        public static void Resize<T>(ref this NativeArray<T> array, int size, Allocator allocator = Allocator.Persistent, NativeArrayOptions options = NativeArrayOptions.ClearMemory) where T : unmanaged
        {
            if (array.IsCreated == false || array.Length < size)
            {
                array.DisposeSafe();
                array = new NativeArray<T>(size, allocator, options);
            }
        }
    }
}
