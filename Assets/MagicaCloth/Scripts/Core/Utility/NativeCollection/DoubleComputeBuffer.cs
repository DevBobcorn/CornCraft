using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// ダブルバッファ対応のComputeBuffer
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DoubleComputeBuffer<T> : IDisposable where T : struct
    {
        ComputeBuffer buffer0;
        ComputeBuffer buffer1;

        NativeArray<T> nativeArrya;

        //=========================================================================================
        public void Dispose()
        {
            if (buffer0 != null)
            {
                buffer0.Release();
                buffer0.Dispose();
                buffer0 = null;
            }
            if (buffer1 != null)
            {
                buffer1.Release();
                buffer1.Dispose();
                buffer1 = null;
            }
        }

        public void Swap()
        {
            var temp = buffer1;
            buffer1 = buffer0;
            buffer0 = temp;
        }

        public void Create(int size, ComputeBufferType type, ComputeBufferMode usage)
        {
            buffer0?.Release();
            buffer0?.Dispose();
            buffer0 = new ComputeBuffer(size, Marshal.SizeOf(typeof(T)), type, usage);
        }

#if UNITY_2021_2_OR_NEWER
        public void BeginWrite(int length)
        {
            nativeArrya = buffer0.BeginWrite<T>(0, length);
        }

        public void EndWrite(int length)
        {
            buffer0.EndWrite<T>(length);
        }
#endif

        public NativeArray<T> GetNativeArray()
        {
            return nativeArrya;
        }

        public ComputeBuffer GetBuffer(int bufferIndex = 0)
        {
            return bufferIndex == 0 ? buffer0 : buffer1;
        }

        public int Count
        {
            get
            {
                return buffer0?.count ?? 0;
            }
        }
    }
}
