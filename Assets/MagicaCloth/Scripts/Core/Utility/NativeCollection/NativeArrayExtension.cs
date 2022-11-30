// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp

using Unity.Collections;
#if UNITY_2020_1_OR_NEWER
using Unity.Collections.LowLevel.Unsafe;
#elif MAGICACLOTH_UNSAFE
using Unity.Collections.LowLevel.Unsafe;
#else
using System.Runtime.CompilerServices;
#endif

namespace MagicaCloth
{
    /// <summary>
    /// NativeArray拡張メソッド
    /// </summary>
    public static class NativeArrayExtension
    {
#if !MAGICACLOTH_UNSAFE
        /// <summary>
        /// 高速なCopyTo
        /// nativeArrayのstartIndexからのデータをarrayに書き出す
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="nativeArray"></param>
        /// <param name="array"></param>
        public static void CopyToFast<T, T2>(this NativeArray<T> nativeArray, int startIndex, T2[] array) where T : struct where T2 : struct
        {
#if UNITY_2020_1_OR_NEWER
            T[] ar = UnsafeUtility.As<T2[], T[]>(ref array);
#else
            T[] ar = Unsafe.As<T2[], T[]>(ref array);
#endif
            NativeArray<T>.Copy(nativeArray, startIndex, ar, 0, array.Length);
        }

        /// <summary>
        /// 高速なCopyTo
        /// nativeArrayのstartIndexからのデータをnativeArrayに書き出す
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="nativeArray"></param>
        /// <param name="array"></param>
        public static void CopyToFast<T>(this NativeArray<T> nativeArray, int startIndex, NativeArray<T> array) where T : struct
        {
            NativeArray<T>.Copy(nativeArray, startIndex, array, 0, array.Length);
        }

        /// <summary>
        /// 高速なCopyTo
        /// nativeArrayのsourceIndexからcountのデータをdestinationIndexにコピーする
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="nativeArray"></param>
        /// <param name="sourceIndex"></param>
        /// <param name="destinationIndex"></param>
        /// <param name="count"></param>
        public static void CopyBlock<T>(this NativeArray<T> nativeArray, int sourceIndex, int destinationIndex, int count) where T : struct
        {
            NativeArray<T>.Copy(nativeArray, sourceIndex, nativeArray, destinationIndex, count);
        }

        /// <summary>
        /// 高速なCopyFrom
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="nativeArray"></param>
        /// <param name="array"></param>
        public static void CopyFromFast<T>(this NativeArray<T> nativeArray, NativeArray<T> array) where T : struct
        {
            NativeArray<T>.Copy(array, 0, nativeArray, 0, array.Length);
        }

        /// <summary>
        /// 高速なCopyFrom
        /// arrayの内容をnativeArrayのstartIndex位置にコピーする
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="nativeArray"></param>
        /// <param name="array"></param>
        public static void CopyFromFast<T, T2>(this NativeArray<T> nativeArray, int startIndex, T2[] array) where T : struct where T2 : struct
        {
#if UNITY_2020_1_OR_NEWER
            T[] ar = UnsafeUtility.As<T2[], T[]>(ref array);
#else
            T[] ar = Unsafe.As<T2[], T[]>(ref array);
#endif
            NativeArray<T>.Copy(ar, 0, nativeArray, startIndex, array.Length);
        }

        /// <summary>
        /// 高速なデータ書き込み
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="nativeArray"></param>
        /// <param name="startIndex"></param>
        /// <param name="count"></param>
        /// <param name="value"></param>
        public static void SetValue<T>(this NativeArray<T> nativeArray, int startIndex, int count, T value) where T : struct
        {
            for (int i = 0; i < count; i++, startIndex++)
            {
                nativeArray[startIndex] = value;
            }
        }

#else
        /// <summary>
        /// 高速なCopyTo
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="nativeArray"></param>
        /// <param name="array"></param>
        public static unsafe void CopyToFast<T, T2>(this NativeArray<T> nativeArray, T2[] array) where T : struct where T2 : struct
        {
            int byteLength = nativeArray.Length * UnsafeUtility.SizeOf<T>();
            void* managedBuffer = UnsafeUtility.AddressOf(ref array[0]);
            void* nativeBuffer = nativeArray.GetUnsafePtr();
            UnsafeUtility.MemCpy(managedBuffer, nativeBuffer, byteLength);
        }

        /// <summary>
        /// 高速なCopyTo
        /// nativeArrayのstartIndexからのデータをarrayに書き出す
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="nativeArray"></param>
        /// <param name="array"></param>
        public static unsafe void CopyToFast<T, T2>(this NativeArray<T> nativeArray, int startIndex, T2[] array) where T : struct where T2 : struct
        {
            int stride = UnsafeUtility.SizeOf<T>();
            int byteLength = array.Length * stride;
            void* managedBuffer = UnsafeUtility.AddressOf(ref array[0]);
            byte* p = (byte*)nativeArray.GetUnsafePtr();
            p += startIndex * stride;
            UnsafeUtility.MemCpy(managedBuffer, (void*)p, byteLength);
        }

        /// <summary>
        /// 高速なCopyTo
        /// nativeArrayのstartIndexからのデータをnativeArrayに書き出す
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="nativeArray"></param>
        /// <param name="array"></param>
        public static unsafe void CopyToFast<T>(this NativeArray<T> nativeArray, int startIndex, NativeArray<T> array) where T : struct
        {
            int stride = UnsafeUtility.SizeOf<T>();
            int byteLength = array.Length * stride;
            byte* p = (byte*)nativeArray.GetUnsafePtr();
            p += startIndex * stride;
            UnsafeUtility.MemCpy(array.GetUnsafePtr(), (void*)p, byteLength);
        }

        /// <summary>
        /// 高速なCopyTo
        /// nativeArrayのsourceIndexからcountのデータをdestinationIndexにコピーする
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="nativeArray"></param>
        /// <param name="sourceIndex"></param>
        /// <param name="destinationIndex"></param>
        /// <param name="count"></param>
        public static unsafe void CopyBlock<T>(this NativeArray<T> nativeArray, int sourceIndex, int destinationIndex, int count) where T : struct
        {
            int stride = UnsafeUtility.SizeOf<T>();
            int byteLength = count * stride;
            byte* p = (byte*)nativeArray.GetUnsafePtr();
            UnsafeUtility.MemCpy((void*)(p + destinationIndex * stride), (void*)(p + sourceIndex * stride), byteLength);
        }

        /// <summary>
        /// 高速なCopyFrom
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="nativeArray"></param>
        /// <param name="array"></param>
        public static unsafe void CopyFromFast<T, T2>(this NativeArray<T> nativeArray, T2[] array) where T : struct where T2 : struct
        {
            int byteLength = nativeArray.Length * UnsafeUtility.SizeOf<T>();
            void* managedBuffer = UnsafeUtility.AddressOf(ref array[0]);
            void* nativeBuffer = nativeArray.GetUnsafePtr();
            UnsafeUtility.MemCpy(nativeBuffer, managedBuffer, byteLength);
        }

        /// <summary>
        /// 高速なCopyFrom
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="nativeArray"></param>
        /// <param name="array"></param>
        public static unsafe void CopyFromFast<T>(this NativeArray<T> nativeArray, NativeArray<T> array) where T : struct
        {
            int byteLength = array.Length * UnsafeUtility.SizeOf<T>();
            void* managedBuffer = array.GetUnsafePtr();
            void* nativeBuffer = nativeArray.GetUnsafePtr();
            UnsafeUtility.MemCpy(nativeBuffer, managedBuffer, byteLength);
        }

        /// <summary>
        /// 高速なCopyFrom
        /// arrayの内容をnativeArrayのstartIndex位置にコピーする
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="nativeArray"></param>
        /// <param name="array"></param>
        public static unsafe void CopyFromFast<T, T2>(this NativeArray<T> nativeArray, int startIndex, T2[] array) where T : struct where T2 : struct
        {
            int stride = UnsafeUtility.SizeOf<T>();
            int byteLength = array.Length * stride;
            void* managedBuffer = UnsafeUtility.AddressOf(ref array[0]);
            byte* p = (byte*)nativeArray.GetUnsafePtr();
            p += startIndex * stride;
            UnsafeUtility.MemCpy((void*)p, managedBuffer, byteLength);
        }

        /// <summary>
        /// 高速なデータ書き込み
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="nativeArray"></param>
        /// <param name="startIndex"></param>
        /// <param name="count"></param>
        /// <param name="value"></param>
        public static unsafe void SetValue<T>(this NativeArray<T> nativeArray, int startIndex, int count, T value) where T : struct
        {
            void* nativeBuffer = nativeArray.GetUnsafePtr();
            for (int i = 0; i < count; i++)
            {
                UnsafeUtility.WriteArrayElement(nativeBuffer, startIndex + i, value);
            }
        }
#endif
    }
}
