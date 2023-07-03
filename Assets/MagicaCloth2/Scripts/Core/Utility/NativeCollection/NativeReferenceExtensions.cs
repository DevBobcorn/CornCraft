// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System.Runtime.CompilerServices;
using System.Threading;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace MagicaCloth2
{
    static class NativeReferenceExtensions
    {
        /// <summary>
        /// カウンターにデータ数を追加してその追加前の開始インデックスを返す
        /// この関数はスレッドセーフである
        /// </summary>
        /// <param name="counter"></param>
        /// <param name="dataCount"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe public static int InterlockedStartIndex(ref this NativeReference<int> counter, int dataCount)
        {
            int* cntPt = (int*)counter.GetUnsafePtr();
            int start = Interlocked.Add(ref *cntPt, dataCount) - dataCount;
            return start;
        }
    }
}
