// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using Unity.Collections;

namespace MagicaCloth2
{
    /// <summary>
    /// NativeArrayの拡張メソッド
    /// </summary>
    public static class NativeArrayExtensions
    {
        /// <summary>
        /// NativeArrayが確保されている場合のみDispose()する
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
        public static void MC2DisposeSafe<T>(ref this NativeArray<T> array) where T : unmanaged
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
        public static void MC2Resize<T>(ref this NativeArray<T> array, int size, Allocator allocator = Allocator.Persistent, NativeArrayOptions options = NativeArrayOptions.ClearMemory) where T : unmanaged
        {
            if (array.IsCreated == false || array.Length < size)
            {
                array.MC2DisposeSafe();
                array = new NativeArray<T>(size, allocator, options);
            }
        }

        /// <summary>
        /// NativeArrayをbyte[]に変換する
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
        /// <returns></returns>
        public static byte[] MC2ToRawBytes<T>(ref this NativeArray<T> array) where T : unmanaged
        {
            if (array.IsCreated == false || array.Length == 0)
                return null;
            var slice = new NativeSlice<T>(array).SliceConvert<byte>();
            var bytes = new byte[slice.Length];
            slice.CopyTo(bytes);
            return bytes;
        }

        /// <summary>
        /// byte[]からNativeArrayを作成する
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="bytes"></param>
        /// <param name="allocator"></param>
        /// <returns></returns>
        public static NativeArray<T> MC2FromRawBytes<T>(byte[] bytes, Allocator allocator = Allocator.Persistent) where T : unmanaged
        {
            if (bytes == null)
                return new NativeArray<T>();

            int structSize = Unity.Collections.LowLevel.Unsafe.UnsafeUtility.SizeOf<T>();

            int length = bytes.Length / structSize;
            var array = new NativeArray<T>(length, allocator);
            if (length > 0)
            {
                using var byteArray = new NativeArray<byte>(bytes, Allocator.Temp);
                //using var byteArray = new NativeArray<byte>(bytes, Allocator.Persistent);
                var slice = new NativeSlice<byte>(byteArray).SliceConvert<T>();
                slice.CopyTo(array);
            }
            return array;
        }
    }
}
